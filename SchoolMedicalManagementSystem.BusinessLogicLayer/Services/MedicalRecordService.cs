using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalRecordRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalCondition;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalRecordResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services;

public class MedicalRecordService : IMedicalRecordService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<MedicalRecordService> _logger;

    private readonly IValidator<CreateMedicalRecordRequest> _createMedicalRecordValidator;
    private readonly IValidator<UpdateMedicalRecordRequest> _updateMedicalRecordValidator;

    private const string MEDICAL_RECORD_CACHE_PREFIX = "medical_record";
    private const string MEDICAL_RECORD_LIST_PREFIX = "medical_records_list";
    private const string MEDICAL_RECORD_CACHE_SET = "medical_record_cache_keys";

    private const string STUDENT_CACHE_PREFIX = "student";
    private const string STUDENT_LIST_PREFIX = "students_list";
    private const string PARENT_CACHE_PREFIX = "parent";
    private const string PARENT_LIST_PREFIX = "parents_list";
    private const string STATISTICS_PREFIX = "statistics";

    public MedicalRecordService(
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<MedicalRecordService> logger,
        IValidator<CreateMedicalRecordRequest> createMedicalRecordValidator,
        IValidator<UpdateMedicalRecordRequest> updateMedicalRecordValidator)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
        _createMedicalRecordValidator = createMedicalRecordValidator;
        _updateMedicalRecordValidator = updateMedicalRecordValidator;
    }

    #region Medical Record Management

    public async Task<BaseListResponse<MedicalRecordResponse>> GetMedicalRecordsAsync(
        int pageIndex,
        int pageSize,
        string searchTerm,
        string orderBy,
        string bloodType = null,
        bool? hasAllergies = null,
        bool? hasChronicDisease = null,
        bool? needsUpdate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(
                MEDICAL_RECORD_LIST_PREFIX,
                pageIndex.ToString(),
                pageSize.ToString(),
                searchTerm ?? "",
                orderBy ?? "",
                bloodType ?? "",
                hasAllergies?.ToString() ?? "",
                hasChronicDisease?.ToString() ?? "",
                needsUpdate?.ToString() ?? ""
            );

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<MedicalRecordResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Medical records list found in cache");
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<MedicalRecord>().GetQueryable()
                .Include(mr => mr.Student)
                .Include(mr => mr.MedicalConditions.Where(mc => !mc.IsDeleted))
                .Where(mr => !mr.IsDeleted && !mr.Student.IsDeleted)
                .AsQueryable();

            query = ApplyMedicalRecordFilters(query, searchTerm, bloodType, hasAllergies, hasChronicDisease,
                needsUpdate);
            query = ApplyMedicalRecordOrdering(query, orderBy);

            var totalCount = await query.CountAsync(cancellationToken);
            var medicalRecords = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = medicalRecords.Select(MapToMedicalRecordResponse).ToList();

            var result = BaseListResponse<MedicalRecordResponse>.SuccessResult(
                responses,
                totalCount,
                pageSize,
                pageIndex,
                "Lấy danh sách hồ sơ y tế thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICAL_RECORD_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medical records");
            return BaseListResponse<MedicalRecordResponse>.ErrorResult("Lỗi lấy danh sách hồ sơ y tế.");
        }
    }

    public async Task<BaseResponse<MedicalRecordDetailResponse>> GetMedicalRecordByIdAsync(Guid recordId)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(MEDICAL_RECORD_CACHE_PREFIX, recordId.ToString());
            var cachedResponse = await _cacheService.GetAsync<BaseResponse<MedicalRecordDetailResponse>>(cacheKey);

            if (cachedResponse != null)
            {
                _logger.LogDebug("Medical record found in cache: {RecordId}", recordId);
                return cachedResponse;
            }

            var recordRepo = _unitOfWork.GetRepositoryByEntity<MedicalRecord>();
            var medicalRecord = await recordRepo.GetQueryable()
                .Include(mr => mr.Student)
                .Include(mr => mr.MedicalConditions.Where(mc => !mc.IsDeleted))
                .Include(mr => mr.VaccinationRecords.Where(vr => !vr.IsDeleted))
                .ThenInclude(vr => vr.VaccinationType)
                .Where(mr => mr.Id == recordId && !mr.IsDeleted)
                .FirstOrDefaultAsync();

            if (medicalRecord == null)
            {
                return new BaseResponse<MedicalRecordDetailResponse>
                {
                    Success = false,
                    Message = "Không tìm thấy hồ sơ y tế."
                };
            }

            var recordResponse = MapToMedicalRecordDetailResponse(medicalRecord);

            var response = new BaseResponse<MedicalRecordDetailResponse>
            {
                Success = true,
                Data = recordResponse,
                Message = "Lấy thông tin hồ sơ y tế thành công."
            };

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICAL_RECORD_CACHE_SET);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting medical record by ID: {RecordId}", recordId);
            return new BaseResponse<MedicalRecordDetailResponse>
            {
                Success = false,
                Message = $"Lỗi lấy thông tin hồ sơ y tế: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<MedicalRecordDetailResponse>> GetMedicalRecordByStudentIdAsync(Guid studentId)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(MEDICAL_RECORD_CACHE_PREFIX, "student", studentId.ToString());
            var cachedResponse = await _cacheService.GetAsync<BaseResponse<MedicalRecordDetailResponse>>(cacheKey);

            if (cachedResponse != null)
            {
                _logger.LogDebug("Medical record found in cache for student: {StudentId}", studentId);
                return cachedResponse;
            }

            var recordRepo = _unitOfWork.GetRepositoryByEntity<MedicalRecord>();
            var medicalRecord = await recordRepo.GetQueryable()
                .Include(mr => mr.Student)
                .Include(mr => mr.MedicalConditions.Where(mc => !mc.IsDeleted))
                .Include(mr => mr.VaccinationRecords.Where(vr => !vr.IsDeleted))
                .ThenInclude(vr => vr.VaccinationType)
                .Where(mr => mr.UserId == studentId && !mr.IsDeleted)
                .FirstOrDefaultAsync();

            if (medicalRecord == null)
            {
                return new BaseResponse<MedicalRecordDetailResponse>
                {
                    Success = false,
                    Message = "Không tìm thấy hồ sơ y tế cho học sinh này."
                };
            }

            var recordResponse = MapToMedicalRecordDetailResponse(medicalRecord);

            var response = new BaseResponse<MedicalRecordDetailResponse>
            {
                Success = true,
                Data = recordResponse,
                Message = "Lấy thông tin hồ sơ y tế thành công."
            };

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICAL_RECORD_CACHE_SET);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting medical record for student: {StudentId}", studentId);
            return new BaseResponse<MedicalRecordDetailResponse>
            {
                Success = false,
                Message = $"Lỗi lấy thông tin hồ sơ y tế: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<MedicalRecordDetailResponse>> CreateMedicalRecordAsync(
        CreateMedicalRecordRequest model)
    {
        try
        {
            var validationResult = await _createMedicalRecordValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return new BaseResponse<MedicalRecordDetailResponse>
                {
                    Success = false,
                    Message = errors
                };
            }

            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var student = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.MedicalRecord)
                .FirstOrDefaultAsync(u => u.Id == model.UserId && !u.IsDeleted);

            if (student == null)
            {
                return new BaseResponse<MedicalRecordDetailResponse>
                {
                    Success = false,
                    Message = "Không tìm thấy học sinh."
                };
            }

            if (!student.UserRoles.Any(ur => ur.Role.Name == "STUDENT"))
            {
                return new BaseResponse<MedicalRecordDetailResponse>
                {
                    Success = false,
                    Message = "Người dùng không phải là học sinh."
                };
            }

            if (student.MedicalRecord != null && !student.MedicalRecord.IsDeleted)
            {
                return new BaseResponse<MedicalRecordDetailResponse>
                {
                    Success = false,
                    Message = "Học sinh đã có hồ sơ y tế."
                };
            }

            var schoolNurseRoleName = await GetSchoolNurseRoleName();

            var medicalRecord = _mapper.Map<MedicalRecord>(model);
            medicalRecord.Id = Guid.NewGuid();
            medicalRecord.CreatedBy = schoolNurseRoleName;
            medicalRecord.CreatedDate = DateTime.Now;

            var recordRepo = _unitOfWork.GetRepositoryByEntity<MedicalRecord>();
            await recordRepo.AddAsync(medicalRecord);
            await _unitOfWork.SaveChangesAsync();

            await InvalidateAllCachesAsync();

            medicalRecord = await recordRepo.GetQueryable()
                .Include(mr => mr.Student)
                .Include(mr => mr.MedicalConditions.Where(mc => !mc.IsDeleted))
                .Include(mr => mr.VaccinationRecords.Where(vr => !vr.IsDeleted))
                .ThenInclude(vr => vr.VaccinationType)
                .FirstOrDefaultAsync(mr => mr.Id == medicalRecord.Id);

            var recordResponse = MapToMedicalRecordDetailResponse(medicalRecord);

            return new BaseResponse<MedicalRecordDetailResponse>
            {
                Success = true,
                Data = recordResponse,
                Message = "Tạo hồ sơ y tế thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating medical record");
            return new BaseResponse<MedicalRecordDetailResponse>
            {
                Success = false,
                Message = $"Lỗi tạo hồ sơ y tế: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<MedicalRecordDetailResponse>> UpdateMedicalRecordAsync(Guid recordId,
        UpdateMedicalRecordRequest model)
    {
        try
        {
            var validationResult = await _updateMedicalRecordValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return new BaseResponse<MedicalRecordDetailResponse>
                {
                    Success = false,
                    Message = errors
                };
            }

            var recordRepo = _unitOfWork.GetRepositoryByEntity<MedicalRecord>();
            var medicalRecord = await recordRepo.GetQueryable()
                .Include(mr => mr.Student)
                .Include(mr => mr.MedicalConditions.Where(mc => !mc.IsDeleted))
                .Include(mr => mr.VaccinationRecords.Where(vr => !vr.IsDeleted))
                .ThenInclude(vr => vr.VaccinationType)
                .FirstOrDefaultAsync(mr => mr.Id == recordId && !mr.IsDeleted);

            if (medicalRecord == null)
            {
                return new BaseResponse<MedicalRecordDetailResponse>
                {
                    Success = false,
                    Message = "Không tìm thấy hồ sơ y tế."
                };
            }

            var schoolNurseRoleName = await GetSchoolNurseRoleName();

            if (!string.IsNullOrEmpty(model.BloodType))
                medicalRecord.BloodType = model.BloodType;

            if (!string.IsNullOrEmpty(model.EmergencyContact))
                medicalRecord.EmergencyContact = model.EmergencyContact;

            if (!string.IsNullOrEmpty(model.EmergencyContactPhone))
                medicalRecord.EmergencyContactPhone = model.EmergencyContactPhone;

            medicalRecord.LastUpdatedBy = schoolNurseRoleName;
            medicalRecord.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            await InvalidateAllCachesAsync();

            var recordResponse = MapToMedicalRecordDetailResponse(medicalRecord);

            return new BaseResponse<MedicalRecordDetailResponse>
            {
                Success = true,
                Data = recordResponse,
                Message = "Cập nhật hồ sơ y tế thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating medical record");
            return new BaseResponse<MedicalRecordDetailResponse>
            {
                Success = false,
                Message = $"Lỗi cập nhật hồ sơ y tế: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<bool>> DeleteMedicalRecordAsync(Guid recordId)
    {
        try
        {
            var recordRepo = _unitOfWork.GetRepositoryByEntity<MedicalRecord>();
            var medicalRecord = await recordRepo.GetQueryable()
                .FirstOrDefaultAsync(mr => mr.Id == recordId && !mr.IsDeleted);

            if (medicalRecord == null)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "Không tìm thấy hồ sơ y tế."
                };
            }

            var schoolNurseRoleName = await GetSchoolNurseRoleName();

            medicalRecord.IsDeleted = true;
            medicalRecord.LastUpdatedBy = schoolNurseRoleName;
            medicalRecord.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            await InvalidateAllCachesAsync();

            return new BaseResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Xóa hồ sơ y tế thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting medical record: {RecordId}", recordId);
            return new BaseResponse<bool>
            {
                Success = false,
                Data = false,
                Message = $"Lỗi xóa hồ sơ y tế: {ex.Message}"
            };
        }
    }

    #endregion

    #region Helper Methods

    private async Task<string> GetSchoolNurseRoleName()
    {
        try
        {
            var schoolNurseRole = await _unitOfWork.GetRepositoryByEntity<Role>().GetQueryable()
                .FirstOrDefaultAsync(r => r.Name == "SCHOOLNURSE");

            return schoolNurseRole?.Name ?? "SCHOOLNURSE";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting school nurse role name");
            return "SCHOOLNURSE";
        }
    }

    private MedicalRecordResponse MapToMedicalRecordResponse(MedicalRecord medicalRecord)
    {
        var response = _mapper.Map<MedicalRecordResponse>(medicalRecord);

        if (medicalRecord.Student != null)
        {
            response.StudentName = medicalRecord.Student.FullName;
            response.StudentCode = medicalRecord.Student.StudentCode;
        }

        if (medicalRecord.MedicalConditions != null)
        {
            var activeConditions = medicalRecord.MedicalConditions.Where(mc => !mc.IsDeleted).ToList();
            response.AllergyCount = activeConditions.Count(mc => mc.Type == MedicalConditionType.Allergy);
            response.ChronicDiseaseCount = activeConditions.Count(mc => mc.Type == MedicalConditionType.ChronicDisease);
        }

        var sixMonthsAgo = DateTime.Now.AddMonths(-6);
        response.NeedsUpdate = medicalRecord.LastUpdatedDate == null || medicalRecord.LastUpdatedDate < sixMonthsAgo;

        return response;
    }

    private MedicalRecordDetailResponse MapToMedicalRecordDetailResponse(MedicalRecord medicalRecord)
    {
        var response = _mapper.Map<MedicalRecordDetailResponse>(medicalRecord);

        if (medicalRecord.Student != null)
        {
            response.StudentName = medicalRecord.Student.FullName;
            response.StudentCode = medicalRecord.Student.StudentCode;
        }

        if (medicalRecord.MedicalConditions != null)
        {
            var activeConditions = medicalRecord.MedicalConditions.Where(mc => !mc.IsDeleted).ToList();
            response.MedicalConditions = _mapper.Map<List<MedicalConditionResponse>>(activeConditions);
        }

        if (medicalRecord.VaccinationRecords != null)
        {
            var activeVaccinations = medicalRecord.VaccinationRecords.Where(vr => !vr.IsDeleted).ToList();
            response.VaccinationRecords = _mapper.Map<List<VaccinationRecordResponse>>(activeVaccinations);
        }

        var sixMonthsAgo = DateTime.Now.AddMonths(-6);
        response.NeedsUpdate = medicalRecord.LastUpdatedDate == null || medicalRecord.LastUpdatedDate < sixMonthsAgo;

        return response;
    }

    private IQueryable<MedicalRecord> ApplyMedicalRecordFilters(
        IQueryable<MedicalRecord> query,
        string searchTerm,
        string bloodType,
        bool? hasAllergies,
        bool? hasChronicDisease,
        bool? needsUpdate)
    {
        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(mr =>
                mr.Student.FullName.ToLower().Contains(searchTerm) ||
                mr.Student.StudentCode.ToLower().Contains(searchTerm) ||
                mr.EmergencyContact.ToLower().Contains(searchTerm));
        }

        if (!string.IsNullOrEmpty(bloodType))
        {
            query = query.Where(mr => mr.BloodType == bloodType);
        }

        if (hasAllergies.HasValue)
        {
            if (hasAllergies.Value)
            {
                query = query.Where(mr =>
                    mr.MedicalConditions.Any(mc => mc.Type == MedicalConditionType.Allergy && !mc.IsDeleted));
            }
            else
            {
                query = query.Where(mr =>
                    !mr.MedicalConditions.Any(mc => mc.Type == MedicalConditionType.Allergy && !mc.IsDeleted));
            }
        }

        if (hasChronicDisease.HasValue)
        {
            if (hasChronicDisease.Value)
            {
                query = query.Where(mr =>
                    mr.MedicalConditions.Any(mc => mc.Type == MedicalConditionType.ChronicDisease && !mc.IsDeleted));
            }
            else
            {
                query = query.Where(mr =>
                    !mr.MedicalConditions.Any(mc => mc.Type == MedicalConditionType.ChronicDisease && !mc.IsDeleted));
            }
        }

        if (needsUpdate.HasValue)
        {
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            if (needsUpdate.Value)
            {
                query = query.Where(mr => mr.LastUpdatedDate == null || mr.LastUpdatedDate < sixMonthsAgo);
            }
            else
            {
                query = query.Where(mr => mr.LastUpdatedDate != null && mr.LastUpdatedDate >= sixMonthsAgo);
            }
        }

        return query;
    }

    private IQueryable<MedicalRecord> ApplyMedicalRecordOrdering(IQueryable<MedicalRecord> query, string orderBy)
    {
        return orderBy?.ToLower() switch
        {
            "studentname" => query.OrderBy(mr => mr.Student.FullName),
            "studentname_desc" => query.OrderByDescending(mr => mr.Student.FullName),
            "studentcode" => query.OrderBy(mr => mr.Student.StudentCode),
            "studentcode_desc" => query.OrderByDescending(mr => mr.Student.StudentCode),
            "bloodtype" => query.OrderBy(mr => mr.BloodType),
            "bloodtype_desc" => query.OrderByDescending(mr => mr.BloodType),
            "lastupdated" => query.OrderBy(mr => mr.LastUpdatedDate ?? mr.CreatedDate),
            "lastupdated_desc" => query.OrderByDescending(mr => mr.LastUpdatedDate ?? mr.CreatedDate),
            "createdate" => query.OrderBy(mr => mr.CreatedDate),
            "createdate_desc" => query.OrderByDescending(mr => mr.CreatedDate),
            _ => query.OrderByDescending(mr => mr.LastUpdatedDate ?? mr.CreatedDate)
        };
    }

    private async Task InvalidateAllCachesAsync()
    {
        try
        {
            _logger.LogDebug("Starting comprehensive cache invalidation for medical records");

            await Task.WhenAll(
                _cacheService.RemoveByPrefixAsync(MEDICAL_RECORD_CACHE_PREFIX),
                _cacheService.RemoveByPrefixAsync(MEDICAL_RECORD_LIST_PREFIX),
                _cacheService.RemoveByPrefixAsync(STUDENT_CACHE_PREFIX),
                _cacheService.RemoveByPrefixAsync(STUDENT_LIST_PREFIX),
                _cacheService.RemoveByPrefixAsync(PARENT_CACHE_PREFIX),
                _cacheService.RemoveByPrefixAsync(PARENT_LIST_PREFIX),
                _cacheService.RemoveByPrefixAsync(STATISTICS_PREFIX)
            );

            await Task.Delay(100);

            _logger.LogDebug("Completed comprehensive cache invalidation for medical records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in comprehensive cache invalidation for medical records");
        }
    }

    #endregion
}