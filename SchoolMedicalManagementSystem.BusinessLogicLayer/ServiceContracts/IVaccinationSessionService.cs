using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineSessionRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccinationSessionResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccineRecordResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts
{
    public interface IVaccinationSessionService
    {

        #region CRUD Vaccination Session

        Task<BaseListResponse<VaccinationSessionResponse>> GetVaccinationSessionsAsync(
            int pageIndex,
            int pageSize,
            string searchTerm,
            string orderBy,
            CancellationToken cancellationToken = default);

        Task<BaseListResponse<VaccinationSessionResponse>> GetSessionsByStudentIdAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

        Task<BaseListResponse<ClassStudentConsentStatusResponse>> GetAllClassStudentConsentStatusAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);

        Task<BaseResponse<VaccinationSessionDetailResponse>> GetSessionDetailAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

        Task<BaseResponse<VaccinationSessionResponse>> CreateVaccinationSessionAsync(
            CreateVaccinationSessionRequest model);

        Task<BaseResponse<CreateWholeVaccinationSessionResponse>> CreateWholeVaccinationSessionAsync(
            CreateWholeVaccinationSessionRequest model);

        Task<BaseResponse<VaccinationSessionResponse>> UpdateVaccinationSessionAsync(
            Guid sessionId,
            UpdateVaccinationSessionRequest model);

        Task<BaseResponse<bool>> DeleteVaccinationSessionAsync(Guid sessionId);

        #endregion

        #region Process  Session

        Task<BaseResponse<bool>> ApproveSessionAsync(
            Guid sessionId
            , CancellationToken cancellationToken = default);

        Task<BaseResponse<bool>> DeclineSessionAsync(
            Guid sessionId,
            string reason,
            CancellationToken cancellationToken = default);

        Task<BaseResponse<bool>> FinalizeSessionAsync(Guid sessionId);

        Task<BaseResponse<bool>> ParentApproveAsync(
             Guid sessionId,
             Guid studentId,
             ParentApproveRequest request,
             CancellationToken cancellationToken = default);

        Task<BaseResponse<bool>> AssignNurseToSessionAsync(
            AssignNurseToSessionRequest request,
            CancellationToken cancellationToken = default);

        Task<BaseResponse<bool>> MarkStudentVaccinatedAsync(
            Guid sessionId,
            MarkStudentVaccinatedRequest request,
            CancellationToken cancellationToken = default);

        Task<BaseListResponse<ClassStudentConsentStatusResponse>> GetClassStudentConsentStatusAsync(
            Guid sessionId,
            Guid classId,
            CancellationToken cancellationToken = default);

        Task<BaseResponse<ParentConsentStatusResponse>> GetParentConsentStatusAsync(
            Guid sessionId, Guid studentId, CancellationToken cancellationToken = default);

        Task<BaseResponse<StudentVaccinationResultResponse>> GetStudentVaccinationResultAsync(
            Guid sessionId, Guid studentId, CancellationToken cancellationToken = default);

        Task<BaseResponse<bool>> CompleteSessionAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);

        #endregion
    }
}
