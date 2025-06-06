using System.Security.Claims;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalConditionRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalCondition;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services;

public class MedicalConditionService : IMedicalConditionService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<MedicalConditionService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IValidator<CreateMedicalConditionRequest> _createMedicalConditionValidator;
    private readonly IValidator<UpdateMedicalConditionRequest> _updateMedicalConditionValidator;

    private const string MEDICAL_CONDITION_CACHE_PREFIX = "medical_condition";
    private const string MEDICAL_CONDITION_LIST_PREFIX = "medical_conditions_list";
    private const string MEDICAL_CONDITION_CACHE_SET = "medical_condition_cache_keys";

    private const string MEDICAL_RECORD_CACHE_PREFIX = "medical_record";
    private const string MEDICAL_RECORD_LIST_PREFIX = "medical_records_list";
    private const string STUDENT_CACHE_PREFIX = "student";
    private const string STUDENT_LIST_PREFIX = "students_list";
    private const string PARENT_CACHE_PREFIX = "parent";
    private const string PARENT_LIST_PREFIX = "parents_list";
    private const string STATISTICS_PREFIX = "statistics";

    public MedicalConditionService(
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<MedicalConditionService> logger,
        IValidator<CreateMedicalConditionRequest> createMedicalConditionValidator,
        IValidator<UpdateMedicalConditionRequest> updateMedicalConditionValidator,
        IHttpContextAccessor httpContextAccessor)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
        _createMedicalConditionValidator = createMedicalConditionValidator;
        _updateMedicalConditionValidator = updateMedicalConditionValidator;
        _httpContextAccessor = httpContextAccessor;
    }

    #region Medical Condition Management

    public async Task<BaseListResponse<MedicalConditionResponse>> GetMedicalConditionsAsync(
        int pageIndex,
        int pageSize,
        string searchTerm,
        string orderBy,
        Guid? medicalRecordId = null,
        MedicalConditionType? type = null,
        string severity = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(
                MEDICAL_CONDITION_LIST_PREFIX,
                pageIndex.ToString(),
                pageSize.ToString(),
                searchTerm ?? "",
                orderBy ?? "",
                medicalRecordId?.ToString() ?? "",
                type?.ToString() ?? "",
                severity ?? ""
            );

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<MedicalConditionResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Medical conditions list found in cache");
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<MedicalCondition>().GetQueryable()
                .Include(mc => mc.MedicalRecord)
                .ThenInclude(mr => mr.Student)
                .Where(mc => !mc.IsDeleted)
                .AsQueryable();

            query = ApplyMedicalConditionFilters(query, searchTerm, medicalRecordId, type, severity);
            query = ApplyMedicalConditionOrdering(query, orderBy);

            var totalCount = await query.CountAsync(cancellationToken);
            var medicalConditions = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = medicalConditions.Select(MapToMedicalConditionResponse).ToList();

            var result = BaseListResponse<MedicalConditionResponse>.SuccessResult(
                responses,
                totalCount,
                pageSize,
                pageIndex,
                "Lấy danh sách tình trạng y tế thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICAL_CONDITION_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medical conditions");
            return BaseListResponse<MedicalConditionResponse>.ErrorResult("Lỗi lấy danh sách tình trạng y tế.");
        }
    }

    public async Task<BaseResponse<MedicalConditionResponse>> GetMedicalConditionByIdAsync(Guid conditionId)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(MEDICAL_CONDITION_CACHE_PREFIX, conditionId.ToString());
            var cachedResponse = await _cacheService.GetAsync<BaseResponse<MedicalConditionResponse>>(cacheKey);

            if (cachedResponse != null)
            {
                _logger.LogDebug("Medical condition found in cache: {ConditionId}", conditionId);
                return cachedResponse;
            }

            var conditionRepo = _unitOfWork.GetRepositoryByEntity<MedicalCondition>();
            var medicalCondition = await conditionRepo.GetQueryable()
                .Include(mc => mc.MedicalRecord)
                .ThenInclude(mr => mr.Student)
                .Where(mc => mc.Id == conditionId && !mc.IsDeleted)
                .FirstOrDefaultAsync();

            if (medicalCondition == null)
            {
                return new BaseResponse<MedicalConditionResponse>
                {
                    Success = false,
                    Message = "Không tìm thấy tình trạng y tế."
                };
            }

            var conditionResponse = MapToMedicalConditionResponse(medicalCondition);

            var response = new BaseResponse<MedicalConditionResponse>
            {
                Success = true,
                Data = conditionResponse,
                Message = "Lấy thông tin tình trạng y tế thành công."
            };

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICAL_CONDITION_CACHE_SET);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting medical condition by ID: {ConditionId}", conditionId);
            return new BaseResponse<MedicalConditionResponse>
            {
                Success = false,
                Message = $"Lỗi lấy thông tin tình trạng y tế: {ex.Message}"
            };
        }
    }

    public async Task<BaseListResponse<MedicalConditionResponse>> GetMedicalConditionsByRecordIdAsync(
        Guid medicalRecordId,
        MedicalConditionType? type = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(
                MEDICAL_CONDITION_LIST_PREFIX,
                "by_record",
                medicalRecordId.ToString(),
                type?.ToString() ?? ""
            );

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<MedicalConditionResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Medical conditions found in cache for record: {RecordId}", medicalRecordId);
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<MedicalCondition>().GetQueryable()
                .Include(mc => mc.MedicalRecord)
                .ThenInclude(mr => mr.Student)
                .Where(mc => mc.MedicalRecordId == medicalRecordId && !mc.IsDeleted)
                .AsQueryable();

            if (type.HasValue)
            {
                query = query.Where(mc => mc.Type == type.Value);
            }

            var medicalConditions = await query
                .OrderByDescending(mc => mc.CreatedDate)
                .ToListAsync(cancellationToken);

            var responses = medicalConditions.Select(MapToMedicalConditionResponse).ToList();

            var result = BaseListResponse<MedicalConditionResponse>.SuccessResult(
                responses,
                responses.Count,
                responses.Count,
                1,
                "Lấy danh sách tình trạng y tế theo hồ sơ thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICAL_CONDITION_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting medical conditions for record: {RecordId}", medicalRecordId);
            return BaseListResponse<MedicalConditionResponse>.ErrorResult(
                "Lỗi lấy danh sách tình trạng y tế theo hồ sơ.");
        }
    }

    public async Task<BaseResponse<MedicalConditionResponse>> CreateMedicalConditionAsync(
        CreateMedicalConditionRequest model)
    {
        try
        {
            var validationResult = await _createMedicalConditionValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BaseResponse<MedicalConditionResponse>.ErrorResult(errors);
            }

            var recordRepo = _unitOfWork.GetRepositoryByEntity<MedicalRecord>();
            var medicalRecord = await recordRepo.GetQueryable()
                .Include(mr => mr.Student)
                .FirstOrDefaultAsync(mr => mr.Id == model.MedicalRecordId && !mr.IsDeleted);

            if (medicalRecord == null)
            {
                return BaseResponse<MedicalConditionResponse>.ErrorResult("Không tìm thấy hồ sơ y tế.");
            }

            var currentUserId = GetCurrentUserId();
            var schoolNurseRoleName = await GetSchoolNurseRoleName();

            var medicalCondition = _mapper.Map<MedicalCondition>(model);
            medicalCondition.Id = Guid.NewGuid();
            medicalCondition.CreatedBy = schoolNurseRoleName;
            medicalCondition.CreatedDate = DateTime.Now;

            var conditionRepo = _unitOfWork.GetRepositoryByEntity<MedicalCondition>();
            await conditionRepo.AddAsync(medicalCondition);

            await CreateMedicalConditionNotificationAsync(
                medicalRecord.Student,
                medicalCondition,
                currentUserId);

            if (medicalCondition.Severity == SeverityType.Severe)
            {
                await CreateSevereConditionAlertForNurseAsync(
                    medicalRecord.Student,
                    medicalCondition);
            }

            await _unitOfWork.SaveChangesAsync();
            await InvalidateAllCachesAsync();

            medicalCondition = await conditionRepo.GetQueryable()
                .Include(mc => mc.MedicalRecord)
                .ThenInclude(mr => mr.Student)
                .FirstOrDefaultAsync(mc => mc.Id == medicalCondition.Id);

            var conditionResponse = MapToMedicalConditionResponse(medicalCondition);

            _logger.LogInformation(
                "Created medical condition {ConditionId} for student {StudentId}, severity: {Severity}",
                medicalCondition.Id, medicalRecord.Student.Id, medicalCondition.Severity);

            return BaseResponse<MedicalConditionResponse>.SuccessResult(
                conditionResponse, "Thêm tình trạng y tế thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating medical condition");
            return BaseResponse<MedicalConditionResponse>.ErrorResult($"Lỗi thêm tình trạng y tế: {ex.Message}");
        }
    }

    public async Task<BaseResponse<MedicalConditionResponse>> UpdateMedicalConditionAsync(
        Guid conditionId, UpdateMedicalConditionRequest model)
    {
        try
        {
            var validationResult = await _updateMedicalConditionValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BaseResponse<MedicalConditionResponse>.ErrorResult(errors);
            }

            var conditionRepo = _unitOfWork.GetRepositoryByEntity<MedicalCondition>();
            var medicalCondition = await conditionRepo.GetQueryable()
                .Include(mc => mc.MedicalRecord)
                .ThenInclude(mr => mr.Student)
                .FirstOrDefaultAsync(mc => mc.Id == conditionId && !mc.IsDeleted);

            if (medicalCondition == null)
            {
                return BaseResponse<MedicalConditionResponse>.ErrorResult("Không tìm thấy tình trạng y tế.");
            }

            var currentUserId = GetCurrentUserId();
            var schoolNurseRoleName = await GetSchoolNurseRoleName();

            // Lưu severity cũ để so sánh
            var oldSeverity = medicalCondition.Severity;

            _mapper.Map(model, medicalCondition);
            medicalCondition.LastUpdatedBy = schoolNurseRoleName;
            medicalCondition.LastUpdatedDate = DateTime.Now;

            if (oldSeverity != SeverityType.Severe && medicalCondition.Severity == SeverityType.Severe)
            {
                await CreateMedicalConditionNotificationAsync(
                    medicalCondition.MedicalRecord.Student,
                    medicalCondition,
                    currentUserId,
                    isUpdate: true);

                await CreateSevereConditionAlertForNurseAsync(
                    medicalCondition.MedicalRecord.Student,
                    medicalCondition);
            }

            await _unitOfWork.SaveChangesAsync();
            await InvalidateAllCachesAsync();

            var conditionResponse = MapToMedicalConditionResponse(medicalCondition);

            _logger.LogInformation(
                "Updated medical condition {ConditionId}, severity changed from {OldSeverity} to {NewSeverity}",
                conditionId, oldSeverity, medicalCondition.Severity);

            return BaseResponse<MedicalConditionResponse>.SuccessResult(
                conditionResponse, "Cập nhật tình trạng y tế thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating medical condition: {ConditionId}", conditionId);
            return BaseResponse<MedicalConditionResponse>.ErrorResult($"Lỗi cập nhật tình trạng y tế: {ex.Message}");
        }
    }

    public async Task<BaseResponse<bool>> DeleteMedicalConditionAsync(Guid conditionId)
    {
        try
        {
            var conditionRepo = _unitOfWork.GetRepositoryByEntity<MedicalCondition>();
            var medicalCondition = await conditionRepo.GetQueryable()
                .FirstOrDefaultAsync(mc => mc.Id == conditionId && !mc.IsDeleted);

            if (medicalCondition == null)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "Không tìm thấy tình trạng y tế."
                };
            }

            var schoolNurseRoleName = await GetSchoolNurseRoleName();

            medicalCondition.IsDeleted = true;
            medicalCondition.LastUpdatedBy = schoolNurseRoleName;
            medicalCondition.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            await InvalidateAllCachesAsync();

            return new BaseResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Xóa tình trạng y tế thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting medical condition: {ConditionId}", conditionId);
            return new BaseResponse<bool>
            {
                Success = false,
                Data = false,
                Message = $"Lỗi xóa tình trạng y tế: {ex.Message}"
            };
        }
    }

    #endregion

    #region Helper Methods

    private Guid GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out Guid userId))
        {
            return userId;
        }

        return Guid.Empty;
    }

    private string GetSeverityDisplayName(SeverityType? severityType)
    {
        if (!severityType.HasValue)
            return "";

        return severityType.Value switch
        {
            SeverityType.Mild => "Nhẹ",
            SeverityType.Moderate => "Trung bình",
            SeverityType.Severe => "Nghiêm trọng",
            _ => severityType.ToString()
        };
    }

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

    private MedicalConditionResponse MapToMedicalConditionResponse(MedicalCondition medicalCondition)
    {
        var response = _mapper.Map<MedicalConditionResponse>(medicalCondition);

        if (medicalCondition.MedicalRecord?.Student != null)
        {
            response.StudentName = medicalCondition.MedicalRecord.Student.FullName;
            response.StudentCode = medicalCondition.MedicalRecord.Student.StudentCode;
        }

        return response;
    }

    private IQueryable<MedicalCondition> ApplyMedicalConditionFilters(
        IQueryable<MedicalCondition> query,
        string searchTerm,
        Guid? medicalRecordId,
        MedicalConditionType? type,
        string severity)
    {
        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(mc =>
                mc.Name.ToLower().Contains(searchTerm) ||
                (mc.Treatment != null && mc.Treatment.ToLower().Contains(searchTerm)) ||
                (mc.Medication != null && mc.Medication.ToLower().Contains(searchTerm)) ||
                mc.MedicalRecord.Student.FullName.ToLower().Contains(searchTerm) ||
                mc.MedicalRecord.Student.StudentCode.ToLower().Contains(searchTerm));
        }

        if (medicalRecordId.HasValue)
        {
            query = query.Where(mc => mc.MedicalRecordId == medicalRecordId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(mc => mc.Type == type.Value);
        }

        if (!string.IsNullOrEmpty(severity))
        {
            if (Enum.TryParse<SeverityType>(severity, out var severityEnum))
            {
                query = query.Where(mc => mc.Severity == severityEnum);
            }
        }

        return query;
    }

    private IQueryable<MedicalCondition> ApplyMedicalConditionOrdering(IQueryable<MedicalCondition> query,
        string orderBy)
    {
        return orderBy?.ToLower() switch
        {
            "name" => query.OrderBy(mc => mc.Name),
            "name_desc" => query.OrderByDescending(mc => mc.Name),
            "type" => query.OrderBy(mc => mc.Type),
            "type_desc" => query.OrderByDescending(mc => mc.Type),
            "severity" => query.OrderBy(mc => mc.Severity),
            "severity_desc" => query.OrderByDescending(mc => mc.Severity),
            "studentname" => query.OrderBy(mc => mc.MedicalRecord.Student.FullName),
            "studentname_desc" => query.OrderByDescending(mc => mc.MedicalRecord.Student.FullName),
            "createdate" => query.OrderBy(mc => mc.CreatedDate),
            "createdate_desc" => query.OrderByDescending(mc => mc.CreatedDate),
            _ => query.OrderByDescending(mc => mc.CreatedDate)
        };
    }

    private async Task InvalidateAllCachesAsync()
    {
        try
        {
            _logger.LogDebug("Starting comprehensive cache invalidation for medical conditions");

            await Task.WhenAll(
                _cacheService.RemoveByPrefixAsync(MEDICAL_CONDITION_CACHE_PREFIX),
                _cacheService.RemoveByPrefixAsync(MEDICAL_CONDITION_LIST_PREFIX),
                _cacheService.RemoveByPrefixAsync(MEDICAL_RECORD_CACHE_PREFIX),
                _cacheService.RemoveByPrefixAsync(MEDICAL_RECORD_LIST_PREFIX),
                _cacheService.RemoveByPrefixAsync(STUDENT_CACHE_PREFIX),
                _cacheService.RemoveByPrefixAsync(STUDENT_LIST_PREFIX),
                _cacheService.RemoveByPrefixAsync(PARENT_CACHE_PREFIX),
                _cacheService.RemoveByPrefixAsync(PARENT_LIST_PREFIX),
                _cacheService.RemoveByPrefixAsync(STATISTICS_PREFIX)
            );

            await Task.Delay(100);

            _logger.LogDebug("Completed comprehensive cache invalidation for medical conditions");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in comprehensive cache invalidation for medical conditions");
        }
    }

    #endregion

    #region Auto Notification Methods

    private async Task CreateMedicalConditionNotificationAsync(
        ApplicationUser student,
        MedicalCondition condition,
        Guid senderId,
        bool isUpdate = false)
    {
        try
        {
            if (!student.ParentId.HasValue)
            {
                _logger.LogWarning("Student {StudentId} has no parent, skipping notification", student.Id);
                return;
            }

            var senderExists = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>()
                .GetQueryable()
                .AnyAsync(u => u.Id == senderId && !u.IsDeleted);

            if (!senderExists && senderId != Guid.Empty)
            {
                _logger.LogWarning("Sender {SenderId} not found, setting to null", senderId);
                senderId = Guid.Empty;
            }

            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();
            var severityText = GetSeverityDisplayName(condition.Severity);

            var title = isUpdate
                ? $"Cập nhật tình trạng y tế - {student.FullName}"
                : $"Thông báo tình trạng y tế - {student.FullName}";

            var content = $"Con em Quý phụ huynh ({student.FullName} - {student.StudentCode}) " +
                          $"đã được {(isUpdate ? "cập nhật" : "thêm")} tình trạng y tế: {condition.Name}";

            if (!string.IsNullOrEmpty(severityText))
            {
                content += $" với mức độ: {severityText}";
            }

            content += ". Vui lòng liên hệ với nhà trường để biết thêm chi tiết.";

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = title,
                Content = content,
                NotificationType = NotificationType.General,
                SenderId = senderExists ? senderId : null,
                RecipientId = student.ParentId.Value,
                RequiresConfirmation = condition.Severity == SeverityType.Severe,
                IsRead = false,
                IsConfirmed = false,
                ConfirmationNotes = "",
                IsDismissed = false,
                CreatedDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(30)
            };

            await notificationRepo.AddAsync(notification);

            _logger.LogInformation(
                "Created medical condition notification {NotificationId} for parent {ParentId}, student {StudentId}",
                notification.Id, student.ParentId.Value, student.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating medical condition notification for student {StudentId}", student.Id);
        }
    }

    private async Task CreateSevereConditionAlertForNurseAsync(
        ApplicationUser student,
        MedicalCondition condition)
    {
        try
        {
            var nurseRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var nurses = await nurseRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "SCHOOLNURSE") &&
                            !u.IsDeleted && u.IsActive)
                .ToListAsync();

            if (!nurses.Any())
            {
                _logger.LogWarning("No active school nurses found for severe condition alert");
                return;
            }

            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();
            var notifications = new List<Notification>();

            foreach (var nurse in nurses)
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = "⚠️ CẢNH BÁO: Tình trạng y tế nghiêm trọng",
                    Content =
                        $"Học sinh {student.FullName} ({student.StudentCode}) có tình trạng y tế nghiêm trọng: {condition.Name}. " +
                        $"Điều trị: {condition.Treatment ?? "Chưa có"}. " +
                        $"Thuốc: {condition.Medication ?? "Chưa có"}. " +
                        "Vui lòng chú ý đặc biệt đến học sinh này.",
                    NotificationType = NotificationType.General,
                    SenderId = null,
                    RecipientId = nurse.Id,
                    RequiresConfirmation = false,
                    IsRead = false,
                    IsConfirmed = false,
                    IsDismissed = false,
                    ConfirmationNotes = "",
                    CreatedDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(60)
                };

                notifications.Add(notification);
            }

            await notificationRepo.AddRangeAsync(notifications);

            _logger.LogInformation("Created severe condition alerts for {Count} nurses, student {StudentId}",
                nurses.Count, student.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating severe condition alerts for nurses, student {StudentId}", student.Id);
        }
    }

    #endregion
}