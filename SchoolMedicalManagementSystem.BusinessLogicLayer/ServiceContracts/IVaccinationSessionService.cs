using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineSessionRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccinationSessionResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts
{
    public interface IVaccinationSessionService
    {
        Task<BaseListResponse<VaccinationSessionResponse>> GetVaccinationSessionsAsync(
            int pageIndex,
            int pageSize,
            string searchTerm,
            string orderBy,
            CancellationToken cancellationToken = default);

        Task<BaseResponse<VaccinationSessionResponse>> CreateVaccinationSessionAsync(
            CreateVaccinationSessionRequest model);

        Task<BaseResponse<VaccinationSessionResponse>> UpdateVaccinationSessionAsync(
            Guid sessionId,
            UpdateVaccinationSessionRequest model);

        Task<BaseResponse<bool>> DeleteVaccinationSessionAsync(Guid sessionId);

        Task<BaseResponse<bool>> ApproveSessionAsync(Guid sessionId);

        Task<BaseResponse<bool>> FinalizeSessionAsync(Guid sessionId);
    }
}
