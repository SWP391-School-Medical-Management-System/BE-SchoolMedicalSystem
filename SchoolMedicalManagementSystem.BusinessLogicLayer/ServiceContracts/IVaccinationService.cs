// SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts/IVaccinationService.cs
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccineResponse;
using System;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts
{
    public interface IVaccinationService
    {
        Task<BaseListResponse<VaccinationTypeResponse>> GetVaccinationTypesAsync(
              int pageIndex,
              int pageSize,
              string searchTerm,
              string orderBy,
              CancellationToken cancellationToken = default);

        Task<BaseResponse<VaccinationTypeResponse>> GetVaccineTypeDetailAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<BaseResponse<VaccinationTypeResponse>> CreateVaccinationTypeAsync(CreateVaccinationTypeRequest model);

        Task<BaseResponse<VaccinationTypeResponse>> UpdateVaccinationTypeAsync(Guid id, UpdateVaccinationTypeRequest model);

        Task<BaseResponse<bool>> DeleteVaccinationTypeAsync(Guid id);
    }
}