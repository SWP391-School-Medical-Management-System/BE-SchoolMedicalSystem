using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthCheckRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthCheckResponse;
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
    }
}
