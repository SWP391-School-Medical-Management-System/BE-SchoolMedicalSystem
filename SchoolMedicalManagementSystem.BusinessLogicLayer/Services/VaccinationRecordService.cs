using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
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
        private readonly IHttpContextAccessor _httpContextAccessor;
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
            IHttpContextAccessor httpContextAccessor,
            IValidator<CreateVaccinationRecordRequest> createVaccinationRecordValidator,
            IValidator<UpdateVaccinationRecordRequest> updateVaccinationRecordValidator)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
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
                    _logger.LogDebug("Vaccination records list found in cache for medical record: {MedicalRecordId}, cacheKey: {CacheKey}", medicalRecordId, cacheKey);
                    return cachedResult;
                }

                var query = _unitOfWork.GetRepositoryByEntity<VaccinationRecord>().GetQueryable()
                    .Include(vr => vr.VaccinationType)
                    .Include(vr => vr.AdministeredByUser)
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
                foreach (var response in responses)
                {
                    _logger.LogDebug("Vaccination record {Id}: AdministeredBy = {AdministeredBy}", response.Id, response.AdministeredBy);
                }

                var result = BaseListResponse<VaccinationRecordResponse>.SuccessResult(
                    responses,
                    totalCount,
                    pageSize,
                    pageIndex,
                    "Lấy danh sách hồ sơ tiêm chủng thành công.");

                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
                await _cacheService.AddToTrackingSetAsync(cacheKey, VACCINATION_RECORD_CACHE_SET);

                _logger.LogDebug("Cached vaccination records list with key: {CacheKey}", cacheKey);
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
                // Validate request
                var validationResult = await _createVaccinationRecordValidator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return BaseResponse<VaccinationRecordResponse>.ErrorResult(errors);
                }

                // Check medical record
                var recordRepo = _unitOfWork.GetRepositoryByEntity<MedicalRecord>();
                var medicalRecord = await recordRepo.GetQueryable()
                    .Include(mr => mr.Student)
                    .FirstOrDefaultAsync(mr => mr.Id == medicalRecordId && !mr.IsDeleted);

                if (medicalRecord == null)
                {
                    _logger.LogWarning("Medical record not found with ID: {MedicalRecordId}", medicalRecordId);
                    return BaseResponse<VaccinationRecordResponse>.ErrorResult("Medical record not found.");
                }

                if (medicalRecord.Student == null || medicalRecord.Student.Id == Guid.Empty)
                {
                    _logger.LogWarning("Medical record {MedicalRecordId} is not linked to a valid student.", medicalRecordId);
                    return BaseResponse<VaccinationRecordResponse>.ErrorResult("Medical record is not linked to a student.");
                }

                // Check vaccination type
                var vaccinationTypeRepo = _unitOfWork.GetRepositoryByEntity<VaccinationType>();
                var vaccinationType = await vaccinationTypeRepo.GetQueryable()
                    .FirstOrDefaultAsync(vt => vt.Id == model.VaccinationTypeId && !vt.IsDeleted);

                if (vaccinationType == null)
                {
                    _logger.LogWarning("Vaccination type not found with ID: {VaccinationTypeId}", model.VaccinationTypeId);
                    return BaseResponse<VaccinationRecordResponse>.ErrorResult("Vaccination type not found.");
                }

                // Map to entity
                var vaccinationRecord = _mapper.Map<VaccinationRecord>(model);
                vaccinationRecord.Id = Guid.NewGuid();
                vaccinationRecord.MedicalRecordId = medicalRecordId;
                vaccinationRecord.UserId = medicalRecord.Student.Id;
                vaccinationRecord.CreatedDate = DateTime.UtcNow;
                vaccinationRecord.IsDeleted = false;
                vaccinationRecord.Code = $"VACREC-{Guid.NewGuid().ToString().Substring(0, 8)}";

                // Determine CreatedBy and AdministeredBy logic
                var isSchoolNurse = _httpContextAccessor.HttpContext?.User.IsInRole("SCHOOLNURSE") == true;
                vaccinationRecord.CreatedBy = isSchoolNurse ? "SCHOOLNURSE" : "PARENT";

                if (isSchoolNurse && model.SessionId.HasValue)
                {
                    // For SCHOOLNURSE with session, use AdministeredByUserId
                    vaccinationRecord.SessionId = model.SessionId;
                    vaccinationRecord.AdministeredByUserId = model.AdministeredByUserId;

                    // Fetch user to set AdministeredBy as FullName
                    var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
                    var administeredByUser = await userRepo.GetQueryable()
                        .FirstOrDefaultAsync(u => u.Id == model.AdministeredByUserId && !u.IsDeleted);
                    if (administeredByUser != null)
                    {
                        vaccinationRecord.AdministeredBy = administeredByUser.FullName;
                    }
                }
                else if (!isSchoolNurse)
                {
                    // For PARENT, ensure AdministeredByUserId is null
                    vaccinationRecord.AdministeredByUserId = null;
                    vaccinationRecord.SessionId = null;
                }

                _logger.LogDebug("VaccinationRecord before saving: {Entity}", System.Text.Json.JsonSerializer.Serialize(vaccinationRecord));

                // Save to database
                var recordRepoVac = _unitOfWork.GetRepositoryByEntity<VaccinationRecord>();
                await recordRepoVac.AddAsync(vaccinationRecord);
                await _unitOfWork.SaveChangesAsync();

                // Clear cache
                var recordCacheKey = _cacheService.GenerateCacheKey(VACCINATION_RECORD_CACHE_PREFIX, vaccinationRecord.Id.ToString());
                await _cacheService.RemoveAsync(recordCacheKey);
                _logger.LogDebug("Cleared cache for vaccination record detail: {CacheKey}", recordCacheKey);
                await _cacheService.RemoveByPrefixAsync(VACCINATION_RECORD_LIST_PREFIX);
                _logger.LogDebug("Cleared cache for vaccination records list with prefix: {Prefix}", VACCINATION_RECORD_LIST_PREFIX);

                var vaccinationResultCacheKey = _cacheService.GenerateCacheKey(
                    "student_vaccination_result",
                    vaccinationRecord.SessionId?.ToString() ?? "",
                    vaccinationRecord.UserId.ToString());
                await _cacheService.RemoveAsync(vaccinationResultCacheKey);
                _logger.LogDebug("Cleared cache for student vaccination result: {CacheKey}", vaccinationResultCacheKey);

                await InvalidateAllCachesAsync();

                // Retrieve saved record
                vaccinationRecord = await recordRepoVac.GetQueryable()
                    .Include(vr => vr.VaccinationType)
                    .FirstOrDefaultAsync(vr => vr.Id == vaccinationRecord.Id);

                if (vaccinationRecord == null)
                {
                    _logger.LogError("Failed to retrieve created vaccination record with ID: {VaccinationRecordId}", vaccinationRecord.Id);
                    return BaseResponse<VaccinationRecordResponse>.ErrorResult("Failed to retrieve vaccination record data.");
                }

                var response = _mapper.Map<VaccinationRecordResponse>(vaccinationRecord);

                _logger.LogInformation("Successfully created vaccination record with ID: {VaccinationRecordId}", vaccinationRecord.Id);
                return BaseResponse<VaccinationRecordResponse>.SuccessResult(response, "Vaccination record created successfully.");
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                _logger.LogError(ex, "Error saving vaccination record for medical record {MedicalRecordId}: {Error}", medicalRecordId, innerException);
                return BaseResponse<VaccinationRecordResponse>.ErrorResult($"Failed to create vaccination record: {innerException}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vaccination record for medical record {MedicalRecordId}: {Error}", medicalRecordId, ex.Message);
                return BaseResponse<VaccinationRecordResponse>.ErrorResult($"Failed to create vaccination record: {ex.Message}");
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

                // Xóa cache cụ thể cho vaccination record detail và danh sách vaccination records
                var recordCacheKey = _cacheService.GenerateCacheKey(VACCINATION_RECORD_CACHE_PREFIX, recordId.ToString());
                await _cacheService.RemoveAsync(recordCacheKey);
                _logger.LogDebug("Đã xóa cache cụ thể cho vaccination record detail: {CacheKey}", recordCacheKey);
                await _cacheService.RemoveByPrefixAsync(VACCINATION_RECORD_LIST_PREFIX);
                _logger.LogDebug("Đã xóa cache danh sách vaccination records với prefix: {Prefix}", VACCINATION_RECORD_LIST_PREFIX);
                // Xóa cache student_vaccination_result liên quan
                var vaccinationResultCacheKey = _cacheService.GenerateCacheKey("student_vaccination_result", vaccinationRecord.SessionId?.ToString() ?? "", vaccinationRecord.UserId.ToString());
                await _cacheService.RemoveAsync(vaccinationResultCacheKey);
                _logger.LogDebug("Đã xóa cache student vaccination result: {CacheKey}", vaccinationResultCacheKey);

                await InvalidateAllCachesAsync();

                var response = MapToVaccinationRecordResponse(vaccinationRecord);

                _logger.LogInformation("Cập nhật hồ sơ tiêm chủng thành công với ID: {VaccinationRecordId}", recordId);
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

                // Xóa cache cụ thể cho vaccination record detail và danh sách vaccination records
                var recordCacheKey = _cacheService.GenerateCacheKey(VACCINATION_RECORD_CACHE_PREFIX, recordId.ToString());
                await _cacheService.RemoveAsync(recordCacheKey);
                _logger.LogDebug("Đã xóa cache cụ thể cho vaccination record detail: {CacheKey}", recordCacheKey);
                await _cacheService.RemoveByPrefixAsync(VACCINATION_RECORD_LIST_PREFIX);
                _logger.LogDebug("Đã xóa cache danh sách vaccination records với prefix: {Prefix}", VACCINATION_RECORD_LIST_PREFIX);
                // Xóa cache student_vaccination_result liên quan
                var vaccinationResultCacheKey = _cacheService.GenerateCacheKey("student_vaccination_result", vaccinationRecord.SessionId?.ToString() ?? "", vaccinationRecord.UserId.ToString());
                await _cacheService.RemoveAsync(vaccinationResultCacheKey);
                _logger.LogDebug("Đã xóa cache student vaccination result: {CacheKey}", vaccinationResultCacheKey);

                await InvalidateAllCachesAsync();

                _logger.LogInformation("Xóa hồ sơ tiêm chủng thành công với ID: {VaccinationRecordId}", recordId);
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

        private VaccinationRecordResponse MapToVaccinationRecordResponse(VaccinationRecord record)
        {
            return new VaccinationRecordResponse
            {
                Id = record.Id,
                VaccinationTypeName = record.VaccinationType?.Name ?? "Không xác định",
                DoseNumber = record.DoseNumber,
                AdministeredDate = record.AdministeredDate ?? DateTime.MinValue,
                AdministeredBy = record.AdministeredByUser?.FullName ?? record.AdministeredBy ?? "Không xác định",
                BatchNumber = record.BatchNumber ?? string.Empty,
                Notes = record.Notes ?? string.Empty,
                NoteAfterSession = record.NoteAfterSession ?? string.Empty,
                VaccinationStatus = record.VaccinationStatus ?? string.Empty,
                Symptoms = record.Symptoms ?? string.Empty
            };
        }

        private IQueryable<VaccinationRecord> ApplyVaccinationRecordOrdering(IQueryable<VaccinationRecord> query, string orderBy)
        {
            if (string.IsNullOrEmpty(orderBy))
                return query.OrderBy(vr => vr.AdministeredDate);

            return orderBy.ToLower() switch
            {
                "dateasc" => query.OrderBy(vr => vr.AdministeredDate),
                "datedesc" => query.OrderByDescending(vr => vr.AdministeredDate),
                "typeasc" => query.OrderBy(vr => vr.VaccinationType.Name),
                "typedesc" => query.OrderByDescending(vr => vr.VaccinationType.Name),
                _ => query.OrderBy(vr => vr.AdministeredDate)
            };
        }

        private async Task InvalidateAllCachesAsync()
        {
            try
            {
                _logger.LogDebug("Starting comprehensive cache invalidation for vaccination records and related entities");
                // Xóa toàn bộ tracking set của VaccinationRecord
                await _cacheService.InvalidateTrackingSetAsync(VACCINATION_RECORD_CACHE_SET);
                // Xóa các tiền tố cụ thể của VaccinationRecord và các thực thể liên quan
                await Task.WhenAll(
                    _cacheService.RemoveByPrefixAsync(VACCINATION_RECORD_CACHE_PREFIX),
                    _cacheService.RemoveByPrefixAsync(VACCINATION_RECORD_LIST_PREFIX),
                    // Xóa các cache liên quan đến VaccinationSessionService
                    _cacheService.RemoveByPrefixAsync("vaccination_session"),
                    _cacheService.RemoveByPrefixAsync("vaccination_sessions_list"),
                    _cacheService.RemoveByPrefixAsync("student_sessions"),
                    _cacheService.RemoveByPrefixAsync("parent_consent_status"),
                    _cacheService.RemoveByPrefixAsync("student_vaccination_result")
                );
                _logger.LogDebug("Completed comprehensive cache invalidation for vaccination records and related entities");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in comprehensive cache invalidation for vaccination records");
            }
        }

        #endregion
    }
}