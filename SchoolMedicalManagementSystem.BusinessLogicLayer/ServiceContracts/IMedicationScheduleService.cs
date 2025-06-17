using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicationScheduleRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicationScheduleResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationAdministrationResponse;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

public interface IMedicationScheduleService
{
    #region Generate Schedules

    Task<BaseResponse<BatchOperationResponse>> GenerateSchedulesAsync(CreateMedicationScheduleRequest request);

    #endregion

    #region Daily Views

    Task<BaseResponse<DailyMedicationScheduleResponse>> GetDailyScheduleAsync(
        DateTime date,
        Guid? studentId = null,
        MedicationScheduleStatus? status = null,
        bool includeCompleted = true);

    Task<BaseResponse<List<MedicationScheduleResponse>>> GetMySchedulesAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        MedicationScheduleStatus? status = null);

    Task<BaseResponse<List<DailyMedicationScheduleResponse>>> GetChildrenSchedulesAsync(
        DateTime? startDate = null,
        int days = 7);

    Task<BaseResponse<MedicationScheduleResponse>> GetScheduleDetailAsync(Guid scheduleId);

    #endregion

    #region Schedule Actions

    Task<BaseResponse<AdministerScheduleResponse>> AdministerScheduleAsync(
        Guid scheduleId, AdministerScheduleRequest request);
    Task<BaseResponse<MedicationScheduleResponse>> QuickCompleteScheduleAsync(
        Guid scheduleId, QuickCompleteRequest request);
    Task<BaseResponse<BulkAdministerResponse>> BulkAdministerSchedulesAsync(
        BulkAdministerRequest request);
    Task<BaseResponse<MedicationScheduleResponse>> MarkMissedAsync(MarkMissedMedicationRequest request);
    Task<BaseResponse<MedicationScheduleResponse>> MarkStudentAbsentAsync(MarkStudentAbsentRequest request);

    #endregion

    #region Background Service Support

    Task<BaseResponse<BatchOperationResponse>> GenerateSchedulesForMedicationAsync
    (
        Guid studentMedicationId,
        DateTime? startDate = null,
        DateTime? endDate = null
    );
    Task<BaseResponse<BatchOperationResponse>> AutoMarkOverdueSchedulesAsync();
    Task<BaseResponse<CleanupOperationResponse>> CleanupOldSchedulesAsync(int daysOld);

    #endregion
}