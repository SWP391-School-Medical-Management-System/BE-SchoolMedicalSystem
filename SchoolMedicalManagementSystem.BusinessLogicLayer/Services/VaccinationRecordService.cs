using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineRecordRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccineRecordResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services
{
    public class VaccinationRecordService : IVaccinationRecordService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly ILogger<VaccinationRecordService> _logger;
        private readonly IValidator<CreateVaccinationRecordRequest> _createVaccinationRecordValidator;
        private readonly IValidator<UpdateVaccinationRecordRequest> _updateVaccinationRecordValidator;

        private const string VACCINATION_RECORD_CACHE_PREFIX = "vaccination_record";
        private const string VACCINATION_RECORD_LIST_PREFIX = "vaccination_records_list";
        private const string VACCINATION_RECORD_CACHE_SET = "vaccination_record_cache_keys";

        public VaccinationRecordService(
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ILogger<VaccinationRecordService> logger,
            IValidator<CreateVaccinationRecordRequest> createVaccinationRecordValidator,
            IValidator<UpdateVaccinationRecordRequest> updateVaccinationRecordValidator)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _logger = logger;
            _createVaccinationRecordValidator = createVaccinationRecordValidator;
            _updateVaccinationRecordValidator = updateVaccinationRecordValidator;
        }

        public async Task<BaseListResponse<VaccinationRecordResponse>> GetVaccinationRecordsAsync(
            Guid medicalRecordId,
            int pageIndex,
            int pageSize,
            string searchTerm,
            string orderBy,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = _cacheService.GenerateCacheKey(
                    VACCINATION_RECORD_LIST_PREFIX,
                    medicalRecordId.ToString(),
                    pageIndex.ToString(),
                    pageSize.ToString(),
                    searchTerm ?? "",
                    orderBy ?? ""
                );

                var cachedResult = await _cacheService.GetAsync<BaseListResponse<VaccinationRecordResponse>>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogDebug("Vaccination records list found in cache for medical record: {MedicalRecordId}", medicalRecordId);
                    return cachedResult;
                }

                var query = _unitOfWork.GetRepositoryByEntity<VaccinationRecord>().GetQueryable()
                    .Include(vr => vr.VaccinationType)
                    .Where(vr => vr.MedicalRecordId == medicalRecordId && !vr.IsDeleted)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(vr =>
                        vr.VaccinationType.Name.ToLower().Contains(searchTerm) ||
                        vr.BatchNumber.ToLower().Contains(searchTerm) ||
                        vr.Notes.ToLower().Contains(searchTerm));
                }

                query = ApplyVaccinationRecordOrdering(query, orderBy);

                var totalCount = await query.CountAsync(cancellationToken);
                var vaccinationRecords = await query
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                var responses = vaccinationRecords.Select(MapToVaccinationRecordResponse).ToList();

                var result = BaseListResponse<VaccinationRecordResponse>.SuccessResult(
                    responses,
                    totalCount,
                    pageSize,
                    pageIndex,
                    "Lấy danh sách hồ sơ tiêm chủng thành công.");

                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
                await _cacheService.AddToTrackingSetAsync(cacheKey, VACCINATION_RECORD_CACHE_SET);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vaccination records for medical record: {MedicalRecordId}", medicalRecordId);
                return BaseListResponse<VaccinationRecordResponse>.ErrorResult("Lỗi lấy danh sách hồ sơ tiêm chủng.");
            }
        }

        public async Task<BaseResponse<VaccinationRecordResponse>> CreateVaccinationRecordAsync(
            Guid medicalRecordId,
            CreateVaccinationRecordRequest model)
        {
            try
            {
                var validationResult = await _createVaccinationRecordValidator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return new BaseResponse<VaccinationRecordResponse>
                    {
                        Success = false,
                        Message = errors
                    };
                }

                var recordRepo = _unitOfWork.GetRepositoryByEntity<MedicalRecord>();
                var medicalRecord = await recordRepo.GetQueryable()
                    .Include(mr => mr.Student)
                    .FirstOrDefaultAsync(mr => mr.Id == medicalRecordId && !mr.IsDeleted);

                if (medicalRecord == null)
                {
                    _logger.LogWarning("Không tìm thấy hồ sơ y tế với ID: {MedicalRecordId}", medicalRecordId);
                    return new BaseResponse<VaccinationRecordResponse>
                    {
                        Success = false,
                        Message = "Không tìm thấy hồ sơ y tế."
                    };
                }

                if (medicalRecord.Student == null || medicalRecord.Student.Id == Guid.Empty)
                {
                    _logger.LogWarning("Hồ sơ y tế {MedicalRecordId} không liên kết với học sinh hợp lệ.", medicalRecordId);
                    return new BaseResponse<VaccinationRecordResponse>
                    {
                        Success = false,
                        Message = "Hồ sơ y tế không liên kết với học sinh."
                    };
                }

                var vaccinationTypeRepo = _unitOfWork.GetRepositoryByEntity<VaccinationType>();
                var vaccinationType = await vaccinationTypeRepo.GetQueryable()
                    .FirstOrDefaultAsync(vt => vt.Id == model.VaccinationTypeId && !vt.IsDeleted);

                if (vaccinationType == null)
                {
                    _logger.LogWarning("Không tìm thấy loại vaccine với ID: {VaccinationTypeId}", model.VaccinationTypeId);
                    return new BaseResponse<VaccinationRecordResponse>
                    {
                        Success = false,
                        Message = "Không tìm thấy loại vaccine."
                    };
                }

                var vaccinationRecord = _mapper.Map<VaccinationRecord>(model);
                vaccinationRecord.Id = Guid.NewGuid();
                vaccinationRecord.MedicalRecordId = medicalRecordId;
                vaccinationRecord.UserId = medicalRecord.Student.Id;
                vaccinationRecord.CreatedBy = "SCHOOLNURSE";
                vaccinationRecord.CreatedDate = DateTime.UtcNow;
                vaccinationRecord.IsDeleted = false;
                vaccinationRecord.Code = $"VACREC-{Guid.NewGuid().ToString().Substring(0, 8)}";

                _logger.LogDebug("VaccinationRecord trước khi lưu: {Entity}", System.Text.Json.JsonSerializer.Serialize(vaccinationRecord));

                var recordRepoVac = _unitOfWork.GetRepositoryByEntity<VaccinationRecord>();
                await recordRepoVac.AddAsync(vaccinationRecord);
                await _unitOfWork.SaveChangesAsync();

                await InvalidateAllCachesAsync();

                vaccinationRecord = await recordRepoVac.GetQueryable()
                    .Include(vr => vr.VaccinationType)
                    .FirstOrDefaultAsync(vr => vr.Id == vaccinationRecord.Id);

                if (vaccinationRecord == null)
                {
                    _logger.LogError("Không thể lấy lại hồ sơ tiêm chủng vừa tạo với ID: {VaccinationRecordId}", vaccinationRecord.Id);
                    return new BaseResponse<VaccinationRecordResponse>
                    {
                        Success = false,
                        Message = "Lỗi khi lấy dữ liệu hồ sơ tiêm chủng."
                    };
                }

                var response = MapToVaccinationRecordResponse(vaccinationRecord);

                _logger.LogInformation("Tạo hồ sơ tiêm chủng thành công với ID: {VaccinationRecordId}", vaccinationRecord.Id);
                return new BaseResponse<VaccinationRecordResponse>
                {
                    Success = true,
                    Data = response,
                    Message = "Tạo hồ sơ tiêm chủng thành công."
                };
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                _logger.LogError(ex, "Lỗi lưu hồ sơ tiêm chủng cho hồ sơ y tế {MedicalRecordId}: {Error}", medicalRecordId, innerException);
                return new BaseResponse<VaccinationRecordResponse>
                {
                    Success = false,
                    Message = $"Lỗi tạo hồ sơ tiêm chủng: {innerException}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tạo hồ sơ tiêm chủng cho hồ sơ y tế {MedicalRecordId}: {Error}", medicalRecordId, ex.Message);
                return new BaseResponse<VaccinationRecordResponse>
                {
                    Success = false,
                    Message = $"Lỗi tạo hồ sơ tiêm chủng: {ex.Message}"
                };
            }
        }

        public async Task<BaseResponse<VaccinationRecordResponse>> UpdateVaccinationRecordAsync(
            Guid recordId,
            UpdateVaccinationRecordRequest model)
        {
            try
            {
                var validationResult = await _updateVaccinationRecordValidator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return new BaseResponse<VaccinationRecordResponse>
                    {
                        Success = false,
                        Message = errors
                    };
                }

                var recordRepo = _unitOfWork.GetRepositoryByEntity<VaccinationRecord>();
                var vaccinationRecord = await recordRepo.GetQueryable()
                    .Include(vr => vr.VaccinationType)
                    .FirstOrDefaultAsync(vr => vr.Id == recordId && !vr.IsDeleted);

                if (vaccinationRecord == null)
                {
                    return new BaseResponse<VaccinationRecordResponse>
                    {
                        Success = false,
                        Message = "Không tìm thấy hồ sơ tiêm chủng."
                    };
                }

                _mapper.Map(model, vaccinationRecord);
                vaccinationRecord.LastUpdatedBy = "SCHOOLNURSE";
                vaccinationRecord.LastUpdatedDate = DateTime.UtcNow; 

                await _unitOfWork.SaveChangesAsync();
                await InvalidateAllCachesAsync();

                var response = MapToVaccinationRecordResponse(vaccinationRecord);

                return new BaseResponse<VaccinationRecordResponse>
                {
                    Success = true,
                    Data = response,
                    Message = "Cập nhật hồ sơ tiêm chủng thành công."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vaccination record: {RecordId}", recordId);
                return new BaseResponse<VaccinationRecordResponse>
                {
                    Success = false,
                    Message = $"Lỗi cập nhật hồ sơ tiêm chủng: {ex.Message}"
                };
            }
        }

        public async Task<BaseResponse<bool>> DeleteVaccinationRecordAsync(Guid recordId)
        {
            try
            {
                var recordRepo = _unitOfWork.GetRepositoryByEntity<VaccinationRecord>();
                var vaccinationRecord = await recordRepo.GetQueryable()
                    .FirstOrDefaultAsync(vr => vr.Id == recordId && !vr.IsDeleted);

                if (vaccinationRecord == null)
                {
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "Không tìm thấy hồ sơ tiêm chủng."
                    };
                }

                vaccinationRecord.IsDeleted = true;
                vaccinationRecord.LastUpdatedBy = "SCHOOLNURSE";
                vaccinationRecord.LastUpdatedDate = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();
                await InvalidateAllCachesAsync();

                return new BaseResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Xóa hồ sơ tiêm chủng thành công."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vaccination record: {RecordId}", recordId);
                return new BaseResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Lỗi xóa hồ sơ tiêm chủng: {ex.Message}"
                };
            }
        }

        #region Helper Methods

        private VaccinationRecordResponse MapToVaccinationRecordResponse(VaccinationRecord vaccinationRecord)
        {
            return new VaccinationRecordResponse
            {
                Id = vaccinationRecord.Id,
                VaccinationTypeName = vaccinationRecord.VaccinationType.Name,
                DoseNumber = vaccinationRecord.DoseNumber,
                AdministeredDate = (DateTime)vaccinationRecord.AdministeredDate,
                AdministeredBy = vaccinationRecord.AdministeredBy,
                BatchNumber = vaccinationRecord.BatchNumber,
                Symptoms = vaccinationRecord.Symptoms,
                VaccinationStatus = vaccinationRecord.VaccinationStatus,
                Notes = vaccinationRecord.Notes
            };
        }

        private IQueryable<VaccinationRecord> ApplyVaccinationRecordOrdering(IQueryable<VaccinationRecord> query, string orderBy)
        {
            return orderBy?.ToLower() switch
            {
                "vaccinationtype" => query.OrderBy(vr => vr.VaccinationType.Name),
                "vaccinationtype_desc" => query.OrderByDescending(vr => vr.VaccinationType.Name),
                "dosnumber" => query.OrderBy(vr => vr.DoseNumber),
                "dosnumber_desc" => query.OrderByDescending(vr => vr.DoseNumber),
                "administereddate" => query.OrderBy(vr => vr.AdministeredDate),
                "administereddate_desc" => query.OrderByDescending(vr => vr.AdministeredDate),
                _ => query.OrderByDescending(vr => vr.AdministeredDate)
            };
        }

        private async Task InvalidateAllCachesAsync()
        {
            try
            {
                _logger.LogDebug("Starting comprehensive cache invalidation for vaccination records");
                await Task.WhenAll(
                    _cacheService.RemoveByPrefixAsync(VACCINATION_RECORD_CACHE_PREFIX),
                    _cacheService.RemoveByPrefixAsync(VACCINATION_RECORD_LIST_PREFIX)
                );
                await Task.Delay(100);
                _logger.LogDebug("Completed comprehensive cache invalidation for vaccination records");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in comprehensive cache invalidation for vaccination records");
            }
        }

        #endregion
    }
}