using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthEventRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthEventResponse;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

public interface IHealthEventService
{
    Task<BaseListResponse<HealthEventResponse>> GetHealthEventsAsync(
        int pageIndex, int pageSize, string searchTerm, string orderBy,
        Guid? studentId = null, HealthEventType? eventType = null, bool? isEmergency = null,
        DateTime? fromDate = null, DateTime? toDate = null, string? location = null,
        CancellationToken cancellationToken = default);

    Task<BaseResponse<HealthEventResponse>> GetHealthEventByIdAsync(Guid eventId);
    Task<BaseResponse<HealthEventResponse>> CreateHealthEventAsync(CreateHealthEventRequest model);
    Task<BaseResponse<HealthEventResponse>> UpdateHealthEventAsync(Guid eventId, UpdateHealthEventRequest model);
    Task<BaseResponse<bool>> DeleteHealthEventAsync(Guid eventId);
    Task<BaseListResponse<HealthEventResponse>> GetHealthEventsByStudentAsync(Guid studentId, int pageIndex, int pageSize, HealthEventType? eventType = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<BaseListResponse<HealthEventResponse>> GetEmergencyEventsAsync(int pageIndex, int pageSize, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<BaseListResponse<HealthEventResponse>> GetEventsByMedicalConditionAsync(Guid medicalConditionId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    Task<BaseResponse<HealthEventResponse>> TakeOwnershipAsync(Guid eventId);
    Task<BaseResponse<HealthEventResponse>> AssignToNurseAsync(Guid eventId, AssignHealthEventRequest request);
    Task<BaseListResponse<HealthEventResponse>> GetUnassignedEventsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<BaseResponse<HealthEventResponse>> CompleteEventAsync(Guid eventId, CompleteHealthEventRequest request);
    Task<BaseListResponse<HealthEventResponse>> GetMyAssignedEventsAsync(int pageIndex, int pageSize, HealthEventStatus? status = null, CancellationToken cancellationToken = default);
}