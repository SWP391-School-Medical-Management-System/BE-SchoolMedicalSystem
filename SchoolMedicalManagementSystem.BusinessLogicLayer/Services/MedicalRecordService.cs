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
                .Include(mr => mr.VisionRecords.Where(vr => !vr.IsDeleted))
                .Include(mr => mr.HearingRecords.Where(hr => !hr.IsDeleted))
                .Include(mr => mr.PhysicalRecords.Where(pr => !pr.IsDeleted))
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
                .Include(mr => mr.VisionRecords.Where(vr => !vr.IsDeleted))
                .Include(mr => mr.HearingRecords.Where(hr => !hr.IsDeleted))
                .Include(mr => mr.PhysicalRecords.Where(pr => !pr.IsDeleted))
                .Where(mr => mr.UserId == studentId && !mr.IsDeleted)
                .FirstOrDefaultAsync();

            if (medicalRecord == null)
            {
                _logger.LogDebug("No medical record found for student: {StudentId}", studentId);
                return new BaseResponse<MedicalRecordDetailResponse>
                {
                    Success = false,
                    Message = "Không tìm thấy hồ sơ y tế cho học sinh này."
                };
            }

            _logger.LogDebug("Found {Count} active vaccination records for student {StudentId}",
                medicalRecord.VaccinationRecords?.Count ?? 0, studentId);
            foreach (var vr in medicalRecord.VaccinationRecords)
            {
                _logger.LogDebug("VaccinationRecord: Id={Id}, VaccineTypeId={VaccineTypeId}, VaccineType={VaccineTypeIdFromType}, VaccineTypeName={VaccineTypeName}, IsVaccinationTypeNull={IsVaccinationTypeNull}",
                    vr.Id, vr.VaccinationTypeId, vr.VaccinationType?.Id, vr.VaccinationType?.Name ?? "null", vr.VaccinationType == null);
            }

            // Thêm log chi tiết trước khi ánh xạ
            _logger.LogDebug("Starting mapping for MedicalRecord with Id: {MedicalRecordId}", medicalRecord.Id);
            var recordResponse = MapToMedicalRecordDetailResponse(medicalRecord);

            // Log dữ liệu sau khi ánh xạ để kiểm tra
            if (recordResponse.VaccinationRecords != null)
            {
                foreach (var vrResponse in recordResponse.VaccinationRecords)
                {
                    _logger.LogDebug("Mapped VaccinationRecord Response: Id={Id}, VaccineTypeId={VaccineTypeId}, VaccinationTypeName={VaccinationTypeName}",
                        vrResponse.Id, vrResponse.VaccinationTypeId, vrResponse.VaccinationTypeName);
                }
            }

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

    public async Task<BaseResponse<MedicalRecordDetailResponse>> UpdateMedicalRecordByParentAsync(
    Guid studentId,
    UpdateMedicalRecordByParentRequest model,
    Guid parentId)
    {
        try
        {
            // Xác thực phụ huynh
            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var student = await userRepo.GetQueryable()
                .Include(u => u.Parent)
                .FirstOrDefaultAsync(u => u.Id == studentId && !u.IsDeleted);

            if (student == null)
            {
                return new BaseResponse<MedicalRecordDetailResponse>
                {
                    Success = false,
                    Message = "Không tìm thấy học sinh."
                };
            }

            if (student.ParentId == null || student.ParentId != parentId)
            {
                return new BaseResponse<MedicalRecordDetailResponse>
                {
                    Success = false,
                    Message = "Bạn không có quyền chỉnh sửa hồ sơ y tế của học sinh này."
                };
            }

            // Lấy MedicalRecord và các bản ghi liên quan
            var recordRepo = _unitOfWork.GetRepositoryByEntity<MedicalRecord>();
            var medicalRecord = await recordRepo.GetQueryable()
                .Include(mr => mr.VisionRecords.Where(vr => !vr.IsDeleted))
                .Include(mr => mr.HearingRecords.Where(hr => !hr.IsDeleted))
                .Include(mr => mr.PhysicalRecords.Where(pr => !pr.IsDeleted))
                .FirstOrDefaultAsync(mr => mr.UserId == studentId && !mr.IsDeleted);

            if (medicalRecord == null)
            {
                return new BaseResponse<MedicalRecordDetailResponse>
                {
                    Success = false,
                    Message = "Không tìm thấy hồ sơ y tế cho học sinh này."
                };
            }

            // Cập nhật MedicalRecord
            if (!string.IsNullOrEmpty(model.BloodType))
                medicalRecord.BloodType = model.BloodType;
            if (!string.IsNullOrEmpty(model.EmergencyContact))
                medicalRecord.EmergencyContact = model.EmergencyContact;
            if (!string.IsNullOrEmpty(model.EmergencyContactPhone))
                medicalRecord.EmergencyContactPhone = model.EmergencyContactPhone;

            medicalRecord.LastUpdatedBy = "PARENT"; // Hoặc lấy từ một nguồn khác nếu cần
            medicalRecord.LastUpdatedDate = DateTime.Now;

            // Tạo mới VisionRecord
            var visionRecord = new VisionRecord
            {
                Id = Guid.NewGuid(),
                MedicalRecordId = medicalRecord.Id,
                LeftEye = model.LeftEye ?? 0,
                RightEye = model.RightEye ?? 0,
                CheckDate = model.CheckDate ?? DateTime.Now,
                Comments = model.Comments,
                RecordedBy = parentId,
                CreatedDate = DateTime.Now,
                LastUpdatedDate = DateTime.Now
            };
            await _unitOfWork.GetRepositoryByEntity<VisionRecord>().AddAsync(visionRecord);

            // Tạo mới HearingRecord
            var hearingRecord = new HearingRecord
            {
                Id = Guid.NewGuid(),
                MedicalRecordId = medicalRecord.Id,
                LeftEar = model.LeftEar ?? "Not recorded",
                RightEar = model.RightEar ?? "Not recorded",
                CheckDate = model.CheckDateHearing ?? DateTime.Now,
                Comments = model.CommentsHearing,
                RecordedBy = parentId,
                CreatedDate = DateTime.Now,
                LastUpdatedDate = DateTime.Now
            };
            await _unitOfWork.GetRepositoryByEntity<HearingRecord>().AddAsync(hearingRecord);

            // Tạo mới PhysicalRecord với BMI được tính tự động
            decimal bmi = 0;
            if (model.Height.HasValue && model.Weight.HasValue && model.Height.Value > 0)
            {
                decimal heightInMeters = model.Height.Value / 100; // Chuyển từ cm sang m
                bmi = model.Weight.Value / (heightInMeters * heightInMeters); // BMI = Weight / (Height²)
            }

            var physicalRecord = new PhysicalRecord
            {
                Id = Guid.NewGuid(),
                MedicalRecordId = medicalRecord.Id,
                Height = model.Height ?? 0,
                Weight = model.Weight ?? 0,
                BMI = bmi, // Gán BMI đã tính
                CheckDate = model.CheckDatePhysical ?? DateTime.Now,
                Comments = model.CommentsPhysical,
                RecordedBy = parentId,
                CreatedDate = DateTime.Now,
                LastUpdatedDate = DateTime.Now
            };
            await _unitOfWork.GetRepositoryByEntity<PhysicalRecord>().AddAsync(physicalRecord);

            await _unitOfWork.SaveChangesAsync();
            await InvalidateAllCachesAsync();

            // Lấy lại dữ liệu để trả về response
            medicalRecord = await recordRepo.GetQueryable()
                .Include(mr => mr.Student)
                .Include(mr => mr.MedicalConditions.Where(mc => !mc.IsDeleted))
                .Include(mr => mr.VisionRecords.Where(vr => !vr.IsDeleted))
                .Include(mr => mr.HearingRecords.Where(hr => !hr.IsDeleted))
                .Include(mr => mr.PhysicalRecords.Where(pr => !pr.IsDeleted))
                .FirstOrDefaultAsync(mr => mr.Id == medicalRecord.Id);

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
            _logger.LogError(ex, "Error updating medical record by parent for student: {StudentId}", studentId);
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
            _logger.LogDebug("Mapping {Count} active vaccination records for MedicalRecord Id: {MedicalRecordId}", activeVaccinations.Count, medicalRecord.Id);
            response.VaccinationRecords = _mapper.Map<List<VaccinationRecordResponse>>(activeVaccinations);

            // Thêm log để kiểm tra dữ liệu sau ánh xạ
            foreach (var vr in response.VaccinationRecords)
            {
                _logger.LogDebug("Mapped VaccinationRecord: Id={Id}, VaccineTypeId={VaccineTypeId}, VaccinationTypeName={VaccinationTypeName} for MedicalRecord Id: {MedicalRecordId}",
                    vr.Id, vr.VaccinationTypeId, vr.VaccinationTypeName, medicalRecord.Id);
            }
        }

        var sixMonthsAgo = DateTime.Now.AddMonths(-6);
        response.NeedsUpdate = medicalRecord.LastUpdatedDate == null || medicalRecord.LastUpdatedDate < sixMonthsAgo;

        // Ánh xạ và sắp xếp VisionRecords (mới nhất lên đầu)
        if (medicalRecord.VisionRecords != null)
        {
            response.VisionRecords = medicalRecord.VisionRecords
                .Where(vr => !vr.IsDeleted)
                .OrderByDescending(vr => vr.CheckDate)
                .Select(vr => new VisionRecordResponse
                {
                    LeftEye = vr.LeftEye,
                    RightEye = vr.RightEye,
                    CheckDate = vr.CheckDate,
                    Comments = vr.Comments,
                    RecordedBy = vr.RecordedBy
                }).ToList();
        }

        // Ánh xạ và sắp xếp HearingRecords (mới nhất lên đầu)
        if (medicalRecord.HearingRecords != null)
        {
            response.HearingRecords = medicalRecord.HearingRecords
                .Where(hr => !hr.IsDeleted)
                .OrderByDescending(hr => hr.CheckDate)
                .Select(hr => new HearingRecordResponse
                {
                    LeftEar = hr.LeftEar,
                    RightEar = hr.RightEar,
                    CheckDate = hr.CheckDate,
                    Comments = hr.Comments,
                    RecordedBy = hr.RecordedBy
                }).ToList();
        }

        // Ánh xạ và sắp xếp PhysicalRecords (mới nhất lên đầu)
        if (medicalRecord.PhysicalRecords != null)
        {
            response.PhysicalRecords = medicalRecord.PhysicalRecords
                .Where(pr => !pr.IsDeleted)
                .OrderByDescending(pr => pr.CheckDate)
                .Select(pr => new PhysicalRecordResponse
                {
                    Height = pr.Height,
                    Weight = pr.Weight,
                    BMI = pr.BMI,
                    CheckDate = pr.CheckDate,
                    Comments = pr.Comments,
                    RecordedBy = pr.RecordedBy
                }).ToList();
        }

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