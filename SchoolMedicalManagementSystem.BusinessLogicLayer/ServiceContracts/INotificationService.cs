using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.NotificationResponse;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

public interface INotificationService
{
    Task<BaseListResponse<NotificationResponse>> GetAllNotificationsAsync(
        Guid userId,
        int pageIndex = 1,
        int pageSize = 10,
        string searchTerm = null,
        bool? isRead = null,
        CancellationToken cancellationToken = default);

    Task<BaseListResponse<NotificationResponse>> GetPopUpNotificationsAsync(Guid userId);

    Task<BaseResponse<int>> GetUnreadCountAsync(Guid userId);

    Task<BaseResponse<List<NotificationResponse>>> GetSevereConditionAlertsForNurseAsync(Guid nurseId);

    Task<BaseResponse<bool>> MarkNotificationAsReadAsync(Guid notificationId, Guid userId);

    Task<BaseResponse<bool>> MarkAllNotificationsAsReadAsync(Guid userId);

    Task<BaseResponse<bool>> DismissAllNotificationsAsync(Guid userId);

    Task<BaseResponse<bool>> DismissNotificationAsync(Guid notificationId, Guid userId);
}