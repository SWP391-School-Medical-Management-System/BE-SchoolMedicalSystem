using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.NotificationResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using System.Security.Claims;

namespace SchoolMedicalManagementSystem.API.ApiControllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        INotificationService notificationService,
        ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] bool popup = false,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchTerm = null,
        [FromQuery] bool? isRead = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();

            BaseListResponse<NotificationResponse> response;

            if (popup)
            {
                response = await _notificationService.GetPopUpNotificationsAsync(userId);
            }
            else
            {
                response = await _notificationService.GetAllNotificationsAsync(
                    userId, pageIndex, pageSize, searchTerm, isRead, cancellationToken);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications");
            return StatusCode(500, BaseListResponse<NotificationResponse>.ErrorResult("Lỗi lấy danh sách thông báo"));
        }
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count");
            return StatusCode(500, "Lỗi lấy số thông báo chưa đọc");
        }
    }

    [HttpPut("{id}/mark-read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _notificationService.MarkNotificationAsReadAsync(id, userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read: {NotificationId}", id);
            return StatusCode(500, "Lỗi đánh dấu thông báo đã đọc");
        }
    }

    [HttpPut("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _notificationService.MarkAllNotificationsAsReadAsync(userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return StatusCode(500, "Lỗi đánh dấu tất cả thông báo đã đọc");
        }
    }

    [HttpPut("dismiss-all")]
    public async Task<IActionResult> DismissAllNotifications()
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _notificationService.DismissAllNotificationsAsync(userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dismissing all notifications");
            return StatusCode(500, "Lỗi dismiss tất cả thông báo");
        }
    }

    [HttpPut("{id}/dismiss")]
    public async Task<IActionResult> DismissNotification(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _notificationService.DismissNotificationAsync(id, userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dismissing notification: {NotificationId}", id);
            return StatusCode(500, "Lỗi dismiss thông báo");
        }
    }

    [HttpGet("severe-alerts")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<IActionResult> GetSevereConditionAlerts()
    {
        try
        {
            var nurseId = GetCurrentUserId();
            var response = await _notificationService.GetSevereConditionAlertsForNurseAsync(nurseId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting severe condition alerts");
            return StatusCode(500, "Lỗi lấy cảnh báo tình trạng y tế nghiêm trọng");
        }
    }

    #region Helper Methods

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("uid")?.Value;
    
        if (Guid.TryParse(userIdClaim, out Guid userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException("Không thể xác định user hiện tại");
    }

    #endregion
}