using System.Security.Claims;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Helpers;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthEventRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthEventResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services;

public class HealthEventService : IHealthEventService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<HealthEventService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IValidator<CreateHealthEventRequest> _createHealthEventValidator;
    private readonly IValidator<UpdateHealthEventRequest> _updateHealthEventValidator;
    private readonly IValidator<CreateHealthEventWithMedicalItemsRequest> _createHealthEventMedicalItem;

    private const string HEALTH_EVENT_CACHE_PREFIX = "health_event";
    private const string HEALTH_EVENT_LIST_PREFIX = "health_events_list";
    private const string HEALTH_EVENT_CACHE_SET = "health_event_cache_keys";
    private const string STATISTICS_PREFIX = "statistics";

    public HealthEventService(
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<HealthEventService> logger,
        IHttpContextAccessor httpContextAccessor,
        IValidator<CreateHealthEventRequest> createHealthEventValidator,
        IValidator<UpdateHealthEventRequest> updateHealthEventValidator,
        IValidator<CreateHealthEventWithMedicalItemsRequest> createHealthEventMedicalItem)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _createHealthEventValidator = createHealthEventValidator;
        _updateHealthEventValidator = updateHealthEventValidator;
        _createHealthEventMedicalItem = createHealthEventMedicalItem;
    }

    #region Basic CRUD Operations

    public async Task<BaseListResponse<HealthEventResponse>> GetHealthEventsAsync(
        int pageIndex,
        int pageSize,
        string searchTerm,
        string orderBy,
        Guid? studentId = null,
        HealthEventType? eventType = null,
        bool? isEmergency = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? location = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(
                HEALTH_EVENT_LIST_PREFIX,
                pageIndex.ToString(),
                pageSize.ToString(),
                searchTerm ?? "",
                orderBy ?? "",
                studentId?.ToString() ?? "",
                eventType?.ToString() ?? "",
                isEmergency?.ToString() ?? "",
                fromDate?.ToString("yyyy-MM-dd") ?? "",
                toDate?.ToString("yyyy-MM-dd") ?? "",
                location ?? ""
            );

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<HealthEventResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Health events list found in cache with key: {CacheKey}", cacheKey);
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<HealthEvent>().GetQueryable()
                .Include(he => he.Student)
                .Include(he => he.HandledBy)
                .Include(he => he.RelatedMedicalCondition)
                .Where(he => !he.IsDeleted)
                .AsQueryable();

            query = ApplyHealthEventFilters(query, searchTerm, studentId, eventType, isEmergency, fromDate, toDate,
                location);
            query = ApplyHealthEventOrdering(query, orderBy);

            var totalCount = await query.CountAsync(cancellationToken);
            var healthEvents = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = healthEvents.Select(MapToHealthEventResponse).ToList();

            var result = BaseListResponse<HealthEventResponse>.SuccessResult(
                responses,
                totalCount,
                pageSize,
                pageIndex,
                "Lấy danh sách sự kiện y tế thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            await _cacheService.AddToTrackingSetAsync(cacheKey, HEALTH_EVENT_CACHE_SET);
            _logger.LogDebug("Cached health events list with key: {CacheKey}", cacheKey);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health events");
            return BaseListResponse<HealthEventResponse>.ErrorResult("Lỗi lấy danh sách sự kiện y tế.");
        }
    }

    public async Task<BaseResponse<HealthEventResponse>> GetHealthEventByIdAsync(Guid eventId)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(HEALTH_EVENT_CACHE_PREFIX, eventId.ToString());
            var cachedResponse = await _cacheService.GetAsync<BaseResponse<HealthEventResponse>>(cacheKey);

            if (cachedResponse != null)
            {
                _logger.LogDebug("Health event found in cache: {EventId}, cacheKey: {CacheKey}", eventId, cacheKey);
                return cachedResponse;
            }

            var eventRepo = _unitOfWork.GetRepositoryByEntity<HealthEvent>();
            var healthEvent = await eventRepo.GetQueryable()
                .Include(he => he.Student)
                        .ThenInclude(s => s.StudentClasses)
                          .ThenInclude(sc => sc.SchoolClass)

                .Include(he => he.HandledBy)
                .Include(he => he.RelatedMedicalCondition)
                .Include(he => he.HealthEventMedicalItems)
                .Include(he => he.MedicalItemsUsed)
                .ThenInclude(miu => miu.HealthEventMedicalItem)
                .Where(he => he.Id == eventId && !he.IsDeleted)
                .FirstOrDefaultAsync();

            if (healthEvent == null)
            {
                return BaseResponse<HealthEventResponse>.ErrorResult("Không tìm thấy sự kiện y tế.");
            }

            var eventResponse = MapToHealthEventResponse(healthEvent);

            var response = BaseResponse<HealthEventResponse>.SuccessResult(
                eventResponse, "Lấy thông tin sự kiện y tế thành công.");

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15));
            await _cacheService.AddToTrackingSetAsync(cacheKey, HEALTH_EVENT_CACHE_SET);
            _logger.LogDebug("Cached health event with key: {CacheKey}", cacheKey);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting health event by ID: {EventId}", eventId);
            return BaseResponse<HealthEventResponse>.ErrorResult($"Lỗi lấy thông tin sự kiện y tế: {ex.Message}");
        }
    }

    public async Task<BaseResponse<HealthEventResponse>> CreateHealthEventAsync(CreateHealthEventRequest model)
    {
        try
        {
            var validationResult = await _createHealthEventValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BaseResponse<HealthEventResponse>.ErrorResult(errors);
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                _logger.LogError("Unable to get current user ID from claims");
                return BaseResponse<HealthEventResponse>.ErrorResult("Không thể xác định người dùng hiện tại.");
            }

            var currentUser = await ValidateCurrentUserPermissions(currentUserId);
            if (currentUser == null)
            {
                return BaseResponse<HealthEventResponse>.ErrorResult("Bạn không có quyền tạo sự kiện y tế.");
            }

            var studentRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var student = await studentRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == model.UserId && !u.IsDeleted);

            if (student == null)
            {
                _logger.LogError("Student not found with ID: {StudentId}", model.UserId);
                return BaseResponse<HealthEventResponse>.ErrorResult("Không tìm thấy học sinh.");
            }

            var isStudent = student.UserRoles.Any(ur => ur.Role.Name.ToUpper() == "STUDENT");
            if (!isStudent)
            {
                _logger.LogError("User {UserId} is not a student", model.UserId);
                return BaseResponse<HealthEventResponse>.ErrorResult("User được chọn không phải là học sinh.");
            }

            if (model.RelatedMedicalConditionId.HasValue)
            {
                var (isValid, errorMessage) = await ValidateHealthEventMedicalConditionCompatibility(
                    model.EventType, model.RelatedMedicalConditionId.Value);

                if (!isValid)
                {
                    return BaseResponse<HealthEventResponse>.ErrorResult(errorMessage);
                }

                var isValidCondition = await ValidateMedicalConditionOwnership(
                    model.RelatedMedicalConditionId.Value, model.UserId);

                if (!isValidCondition)
                {
                    _logger.LogError("Medical condition {ConditionId} not found for student {StudentId}",
                        model.RelatedMedicalConditionId.Value, model.UserId);
                    return BaseResponse<HealthEventResponse>.ErrorResult(
                        "Không tìm thấy tình trạng y tế liên quan thuộc về học sinh này.");
                }
            }

            var schoolNurseRoleName = await GetSchoolNurseRoleName();

            var healthEvent = _mapper.Map<HealthEvent>(model);
            healthEvent.Id = Guid.NewGuid();
            healthEvent.CreatedBy = schoolNurseRoleName;
            healthEvent.CreatedDate = DateTime.Now;
            healthEvent.Code = await GenerateHealthEventCodeAsync();

            if (model.IsEmergency)
            {
                healthEvent.HandledById = currentUserId;
                healthEvent.Status = HealthEventStatus.InProgress;
                healthEvent.AssignmentMethod = AssignmentMethod.SelfAssigned;
                healthEvent.AssignedAt = DateTime.Now;

                _logger.LogInformation("Emergency event {Code} auto-assigned to current nurse {UserId}",
                    healthEvent.Code, currentUserId);
            }
            else
            {
                healthEvent.HandledById = null;
                healthEvent.Status = HealthEventStatus.Pending;
                healthEvent.AssignmentMethod = AssignmentMethod.Unassigned;
                healthEvent.AssignedAt = null;

                _logger.LogInformation("Normal event {Code} created as unassigned for background processing",
                    healthEvent.Code);
            }

            var eventRepo = _unitOfWork.GetRepositoryByEntity<HealthEvent>();
            await eventRepo.AddAsync(healthEvent);

            await _unitOfWork.SaveChangesAsync();

            if (healthEvent.IsEmergency)
            {
                await CreateEmergencyNotificationAsync(student, healthEvent);
                await NotifyAvailableNursesAsync(healthEvent);
            }
            else
            {
                await CreateHealthEventNotificationAsync(student, healthEvent);
            }

            await InvalidateAllCachesAsync();

            healthEvent = await eventRepo.GetQueryable()
                .Include(he => he.Student)
                .Include(he => he.HandledBy)
                .Include(he => he.RelatedMedicalCondition)
                .FirstOrDefaultAsync(he => he.Id == healthEvent.Id);

            var eventResponse = MapToHealthEventResponse(healthEvent);

            _logger.LogInformation(
                "Created health event {EventId} for student {StudentId}, emergency: {IsEmergency}, status: {Status}",
                healthEvent.Id, model.UserId, healthEvent.IsEmergency, healthEvent.Status);

            return BaseResponse<HealthEventResponse>.SuccessResult(
                eventResponse, "Ghi nhận sự kiện y tế thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating health event for student {StudentId}", model.UserId);
            return BaseResponse<HealthEventResponse>.ErrorResult($"Lỗi ghi nhận sự kiện y tế: {ex.Message}");
        }
    }

    public async Task<BaseResponse<HealthEventResponse>> CreateHealthEventWithMedicalItemsAsync(
    CreateHealthEventWithMedicalItemsRequest model)
    {
        try
        {
            var validationResult = await _createHealthEventMedicalItem.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BaseResponse<HealthEventResponse>.ErrorResult(errors);
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                _logger.LogError("Không thể lấy ID người dùng hiện tại từ claims");
                return BaseResponse<HealthEventResponse>.ErrorResult("Không thể xác định người dùng hiện tại.");
            }

            var currentUser = await ValidateCurrentUserPermissions(currentUserId);
            if (currentUser == null)
            {
                return BaseResponse<HealthEventResponse>.ErrorResult("Bạn không có quyền tạo sự kiện y tế.");
            }

            _logger.LogInformation("Người dùng hiện tại: ID={Id}, FullName={FullName}", currentUser?.Id, currentUser?.FullName);

            var studentRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var student = await studentRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.StudentClasses)
                .ThenInclude(sc => sc.SchoolClass)
                .FirstOrDefaultAsync(u => u.Id == model.UserId && !u.IsDeleted);

            if (student == null)
            {
                _logger.LogError("Không tìm thấy học sinh với ID: {StudentId}", model.UserId);
                return BaseResponse<HealthEventResponse>.ErrorResult("Không tìm thấy học sinh.");
            }

            _logger.LogInformation("Đã lấy thông tin học sinh: ID={Id}, FullName={FullName}", student.Id, student.FullName);

            var isStudent = student.UserRoles.Any(ur => ur.Role.Name.ToUpper() == "STUDENT");
            if (!isStudent)
            {
                _logger.LogError("Người dùng {UserId} không phải là học sinh", model.UserId);
                return BaseResponse<HealthEventResponse>.ErrorResult("Người dùng được chọn không phải là học sinh.");
            }

            if (model.RelatedMedicalConditionId.HasValue)
            {
                var (isValid, errorMessage) = await ValidateHealthEventMedicalConditionCompatibility(
                    model.EventType, model.RelatedMedicalConditionId.Value);

                if (!isValid)
                {
                    return BaseResponse<HealthEventResponse>.ErrorResult(errorMessage);
                }

                var isValidCondition = await ValidateMedicalConditionOwnership(
                    model.RelatedMedicalConditionId.Value, model.UserId);

                if (!isValidCondition)
                {
                    _logger.LogError("Tình trạng y tế {ConditionId} không tìm thấy cho học sinh {StudentId}",
                        model.RelatedMedicalConditionId.Value, model.UserId);
                    return BaseResponse<HealthEventResponse>.ErrorResult(
                        "Không tìm thấy tình trạng y tế liên quan thuộc về học sinh này.");
                }
            }

            var medicalItemRepo = _unitOfWork.GetRepositoryByEntity<MedicalItem>();
            var medicalItemIds = model.MedicalItemUsages.Select(m => m.MedicalItemId).Distinct().ToList();
            var medicalItems = await medicalItemRepo.GetQueryable()
                .Where(mi => medicalItemIds.Contains(mi.Id) && !mi.IsDeleted)
                .ToListAsync();

            if (medicalItems.Count != medicalItemIds.Count)
            {
                return BaseResponse<HealthEventResponse>.ErrorResult("Một hoặc nhiều thuốc/vật tư không tồn tại.");
            }

            // Lấy MedicalRecord của học sinh
            var medicalRecordRepo = _unitOfWork.GetRepositoryByEntity<MedicalRecord>();
            var medicalRecord = await medicalRecordRepo.GetQueryable()
                .FirstOrDefaultAsync(mr => mr.UserId == model.UserId && !mr.IsDeleted);

            if (medicalRecord == null)
            {
                _logger.LogError("Không tìm thấy hồ sơ y tế cho học sinh ID: {StudentId}", model.UserId);
                return BaseResponse<HealthEventResponse>.ErrorResult("Không tìm thấy hồ sơ y tế của học sinh.");
            }

            var schoolNurseRoleName = await GetSchoolNurseRoleName();
            var healthEvent = _mapper.Map<HealthEvent>(model);
            healthEvent.Id = Guid.NewGuid();
            healthEvent.CreatedBy = schoolNurseRoleName;
            healthEvent.CreatedDate = DateTime.Now;
            healthEvent.Code = await GenerateHealthEventCodeAsync();
            healthEvent.HandledById = currentUserId;

            if (model.IsEmergency)
            {
                _logger.LogInformation("Sự kiện khẩn cấp {Code} được gán tự động cho y tá hiện tại {UserId}",
                    healthEvent.Code, currentUserId);
            }
            else
            {
                _logger.LogInformation("Sự kiện thông thường {Code} được tạo và gán cho y tá hiện tại {UserId}",
                    healthEvent.Code, currentUserId);
            }

            var eventRepo = _unitOfWork.GetRepositoryByEntity<HealthEvent>();
            await eventRepo.AddAsync(healthEvent);

            // Xử lý trường hợp khẩn cấp: Tạo MedicalCondition và liên kết với MedicalRecord
            if (healthEvent.IsEmergency)
            {
                var medicalConditionRepo = _unitOfWork.GetRepositoryByEntity<MedicalCondition>();
                var medicalCondition = new MedicalCondition
                {
                    Id = Guid.NewGuid(),
                    MedicalRecordId = medicalRecord.Id,
                    Type = MapHealthEventTypeToMedicalConditionType(healthEvent.EventType),
                    Name = $"Sự kiện khẩn cấp: {healthEvent.EventType}",
                    Severity = SeverityType.Severe,                   
                    DiagnosisDate = healthEvent.OccurredAt,
                    Treatment = healthEvent.ActionTaken ?? "Chưa xác định phương pháp điều trị",
                    Medication = string.Join(", ", model.MedicalItemUsages.Select(m => m.Notes ?? "Không xác định")),
                    Hospital = "Trường học",
                    Doctor = currentUser?.FullName ?? "Y tá trường học",
                    Notes = healthEvent.Outcome ?? "Không có ghi chú bổ sung",
                    CreatedBy = schoolNurseRoleName,
                    CreatedDate = DateTime.Now,
                    IsDeleted = false
                };
                await medicalConditionRepo.AddAsync(medicalCondition);
                healthEvent.RelatedMedicalConditionId = medicalCondition.Id;
                _logger.LogInformation("Đã tạo MedicalCondition {ConditionId} cho sự kiện khẩn cấp {EventId} trong MedicalRecord {RecordId}",
                    medicalCondition.Id, healthEvent.Id, medicalRecord.Id);
            }

            var medicalItemUsageRepo = _unitOfWork.GetRepositoryByEntity<MedicalItemUsage>();
            var healthEventMedicalItemRepo = _unitOfWork.GetRepositoryByEntity<HealthEventMedicalItem>();
            var medicalItemUsages = new List<MedicalItemUsage>();
            var healthEventMedicalItems = new List<HealthEventMedicalItem>();

            foreach (var usageRequest in model.MedicalItemUsages)
            {
                var medicalItem = medicalItems.FirstOrDefault(mi => mi.Id == usageRequest.MedicalItemId);
                if (medicalItem == null)
                {
                    _logger.LogError("Không tìm thấy MedicalItem với ID: {MedicalItemId}", usageRequest.MedicalItemId);
                    continue;
                }

                _logger.LogInformation("Tìm thấy MedicalItem: ID={Id}, Name={Name}, IsNull={IsNull}",
                    medicalItem.Id, medicalItem.Name, medicalItem == null);

                var medicalItemUsage = _mapper.Map<MedicalItemUsage>(usageRequest);
                medicalItemUsage.Id = Guid.NewGuid();
                medicalItemUsage.HealthEventId = healthEvent.Id;
                medicalItemUsage.UsedById = currentUserId;
                medicalItemUsage.CreatedBy = schoolNurseRoleName;
                medicalItemUsage.CreatedDate = DateTime.Now;
                medicalItemUsages.Add(medicalItemUsage);

                var healthEventMedicalItem = new HealthEventMedicalItem
                {
                    Id = Guid.NewGuid(),
                    HealthEventId = healthEvent.Id,
                    MedicalItemUsageId = medicalItemUsage.Id,
                    StudentName = student?.FullName ?? $"Học sinh không xác định (ID: {model.UserId})",
                    StudentClass = student?.StudentClasses.FirstOrDefault()?.SchoolClass.Name ?? "Lớp không xác định",
                    NurseName = currentUser?.FullName ?? $"Y tá không xác định (ID: {currentUserId})",
                    MedicationName = medicalItem.Name ?? "Thuốc không xác định",
                    MedicationQuantity = usageRequest.Quantity,
                    MedicationDosage = usageRequest.Notes ?? "Không có liều lượng cụ thể",
                    SupplyQuantity = null,
                    Dose = usageRequest.Dose,
                    MedicalPerOnce = usageRequest.MedicalPerOnce,
                    CreatedBy = schoolNurseRoleName,
                    CreatedDate = DateTime.Now,
                    IsDeleted = false
                };
                medicalItemUsage.HealthEventMedicalItem = healthEventMedicalItem;
                healthEventMedicalItems.Add(healthEventMedicalItem);
            }

            await medicalItemUsageRepo.AddRangeAsync(medicalItemUsages);
            await healthEventMedicalItemRepo.AddRangeAsync(healthEventMedicalItems);

            foreach (var medicalItemId in medicalItemIds)
            {
                var currentMedicalItem = await medicalItemRepo.GetById(medicalItemId);
                if (currentMedicalItem != null)
                {
                    var newUsageQuantity = model.MedicalItemUsages
                        .Where(miu => miu.MedicalItemId == medicalItemId)
                        .Sum(miu => miu.Quantity);
                    currentMedicalItem.Quantity -= (int)newUsageQuantity;
                    if (currentMedicalItem.Quantity < 0)
                    {
                        _logger.LogWarning("Số lượng MedicalItem {MedicalItemId} âm sau khi sử dụng: {NewQuantity}", medicalItemId, currentMedicalItem.Quantity);
                        currentMedicalItem.Quantity = 0;
                    }
                    var medicalItemCacheKey = _cacheService.GenerateCacheKey("medical_item", medicalItemId.ToString());
                    await _cacheService.RemoveAsync(medicalItemCacheKey);
                    _logger.LogDebug("Đã xóa cache cụ thể cho medical item: {CacheKey}", medicalItemCacheKey);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            var eventCacheKey = _cacheService.GenerateCacheKey(HEALTH_EVENT_CACHE_PREFIX, healthEvent.Id.ToString());
            await _cacheService.RemoveAsync(eventCacheKey);
            _logger.LogDebug("Đã xóa cache cụ thể cho health event detail: {CacheKey}", eventCacheKey);
            await _cacheService.RemoveByPrefixAsync(HEALTH_EVENT_LIST_PREFIX);
            _logger.LogDebug("Đã xóa cache danh sách health events với prefix: {Prefix}", HEALTH_EVENT_LIST_PREFIX);
            await _cacheService.RemoveByPrefixAsync("medical_item_usage");
            _logger.LogDebug("Đã xóa cache medical item usage với prefix: {Prefix}", "medical_item_usage");

            if (healthEvent.IsEmergency)
            {
                await CreateEmergencyNotificationAsync(student, healthEvent);
                await NotifyAvailableNursesAsync(healthEvent);
            }
            else
            {
                await CreateHealthEventNotificationAsync(student, healthEvent);
            }

            await InvalidateAllCachesAsync();

            healthEvent = await eventRepo.GetQueryable()
                .Include(he => he.Student)
                .Include(he => he.HandledBy)
                .Include(he => he.RelatedMedicalCondition)
                .Include(he => he.MedicalItemsUsed)
                .ThenInclude(miu => miu.HealthEventMedicalItem)
                .Include(he => he.HealthEventMedicalItems)
                .FirstOrDefaultAsync(he => he.Id == healthEvent.Id);

            _logger.LogInformation("MedicalItemsUsed count after fetch: {Count}", healthEvent.MedicalItemsUsed?.Count ?? 0);
            if (healthEvent.MedicalItemsUsed != null)
            {
                foreach (var usage in healthEvent.MedicalItemsUsed)
                {
                    var medicalItem = usage.HealthEventMedicalItem;
                    _logger.LogInformation("Fetched MedicalItem: Id={Id}, MedicationName={Name}, Quantity={Qty}, Dosage={Dosage}",
                        usage.Id, medicalItem?.MedicationName, medicalItem?.MedicationQuantity, medicalItem?.MedicationDosage);
                }
            }
            else
            {
                _logger.LogInformation("MedicalItemsUsed is null for HealthEvent {Id}", healthEvent.Id);
            }

            var eventResponse = MapToHealthEventResponse(healthEvent);

            _logger.LogInformation(
                "Đã tạo sự kiện y tế {EventId} cho học sinh {StudentId}, khẩn cấp: {IsEmergency}, với {UsageCount} lần sử dụng vật tư/thuốc",
                healthEvent.Id, model.UserId, healthEvent.IsEmergency, medicalItemUsages.Count);

            return BaseResponse<HealthEventResponse>.SuccessResult(
                eventResponse, "Ghi nhận sự kiện y tế và sử dụng thuốc/vật tư thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo sự kiện y tế với vật tư/thuốc cho học sinh {StudentId}", model.UserId);
            return BaseResponse<HealthEventResponse>.ErrorResult($"Lỗi ghi nhận sự kiện y tế: {ex.Message}");
        }
    }
    private bool IsMedication(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName)) return false;
        var medicationKeywords = new[] { "thuoc", "medication", "pill", "tablet" }; // Tùy chỉnh danh sách
        return medicationKeywords.Any(keyword => itemName.ToLower().Contains(keyword));
    }

    public async Task<BaseResponse<HealthEventResponse>> UpdateHealthEventAsync(Guid eventId,
        UpdateHealthEventRequest model)
    {
        try
        {
            var validationResult = await _updateHealthEventValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BaseResponse<HealthEventResponse>.ErrorResult(errors);
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                _logger.LogError("Unable to get current user ID from claims for update operation");
                return BaseResponse<HealthEventResponse>.ErrorResult("Không thể xác định người dùng hiện tại.");
            }

            var currentUser = await ValidateCurrentUserPermissions(currentUserId);
            if (currentUser == null)
            {
                return BaseResponse<HealthEventResponse>.ErrorResult("Bạn không có quyền cập nhật sự kiện y tế.");
            }

            var eventRepo = _unitOfWork.GetRepositoryByEntity<HealthEvent>();
            var healthEvent = await eventRepo.GetQueryable()
                .Include(he => he.Student)
                .Include(he => he.HandledBy)
                .Include(he => he.RelatedMedicalCondition)
                .FirstOrDefaultAsync(he => he.Id == eventId && !he.IsDeleted);

            if (healthEvent == null)
            {
                return BaseResponse<HealthEventResponse>.ErrorResult("Không tìm thấy sự kiện y tế.");
            }

            if (model.RelatedMedicalConditionId.HasValue)
            {
                var (isCompatible, compatibilityError) = await ValidateHealthEventMedicalConditionCompatibility(
                    model.EventType, model.RelatedMedicalConditionId.Value);

                if (!isCompatible)
                {
                    return BaseResponse<HealthEventResponse>.ErrorResult(compatibilityError);
                }

                var isValidCondition = await ValidateMedicalConditionOwnership(
                    model.RelatedMedicalConditionId.Value, healthEvent.UserId);

                if (!isValidCondition)
                {
                    _logger.LogError("Medical condition {ConditionId} not found for student {StudentId}",
                        model.RelatedMedicalConditionId.Value, healthEvent.UserId);
                    return BaseResponse<HealthEventResponse>.ErrorResult(
                        "Không tìm thấy tình trạng y tế liên quan thuộc về học sinh này.");
                }
            }
            else
            {
                var requiresMedicalCondition = model.EventType == HealthEventType.AllergicReaction ||
                                               model.EventType == HealthEventType.ChronicIllnessEpisode;

                if (requiresMedicalCondition)
                {
                    var eventTypeDisplay = GetEventTypeDisplayName(model.EventType);
                    return BaseResponse<HealthEventResponse>.ErrorResult(
                        $"Sự kiện '{eventTypeDisplay}' bắt buộc phải liên kết với tình trạng y tế phù hợp.");
                }
            }

            var schoolNurseRoleName = await GetSchoolNurseRoleName();
            var wasEmergency = healthEvent.IsEmergency;
            var oldEventType = healthEvent.EventType;

            var oldHandledById = healthEvent.HandledById;
            var oldStatus = healthEvent.Status;

            _mapper.Map(model, healthEvent);
            healthEvent.LastUpdatedBy = schoolNurseRoleName;
            healthEvent.LastUpdatedDate = DateTime.Now;

            if (model.IsEmergency && !wasEmergency)
            {
                if (!healthEvent.HandledById.HasValue)
                {
                    healthEvent.HandledById = currentUserId;
                    healthEvent.Status = HealthEventStatus.InProgress;
                    healthEvent.AssignmentMethod = AssignmentMethod.SelfAssigned;
                    healthEvent.AssignedAt = DateTime.Now;

                    _logger.LogInformation("Event {EventId} became emergency, auto-assigned to user {UserId}",
                        eventId, currentUserId);
                }
            }
            else if (!model.IsEmergency && wasEmergency)
            {
                if (healthEvent.Status == HealthEventStatus.InProgress && healthEvent.HandledById.HasValue)
                {
                    _logger.LogInformation("Event {EventId} became non-emergency, keeping current assignment", eventId);
                }
            }
            else if (!healthEvent.HandledById.HasValue && healthEvent.Status == HealthEventStatus.Pending)
            {
                if (model.IsEmergency)
                {
                    healthEvent.HandledById = currentUserId;
                    healthEvent.Status = HealthEventStatus.InProgress;
                    healthEvent.AssignmentMethod = AssignmentMethod.SelfAssigned;
                    healthEvent.AssignedAt = DateTime.Now;

                    _logger.LogInformation("Emergency event {EventId} auto-assigned to user {UserId}", eventId,
                        currentUserId);
                }
                else
                {
                    healthEvent.HandledById = null;
                    healthEvent.Status = HealthEventStatus.Pending;
                    healthEvent.AssignmentMethod = AssignmentMethod.Unassigned;
                    healthEvent.AssignedAt = null;

                    _logger.LogInformation("Normal event {EventId} kept unassigned for background processing", eventId);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            if (!wasEmergency && healthEvent.IsEmergency)
            {
                await CreateEmergencyNotificationAsync(healthEvent.Student, healthEvent);
                await NotifyAvailableNursesAsync(healthEvent);

                _logger.LogInformation("Emergency notifications sent for event {EventId} that became emergency",
                    eventId);
            }
            else if (wasEmergency && !healthEvent.IsEmergency)
            {
                await CreateHealthEventUpdateNotificationAsync(healthEvent.Student, healthEvent,
                    "Cập nhật: Tình trạng không còn khẩn cấp");
            }
            else if (oldEventType != model.EventType)
            {
                var oldTypeDisplay = GetEventTypeDisplayName(oldEventType);
                var newTypeDisplay = GetEventTypeDisplayName(model.EventType);
                await CreateHealthEventUpdateNotificationAsync(healthEvent.Student, healthEvent,
                    $"Cập nhật: Loại sự kiện thay đổi từ '{oldTypeDisplay}' thành '{newTypeDisplay}'");
            }

            await InvalidateAllCachesAsync();

            var eventResponse = MapToHealthEventResponse(healthEvent);

            _logger.LogInformation(
                "Updated health event {EventId}: emergency {WasEmergency} → {IsEmergency}, " +
                "type {OldType} → {NewType}, assignment {OldHandled} → {NewHandled}",
                eventId, wasEmergency, healthEvent.IsEmergency,
                oldEventType, model.EventType,
                oldHandledById, healthEvent.HandledById);

            return BaseResponse<HealthEventResponse>.SuccessResult(
                eventResponse, "Cập nhật sự kiện y tế thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating health event: {EventId}", eventId);
            return BaseResponse<HealthEventResponse>.ErrorResult($"Lỗi cập nhật sự kiện y tế: {ex.Message}");
        }
    }

    public async Task<BaseResponse<bool>> DeleteHealthEventAsync(Guid eventId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                _logger.LogError("Unable to get current user ID from claims for delete operation");
                return BaseResponse<bool>.ErrorResult("Không thể xác định người dùng hiện tại.");
            }

            var currentUser = await ValidateCurrentUserPermissions(currentUserId);
            if (currentUser == null)
            {
                return BaseResponse<bool>.ErrorResult("Bạn không có quyền xóa sự kiện y tế.");
            }

            var eventRepo = _unitOfWork.GetRepositoryByEntity<HealthEvent>();
            var healthEvent = await eventRepo.GetQueryable()
                .FirstOrDefaultAsync(he => he.Id == eventId && !he.IsDeleted);

            if (healthEvent == null)
            {
                return BaseResponse<bool>.ErrorResult("Không tìm thấy sự kiện y tế.");
            }

            var hasUsage = await _unitOfWork.GetRepositoryByEntity<MedicalItemUsage>().GetQueryable()
                .AnyAsync(miu => miu.HealthEventId == eventId && !miu.IsDeleted);

            if (hasUsage)
            {
                return BaseResponse<bool>.ErrorResult("Không thể xóa sự kiện y tế đã có lịch sử sử dụng thuốc/vật tư.");
            }

            var schoolNurseRoleName = await GetSchoolNurseRoleName();

            healthEvent.IsDeleted = true;
            healthEvent.LastUpdatedBy = schoolNurseRoleName;
            healthEvent.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            await InvalidateAllCachesAsync();

            _logger.LogInformation("Deleted health event {EventId} by user {UserId}", eventId, currentUserId);

            return BaseResponse<bool>.SuccessResult(true, "Xóa sự kiện y tế thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting health event: {EventId}", eventId);
            return BaseResponse<bool>.ErrorResult($"Lỗi xóa sự kiện y tế: {ex.Message}");
        }
    }

    #endregion

    #region Health Event Management

    public async Task<BaseListResponse<HealthEventResponse>> GetHealthEventsByStudentAsync(
        Guid studentId,
        int pageIndex,
        int pageSize,
        HealthEventType? eventType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(
                HEALTH_EVENT_LIST_PREFIX,
                "by_student",
                studentId.ToString(),
                pageIndex.ToString(),
                pageSize.ToString(),
                eventType?.ToString() ?? "",
                fromDate?.ToString("yyyy-MM-dd") ?? "",
                toDate?.ToString("yyyy-MM-dd") ?? ""
            );

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<HealthEventResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Health events by student found in cache: {StudentId}", studentId);
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<HealthEvent>().GetQueryable()
                .Include(he => he.Student)
                .Include(he => he.HandledBy)
                .Include(he => he.RelatedMedicalCondition)
                .Where(he => he.UserId == studentId && !he.IsDeleted)
                .AsQueryable();

            if (eventType.HasValue)
            {
                query = query.Where(he => he.EventType == eventType.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(he => he.OccurredAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(he => he.OccurredAt <= toDate.Value.AddDays(1));
            }

            query = query.OrderByDescending(he => he.OccurredAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var healthEvents = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = healthEvents.Select(MapToHealthEventResponse).ToList();

            var result = BaseListResponse<HealthEventResponse>.SuccessResult(
                responses,
                totalCount,
                pageSize,
                pageIndex,
                "Lấy danh sách sự kiện y tế theo học sinh thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            await _cacheService.AddToTrackingSetAsync(cacheKey, HEALTH_EVENT_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting health events by student: {StudentId}", studentId);
            return BaseListResponse<HealthEventResponse>.ErrorResult("Lỗi lấy danh sách sự kiện y tế theo học sinh.");
        }
    }

    public async Task<BaseListResponse<HealthEventResponse>> GetEmergencyEventsAsync(
        int pageIndex,
        int pageSize,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(
                HEALTH_EVENT_LIST_PREFIX,
                "emergency",
                pageIndex.ToString(),
                pageSize.ToString(),
                fromDate?.ToString("yyyy-MM-dd") ?? "",
                toDate?.ToString("yyyy-MM-dd") ?? ""
            );

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<HealthEventResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Emergency events found in cache");
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<HealthEvent>().GetQueryable()
                .Include(he => he.Student)
                .Include(he => he.HandledBy)
                .Include(he => he.RelatedMedicalCondition)
                .Where(he => he.IsEmergency && !he.IsDeleted)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                query = query.Where(he => he.OccurredAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(he => he.OccurredAt <= toDate.Value.AddDays(1));
            }

            query = query.OrderByDescending(he => he.OccurredAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var healthEvents = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = healthEvents.Select(MapToHealthEventResponse).ToList();

            var result = BaseListResponse<HealthEventResponse>.SuccessResult(
                responses,
                totalCount,
                pageSize,
                pageIndex,
                "Lấy danh sách sự kiện y tế khẩn cấp thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            await _cacheService.AddToTrackingSetAsync(cacheKey, HEALTH_EVENT_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting emergency events");
            return BaseListResponse<HealthEventResponse>.ErrorResult("Lỗi lấy danh sách sự kiện y tế khẩn cấp.");
        }
    }

    /// <summary>
    /// School Nurse tự nhận sự kiện
    /// </summary>
    public async Task<BaseResponse<HealthEventResponse>> TakeOwnershipAsync(Guid eventId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                _logger.LogError("Unable to get current user ID for take ownership");
                return BaseResponse<HealthEventResponse>.ErrorResult("Không thể xác định người dùng hiện tại.");
            }

            var currentUser = await ValidateCurrentUserPermissions(currentUserId);
            if (currentUser == null)
            {
                return BaseResponse<HealthEventResponse>.ErrorResult("Bạn không có quyền nhận sự kiện y tế.");
            }

            var eventRepo = _unitOfWork.GetRepositoryByEntity<HealthEvent>();
            var healthEvent = await eventRepo.GetQueryable()
                .Include(he => he.Student)
                .Include(he => he.HandledBy)
                .Include(he => he.RelatedMedicalCondition)
                .FirstOrDefaultAsync(he => he.Id == eventId && !he.IsDeleted);

            if (healthEvent == null)
            {
                return BaseResponse<HealthEventResponse>.ErrorResult("Không tìm thấy sự kiện y tế.");
            }

            if (healthEvent.HandledById.HasValue)
            {
                return BaseResponse<HealthEventResponse>.ErrorResult("Sự kiện đã được ai đó nhận xử lý.");
            }

            if (healthEvent.Status != HealthEventStatus.Pending)
            {
                return BaseResponse<HealthEventResponse>.ErrorResult(
                    "Chỉ có thể nhận sự kiện đang ở trạng thái chờ xử lý.");
            }

            healthEvent.HandledById = currentUserId;
            healthEvent.AssignmentMethod = AssignmentMethod.SelfAssigned;
            healthEvent.Status = HealthEventStatus.InProgress;
            healthEvent.AssignedAt = DateTime.Now;
            healthEvent.LastUpdatedBy = await GetSchoolNurseRoleName();
            healthEvent.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            await NotifyOtherNursesAboutTakeOwnershipAsync(healthEvent, currentUser);

            await InvalidateAllCachesAsync();

            var eventResponse = MapToHealthEventResponse(healthEvent);

            _logger.LogInformation("User {UserId} took ownership of health event {EventId}", currentUserId, eventId);

            return BaseResponse<HealthEventResponse>.SuccessResult(
                eventResponse, "Nhận xử lý sự kiện y tế thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error taking ownership of health event: {EventId}", eventId);
            return BaseResponse<HealthEventResponse>.ErrorResult($"Lỗi nhận sự kiện y tế: {ex.Message}");
        }
    }

    /// <summary>
    /// Manager phân công cho School Nurse cụ thể
    /// </summary>
    public async Task<BaseResponse<HealthEventResponse>> AssignToNurseAsync(Guid eventId,
        AssignHealthEventRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                _logger.LogError("Unable to get current user ID for assignment");
                return BaseResponse<HealthEventResponse>.ErrorResult("Không thể xác định người dùng hiện tại.");
            }

            var currentUser = await ValidateCurrentUserPermissions(currentUserId);
            if (currentUser == null || !currentUser.UserRoles.Any(ur => ur.Role.Name.ToUpper() == "MANAGER"))
            {
                return BaseResponse<HealthEventResponse>.ErrorResult("Chỉ Manager mới có quyền phân công.");
            }

            var nurse = await ValidateCurrentUserPermissions(request.NurseId);
            if (nurse == null)
            {
                return BaseResponse<HealthEventResponse>.ErrorResult(
                    "Người được phân công không hợp lệ hoặc không có quyền xử lý sự kiện y tế.");
            }

            var eventRepo = _unitOfWork.GetRepositoryByEntity<HealthEvent>();
            var healthEvent = await eventRepo.GetQueryable()
                .Include(he => he.Student)
                .Include(he => he.HandledBy)
                .Include(he => he.RelatedMedicalCondition)
                .FirstOrDefaultAsync(he => he.Id == eventId && !he.IsDeleted);

            if (healthEvent == null)
            {
                return BaseResponse<HealthEventResponse>.ErrorResult("Không tìm thấy sự kiện y tế.");
            }

            if (healthEvent.Status == HealthEventStatus.Completed)
            {
                return BaseResponse<HealthEventResponse>.ErrorResult("Không thể phân công sự kiện đã hoàn thành.");
            }

            var previousHandlerId = healthEvent.HandledById;

            healthEvent.HandledById = request.NurseId;
            healthEvent.AssignmentMethod = AssignmentMethod.ManagerAssigned;
            healthEvent.Status = HealthEventStatus.InProgress;
            healthEvent.AssignedAt = DateTime.Now;
            healthEvent.LastUpdatedBy = await GetSchoolNurseRoleName();
            healthEvent.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            await InvalidateAllCachesAsync();

            await CreateAssignmentNotificationAsync(healthEvent, nurse);

            healthEvent = await eventRepo.GetQueryable()
                .Include(he => he.Student)
                .Include(he => he.HandledBy)
                .Include(he => he.RelatedMedicalCondition)
                .FirstOrDefaultAsync(he => he.Id == eventId);

            var eventResponse = MapToHealthEventResponse(healthEvent);

            _logger.LogInformation(
                "Manager {ManagerId} assigned health event {EventId} from {PreviousHandler} to {NewHandler}",
                currentUserId, eventId, previousHandlerId, request.NurseId);

            return BaseResponse<HealthEventResponse>.SuccessResult(
                eventResponse, "Phân công sự kiện y tế thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning health event: {EventId} to nurse: {NurseId}", eventId,
                request.NurseId);
            return BaseResponse<HealthEventResponse>.ErrorResult($"Lỗi phân công sự kiện y tế: {ex.Message}");
        }
    }

    /// <summary>
    /// Lấy danh sách sự kiện chưa ai nhận
    /// </summary>
    public async Task<BaseListResponse<HealthEventResponse>> GetUnassignedEventsAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(
                HEALTH_EVENT_LIST_PREFIX, "unassigned", pageIndex.ToString(), pageSize.ToString());

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<HealthEventResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Unassigned events found in cache");
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<HealthEvent>().GetQueryable()
                .Include(he => he.Student)
                .Include(he => he.HandledBy)
                .Include(he => he.RelatedMedicalCondition)
                .Where(he => he.Status == HealthEventStatus.Pending &&
                             he.HandledById == null &&
                             !he.IsDeleted)
                .OrderByDescending(he => he.IsEmergency)
                .ThenByDescending(he => he.OccurredAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var events = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = events.Select(MapToHealthEventResponse).ToList();

            var result = BaseListResponse<HealthEventResponse>.SuccessResult(
                responses, totalCount, pageSize, pageIndex,
                "Lấy danh sách sự kiện chưa được phân công thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2));
            await _cacheService.AddToTrackingSetAsync(cacheKey, HEALTH_EVENT_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unassigned events");
            return BaseListResponse<HealthEventResponse>.ErrorResult("Lỗi lấy danh sách sự kiện chưa được phân công.");
        }
    }

    /// <summary>
    /// Đánh dấu sự kiện hoàn thành
    /// </summary>
    public async Task<BaseResponse<HealthEventResponse>> CompleteEventAsync(
        Guid eventId, CompleteHealthEventRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BaseResponse<HealthEventResponse>.ErrorResult("Không thể xác định người dùng hiện tại.");
            }

            var currentUser = await ValidateCurrentUserPermissions(currentUserId);
            if (currentUser == null)
            {
                return BaseResponse<HealthEventResponse>.ErrorResult("Bạn không có quyền hoàn thành sự kiện y tế.");
            }

            var eventRepo = _unitOfWork.GetRepositoryByEntity<HealthEvent>();
            var healthEvent = await eventRepo.GetQueryable()
                .Include(he => he.Student)
                .Include(he => he.HandledBy)
                .Include(he => he.RelatedMedicalCondition)
                .FirstOrDefaultAsync(he => he.Id == eventId && !he.IsDeleted);

            if (healthEvent == null)
            {
                return BaseResponse<HealthEventResponse>.ErrorResult("Không tìm thấy sự kiện y tế.");
            }

            if (healthEvent.HandledById != currentUserId)
            {
                return BaseResponse<HealthEventResponse>.ErrorResult(
                    "Bạn không phải người được phân công xử lý sự kiện này.");
            }

            if (healthEvent.Status == HealthEventStatus.Completed)
            {
                return BaseResponse<HealthEventResponse>.ErrorResult("Sự kiện đã được hoàn thành.");
            }

            healthEvent.ActionTaken = request.ActionTaken;
            healthEvent.Outcome = request.Outcome;
            healthEvent.Status = HealthEventStatus.Completed;
            healthEvent.CompletedAt = DateTime.Now;
            healthEvent.LastUpdatedBy = await GetSchoolNurseRoleName();
            healthEvent.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            await NotifyOtherNursesAboutCompletionAsync(healthEvent, currentUser);

            await InvalidateAllCachesAsync();

            var eventResponse = MapToHealthEventResponse(healthEvent);

            _logger.LogInformation("Health event {EventId} completed by user {UserId}", eventId, currentUserId);

            return BaseResponse<HealthEventResponse>.SuccessResult(
                eventResponse, "Hoàn thành xử lý sự kiện y tế thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing health event: {EventId}", eventId);
            return BaseResponse<HealthEventResponse>.ErrorResult($"Lỗi hoàn thành sự kiện y tế: {ex.Message}");
        }
    }

    /// <summary>
    /// Lấy danh sách sự kiện được phân công cho người dùng hiện tại
    /// </summary>
    public async Task<BaseListResponse<HealthEventResponse>> GetMyAssignedEventsAsync(
        int pageIndex, int pageSize, HealthEventStatus? status = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                _logger.LogError("Unable to get current user ID for GetMyAssignedEventsAsync");
                return BaseListResponse<HealthEventResponse>.ErrorResult("Không thể xác định người dùng hiện tại.");
            }

            var cacheKey = _cacheService.GenerateCacheKey(
                HEALTH_EVENT_LIST_PREFIX, "my_assigned", currentUserId.ToString(),
                pageIndex.ToString(), pageSize.ToString(), status?.ToString() ?? "");

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<HealthEventResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("My assigned events found in cache for user: {UserId}", currentUserId);
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<HealthEvent>().GetQueryable()
                .Include(he => he.Student)
                .Include(he => he.HandledBy)
                .Include(he => he.RelatedMedicalCondition)
                .Where(he => he.HandledById == currentUserId && !he.IsDeleted);

            if (status.HasValue)
            {
                query = query.Where(he => he.Status == status.Value);
            }

            query = query.OrderByDescending(he => he.IsEmergency)
                .ThenBy(he => he.Status)
                .ThenByDescending(he => he.OccurredAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var events = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = events.Select(MapToHealthEventResponse).ToList();

            var result = BaseListResponse<HealthEventResponse>.SuccessResult(
                responses, totalCount, pageSize, pageIndex,
                "Lấy danh sách sự kiện được phân công thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(3));
            await _cacheService.AddToTrackingSetAsync(cacheKey, HEALTH_EVENT_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my assigned events");
            return BaseListResponse<HealthEventResponse>.ErrorResult("Lỗi lấy danh sách sự kiện được phân công.");
        }
    }

    #endregion

    #region Related Medical Conditions

    /// <summary>
    /// Kiểm tra medical condition có thuộc về student không
    /// </summary>
    private async Task<bool> ValidateMedicalConditionOwnership(Guid medicalConditionId, Guid studentId)
    {
        try
        {
            var conditionRepo = _unitOfWork.GetRepositoryByEntity<MedicalCondition>();

            return await conditionRepo.GetQueryable()
                .Join(_unitOfWork.GetRepositoryByEntity<MedicalRecord>().GetQueryable(),
                    mc => mc.MedicalRecordId,
                    mr => mr.Id,
                    (mc, mr) => new { MedicalCondition = mc, MedicalRecord = mr })
                .AnyAsync(joined => joined.MedicalCondition.Id == medicalConditionId &&
                                    joined.MedicalRecord.UserId == studentId &&
                                    !joined.MedicalCondition.IsDeleted &&
                                    !joined.MedicalRecord.IsDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating medical condition ownership: {ConditionId}, {StudentId}",
                medicalConditionId, studentId);
            return false;
        }
    }

    /// <summary>
    /// Lấy thông tin medical condition với student info
    /// </summary>
    private async Task<(MedicalCondition condition, bool belongsToStudent)> GetMedicalConditionWithValidation(
        Guid medicalConditionId, Guid studentId)
    {
        try
        {
            var conditionRepo = _unitOfWork.GetRepositoryByEntity<MedicalCondition>();

            var result = await conditionRepo.GetQueryable()
                .Include(mc => mc.MedicalRecord)
                .FirstOrDefaultAsync(mc => mc.Id == medicalConditionId && !mc.IsDeleted);

            if (result == null)
            {
                return (null, false);
            }

            var belongsToStudent = result.MedicalRecord?.UserId == studentId;
            return (result, belongsToStudent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting medical condition: {ConditionId}", medicalConditionId);
            return (null, false);
        }
    }

    public async Task<BaseListResponse<HealthEventResponse>> GetEventsByMedicalConditionAsync(
        Guid medicalConditionId,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(
                HEALTH_EVENT_LIST_PREFIX,
                "by_condition",
                medicalConditionId.ToString(),
                pageIndex.ToString(),
                pageSize.ToString()
            );

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<HealthEventResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Health events by medical condition found in cache: {ConditionId}",
                    medicalConditionId);
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<HealthEvent>().GetQueryable()
                .Include(he => he.Student)
                .Include(he => he.HandledBy)
                .Include(he => he.RelatedMedicalCondition)
                .Where(he => he.RelatedMedicalConditionId == medicalConditionId && !he.IsDeleted)
                .OrderByDescending(he => he.OccurredAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var healthEvents = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = healthEvents.Select(MapToHealthEventResponse).ToList();

            var result = BaseListResponse<HealthEventResponse>.SuccessResult(
                responses,
                totalCount,
                pageSize,
                pageIndex,
                "Lấy danh sách sự kiện y tế theo tình trạng y tế thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            await _cacheService.AddToTrackingSetAsync(cacheKey, HEALTH_EVENT_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting health events by medical condition: {ConditionId}", medicalConditionId);
            return BaseListResponse<HealthEventResponse>.ErrorResult(
                "Lỗi lấy danh sách sự kiện y tế theo tình trạng y tế.");
        }
    }

    #endregion

    #region Helper Methods

    private MedicalConditionType MapHealthEventTypeToMedicalConditionType(HealthEventType eventType)
    {
        switch (eventType)
        {
            case HealthEventType.AllergicReaction:
                return MedicalConditionType.Allergy;
            case HealthEventType.ChronicIllnessEpisode:
                return MedicalConditionType.ChronicDisease;
            case HealthEventType.Injury:
            case HealthEventType.Fall:
            case HealthEventType.Illness:
                return MedicalConditionType.MedicalHistory;
            default:
                return MedicalConditionType.MedicalHistory; // Mặc định cho các loại khác
        }
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(UserHelper.GetCurrentUserId(_httpContextAccessor.HttpContext));
    }

    private async Task<ApplicationUser> ValidateCurrentUserPermissions(Guid userId)
    {
        try
        {
            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var user = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            if (user == null)
            {
                _logger.LogError("User not found: {UserId}", userId);
                return null;
            }

            var validRoles = new[] { "SCHOOLNURSE", "ADMIN", "MANAGER" };
            var hasValidRole = user.UserRoles.Any(ur => validRoles.Contains(ur.Role.Name.ToUpper()));

            if (!hasValidRole)
            {
                _logger.LogError("User {UserId} does not have permission for health event operations", userId);
                return null;
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user permissions: {UserId}", userId);
            return null;
        }
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

    private HealthEventResponse MapToHealthEventResponse(HealthEvent healthEvent)
    {
        var currentUserId = GetCurrentUserId();
        var response = _mapper.Map<HealthEventResponse>(healthEvent);

        response.EventTypeDisplayName = GetEventTypeDisplayName(healthEvent.EventType);
        response.EmergencyStatusText = healthEvent.IsEmergency ? "Khẩn cấp" : "Bình thường";

        response.CanTakeOwnership = healthEvent.Status == HealthEventStatus.Pending &&
                                    healthEvent.HandledById == null;
        response.CanComplete = healthEvent.HandledById == currentUserId &&
                               healthEvent.Status == HealthEventStatus.InProgress;

        if (healthEvent.Student != null)
        {
            response.StudentName = healthEvent.Student.FullName;
            response.StudentCode = healthEvent.Student.StudentCode;         
        }

        if (healthEvent.HandledBy != null)
        {
            response.HandledByName = healthEvent.HandledBy.FullName;
        }

        if (healthEvent.RelatedMedicalCondition != null)
        {
            response.RelatedMedicalConditionName = healthEvent.RelatedMedicalCondition.Name;
        }

        return response;
    }

    private async Task<(bool isValid, string errorMessage)> ValidateHealthEventMedicalConditionCompatibility(
        HealthEventType eventType, Guid medicalConditionId)
    {
        try
        {
            var conditionRepo = _unitOfWork.GetRepositoryByEntity<MedicalCondition>();
            var medicalCondition = await conditionRepo.GetQueryable()
                .FirstOrDefaultAsync(mc => mc.Id == medicalConditionId && !mc.IsDeleted);

            if (medicalCondition == null)
            {
                return (false, "Không tìm thấy tình trạng y tế liên quan.");
            }

            var isCompatible = eventType switch
            {
                HealthEventType.AllergicReaction =>
                    medicalCondition.Type == MedicalConditionType.Allergy,

                HealthEventType.ChronicIllnessEpisode =>
                    medicalCondition.Type == MedicalConditionType.ChronicDisease,

                HealthEventType.Injury or HealthEventType.Illness or HealthEventType.Fall =>
                    medicalCondition.Type == MedicalConditionType.MedicalHistory,

                HealthEventType.Other =>
                    true,

                _ => false
            };

            if (!isCompatible)
            {
                var expectedType = GetExpectedMedicalConditionType(eventType);
                var actualTypeDisplay = GetMedicalConditionTypeDisplay(medicalCondition.Type);
                var expectedTypeDisplay = GetMedicalConditionTypeDisplay(expectedType);

                return (false, $"Sự kiện '{GetEventTypeDisplayName(eventType)}' chỉ có thể liên kết với " +
                               $"tình trạng loại '{expectedTypeDisplay}', nhưng đang chọn loại '{actualTypeDisplay}'.");
            }

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating medical condition compatibility");
            return (false, "Lỗi kiểm tra tính phù hợp của tình trạng y tế.");
        }
    }

    private MedicalConditionType GetExpectedMedicalConditionType(HealthEventType eventType)
    {
        return eventType switch
        {
            HealthEventType.AllergicReaction => MedicalConditionType.Allergy,
            HealthEventType.ChronicIllnessEpisode => MedicalConditionType.ChronicDisease,
            HealthEventType.Injury or HealthEventType.Illness or HealthEventType.Fall => MedicalConditionType
                .MedicalHistory,
            _ => MedicalConditionType.MedicalHistory
        };
    }

    private string GetMedicalConditionTypeDisplay(MedicalConditionType type)
    {
        return type switch
        {
            MedicalConditionType.Allergy => "Dị ứng",
            MedicalConditionType.ChronicDisease => "Bệnh mãn tính",
            MedicalConditionType.MedicalHistory => "Lịch sử y tế",
            _ => type.ToString()
        };
    }

    private string GetStatusDisplayName(HealthEventStatus status)
    {
        return status switch
        {
            HealthEventStatus.Pending => "Chờ xử lý",
            HealthEventStatus.InProgress => "Đang xử lý",
            HealthEventStatus.Completed => "Hoàn thành",
            HealthEventStatus.Cancelled => "Đã hủy",
            _ => status.ToString()
        };
    }

    private string GetAssignmentMethodDisplayName(AssignmentMethod method)
    {
        return method switch
        {
            AssignmentMethod.Unassigned => "Chưa phân công",
            AssignmentMethod.SelfAssigned => "Tự nhận",
            AssignmentMethod.ManagerAssigned => "Manager phân công",
            _ => method.ToString()
        };
    }

    private string GetEventTypeDisplayName(HealthEventType eventType)
    {
        return eventType switch
        {
            HealthEventType.Injury => "Chấn thương",
            HealthEventType.Illness => "Ốm đau",
            HealthEventType.AllergicReaction => "Phản ứng dị ứng",
            HealthEventType.Fall => "Té ngã",
            HealthEventType.ChronicIllnessEpisode => "Đợt tái phát bệnh mãn tính",
            HealthEventType.Other => "Khác",
            _ => eventType.ToString()
        };
    }

    private IQueryable<HealthEvent> ApplyHealthEventFilters(
        IQueryable<HealthEvent> query,
        string searchTerm,
        Guid? studentId,
        HealthEventType? eventType,
        bool? isEmergency,
        DateTime? fromDate,
        DateTime? toDate,
        string? location)
    {
        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(he =>
                he.Description.ToLower().Contains(searchTerm) ||
                he.Student.FullName.ToLower().Contains(searchTerm) ||
                he.Student.StudentCode.ToLower().Contains(searchTerm) ||
                he.Location.ToLower().Contains(searchTerm) ||
                (he.ActionTaken != null && he.ActionTaken.ToLower().Contains(searchTerm)));
        }

        if (studentId.HasValue)
        {
            query = query.Where(he => he.UserId == studentId.Value);
        }

        if (eventType.HasValue)
        {
            query = query.Where(he => he.EventType == eventType.Value);
        }

        if (isEmergency.HasValue)
        {
            query = query.Where(he => he.IsEmergency == isEmergency.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(he => he.OccurredAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(he => he.OccurredAt <= toDate.Value.AddDays(1));
        }

        if (!string.IsNullOrEmpty(location))
        {
            query = query.Where(he => he.Location.ToLower().Contains(location.ToLower()));
        }

        return query;
    }

    private IQueryable<HealthEvent> ApplyHealthEventOrdering(IQueryable<HealthEvent> query, string orderBy)
    {
        return orderBy?.ToLower() switch
        {
            "occurredat" => query.OrderBy(he => he.OccurredAt),
            "occurredat_desc" => query.OrderByDescending(he => he.OccurredAt),
            "studentname" => query.OrderBy(he => he.Student.FullName),
            "studentname_desc" => query.OrderByDescending(he => he.Student.FullName),
            "eventtype" => query.OrderBy(he => he.EventType),
            "eventtype_desc" => query.OrderByDescending(he => he.EventType),
            "location" => query.OrderBy(he => he.Location),
            "location_desc" => query.OrderByDescending(he => he.Location),
            "emergency" => query.OrderBy(he => he.IsEmergency),
            "emergency_desc" => query.OrderByDescending(he => he.IsEmergency),
            "createdate" => query.OrderBy(he => he.CreatedDate),
            "createdate_desc" => query.OrderByDescending(he => he.CreatedDate),
            _ => query.OrderByDescending(he => he.OccurredAt)
        };
    }

    private async Task InvalidateAllCachesAsync()
    {
        try
        {
            _logger.LogDebug("Starting comprehensive cache invalidation for health events and related entities");
            var keysBefore = await _cacheService.GetKeysByPatternAsync("*health_event*");
            _logger.LogDebug("Cache keys before invalidation: {Keys}", string.Join(", ", keysBefore));

            // Xóa toàn bộ tracking set của HealthEvent
            await _cacheService.InvalidateTrackingSetAsync(HEALTH_EVENT_CACHE_SET);
            // Xóa các tiền tố liên quan đến HealthEvent và các thực thể liên quan
            await Task.WhenAll(
                _cacheService.RemoveByPrefixAsync(HEALTH_EVENT_CACHE_PREFIX),
                _cacheService.RemoveByPrefixAsync(HEALTH_EVENT_LIST_PREFIX),
                _cacheService.RemoveByPrefixAsync(STATISTICS_PREFIX),
                // Xóa cache liên quan đến MedicalItem, MedicalItemUsage, và Notification
                _cacheService.RemoveByPrefixAsync("medical_item"),
                _cacheService.RemoveByPrefixAsync("medical_items_list"),
                _cacheService.RemoveByPrefixAsync("medical_item_usage"),
                _cacheService.RemoveByPrefixAsync("notification"),
                _cacheService.RemoveByPrefixAsync("notifications_list")
            );

            var keysAfter = await _cacheService.GetKeysByPatternAsync("*health_event*");
            _logger.LogDebug("Cache keys after invalidation: {Keys}", string.Join(", ", keysAfter));
            _logger.LogDebug("Completed comprehensive cache invalidation for health events and related entities");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in comprehensive cache invalidation for health events");
        }
    }

    #endregion

    #region Notification Methods

    private async Task CreateHealthEventUpdateNotificationAsync(ApplicationUser student, HealthEvent healthEvent,
        string updateMessage)
    {
        try
        {
            if (!student.ParentId.HasValue)
            {
                _logger.LogWarning("Student {StudentId} has no parent, skipping update notification", student.Id);
                return;
            }

            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = $"Cập nhật sự kiện y tế - {student.FullName}",
                Content = $"Con em Quý phụ huynh ({student.FullName} - {student.StudentCode}) " +
                          $"có cập nhật về sự kiện y tế:{Environment.NewLine}{Environment.NewLine}" +
                          $"{updateMessage}{Environment.NewLine}{Environment.NewLine}" +
                          $"Vị trí: {healthEvent.Location}{Environment.NewLine}" +
                          $"Thời gian: {healthEvent.OccurredAt:dd/MM/yyyy HH:mm}{Environment.NewLine}" +
                          $"Mô tả: {healthEvent.Description}{Environment.NewLine}" +
                          $"Mức độ hiện tại: {(healthEvent.IsEmergency ? "KHẨN CẤP" : "Bình thường")}{Environment.NewLine}{Environment.NewLine}" +
                          "Vui lòng liên hệ với nhà trường nếu có thắc mắc.",
                NotificationType = NotificationType.HealthEvent,
                SenderId = null,
                RecipientId = student.ParentId.Value,
                HealthEventId = healthEvent.Id,
                RequiresConfirmation = false,
                IsRead = false,
                IsConfirmed = false,
                CreatedDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(3)
            };

            await notificationRepo.AddAsync(notification);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Created update notification for parent {ParentId}, student {StudentId}, event {EventId}",
                student.ParentId.Value, student.Id, healthEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating update notification for health event {EventId}", healthEvent.Id);
        }
    }

    private async Task CreateEmergencyNotificationAsync(ApplicationUser student, HealthEvent healthEvent)
    {
        try
        {
            if (!student.ParentId.HasValue)
            {
                _logger.LogWarning("Student {StudentId} has no parent, skipping emergency notification", student.Id);
                return;
            }

            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = $"KHẨN CẤP: Sự kiện y tế - {student.FullName}",
                Content = $"Con em Quý phụ huynh ({student.FullName} - {student.StudentCode}) " +
                          $"đã xảy ra sự kiện y tế KHẨN CẤP tại {healthEvent.Location} " +
                          $"vào lúc {healthEvent.OccurredAt:dd/MM/yyyy HH:mm}.{Environment.NewLine}{Environment.NewLine}" +
                          $"Loại sự kiện: {GetEventTypeDisplayName(healthEvent.EventType)}{Environment.NewLine}" +
                          $"Mô tả: {healthEvent.Description}{Environment.NewLine}" +
                          $"Hành động đã thực hiện: {healthEvent.ActionTaken}{Environment.NewLine}{Environment.NewLine}" +
                          "Vui lòng liên hệ ngay với nhà trường để biết thêm chi tiết.",
                NotificationType = NotificationType.HealthEvent,
                SenderId = null,
                RecipientId = student.ParentId.Value,
                HealthEventId = healthEvent.Id,
                RequiresConfirmation = true,
                IsRead = false,
                IsConfirmed = false,
                CreatedDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(7)
            };

            await notificationRepo.AddAsync(notification);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Created emergency notification for parent {ParentId}, student {StudentId}, event {EventId}",
                student.ParentId.Value, student.Id, healthEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating emergency notification for health event {EventId}", healthEvent.Id);
        }
    }

    private async Task CreateHealthEventNotificationAsync(ApplicationUser student, HealthEvent healthEvent)
    {
        try
        {
            if (!student.ParentId.HasValue)
            {
                _logger.LogWarning("Student {StudentId} has no parent, skipping health event notification", student.Id);
                return;
            }

            if (healthEvent.IsEmergency)
            {
                return;
            }

            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = $"Thông báo sự kiện y tế - {student.FullName}",
                Content = $"Con em Quý phụ huynh ({student.FullName} - {student.StudentCode}) " +
                          $"đã xảy ra sự kiện y tế tại {healthEvent.Location} " +
                          $"vào lúc {healthEvent.OccurredAt:dd/MM/yyyy HH:mm}.{Environment.NewLine}{Environment.NewLine}" +
                          $"Loại sự kiện: {GetEventTypeDisplayName(healthEvent.EventType)}{Environment.NewLine}" +
                          $"Mô tả: {healthEvent.Description}{Environment.NewLine}" +
                          $"Hành động đã thực hiện: {healthEvent.ActionTaken}{Environment.NewLine}" +
                          (string.IsNullOrEmpty(healthEvent.Outcome)
                              ? ""
                              : $"Kết quả: {healthEvent.Outcome}{Environment.NewLine}") +
                          $"{Environment.NewLine}Vui lòng liên hệ với nhà trường nếu cần biết thêm chi tiết.",
                NotificationType = NotificationType.HealthEvent,
                SenderId = null,
                RecipientId = student.ParentId.Value,
                HealthEventId = healthEvent.Id,
                RequiresConfirmation = false,
                IsRead = false,
                IsConfirmed = false,
                CreatedDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(7)
            };

            await notificationRepo.AddAsync(notification);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Created health event notification for parent {ParentId}, student {StudentId}, event {EventId}",
                student.ParentId.Value, student.Id, healthEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating health event notification for event {EventId}", healthEvent.Id);
        }
    }

    private async Task CreateAssignmentNotificationAsync(HealthEvent healthEvent, ApplicationUser assignedNurse)
    {
        try
        {
            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = $"Sự kiện y tế được phân công - {healthEvent.Student?.FullName}",
                Content = $"Bạn được phân công xử lý sự kiện y tế:{Environment.NewLine}{Environment.NewLine}" +
                          $"Học sinh: {healthEvent.Student?.FullName} ({healthEvent.Student?.StudentCode}){Environment.NewLine}" +
                          $"Vị trí: {healthEvent.Location}{Environment.NewLine}" +
                          $"Thời gian: {healthEvent.OccurredAt:dd/MM/yyyy HH:mm}{Environment.NewLine}" +
                          $"Mô tả: {healthEvent.Description}{Environment.NewLine}" +
                          $"Mức độ: {(healthEvent.IsEmergency ? "KHẨN CẤP" : "Bình thường")}{Environment.NewLine}{Environment.NewLine}" +
                          "Vui lòng xử lý sự kiện này.",
                NotificationType = NotificationType.HealthEvent,
                SenderId = null,
                RecipientId = assignedNurse.Id,
                HealthEventId = healthEvent.Id,
                RequiresConfirmation = true,
                IsRead = false,
                CreatedDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(8)
            };

            await notificationRepo.AddAsync(notification);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Created assignment notification for nurse {NurseId}, event {EventId}",
                assignedNurse.Id, healthEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating assignment notification for event {EventId}", healthEvent.Id);
        }
    }

    /// <summary>
    /// Thông báo cho các SchoolNurse khác về việc ai đó đã nhận sự kiện
    /// </summary>
    private async Task NotifyOtherNursesAboutTakeOwnershipAsync(HealthEvent healthEvent, ApplicationUser handlingNurse)
    {
        try
        {
            var nursesRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var otherNurses = await nursesRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "SCHOOLNURSE")
                            && u.IsActive
                            && !u.IsDeleted
                            && u.Id != handlingNurse.Id) // Loại trừ người đã nhận sự kiện
                .ToListAsync();

            if (!otherNurses.Any())
            {
                _logger.LogDebug("No other nurses to notify about take ownership for event {EventId}", healthEvent.Id);
                return;
            }

            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();
            var notifications = new List<Notification>();

            var emergencyText = healthEvent.IsEmergency ? "KHẨN CẤP " : "";
            var titlePrefix = healthEvent.IsEmergency
                ? "Đồng nghiệp đã nhận sự kiện khẩn cấp"
                : "Đồng nghiệp đã nhận sự kiện";

            foreach (var nurse in otherNurses)
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = $"{titlePrefix} - {healthEvent.Student?.FullName}",
                    Content =
                        $"Đồng nghiệp {handlingNurse.FullName} đã nhận xử lý sự kiện y tế {emergencyText}:{Environment.NewLine}{Environment.NewLine}" +
                        $"Học sinh: {healthEvent.Student?.FullName} ({healthEvent.Student?.StudentCode}){Environment.NewLine}" +
                        $"Loại sự kiện: {GetEventTypeDisplayName(healthEvent.EventType)}{Environment.NewLine}" +
                        $"Vị trí: {healthEvent.Location}{Environment.NewLine}" +
                        $"Thời gian: {healthEvent.OccurredAt:dd/MM/yyyy HH:mm}{Environment.NewLine}" +
                        $"Mô tả: {healthEvent.Description}{Environment.NewLine}" +
                        $"Mức độ: {(healthEvent.IsEmergency ? "KHẨN CẤP" : "Bình thường")}{Environment.NewLine}" +
                        $"Nhận bởi: {handlingNurse.FullName}{Environment.NewLine}" +
                        $"Nhận lúc: {healthEvent.AssignedAt:dd/MM/yyyy HH:mm}{Environment.NewLine}{Environment.NewLine}" +
                        "Sẵn sàng hỗ trợ nếu cần thiết.",
                    NotificationType = NotificationType.HealthEvent,
                    SenderId = handlingNurse.Id,
                    RecipientId = nurse.Id,
                    HealthEventId = healthEvent.Id,
                    RequiresConfirmation = false,
                    IsRead = false,
                    CreatedDate = DateTime.Now,
                    EndDate = DateTime.Now.AddHours(healthEvent.IsEmergency
                        ? 8
                        : 4)
                };

                notifications.Add(notification);
            }

            await notificationRepo.AddRangeAsync(notifications);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Notified {Count} other nurses about take ownership of event {EventId} by {NurseId}",
                otherNurses.Count, healthEvent.Id, handlingNurse.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying other nurses about take ownership of event {EventId}",
                healthEvent.Id);
        }
    }

    /// <summary>
    /// Thông báo cho các SchoolNurse khác về việc hoàn thành sự kiện
    /// </summary>
    private async Task NotifyOtherNursesAboutCompletionAsync(HealthEvent healthEvent, ApplicationUser completingNurse)
    {
        try
        {
            var nursesRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var otherNurses = await nursesRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "SCHOOLNURSE")
                            && u.IsActive
                            && !u.IsDeleted
                            && u.Id != completingNurse.Id) // Loại trừ người đã hoàn thành sự kiện
                .ToListAsync();

            if (!otherNurses.Any())
            {
                _logger.LogDebug("No other nurses to notify about completion for event {EventId}", healthEvent.Id);
                return;
            }

            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();
            var notifications = new List<Notification>();

            var emergencyText = healthEvent.IsEmergency ? "khẩn cấp " : "";
            var titlePrefix = healthEvent.IsEmergency ? "Sự kiện khẩn cấp đã hoàn thành" : "Sự kiện y tế đã hoàn thành";

            foreach (var nurse in otherNurses)
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = $"{titlePrefix} - {healthEvent.Student?.FullName}",
                    Content =
                        $"Đồng nghiệp {completingNurse.FullName} đã hoàn thành xử lý sự kiện y tế {emergencyText}:{Environment.NewLine}{Environment.NewLine}" +
                        $"Học sinh: {healthEvent.Student?.FullName} ({healthEvent.Student?.StudentCode}){Environment.NewLine}" +
                        $"Loại sự kiện: {GetEventTypeDisplayName(healthEvent.EventType)}{Environment.NewLine}" +
                        $"Vị trí: {healthEvent.Location}{Environment.NewLine}" +
                        $"Thời gian xảy ra: {healthEvent.OccurredAt:dd/MM/yyyy HH:mm}{Environment.NewLine}" +
                        $"Mô tả: {healthEvent.Description}{Environment.NewLine}" +
                        $"Mức độ: {(healthEvent.IsEmergency ? "KHẨN CẤP" : "Bình thường")}{Environment.NewLine}" +
                        $"Xử lý bởi: {completingNurse.FullName}{Environment.NewLine}" +
                        $"Hoàn thành lúc: {healthEvent.CompletedAt:dd/MM/yyyy HH:mm}{Environment.NewLine}" +
                        (!string.IsNullOrEmpty(healthEvent.ActionTaken)
                            ? $"Hành động đã thực hiện: {healthEvent.ActionTaken}{Environment.NewLine}"
                            : "") +
                        (!string.IsNullOrEmpty(healthEvent.Outcome)
                            ? $"Kết quả: {healthEvent.Outcome}{Environment.NewLine}"
                            : "") +
                        $"{Environment.NewLine}Sự kiện đã được xử lý thành công.",
                    NotificationType = NotificationType.HealthEvent,
                    SenderId = completingNurse.Id,
                    RecipientId = nurse.Id,
                    HealthEventId = healthEvent.Id,
                    RequiresConfirmation = false,
                    IsRead = false,
                    CreatedDate = DateTime.Now,
                    EndDate = DateTime.Now.AddHours(healthEvent.IsEmergency ? 12 : 6)
                };

                notifications.Add(notification);
            }

            await notificationRepo.AddRangeAsync(notifications);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Notified {Count} other nurses about completion of event {EventId} by {NurseId}",
                otherNurses.Count, healthEvent.Id, completingNurse.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying other nurses about completion of event {EventId}", healthEvent.Id);
        }
    }

    private async Task NotifyAvailableNursesAsync(HealthEvent healthEvent)
    {
        try
        {
            var nursesRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var availableNurses = await nursesRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "SCHOOLNURSE")
                            && u.IsActive
                            && !u.IsDeleted
                            && u.Id != healthEvent.HandledById)
                .ToListAsync();

            if (!availableNurses.Any()) return;

            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();
            var notifications = new List<Notification>();

            foreach (var nurse in availableNurses)
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = $"Đồng nghiệp đang xử lý khẩn cấp - {healthEvent.Student?.FullName}",
                    Content =
                        $"Đồng nghiệp đang xử lý sự kiện y tế KHẨN CẤP:{Environment.NewLine}{Environment.NewLine}" +
                        $"Học sinh: {healthEvent.Student?.FullName} ({healthEvent.Student?.StudentCode}){Environment.NewLine}" +
                        $"Vị trí: {healthEvent.Location}{Environment.NewLine}" +
                        $"Thời gian: {healthEvent.OccurredAt:dd/MM/yyyy HH:mm}{Environment.NewLine}" +
                        $"Mô tả: {healthEvent.Description}{Environment.NewLine}{Environment.NewLine}" +
                        "Sẵn sàng hỗ trợ nếu cần thiết.",
                    NotificationType = NotificationType.HealthEvent,
                    SenderId = null,
                    RecipientId = nurse.Id,
                    HealthEventId = healthEvent.Id,
                    RequiresConfirmation = false,
                    IsRead = false,
                    CreatedDate = DateTime.Now,
                    EndDate = DateTime.Now.AddHours(4)
                };

                notifications.Add(notification);
            }

            await notificationRepo.AddRangeAsync(notifications);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Notified {Count} available nurses about emergency {EventId}",
                availableNurses.Count, healthEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying available nurses about emergency {EventId}", healthEvent.Id);
        }
    }

    private async Task<string> GenerateHealthEventCodeAsync()
    {
        try
        {
            var now = DateTime.Now;
            var year = now.Year.ToString().Substring(2);
            var month = now.Month.ToString().PadLeft(2, '0');
            var day = now.Day.ToString().PadLeft(2, '0');

            var datePrefix = $"{year}{month}{day}";
            var prefix = "HE";

            var todayStart = now.Date;
            var todayEnd = todayStart.AddDays(1);

            var eventRepo = _unitOfWork.GetRepositoryByEntity<HealthEvent>();
            var todayEvents = await eventRepo.GetQueryable()
                .Where(he => he.CreatedDate.HasValue &&
                             he.CreatedDate.Value >= todayStart &&
                             he.CreatedDate.Value < todayEnd &&
                             !string.IsNullOrEmpty(he.Code) &&
                             he.Code.StartsWith($"{prefix}-{datePrefix}"))
                .Select(he => he.Code)
                .ToListAsync();

            int maxSequence = 0;
            foreach (var code in todayEvents)
            {
                try
                {
                    var parts = code.Split('-');
                    if (parts.Length == 3 && int.TryParse(parts[2], out int sequence))
                    {
                        maxSequence = Math.Max(maxSequence, sequence);
                    }
                }
                catch
                {
                    continue;
                }
            }

            var newSequence = maxSequence + 1;
            var sequenceStr = newSequence.ToString().PadLeft(6, '0');
            var generatedCode = $"{prefix}-{datePrefix}-{sequenceStr}";

            _logger.LogInformation("Generated health event code: {Code} (sequence: {Sequence})",
                generatedCode, newSequence);

            return generatedCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating health event code");

            var fallbackCode = $"HE-{DateTime.Now:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
            _logger.LogWarning("Using fallback code: {Code}", fallbackCode);

            return fallbackCode;
        }
    }

    #endregion
}