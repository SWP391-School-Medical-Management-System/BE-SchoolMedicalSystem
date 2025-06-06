using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.NotificationResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services;

public class NotificationService : INotificationService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<NotificationService> _logger;

    private const string NOTIFICATION_CACHE_PREFIX = "notification";
    private const string NOTIFICATION_LIST_PREFIX = "notifications_list";

    public NotificationService(
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<NotificationService> logger)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    #region Read Operations

    public async Task<BaseListResponse<NotificationResponse>> GetAllNotificationsAsync(
        Guid userId,
        int pageIndex = 1,
        int pageSize = 10,
        string searchTerm = null,
        bool? isRead = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(
                NOTIFICATION_LIST_PREFIX,
                userId.ToString(),
                pageIndex.ToString(),
                pageSize.ToString(),
                searchTerm ?? "",
                isRead?.ToString() ?? ""
            );

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<NotificationResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Notifications found in cache for user: {UserId}", userId);
                return cachedResult;
            }

            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();
            var query = notificationRepo.GetQueryable()
                .Include(n => n.Sender)
                .Where(n => n.RecipientId == userId && !n.IsDeleted);

            // Apply filters
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(n => n.Title.Contains(searchTerm) || n.Content.Contains(searchTerm));
            }

            if (isRead.HasValue)
            {
                query = query.Where(n => n.IsRead == isRead.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var unreadCount = await query.Where(n => !n.IsRead).CountAsync(cancellationToken);

            var notifications = await query
                .OrderByDescending(n => n.CreatedDate)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = notifications.Select(MapToNotificationResponse).ToList();

            var result = BaseListResponse<NotificationResponse>.SuccessResult(
                responses,
                unreadCount,
                pageSize,
                pageIndex,
                "Lấy danh sách thông báo thành công");

            result.TotalCount = totalCount;

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications for user: {UserId}", userId);
            return BaseListResponse<NotificationResponse>.ErrorResult("Lỗi lấy danh sách thông báo");
        }
    }

    public async Task<BaseListResponse<NotificationResponse>> GetPopUpNotificationsAsync(Guid userId)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey("popup_notifications", userId.ToString());
            var cachedResult = await _cacheService.GetAsync<BaseListResponse<NotificationResponse>>(cacheKey);

            if (cachedResult != null)
            {
                return cachedResult;
            }

            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();
            var notifications = await notificationRepo.GetQueryable()
                .Include(n => n.Sender)
                .Where(n => n.RecipientId == userId &&
                            !n.IsDeleted &&
                            !n.IsDismissed)
                .OrderByDescending(n => n.CreatedDate)
                .Take(20)
                .ToListAsync();

            var responses = notifications.Select(MapToNotificationResponse).ToList();

            var result = BaseListResponse<NotificationResponse>.SuccessResult(
                responses,
                responses.Count,
                responses.Count,
                1,
                "Lấy popup notifications thành công");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popup notifications for user: {UserId}", userId);
            return BaseListResponse<NotificationResponse>.ErrorResult("Lỗi lấy popup notifications");
        }
    }

    public async Task<BaseResponse<int>> GetUnreadCountAsync(Guid userId)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey("unread_count", userId.ToString());
            var cachedCount = await _cacheService.GetAsync<BaseResponse<int>>(cacheKey);

            if (cachedCount != null)
            {
                return cachedCount;
            }

            var unreadCount = await _unitOfWork.GetRepositoryByEntity<Notification>().GetQueryable()
                .CountAsync(n => n.RecipientId == userId && !n.IsRead && !n.IsDeleted);

            var result = BaseResponse<int>.SuccessResult(unreadCount, "Lấy số thông báo chưa đọc thành công");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(1));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for user: {UserId}", userId);
            return BaseResponse<int>.ErrorResult("Lỗi lấy số thông báo chưa đọc");
        }
    }

    public async Task<BaseResponse<List<NotificationResponse>>> GetSevereConditionAlertsForNurseAsync(Guid nurseId)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey("severe_condition_alerts", nurseId.ToString());
            var cachedResult = await _cacheService.GetAsync<BaseResponse<List<NotificationResponse>>>(cacheKey);

            if (cachedResult != null)
            {
                return cachedResult;
            }

            var medicalConditionRepo = _unitOfWork.GetRepositoryByEntity<MedicalCondition>();
            var severeConditions = await medicalConditionRepo.GetQueryable()
                .Include(mc => mc.MedicalRecord)
                .ThenInclude(mr => mr.Student)
                .Where(mc => mc.Severity == SeverityType.Severe && !mc.IsDeleted)
                .OrderByDescending(mc => mc.CreatedDate)
                .ToListAsync();

            var alerts = new List<NotificationResponse>();

            foreach (var condition in severeConditions)
            {
                var student = condition.MedicalRecord?.Student;
                if (student != null)
                {
                    alerts.Add(new NotificationResponse
                    {
                        Id = Guid.NewGuid(),
                        Title = $"⚠️ CẢNH BÁO: Tình trạng y tế nghiêm trọng",
                        Content =
                            $"Học sinh {student.FullName} ({student.StudentCode}) có tình trạng y tế nghiêm trọng: {condition.Name}. " +
                            $"Điều trị: {condition.Treatment ?? "Chưa có"}. " +
                            $"Thuốc: {condition.Medication ?? "Chưa có"}.",
                        NotificationType = NotificationType.General,
                        CreatedDate = condition.CreatedDate,
                        IsRead = false,
                        StudentName = student.FullName,
                        StudentCode = student.StudentCode,
                        MedicalConditionName = condition.Name,
                        Severity = GetSeverityDisplayName(condition.Severity)
                    });
                }
            }

            var result = BaseResponse<List<NotificationResponse>>.SuccessResult(
                alerts, $"Tìm thấy {alerts.Count} cảnh báo tình trạng y tế nghiêm trọng");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting severe condition alerts for nurse: {NurseId}", nurseId);
            return BaseResponse<List<NotificationResponse>>.ErrorResult(
                "Lỗi lấy cảnh báo tình trạng y tế nghiêm trọng");
        }
    }

    #endregion

    #region Update Operations (Simple)

    public async Task<BaseResponse<bool>> MarkNotificationAsReadAsync(Guid notificationId, Guid userId)
    {
        try
        {
            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();
            var notification = await notificationRepo.GetQueryable()
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientId == userId && !n.IsDeleted);

            if (notification == null)
            {
                return BaseResponse<bool>.ErrorResult("Không tìm thấy thông báo");
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
                notification.LastUpdatedDate = DateTime.Now;

                await _unitOfWork.SaveChangesAsync();
                await InvalidateNotificationCacheAsync(userId);

                _logger.LogInformation("Notification {NotificationId} marked as read by user {UserId}",
                    notificationId, userId);
            }

            return BaseResponse<bool>.SuccessResult(true, "Đánh dấu đã đọc thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read: {NotificationId}", notificationId);
            return BaseResponse<bool>.ErrorResult("Lỗi đánh dấu thông báo đã đọc");
        }
    }

    public async Task<BaseResponse<bool>> MarkAllNotificationsAsReadAsync(Guid userId)
    {
        try
        {
            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();
            var unreadNotifications = await notificationRepo.GetQueryable()
                .Where(n => n.RecipientId == userId && !n.IsRead && !n.IsDeleted)
                .ToListAsync();

            if (!unreadNotifications.Any())
            {
                return BaseResponse<bool>.SuccessResult(true, "Không có thông báo chưa đọc");
            }

            var updateTime = DateTime.Now;
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = updateTime;
                notification.LastUpdatedDate = updateTime;
            }

            await _unitOfWork.SaveChangesAsync();
            await InvalidateNotificationCacheAsync(userId);

            _logger.LogInformation("Marked {Count} notifications as read for user {UserId}",
                unreadNotifications.Count, userId);

            return BaseResponse<bool>.SuccessResult(true,
                $"Đánh dấu {unreadNotifications.Count} thông báo đã đọc thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user: {UserId}", userId);
            return BaseResponse<bool>.ErrorResult("Lỗi đánh dấu tất cả thông báo đã đọc");
        }
    }

    public async Task<BaseResponse<bool>> DismissAllNotificationsAsync(Guid userId)
    {
        try
        {
            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();
            var notifications = await notificationRepo.GetQueryable()
                .Where(n => n.RecipientId == userId && !n.IsDeleted && !n.IsDismissed)
                .ToListAsync();

            if (!notifications.Any())
            {
                return BaseResponse<bool>.SuccessResult(true, "Không có thông báo để dismiss");
            }

            var dismissTime = DateTime.Now;
            foreach (var notification in notifications)
            {
                notification.IsDismissed = true;
                notification.DismissedAt = dismissTime;
                notification.LastUpdatedDate = dismissTime;
            }

            await _unitOfWork.SaveChangesAsync();
            await InvalidateNotificationCacheAsync(userId);

            _logger.LogInformation("Dismissed {Count} notifications for user {UserId}",
                notifications.Count, userId);

            return BaseResponse<bool>.SuccessResult(true,
                $"Dismiss {notifications.Count} thông báo thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dismissing all notifications for user: {UserId}", userId);
            return BaseResponse<bool>.ErrorResult("Lỗi dismiss thông báo");
        }
    }

    public async Task<BaseResponse<bool>> DismissNotificationAsync(Guid notificationId, Guid userId)
    {
        try
        {
            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();
            var notification = await notificationRepo.GetQueryable()
                .FirstOrDefaultAsync(n => n.Id == notificationId &&
                                          n.RecipientId == userId &&
                                          !n.IsDeleted);

            if (notification == null)
            {
                return BaseResponse<bool>.ErrorResult("Không tìm thấy thông báo");
            }

            if (!notification.IsDismissed)
            {
                notification.IsDismissed = true;
                notification.DismissedAt = DateTime.Now;
                notification.LastUpdatedDate = DateTime.Now;

                await _unitOfWork.SaveChangesAsync();
                await InvalidateNotificationCacheAsync(userId);

                _logger.LogInformation("Dismissed notification {NotificationId} by user {UserId}",
                    notificationId, userId);
            }

            return BaseResponse<bool>.SuccessResult(true, "Dismiss thông báo thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dismissing notification {NotificationId}", notificationId);
            return BaseResponse<bool>.ErrorResult("Lỗi dismiss thông báo");
        }
    }

    #endregion

    #region Helper Methods

    private NotificationResponse MapToNotificationResponse(Notification notification)
    {
        var response = _mapper.Map<NotificationResponse>(notification);

        if (notification.Sender != null)
        {
            response.SenderName = notification.Sender.FullName;
        }

        return response;
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

    private async Task InvalidateNotificationCacheAsync(Guid? userId = null)
    {
        try
        {
            if (userId.HasValue)
            {
                await _cacheService.RemoveByPrefixAsync($"{NOTIFICATION_LIST_PREFIX}_{userId}");
                await _cacheService.RemoveByPrefixAsync($"popup_notifications_{userId}");
                await _cacheService.RemoveByPrefixAsync($"unread_count_{userId}");
            }
            else
            {
                await _cacheService.RemoveByPrefixAsync(NOTIFICATION_CACHE_PREFIX);
                await _cacheService.RemoveByPrefixAsync(NOTIFICATION_LIST_PREFIX);
                await _cacheService.RemoveByPrefixAsync("popup_notifications");
                await _cacheService.RemoveByPrefixAsync("unread_count");
            }

            _logger.LogDebug("Invalidated notification cache for user: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating notification cache");
        }
    }

    #endregion
}