using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthCheckRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthCheckResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalCondition;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalRecordResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccinationSessionResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts
{
    public interface IHealthCheckService
    {
        Task<BaseListResponse<HealthCheckResponse>> GetHealthChecksAsync(int pageIndex, int pageSize, string searchTerm, string orderBy, Guid? nurseId = null, CancellationToken cancellationToken = default);
        Task<BaseResponse<HealthCheckDetailResponse>> GetHealthCheckDetailAsync(Guid healthCheckId, CancellationToken cancellationToken = default);
        Task<BaseResponse<CreateWholeHealthCheckResponse>> CreateWholeHealthCheckAsync(CreateWholeHealthCheckRequest model);
        Task<BaseResponse<HealthCheckResponse>> UpdateHealthCheckAsync(Guid healthCheckId, UpdateHealthCheckRequest model);
        Task<BaseResponse<bool>> DeleteHealthCheckAsync(Guid healthCheckId);
        Task<BaseResponse<bool>> ApproveHealthCheckAsync(Guid healthCheckId, CancellationToken cancellationToken = default);
        Task<BaseResponse<bool>> DeclineHealthCheckAsync(Guid healthCheckId, string reason, CancellationToken cancellationToken = default);
        Task<BaseResponse<bool>> FinalizeHealthCheckAsync(Guid healthCheckId);
        Task<BaseResponse<bool>> ParentApproveAsync(
            Guid healthCheckId, Guid studentId,
            ParentApproveHealthCheckRequest request,
            CancellationToken cancellationToken = default);
        Task<BaseResponse<bool>> AssignNurseToHealthCheckAsync(
            AssignNurseToHealthCheckRequest request,
            CancellationToken cancellationToken = default);
        Task<BaseResponse<bool>> ReassignNurseToHealthCheckAsync(
            Guid healthCheckId,
            ReAssignNurseToHealthCheckRequest request,
            CancellationToken cancellationToken = default);
        Task<BaseResponse<bool>> CompleteHealthCheckAsync(
            Guid healthCheckId,
            CancellationToken cancellationToken = default);

        Task<BaseListResponse<StudentConsentStatusHealthCheckResponse>> GetAllStudentConsentStatusAsync(
            Guid healthCheckId,
            CancellationToken cancellationToken = default);

        Task<BaseListResponse<HealthCheckNurseAssignmentStatusResponse>> GetHealthCheckNurseAssignmentsAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<BaseListResponse<HealthCheckResponse>> GetHealthCheckByStudentIdAsync(
            Guid studentId,
            int pageIndex,
            int pageSize,
            string searchTerm = "",
            CancellationToken cancellationToken = default);

        #region HealthCheck Flow

        Task<BaseResponse<VisionRecordResponseHealth>> SaveLeftEyeCheckAsync(
            Guid healthCheckId, SaveVisionCheckRequest request, CancellationToken cancellationToken = default);
        Task<BaseResponse<VisionRecordResponseHealth>> SaveRightEyeCheckAsync(
         Guid healthCheckId, SaveVisionCheckRequest request, CancellationToken cancellationToken = default);
        //Task<BaseResponse<HearingRecordResponse>> SaveLeftEarCheckAsync(Guid healthCheckId, SaveHearingCheckRequest request, CancellationToken cancellationToken = default);
        //Task<BaseResponse<HearingRecordResponse>> SaveRightEarCheckAsync(Guid healthCheckId, SaveHearingCheckRequest request, CancellationToken cancellationToken = default);
        //Task<BaseResponse<PhysicalRecordResponse>> SaveHeightCheckAsync(Guid healthCheckId, SaveHeightCheckRequest request, CancellationToken cancellationToken = default);
        //Task<BaseResponse<PhysicalRecordResponse>> SaveWeightCheckAsync(Guid healthCheckId, SaveWeightCheckRequest request, CancellationToken cancellationToken = default);
        //Task<BaseResponse<MedicalConditionResponse>> SaveBloodPressureCheckAsync(Guid healthCheckId, SaveBloodPressureCheckRequest request, CancellationToken cancellationToken = default);
        //Task<BaseResponse<MedicalConditionResponse>> SaveHeartRateCheckAsync(Guid healthCheckId, SaveHeartRateCheckRequest request, CancellationToken cancellationToken = default);

        #endregion

    }
}
