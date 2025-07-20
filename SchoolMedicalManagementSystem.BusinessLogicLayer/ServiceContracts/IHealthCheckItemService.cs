using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthCheckItemRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthCheckItemResponse;
using System.Threading;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts
{
    public interface IHealthCheckItemService
    {
        Task<BaseListResponse<HealthCheckItemResponse>> GetHealthCheckItemsAsync(
            int pageIndex, int pageSize, string searchTerm, string orderBy, CancellationToken cancellationToken = default);

        Task<BaseResponse<HealthCheckItemResponse>> GetHealthCheckItemDetailAsync(
            Guid id, CancellationToken cancellationToken = default);

        Task<BaseResponse<HealthCheckItemResponse>> CreateHealthCheckItemAsync(
            CreateHealthCheckItemRequest model);

        Task<BaseResponse<HealthCheckItemResponse>> UpdateHealthCheckItemAsync(
            Guid id, UpdateHealthCheckItemRequest model);

        Task<BaseResponse<bool>> DeleteHealthCheckItemAsync(Guid id);
    }
}