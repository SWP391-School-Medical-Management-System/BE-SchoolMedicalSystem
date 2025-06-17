using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Helpers;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicationScheduleRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicationScheduleResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationAdministrationResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services;

public class MedicationScheduleService : IMedicationScheduleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<MedicationScheduleService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IValidator<CreateMedicationScheduleRequest> _createValidator;
    private readonly IValidator<MarkStudentAbsentRequest> _absentValidator;
    private readonly IValidator<MarkMissedMedicationRequest> _markMissedValidator;
    private readonly IValidator<AdministerScheduleRequest> _administerValidator;
    private readonly IValidator<BulkAdministerRequest> _bulkAdministerValidator;
    private readonly IValidator<QuickCompleteRequest> _quickCompleteValidator;
    private readonly IMapper _mapper;

    private const string SCHEDULE_CACHE_PREFIX = "medication_schedule";
    private const string SCHEDULE_LIST_PREFIX = "medication_schedules_list";
    private const string SCHEDULE_CACHE_SET = "medication_schedule_cache_keys";
    private const string SCHEDULE_STATISTICS_PREFIX = "schedule_statistics";
    private const string DAILY_SCHEDULE_PREFIX = "daily_schedule";
    private const string STUDENT_SCHEDULE_PREFIX = "student_schedule";
    private const string PARENT_SCHEDULE_PREFIX = "parent_schedule";

    public MedicationScheduleService(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<MedicationScheduleService> logger,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        IValidator<CreateMedicationScheduleRequest> createValidator,
        IValidator<MarkStudentAbsentRequest> absentValidator,
        IValidator<MarkMissedMedicationRequest> markMissedValidator,
        IValidator<AdministerScheduleRequest> administerValidator,
        IValidator<BulkAdministerRequest> bulkAdministerValidator,
        IValidator<QuickCompleteRequest> quickCompleteValidator)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        _createValidator = createValidator;
        _absentValidator = absentValidator;
        _markMissedValidator = markMissedValidator;
        _administerValidator = administerValidator;
        _bulkAdministerValidator = bulkAdministerValidator;
        _quickCompleteValidator = quickCompleteValidator;
    }

    #region Create Medication Schedule

    public async Task<BaseResponse<BatchOperationResponse>> GenerateSchedulesAsync(
        CreateMedicationScheduleRequest request)
    {
        try
        {
            _logger.LogInformation("Generating schedules for StudentMedication {StudentMedicationId}",
                request.StudentMedicationId);

            var medication = await _unitOfWork.GetRepositoryByEntity<StudentMedication>()
                .GetQueryable()
                .Include(sm => sm.Student)
                .FirstOrDefaultAsync(sm => sm.Id == request.StudentMedicationId && !sm.IsDeleted);

            if (medication == null)
            {
                return BaseResponse<BatchOperationResponse>.ErrorResult("Không tìm thấy thuốc học sinh.");
            }

            if (medication.Status != StudentMedicationStatus.Active &&
                medication.Status != StudentMedicationStatus.Approved)
            {
                return BaseResponse<BatchOperationResponse>.ErrorResult(
                    "Chỉ có thể tạo lịch trình cho thuốc đã được phê duyệt.");
            }

            var scheduleDates = GenerateScheduleDates(request);
            if (!scheduleDates.Any())
            {
                return BaseResponse<BatchOperationResponse>.ErrorResult(
                    "Không thể tạo lịch trình với thông tin đã cung cấp.");
            }

            var scheduleRepo = _unitOfWork.GetRepositoryByEntity<MedicationSchedule>();
            var newSchedules = new List<MedicationSchedule>();
            var errors = new List<string>();

            foreach (var date in scheduleDates)
            {
                foreach (var time in request.ScheduledTimes)
                {
                    try
                    {
                        var exists = await scheduleRepo.GetQueryable()
                            .AnyAsync(ms => ms.StudentMedicationId == request.StudentMedicationId &&
                                            ms.ScheduledDate.Date == date.Date &&
                                            ms.ScheduledTime == time && !ms.IsDeleted);

                        if (exists) continue;

                        var schedule = new MedicationSchedule
                        {
                            Id = Guid.NewGuid(),
                            StudentMedicationId = request.StudentMedicationId,
                            ScheduledDate = date,
                            ScheduledTime = time,
                            ScheduledDosage = medication.Dosage,
                            Status = MedicationScheduleStatus.Pending,
                            Priority = request.Priority,
                            RequiresNurseConfirmation = request.RequireNurseConfirmation,
                            SpecialInstructions = request.SpecialInstructions,
                            CreatedBy = "SYSTEM",
                            CreatedDate = DateTime.Now
                        };

                        newSchedules.Add(schedule);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Lỗi tạo lịch {date:dd/MM} {time}: {ex.Message}");
                    }
                }
            }

            if (newSchedules.Any())
            {
                await scheduleRepo.AddRangeAsync(newSchedules);
                await _unitOfWork.SaveChangesAsync();

                await InvalidateAllCachesAsync();
            }

            var response = new BatchOperationResponse
            {
                TotalRequested = scheduleDates.Count * request.ScheduledTimes.Count,
                SuccessCount = newSchedules.Count,
                FailureCount = errors.Count,
                Errors = errors,
                SuccessfulIds = newSchedules.Select(s => s.Id).ToList()
            };

            _logger.LogInformation("Created {Count} schedules for StudentMedication {StudentMedicationId}",
                newSchedules.Count, request.StudentMedicationId);

            return BaseResponse<BatchOperationResponse>.SuccessResult(response,
                $"Tạo lịch trình hoàn tất. Thành công: {newSchedules.Count}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating schedules");
            return BaseResponse<BatchOperationResponse>.ErrorResult($"Lỗi tạo lịch trình: {ex.Message}");
        }
    }

    #endregion

    #region Daily Views

    public async Task<BaseResponse<DailyMedicationScheduleResponse>> GetDailyScheduleAsync(
        DateTime date,
        Guid? studentId = null,
        MedicationScheduleStatus? status = null,
        bool includeCompleted = true)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(
                DAILY_SCHEDULE_PREFIX,
                date.ToString("yyyy-MM-dd"),
                studentId?.ToString() ?? "all",
                status?.ToString() ?? "all",
                includeCompleted.ToString()
            );

            var cachedResult = await _cacheService.GetAsync<BaseResponse<DailyMedicationScheduleResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Cache hit for daily schedule: {CacheKey}", cacheKey);
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<MedicationSchedule>()
                .GetQueryable()
                .Include(ms => ms.StudentMedication).ThenInclude(sm => sm.Student)
                .Include(ms => ms.StudentMedication).ThenInclude(sm => sm.Parent)
                .Include(ms => ms.Administration).ThenInclude(ma => ma.AdministeredBy)
                .Where(ms => ms.ScheduledDate.Date == date.Date && !ms.IsDeleted);

            if (studentId.HasValue)
                query = query.Where(ms => ms.StudentMedication.StudentId == studentId.Value);

            if (status.HasValue)
                query = query.Where(ms => ms.Status == status.Value);

            if (!includeCompleted)
                query = query.Where(ms => ms.Status != MedicationScheduleStatus.Completed);

            var schedules = await query
                .OrderBy(ms => ms.Priority).ThenBy(ms => ms.ScheduledTime)
                .ThenBy(ms => ms.StudentMedication.Student.FullName)
                .ToListAsync();

            var response = new DailyMedicationScheduleResponse
            {
                Date = date,
                Schedules = schedules.Select(s => _mapper.Map<MedicationScheduleResponse>(s)).ToList(),
                TotalScheduled = schedules.Count,
                Completed = schedules.Count(s => s.Status == MedicationScheduleStatus.Completed),
                Pending = schedules.Count(s => s.Status == MedicationScheduleStatus.Pending),
                Missed = schedules.Count(s => s.Status == MedicationScheduleStatus.Missed)
            };

            var result = BaseResponse<DailyMedicationScheduleResponse>.SuccessResult(response,
                "Lấy lịch trình hàng ngày thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(3));
            await _cacheService.AddToTrackingSetAsync(cacheKey, SCHEDULE_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily schedule");
            return BaseResponse<DailyMedicationScheduleResponse>.ErrorResult("Lỗi lấy lịch trình hàng ngày.");
        }
    }

    /// <summary>
    /// STUDENT: Xem lịch trình thuốc của chính mình
    /// </summary>
    public async Task<BaseResponse<List<MedicationScheduleResponse>>> GetMySchedulesAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        MedicationScheduleStatus? status = null)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BaseResponse<List<MedicationScheduleResponse>>.ErrorResult(
                    "Không thể xác định người dùng hiện tại.");
            }

            var currentUser = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>()
                .GetQueryable()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == currentUserId && !u.IsDeleted);

            if (currentUser == null || !currentUser.UserRoles.Any(ur => ur.Role.Name == "STUDENT"))
            {
                return BaseResponse<List<MedicationScheduleResponse>>.ErrorResult(
                    "Chỉ học sinh mới có thể xem lịch trình cá nhân.");
            }

            var start = startDate ?? DateTime.Today;
            var end = endDate ?? DateTime.Today.AddDays(7);

            var cacheKey = _cacheService.GenerateCacheKey(
                STUDENT_SCHEDULE_PREFIX,
                currentUserId.ToString(),
                start.ToString("yyyy-MM-dd"),
                end.ToString("yyyy-MM-dd"),
                status?.ToString() ?? "all"
            );

            var cachedResult = await _cacheService.GetAsync<BaseResponse<List<MedicationScheduleResponse>>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Cache hit for student schedules: {StudentId}", currentUserId);
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<MedicationSchedule>()
                .GetQueryable()
                .Include(ms => ms.StudentMedication).ThenInclude(sm => sm.Student)
                .Include(ms => ms.StudentMedication).ThenInclude(sm => sm.Parent)
                .Include(ms => ms.Administration).ThenInclude(ma => ma.AdministeredBy)
                .Where(ms => ms.StudentMedication.StudentId == currentUserId &&
                             ms.ScheduledDate >= start && ms.ScheduledDate <= end && !ms.IsDeleted);

            if (status.HasValue)
                query = query.Where(ms => ms.Status == status.Value);

            var schedules = await query
                .OrderBy(ms => ms.ScheduledDate).ThenBy(ms => ms.ScheduledTime)
                .ToListAsync();

            var responses = schedules.Select(s => _mapper.Map<MedicationScheduleResponse>(s)).ToList();

            var result = BaseResponse<List<MedicationScheduleResponse>>.SuccessResult(responses,
                "Lấy lịch trình thuốc cá nhân thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            await _cacheService.AddToTrackingSetAsync(cacheKey, SCHEDULE_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student schedules");
            return BaseResponse<List<MedicationScheduleResponse>>.ErrorResult("Lỗi lấy lịch trình cá nhân.");
        }
    }

    /// <summary>
    /// PARENT: Xem lịch trình thuốc của tất cả con em
    /// </summary>
    public async Task<BaseResponse<List<DailyMedicationScheduleResponse>>> GetChildrenSchedulesAsync(
        DateTime? startDate = null,
        int days = 7)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BaseResponse<List<DailyMedicationScheduleResponse>>.ErrorResult(
                    "Không thể xác định người dùng hiện tại.");
            }

            var currentUser = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>()
                .GetQueryable()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == currentUserId && !u.IsDeleted);

            if (currentUser == null || !currentUser.UserRoles.Any(ur => ur.Role.Name == "PARENT"))
            {
                return BaseResponse<List<DailyMedicationScheduleResponse>>.ErrorResult(
                    "Chỉ phụ huynh mới có thể xem lịch trình con em.");
            }

            var start = startDate ?? DateTime.Today;
            var end = start.AddDays(days - 1);

            var cacheKey = _cacheService.GenerateCacheKey(
                PARENT_SCHEDULE_PREFIX,
                currentUserId.ToString(),
                start.ToString("yyyy-MM-dd"),
                days.ToString()
            );

            var cachedResult =
                await _cacheService.GetAsync<BaseResponse<List<DailyMedicationScheduleResponse>>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Cache hit for parent schedules: {ParentId}", currentUserId);
                return cachedResult;
            }

            var children = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>()
                .GetQueryable()
                .Where(u => u.ParentId == currentUserId && !u.IsDeleted)
                .Select(u => u.Id)
                .ToListAsync();

            if (!children.Any())
            {
                var emptyResult = BaseResponse<List<DailyMedicationScheduleResponse>>.SuccessResult(
                    new List<DailyMedicationScheduleResponse>(), "Không có con em nào.");

                await _cacheService.SetAsync(cacheKey, emptyResult, TimeSpan.FromMinutes(5));
                await _cacheService.AddToTrackingSetAsync(cacheKey, SCHEDULE_CACHE_SET);

                return emptyResult;
            }

            var schedules = await _unitOfWork.GetRepositoryByEntity<MedicationSchedule>()
                .GetQueryable()
                .Include(ms => ms.StudentMedication).ThenInclude(sm => sm.Student)
                .Include(ms => ms.StudentMedication).ThenInclude(sm => sm.Parent)
                .Include(ms => ms.Administration).ThenInclude(ma => ma.AdministeredBy)
                .Where(ms => children.Contains(ms.StudentMedication.StudentId) &&
                             ms.ScheduledDate >= start && ms.ScheduledDate <= end && !ms.IsDeleted)
                .OrderBy(ms => ms.ScheduledDate).ThenBy(ms => ms.ScheduledTime)
                .ToListAsync();

            var dailySchedules = schedules
                .GroupBy(s => s.ScheduledDate.Date)
                .Select(g => new DailyMedicationScheduleResponse
                {
                    Date = g.Key,
                    Schedules = g.Select(s => _mapper.Map<MedicationScheduleResponse>(s)).ToList(),
                    TotalScheduled = g.Count(),
                    Completed = g.Count(s => s.Status == MedicationScheduleStatus.Completed),
                    Pending = g.Count(s => s.Status == MedicationScheduleStatus.Pending),
                    Missed = g.Count(s => s.Status == MedicationScheduleStatus.Missed)
                })
                .OrderBy(d => d.Date)
                .ToList();

            var response = BaseResponse<List<DailyMedicationScheduleResponse>>.SuccessResult(dailySchedules,
                "Lấy lịch trình con em thành công.");

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5));
            await _cacheService.AddToTrackingSetAsync(cacheKey, SCHEDULE_CACHE_SET);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting children schedules");
            return BaseResponse<List<DailyMedicationScheduleResponse>>.ErrorResult("Lỗi lấy lịch trình con em.");
        }
    }

    public async Task<BaseResponse<MedicationScheduleResponse>> GetScheduleDetailAsync(Guid scheduleId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BaseResponse<MedicationScheduleResponse>.ErrorResult("Không thể xác định người dùng hiện tại.");
            }

            var cacheKey = _cacheService.GenerateCacheKey(SCHEDULE_CACHE_PREFIX, scheduleId.ToString());

            var cachedResult = await _cacheService.GetAsync<BaseResponse<MedicationScheduleResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Cache hit for schedule detail: {ScheduleId}", scheduleId);

                var permissionCheckCached = await CheckSchedulePermissionAsync(scheduleId, currentUserId);
                if (!permissionCheckCached.Success)
                {
                    return BaseResponse<MedicationScheduleResponse>.ErrorResult(permissionCheckCached.Message);
                }

                return cachedResult;
            }

            var schedule = await _unitOfWork.GetRepositoryByEntity<MedicationSchedule>()
                .GetQueryable()
                .Include(ms => ms.StudentMedication).ThenInclude(sm => sm.Student)
                .Include(ms => ms.StudentMedication).ThenInclude(sm => sm.Parent)
                .Include(ms => ms.Administration).ThenInclude(ma => ma.AdministeredBy)
                .FirstOrDefaultAsync(ms => ms.Id == scheduleId && !ms.IsDeleted);

            if (schedule == null)
            {
                return BaseResponse<MedicationScheduleResponse>.ErrorResult("Không tìm thấy lịch trình.");
            }

            var permissionCheck = await CheckSchedulePermissionAsync(scheduleId, currentUserId);
            if (!permissionCheck.Success)
            {
                return BaseResponse<MedicationScheduleResponse>.ErrorResult(permissionCheck.Message);
            }

            var response = _mapper.Map<MedicationScheduleResponse>(schedule);

            var result = BaseResponse<MedicationScheduleResponse>.SuccessResult(response,
                "Lấy chi tiết lịch trình thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            await _cacheService.AddToTrackingSetAsync(cacheKey, SCHEDULE_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting schedule detail: {ScheduleId}", scheduleId);
            return BaseResponse<MedicationScheduleResponse>.ErrorResult("Lỗi lấy chi tiết lịch trình.");
        }
    }

    #endregion

    #region Schedule Actions

    public async Task<BaseResponse<AdministerScheduleResponse>> AdministerScheduleAsync(
        Guid scheduleId, AdministerScheduleRequest request)
    {
        try
        {
            var validationResult = await _administerValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BaseResponse<AdministerScheduleResponse>.ErrorResult(errors);
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BaseResponse<AdministerScheduleResponse>.ErrorResult(
                    "Không thể xác định người dùng hiện tại.");
            }

            var currentUser = await ValidateSchoolNursePermissions(currentUserId);
            if (currentUser == null)
            {
                return BaseResponse<AdministerScheduleResponse>.ErrorResult(
                    "Chỉ School Nurse mới có quyền cho uống thuốc.");
            }

            var schedule = await _unitOfWork.GetRepositoryByEntity<MedicationSchedule>()
                .GetQueryable()
                .Include(ms => ms.StudentMedication).ThenInclude(sm => sm.Student)
                .Include(ms => ms.StudentMedication).ThenInclude(sm => sm.Parent)
                .FirstOrDefaultAsync(ms => ms.Id == scheduleId && !ms.IsDeleted);

            if (schedule == null)
            {
                return BaseResponse<AdministerScheduleResponse>.ErrorResult("Không tìm thấy lịch trình.");
            }

            var validationCheckResult = ValidateScheduleForAdministration(schedule);
            if (!validationCheckResult.Success)
            {
                return BaseResponse<AdministerScheduleResponse>.ErrorResult(validationCheckResult.Message);
            }

            var administration = new MedicationAdministration
            {
                Id = Guid.NewGuid(),
                StudentMedicationId = schedule.StudentMedicationId,
                AdministeredById = currentUserId,
                AdministeredAt = DateTime.Now,
                ActualDosage = request.ActualDosage,
                Notes = request.Notes,
                StudentRefused = request.StudentRefused,
                RefusalReason = request.RefusalReason,
                SideEffectsObserved = request.SideEffectsObserved,
                CreatedBy = await GetCurrentUserRoleName(),
                CreatedDate = DateTime.Now
            };

            await _unitOfWork.GetRepositoryByEntity<MedicationAdministration>()
                .AddAsync(administration);

            schedule.Status = request.StudentRefused
                ? MedicationScheduleStatus.Missed
                : MedicationScheduleStatus.Completed;
            schedule.AdministrationId = administration.Id;
            schedule.CompletedAt = DateTime.Now;
            schedule.Notes = CombineNotes(schedule.Notes, request.Notes);
            schedule.LastUpdatedBy = await GetCurrentUserRoleName();
            schedule.LastUpdatedDate = DateTime.Now;

            if (!request.StudentRefused)
            {
                await UpdateMedicationStockAsync(schedule.StudentMedication);
            }

            await _unitOfWork.SaveChangesAsync();

            var response = new AdministerScheduleResponse
            {
                ScheduleId = schedule.Id,
                AdministrationId = administration.Id,
                MedicationName = schedule.StudentMedication.MedicationName,
                StudentName = schedule.StudentMedication.Student?.FullName ?? "",
                StudentCode = schedule.StudentMedication.Student?.StudentCode ?? "",
                ScheduledTime = schedule.ScheduledDate.Add(schedule.ScheduledTime),
                AdministeredAt = administration.AdministeredAt,
                ActualDosage = administration.ActualDosage,
                StudentRefused = administration.StudentRefused,
                RefusalReason = administration.RefusalReason,
                SideEffectsObserved = administration.SideEffectsObserved,
                Notes = administration.Notes,
                AdministeredByName = currentUser.FullName,
                Status = schedule.Status
            };

            ExecuteBackgroundTasksAfterAdministration(schedule, response, scheduleId);

            _logger.LogInformation(
                "Successfully administered schedule {ScheduleId} for student {StudentId}",
                scheduleId, schedule.StudentMedication.StudentId);

            return BaseResponse<AdministerScheduleResponse>.SuccessResult(
                response, "Cho uống thuốc và hoàn thành lịch trình thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error administering schedule: {ScheduleId}", scheduleId);
            return BaseResponse<AdministerScheduleResponse>.ErrorResult($"Lỗi cho uống thuốc: {ex.Message}");
        }
    }

    public async Task<BaseResponse<BulkAdministerResponse>> BulkAdministerSchedulesAsync(
        BulkAdministerRequest request)
    {
        try
        {
            var validationResult = await _bulkAdministerValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var listErrors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BaseResponse<BulkAdministerResponse>.ErrorResult(listErrors);
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BaseResponse<BulkAdministerResponse>.ErrorResult("Không thể xác định người dùng hiện tại.");
            }

            var currentUser = await ValidateSchoolNursePermissions(currentUserId);
            if (currentUser == null)
            {
                return BaseResponse<BulkAdministerResponse>.ErrorResult(
                    "Chỉ School Nurse mới có quyền cho uống thuốc.");
            }

            var successfulAdministrations = new List<AdministerScheduleResponse>();
            var errors = new List<string>();

            foreach (var item in request.Schedules)
            {
                try
                {
                    var scheduleRequest = new AdministerScheduleRequest
                    {
                        ActualDosage = item.ActualDosage,
                        Notes = item.Notes,
                        StudentRefused = item.StudentRefused,
                        RefusalReason = item.RefusalReason,
                        SideEffectsObserved = item.SideEffectsObserved
                    };

                    var result = await AdministerScheduleAsync(item.ScheduleId, scheduleRequest);

                    if (result.Success)
                    {
                        successfulAdministrations.Add(result.Data);
                    }
                    else
                    {
                        errors.Add($"Schedule {item.ScheduleId}: {result.Message}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Schedule {item.ScheduleId}: {ex.Message}");
                    _logger.LogError(ex, "Error processing schedule {ScheduleId} in bulk operation", item.ScheduleId);
                }
            }

            if (successfulAdministrations.Any())
            {
                ExecuteBulkBackgroundTasks();
            }

            var response = new BulkAdministerResponse
            {
                TotalRequested = request.Schedules.Count,
                SuccessCount = successfulAdministrations.Count,
                FailureCount = errors.Count,
                SuccessfulAdministrations = successfulAdministrations,
                Errors = errors
            };

            _logger.LogInformation("Bulk administered {Success}/{Total} schedules",
                successfulAdministrations.Count, request.Schedules.Count);

            return BaseResponse<BulkAdministerResponse>.SuccessResult(response,
                $"Xử lý hoàn tất: {successfulAdministrations.Count}/{request.Schedules.Count} thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk administer");
            return BaseResponse<BulkAdministerResponse>.ErrorResult($"Lỗi xử lý hàng loạt: {ex.Message}");
        }
    }

    public async Task<BaseResponse<MedicationScheduleResponse>> QuickCompleteScheduleAsync(
        Guid scheduleId, QuickCompleteRequest request)
    {
        try
        {
            var validationResult = await _quickCompleteValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BaseResponse<MedicationScheduleResponse>.ErrorResult(errors);
            }

            var schedule = await GetScheduleWithValidation(scheduleId);
            if (schedule == null)
                return BaseResponse<MedicationScheduleResponse>.ErrorResult("Không tìm thấy lịch trình.");

            if (schedule.Status != MedicationScheduleStatus.Pending)
                return BaseResponse<MedicationScheduleResponse>.ErrorResult(
                    "Chỉ có thể hoàn thành lịch trình đang chờ.");

            schedule.Status = MedicationScheduleStatus.Completed;
            schedule.CompletedAt = DateTime.Now;
            schedule.Notes = request.Notes ?? "Hoàn thành nhanh";
            schedule.LastUpdatedBy = await GetCurrentUserRoleName();
            schedule.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            await InvalidateAllCachesAsync();

            var response = _mapper.Map<MedicationScheduleResponse>(schedule);
            return BaseResponse<MedicationScheduleResponse>.SuccessResult(response,
                "Hoàn thành lịch trình thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error quick completing schedule: {ScheduleId}", scheduleId);
            return BaseResponse<MedicationScheduleResponse>.ErrorResult($"Lỗi hoàn thành nhanh: {ex.Message}");
        }
    }

    public async Task<BaseResponse<MedicationScheduleResponse>> MarkStudentAbsentAsync(
        MarkStudentAbsentRequest request)
    {
        try
        {
            var validationResult = await _absentValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BaseResponse<MedicationScheduleResponse>.ErrorResult(errors);
            }

            var schedule = await GetScheduleWithValidation(request.ScheduleId);
            if (schedule == null)
                return BaseResponse<MedicationScheduleResponse>.ErrorResult("Không tìm thấy lịch trình.");

            if (schedule.ScheduledDate.Date != DateTime.Today)
                return BaseResponse<MedicationScheduleResponse>.ErrorResult(
                    "Chỉ có thể đánh dấu vắng mặt cho lịch trình hôm nay.");

            if (schedule.Status != MedicationScheduleStatus.Pending)
                return BaseResponse<MedicationScheduleResponse>.ErrorResult(
                    "Chỉ có thể đánh dấu vắng mặt cho lịch trình đang chờ.");

            schedule.Status = MedicationScheduleStatus.StudentAbsent;
            schedule.StudentPresent = false;
            schedule.AttendanceCheckedAt = DateTime.Now;
            schedule.Notes = request.Notes ?? "Học sinh vắng mặt";
            schedule.LastUpdatedBy = await GetCurrentUserRoleName();
            schedule.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();

            await CreateStudentAbsentNotificationAsync(schedule);

            await InvalidateAllCachesAsync();

            var response = _mapper.Map<MedicationScheduleResponse>(schedule);
            return BaseResponse<MedicationScheduleResponse>.SuccessResult(response,
                "Đánh dấu học sinh vắng mặt thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking student absent");
            return BaseResponse<MedicationScheduleResponse>.ErrorResult($"Lỗi hệ thống: {ex.Message}");
        }
    }

    public async Task<BaseResponse<MedicationScheduleResponse>> MarkMissedAsync(
        MarkMissedMedicationRequest request)
    {
        try
        {
            var validationResult = await _markMissedValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BaseResponse<MedicationScheduleResponse>.ErrorResult(errors);
            }

            var schedule = await GetScheduleWithValidation(request.ScheduleId);
            if (schedule == null)
                return BaseResponse<MedicationScheduleResponse>.ErrorResult("Không tìm thấy lịch trình.");

            if (schedule.Status != MedicationScheduleStatus.Pending)
                return BaseResponse<MedicationScheduleResponse>.ErrorResult(
                    "Chỉ có thể đánh dấu bỏ lỡ lịch trình đang chờ.");

            schedule.Status = MedicationScheduleStatus.Missed;
            schedule.StudentPresent = true;
            schedule.MissedAt = DateTime.Now;
            schedule.MissedReason = request.MissedReason;
            schedule.Notes = request.Notes;
            schedule.LastUpdatedBy = await GetCurrentUserRoleName();
            schedule.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();

            await CreateMissedMedicationNotificationAsync(schedule);

            await InvalidateAllCachesAsync();

            _logger.LogWarning("Medication missed for student {StudentId}, medication {MedicationId}, reason: {Reason}",
                schedule.StudentMedication.StudentId,
                schedule.StudentMedicationId,
                request.MissedReason);

            var response = _mapper.Map<MedicationScheduleResponse>(schedule);
            return BaseResponse<MedicationScheduleResponse>.SuccessResult(response,
                "Đánh dấu bỏ lỡ thuốc thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking medication as missed");
            return BaseResponse<MedicationScheduleResponse>.ErrorResult($"Lỗi đánh dấu bỏ lỡ: {ex.Message}");
        }
    }

    #endregion

    #region Background Service Support

    public async Task<BaseResponse<BatchOperationResponse>> GenerateSchedulesForMedicationAsync
    (
        Guid studentMedicationId,
        DateTime? startDate = null,
        DateTime? endDate = null
    )
    {
        try
        {
            var medication = await _unitOfWork.GetRepositoryByEntity<StudentMedication>()
                .GetQueryable()
                .Include(sm => sm.Student)
                .Include(sm => sm.Schedules)
                .FirstOrDefaultAsync(sm => sm.Id == studentMedicationId && !sm.IsDeleted);

            if (medication == null)
            {
                return BaseResponse<BatchOperationResponse>.ErrorResult("Không tìm thấy thuốc học sinh.");
            }

            if (medication.Status != StudentMedicationStatus.Active)
            {
                return BaseResponse<BatchOperationResponse>.ErrorResult(
                    "Chỉ có thể tạo lịch trình cho thuốc ở trạng thái Active.");
            }

            var scheduleStartDate = startDate ?? DateTime.Today;
            var scheduleEndDate = endDate ?? medication.EndDate;

            var scheduleDates = GenerateSimpleScheduleDates(medication, scheduleStartDate, scheduleEndDate.Value);
            var scheduleTimes = GetSimpleScheduleTimes(medication);

            if (!scheduleDates.Any() || !scheduleTimes.Any())
            {
                return BaseResponse<BatchOperationResponse>.ErrorResult(
                    "Không thể tạo lịch trình với cài đặt hiện tại.");
            }

            var scheduleRepo = _unitOfWork.GetRepositoryByEntity<MedicationSchedule>();
            var newSchedules = new List<MedicationSchedule>();
            var errors = new List<string>();

            foreach (var date in scheduleDates)
            {
                foreach (var time in scheduleTimes)
                {
                    try
                    {
                        var exists = await scheduleRepo.GetQueryable()
                            .AnyAsync(ms => ms.StudentMedicationId == studentMedicationId &&
                                            ms.ScheduledDate.Date == date.Date &&
                                            ms.ScheduledTime == time && !ms.IsDeleted);

                        if (exists) continue;

                        var schedule = new MedicationSchedule
                        {
                            Id = Guid.NewGuid(),
                            StudentMedicationId = studentMedicationId,
                            ScheduledDate = date,
                            ScheduledTime = time,
                            ScheduledDosage = medication.Dosage,
                            Status = MedicationScheduleStatus.Pending,
                            Priority = medication.Priority,
                            RequiresNurseConfirmation = medication.RequireNurseConfirmation,
                            SpecialInstructions = medication.Instructions,
                            CreatedBy = "SYSTEM",
                            CreatedDate = DateTime.Now
                        };

                        newSchedules.Add(schedule);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Lỗi tạo lịch {date:dd/MM} {time}: {ex.Message}");
                    }
                }
            }

            if (newSchedules.Any())
            {
                await scheduleRepo.AddRangeAsync(newSchedules);
                await _unitOfWork.SaveChangesAsync();
                await InvalidateAllCachesAsync();
            }

            var response = new BatchOperationResponse
            {
                TotalRequested = scheduleDates.Count * scheduleTimes.Count,
                SuccessCount = newSchedules.Count,
                FailureCount = errors.Count,
                Errors = errors,
                SuccessfulIds = newSchedules.Select(s => s.Id).ToList()
            };

            _logger.LogInformation("Generated {Count} schedules for medication {MedicationId}",
                newSchedules.Count, studentMedicationId);

            return BaseResponse<BatchOperationResponse>.SuccessResult(response,
                $"Tạo lịch trình thành công: {newSchedules.Count}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating schedules for medication {MedicationId}", studentMedicationId);
            return BaseResponse<BatchOperationResponse>.ErrorResult($"Lỗi tạo lịch trình: {ex.Message}");
        }
    }

    private List<DateTime> GenerateSimpleScheduleDates(StudentMedication medication, DateTime startDate,
        DateTime endDate)
    {
        var dates = new List<DateTime>();
        var current = startDate.Date;
        var end = endDate.Date;

        while (current <= end)
        {
            if (medication.AutoGenerateSchedule)
            {
                bool isWeekend = current.DayOfWeek == DayOfWeek.Saturday || current.DayOfWeek == DayOfWeek.Sunday;
                if (medication.SkipWeekends && isWeekend)
                {
                    current = current.AddDays(1);
                    continue;
                }

                if (!string.IsNullOrEmpty(medication.SkipDates))
                {
                    try
                    {
                        var skipDates = System.Text.Json.JsonSerializer.Deserialize<List<string>>(medication.SkipDates);
                        var dateString = current.ToString("yyyy-MM-dd");
                        if (skipDates?.Contains(dateString) == true)
                        {
                            current = current.AddDays(1);
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing SkipDates for medication {MedicationId}", medication.Id);
                    }
                }

                dates.Add(current);
            }

            current = current.AddDays(1);
        }

        return dates;
    }

    private List<TimeSpan> GetSimpleScheduleTimes(StudentMedication medication)
    {
        var times = new List<TimeSpan>();

        if (!string.IsNullOrEmpty(medication.SpecificTimes))
        {
            try
            {
                var timeStrings = System.Text.Json.JsonSerializer.Deserialize<List<string>>(medication.SpecificTimes);
                if (timeStrings != null && timeStrings.Any())
                {
                    foreach (var timeString in timeStrings)
                    {
                        if (TimeSpan.TryParse(timeString, out var timeSpan))
                        {
                            times.Add(timeSpan);
                        }
                    }

                    if (times.Any())
                        return times.OrderBy(t => t).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Error parsing SpecificTimes for medication {MedicationId}, fallback to TimeOfDay", medication.Id);
            }
        }

        return MapTimeOfDayToTimeSpan(medication.TimeOfDay);
    }

    public async Task<BaseResponse<BatchOperationResponse>> AutoMarkOverdueSchedulesAsync()
    {
        try
        {
            var currentTime = DateTime.Now;
            var overdueThreshold = currentTime.AddHours(-1);

            var pendingSchedules = await _unitOfWork.GetRepositoryByEntity<MedicationSchedule>()
                .GetQueryable()
                .Where(ms => ms.Status == MedicationScheduleStatus.Pending && !ms.IsDeleted)
                .ToListAsync();

            var overdueSchedules = pendingSchedules
                .Where(ms => ms.ScheduledDate.Add(ms.ScheduledTime) <= overdueThreshold)
                .ToList();

            var successCount = 0;
            var errors = new List<string>();

            foreach (var schedule in overdueSchedules)
            {
                try
                {
                    schedule.Status = MedicationScheduleStatus.Missed;
                    schedule.MissedAt = DateTime.Now;
                    schedule.MissedReason = "Tự động đánh dấu - quá thời gian quy định";
                    schedule.LastUpdatedBy = "SYSTEM";
                    schedule.LastUpdatedDate = DateTime.Now;
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Lỗi cập nhật schedule {schedule.Id}: {ex.Message}");
                }
            }

            if (successCount > 0)
            {
                await _unitOfWork.SaveChangesAsync();
                await InvalidateAllCachesAsync();
            }

            var response = new BatchOperationResponse
            {
                TotalRequested = overdueSchedules.Count,
                SuccessCount = successCount,
                FailureCount = errors.Count,
                Errors = errors,
                SuccessfulIds = overdueSchedules.Take(successCount).Select(s => s.Id).ToList()
            };

            _logger.LogInformation("Auto-marked {Count} overdue schedules", successCount);

            return BaseResponse<BatchOperationResponse>.SuccessResult(response,
                $"Tự động đánh dấu quá hạn: {successCount}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-marking overdue schedules");
            return BaseResponse<BatchOperationResponse>.ErrorResult($"Lỗi tự động đánh dấu quá hạn: {ex.Message}");
        }
    }

    public async Task<BaseResponse<CleanupOperationResponse>> CleanupOldSchedulesAsync(int daysOld)
    {
        try
        {
            var cutoffDate = DateTime.Today.AddDays(-daysOld);

            var oldSchedules = await _unitOfWork.GetRepositoryByEntity<MedicationSchedule>()
                .GetQueryable()
                .Where(ms => ms.ScheduledDate < cutoffDate &&
                             (ms.Status == MedicationScheduleStatus.Completed ||
                              ms.Status == MedicationScheduleStatus.Missed ||
                              ms.Status == MedicationScheduleStatus.Cancelled) && !ms.IsDeleted)
                .ToListAsync();

            var deletedCount = 0;

            foreach (var schedule in oldSchedules)
            {
                schedule.IsDeleted = true;
                schedule.LastUpdatedBy = "SYSTEM_CLEANUP";
                schedule.LastUpdatedDate = DateTime.Now;
                deletedCount++;
            }

            if (deletedCount > 0)
            {
                await _unitOfWork.SaveChangesAsync();
                await InvalidateAllCachesAsync();
            }

            var response = new CleanupOperationResponse
            {
                RecordsProcessed = oldSchedules.Count,
                RecordsDeleted = deletedCount,
                CleanupDate = DateTime.Now
            };

            _logger.LogInformation("Cleanup: {Deleted} records deleted", deletedCount);

            return BaseResponse<CleanupOperationResponse>.SuccessResult(response, "Dọn dẹp dữ liệu cũ thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old schedules");
            return BaseResponse<CleanupOperationResponse>.ErrorResult($"Lỗi dọn dẹp dữ liệu: {ex.Message}");
        }
    }

    #endregion

    #region Helper Methods

    private void ExecuteBackgroundTasksAfterAdministration(MedicationSchedule schedule,
        AdministerScheduleResponse response, Guid scheduleId)
    {
        var backgroundAction = new Action(async () =>
        {
            try
            {
                await CreateAdministrationNotificationsAsync(schedule, response);
                await InvalidateAllCachesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background task after administering schedule {ScheduleId}", scheduleId);
            }
        });

        Task.Run(backgroundAction);
    }

    private void ExecuteBulkBackgroundTasks()
    {
        var backgroundAction = new Action(async () =>
        {
            try
            {
                await InvalidateAllCachesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating caches after bulk administration");
            }
        });

        Task.Run(backgroundAction);
    }

    private List<DateTime> GenerateScheduleDates(CreateMedicationScheduleRequest request)
    {
        var dates = new List<DateTime>();
        var current = request.StartDate.Date;
        var end = request.EndDate.Date;

        while (current <= end)
        {
            var shouldInclude = request.FrequencyType switch
            {
                MedicationFrequencyType.Daily => true,
                MedicationFrequencyType.EveryOtherDay => (current - request.StartDate.Date).Days % 2 == 0,
                MedicationFrequencyType.Weekly => current.DayOfWeek == request.StartDate.DayOfWeek,
                MedicationFrequencyType.SpecificDays => request.SpecificDays?.Contains(current.DayOfWeek) == true,
                MedicationFrequencyType.BiWeekly => (current - request.StartDate.Date).Days % 14 == 0,
                MedicationFrequencyType.Monthly => current.Day == request.StartDate.Day,
                _ => false
            };

            if (shouldInclude && (!request.SkipWeekends ||
                                  (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)))
            {
                dates.Add(current);
            }

            current = current.AddDays(1);
        }

        return dates;
    }

    private async Task<MedicationSchedule?> GetScheduleWithValidation(Guid scheduleId)
    {
        return await _unitOfWork.GetRepositoryByEntity<MedicationSchedule>()
            .GetQueryable()
            .Include(ms => ms.StudentMedication).ThenInclude(sm => sm.Student)
            .Include(ms => ms.StudentMedication).ThenInclude(sm => sm.Parent)
            .FirstOrDefaultAsync(ms => ms.Id == scheduleId && !ms.IsDeleted);
    }

    private async Task<string> GetCurrentUserRoleName()
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty) return "SYSTEM";

            var user = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>()
                .GetQueryable()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == currentUserId && !u.IsDeleted);

            return user?.UserRoles?.FirstOrDefault()?.Role?.Name ?? "SYSTEM";
        }
        catch
        {
            return "SYSTEM";
        }
    }

    private Guid GetCurrentUserId()
    {
        try
        {
            return Guid.Parse(UserHelper.GetCurrentUserId(_httpContextAccessor.HttpContext));
        }
        catch
        {
            return Guid.Empty;
        }
    }

    private async Task<(bool Success, string Message)> CheckSchedulePermissionAsync(Guid scheduleId, Guid currentUserId)
    {
        try
        {
            var currentUser = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>()
                .GetQueryable()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == currentUserId && !u.IsDeleted);

            if (currentUser == null)
            {
                return (false, "Không tìm thấy thông tin người dùng.");
            }

            var userRole = currentUser.UserRoles.FirstOrDefault()?.Role?.Name;

            if (userRole == "SCHOOLNURSE" || userRole == "MANAGER")
            {
                return (true, "");
            }

            var schedule = await _unitOfWork.GetRepositoryByEntity<MedicationSchedule>()
                .GetQueryable()
                .Include(ms => ms.StudentMedication)
                .FirstOrDefaultAsync(ms => ms.Id == scheduleId && !ms.IsDeleted);

            if (schedule == null)
            {
                return (false, "Không tìm thấy lịch trình.");
            }

            if (userRole == "STUDENT")
            {
                if (schedule.StudentMedication.StudentId == currentUserId)
                {
                    return (true, "");
                }

                return (false, "Bạn chỉ có thể xem lịch trình thuốc của chính mình.");
            }

            if (userRole == "PARENT")
            {
                if (schedule.StudentMedication.ParentId == currentUserId)
                {
                    return (true, "");
                }

                return (false, "Bạn chỉ có thể xem lịch trình thuốc của con em mình.");
            }

            return (false, "Không có quyền truy cập.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking schedule permission");
            return (false, "Lỗi kiểm tra quyền truy cập.");
        }
    }

    private List<TimeSpan> MapTimeOfDayToTimeSpan(MedicationTimeOfDay timeOfDay)
    {
        return timeOfDay switch
        {
            MedicationTimeOfDay.BeforeBreakfast => new List<TimeSpan> { new TimeSpan(7, 0, 0) },
            MedicationTimeOfDay.AfterBreakfast => new List<TimeSpan> { new TimeSpan(8, 30, 0) },
            MedicationTimeOfDay.BeforeLunch => new List<TimeSpan> { new TimeSpan(11, 30, 0) },
            MedicationTimeOfDay.AfterLunch => new List<TimeSpan> { new TimeSpan(13, 0, 0) },
            MedicationTimeOfDay.BeforeDinner => new List<TimeSpan> { new TimeSpan(17, 30, 0) },
            MedicationTimeOfDay.AfterDinner => new List<TimeSpan> { new TimeSpan(19, 0, 0) },
            MedicationTimeOfDay.BeforeBed => new List<TimeSpan> { new TimeSpan(21, 0, 0) },
            _ => new List<TimeSpan> { new TimeSpan(8, 0, 0) }
        };
    }

    private async Task InvalidateAllCachesAsync()
    {
        try
        {
            await Task.WhenAll(
                _cacheService.RemoveByPrefixAsync(SCHEDULE_CACHE_PREFIX),
                _cacheService.RemoveByPrefixAsync(SCHEDULE_LIST_PREFIX),
                _cacheService.RemoveByPrefixAsync(DAILY_SCHEDULE_PREFIX),
                _cacheService.RemoveByPrefixAsync(STUDENT_SCHEDULE_PREFIX),
                _cacheService.RemoveByPrefixAsync(PARENT_SCHEDULE_PREFIX),
                _cacheService.RemoveByPrefixAsync(SCHEDULE_STATISTICS_PREFIX),
                _cacheService.InvalidateTrackingSetAsync(SCHEDULE_CACHE_SET)
            );

            _logger.LogDebug("Successfully invalidated all medication schedule caches");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating medication schedule caches");
        }
    }

    private (bool Success, string Message) ValidateScheduleForAdministration(MedicationSchedule schedule)
    {
        if (schedule.Status != MedicationScheduleStatus.Pending)
            return (false, "Chỉ có thể cho uống thuốc cho lịch trình đang chờ.");

        if (schedule.StudentMedication.Status != StudentMedicationStatus.Active)
            return (false, "Thuốc không ở trạng thái Active.");

        var today = DateTime.Today;
        if (today < schedule.StudentMedication.StartDate || today > schedule.StudentMedication.EndDate)
            return (false, "Không trong thời gian cho phép uống thuốc.");

        if (schedule.StudentMedication.ExpiryDate <= today)
            return (false, "Thuốc đã hết hạn sử dụng.");

        if (schedule.StudentMedication.RemainingDoses <= 0)
        {
            return (true, "WARNING: Thuốc sắp hết, vui lòng thông báo Parent gửi thêm.");
        }

        return (true, "");
    }

    private async Task UpdateMedicationStockAsync(StudentMedication medication)
    {
        if (medication.RemainingDoses > 0)
        {
            medication.RemainingDoses--;
            medication.LastUpdatedBy = "SYSTEM";
            medication.LastUpdatedDate = DateTime.Now;

            if (medication.RemainingDoses <= medication.MinStockThreshold && !medication.LowStockAlertSent)
            {
                medication.LowStockAlertSent = true;

                var lowStockAction = new Action(async () =>
                {
                    try
                    {
                        await CreateEnhancedLowStockNotificationAsync(medication);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating low stock notification");
                    }
                });

                Task.Run(lowStockAction);
            }
        }
        else
        {
            _logger.LogWarning("Medication {MedicationId} has 0 remaining doses but was administered - emergency case",
                medication.Id);
        }
    }

    private string CombineNotes(string existingNotes, string newNotes)
    {
        if (string.IsNullOrEmpty(existingNotes)) return newNotes ?? "";
        if (string.IsNullOrEmpty(newNotes)) return existingNotes;
        return $"{existingNotes}\n---\n{newNotes}";
    }

    private async Task<ApplicationUser> ValidateSchoolNursePermissions(Guid userId)
    {
        try
        {
            var user = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>()
                .GetQueryable()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            if (user == null) return null;

            var validRoles = new[] { "SCHOOLNURSE", "ADMIN", "MANAGER" };
            return user.UserRoles.Any(ur => validRoles.Contains(ur.Role.Name.ToUpper())) ? user : null;
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Notification Methods

    private async Task CreateAdministrationNotificationsAsync(MedicationSchedule schedule,
        AdministerScheduleResponse response)
    {
        try
        {
            if (schedule.StudentMedication?.ParentId == null) return;

            var shouldNotify = !string.IsNullOrEmpty(response.SideEffectsObserved) ||
                               response.StudentRefused ||
                               schedule.StudentMedication.Priority >= MedicationPriority.High;

            if (shouldNotify)
            {
                var title = response.StudentRefused
                    ? $"⚠️ Học sinh từ chối uống thuốc - {schedule.StudentMedication.Student?.FullName}"
                    : !string.IsNullOrEmpty(response.SideEffectsObserved)
                        ? $"Quan sát tác dụng phụ - {schedule.StudentMedication.Student?.FullName}"
                        : $"Đã cho uống thuốc - {schedule.StudentMedication.Student?.FullName}";

                var content = response.StudentRefused
                    ? $"Con em {schedule.StudentMedication.Student?.FullName} đã từ chối uống thuốc " +
                      $"'{schedule.StudentMedication.MedicationName}' vào lúc {response.AdministeredAt:HH:mm dd/MM/yyyy}. " +
                      $"Lý do: {response.RefusalReason}"
                    : !string.IsNullOrEmpty(response.SideEffectsObserved)
                        ? $"Khi cho con em uống thuốc '{schedule.StudentMedication.MedicationName}', " +
                          $"y tá đã quan sát thấy: {response.SideEffectsObserved}"
                        : $"Con em {schedule.StudentMedication.Student?.FullName} đã uống thuốc " +
                          $"'{schedule.StudentMedication.MedicationName}' thành công vào lúc {response.AdministeredAt:HH:mm}." +
                          $"\n\nThông tin chi tiết:" +
                          $"\n• Liều lượng: {response.ActualDosage}" +
                          $"\n• Số liều còn lại: {schedule.StudentMedication.RemainingDoses}" +
                          $"\n• Y tá thực hiện: {response.AdministeredByName}";

                if (schedule.StudentMedication.RemainingDoses <= schedule.StudentMedication.MinStockThreshold)
                {
                    content += $"\n\n⚠️ LƯU Ý: Thuốc sắp hết (còn {schedule.StudentMedication.RemainingDoses} liều). " +
                               "Vui lòng chuẩn bị gửi thêm thuốc.";
                }

                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    Content = content,
                    NotificationType = NotificationType.General,
                    SenderId = response.AdministrationId,
                    RecipientId = schedule.StudentMedication.ParentId,
                    RequiresConfirmation =
                        response.StudentRefused || !string.IsNullOrEmpty(response.SideEffectsObserved),
                    IsRead = false,
                    CreatedDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(7)
                };

                await _unitOfWork.GetRepositoryByEntity<Notification>().AddAsync(notification);
                await _unitOfWork.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating administration notification");
        }
    }

    private async Task CreateEnhancedLowStockNotificationAsync(StudentMedication medication)
    {
        try
        {
            var estimatedDaysLeft = EstimateDaysLeft(medication.RemainingDoses, medication.Frequency);
            var urgency = GetStockUrgency(medication.RemainingDoses, medication.MinStockThreshold);

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = $"{urgency.Icon} Thuốc sắp hết - {medication.Student?.FullName}",
                Content = $"THÔNG BÁO SẮP HẾT THUỐC\n\n" +
                          $"Thuốc: {medication.MedicationName}\n" +
                          $"Học sinh: {medication.Student?.FullName} ({medication.Student?.StudentCode})\n\n" +
                          $"Tình trạng hiện tại:\n" +
                          $"• Số liều còn lại: {medication.RemainingDoses}/{medication.TotalDoses}\n" +
                          $"• Ngưỡng cảnh báo: {medication.MinStockThreshold} liều\n" +
                          $"• Ước tính đủ dùng: {estimatedDaysLeft}\n" +
                          $"• Số lượng đã gửi: {medication.QuantitySent} {medication.QuantityUnit}\n" +
                          $"• Ngày hết hạn: {medication.ExpiryDate:dd/MM/yyyy}\n\n" +
                          $" {urgency.Action}",
                NotificationType = NotificationType.General,
                SenderId = null,
                RecipientId = medication.ParentId,
                RequiresConfirmation = true,
                IsRead = false,
                CreatedDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(7)
            };

            await _unitOfWork.GetRepositoryByEntity<Notification>().AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating enhanced low stock notification");
        }
    }

    private async Task CreateStudentAbsentNotificationAsync(MedicationSchedule schedule)
    {
        try
        {
            if (schedule.StudentMedication?.ParentId == null) return;

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = "Thông báo - Con em vắng mặt",
                Content = $"Con em {schedule.StudentMedication.Student?.FullName} " +
                          $"vắng mặt hôm nay ({schedule.ScheduledDate:dd/MM/yyyy}) nên chưa thể uống thuốc " +
                          $"'{schedule.StudentMedication.MedicationName}' vào lúc {schedule.ScheduledTime:hh\\:mm}.\n\n" +
                          $"Điều này là bình thường khi con em nghỉ học. " +
                          $"Vui lòng cho con em uống thuốc tại nhà theo hướng dẫn của bác sĩ.",
                NotificationType = NotificationType.General,
                SenderId = null,
                RecipientId = schedule.StudentMedication.ParentId,
                RequiresConfirmation = false,
                IsRead = false,
                CreatedDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(2)
            };

            await _unitOfWork.GetRepositoryByEntity<Notification>().AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Created student absent notification for schedule {ScheduleId}", schedule.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student absent notification");
        }
    }

    private async Task CreateMissedMedicationNotificationAsync(MedicationSchedule schedule)
    {
        try
        {
            if (schedule.StudentMedication?.ParentId == null) return;

            var urgencyLevel = GetMissedMedicationUrgency(schedule.StudentMedication.Priority);

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = $"{urgencyLevel.Icon} CẢNH BÁO - Con em bỏ lỡ thuốc",
                Content = $"Con em {schedule.StudentMedication.Student?.FullName} " +
                          $"({schedule.StudentMedication.Student?.StudentCode}) đã bỏ lỡ việc uống thuốc " +
                          $"'{schedule.StudentMedication.MedicationName}' vào lúc {schedule.ScheduledTime:hh\\:mm} " +
                          $"ngày {schedule.ScheduledDate:dd/MM/yyyy}.\n\n" +
                          $"Lý do: {schedule.MissedReason}\n" +
                          $"⚡ Mức độ: {urgencyLevel.Text}\n\n" +
                          $"{urgencyLevel.ActionRequired}",
                NotificationType = NotificationType.General,
                SenderId = null,
                RecipientId = schedule.StudentMedication.ParentId,
                RequiresConfirmation = true,
                IsRead = false,
                CreatedDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(7)
            };

            await _unitOfWork.GetRepositoryByEntity<Notification>().AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Created missed medication notification for schedule {ScheduleId}", schedule.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating missed medication notification");
        }
    }

    private (string Icon, string Text, string ActionRequired) GetMissedMedicationUrgency(
        MedicationPriority priority)
    {
        return priority switch
        {
            MedicationPriority.Critical => (
                "🚨",
                "RẤT NGHIÊM TRỌNG",
                "Vui lòng LIÊN HỆ NGAY bác sĩ điều trị để được tư vấn về việc uống bù liều thuốc."
            ),
            MedicationPriority.High => (
                "⚠️",
                "QUAN TRỌNG",
                "Khuyến nghị liên hệ bác sĩ hoặc dược sĩ để được tư vấn về liều thuốc tiếp theo."
            ),
            MedicationPriority.Normal => (
                "📋",
                "Chú ý",
                "Vui lòng theo dõi và đảm bảo con em uống đủ các liều thuốc tiếp theo."
            ),
            MedicationPriority.Low => (
                "ℹ️",
                "Thông tin",
                "Tiếp tục theo dõi lịch trình uống thuốc của con em."
            ),
            _ => ("📋", "Không xác định", "Vui lòng liên hệ y tá trường để biết thêm chi tiết.")
        };
    }

    private string EstimateDaysLeft(int remainingDoses, string frequency)
    {
        try
        {
            var freq = frequency.ToLower();

            if (freq.Contains("1 lần/ngày") || freq.Contains("hàng ngày"))
                return $"khoảng {remainingDoses} ngày";
            else if (freq.Contains("2 lần/ngày"))
                return $"khoảng {Math.Max(1, remainingDoses / 2)} ngày";
            else if (freq.Contains("3 lần/ngày"))
                return $"khoảng {Math.Max(1, remainingDoses / 3)} ngày";
            else if (freq.Contains("khi cần"))
                return "tùy theo tình trạng sử dụng";

            return $"khoảng {Math.Max(1, remainingDoses / 2)} ngày";
        }
        catch
        {
            return "không xác định";
        }
    }

    private (string Icon, string Action) GetStockUrgency(int remaining, int threshold)
    {
        if (remaining <= 1)
            return ("🚨", "VUI LÒNG GỬI THUỐC NGAY HÔM NAY!");

        if (remaining <= threshold / 2)
            return ("⚠️", "Khuyến nghị gửi thuốc trong 1-2 ngày tới.");

        return ("📋", "Chuẩn bị thuốc để gửi trong vài ngày tới.");
    }

    #endregion
}