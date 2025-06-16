using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineRecordRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalRecordResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts
{
    public interface IVaccinationRecordService
    {
        Task<BaseListResponse<VaccinationRecordResponse>> GetVaccinationRecordsAsync(
            Guid medicalRecordId,
            int pageIndex,
            int pageSize,
            string searchTerm,
            string orderBy,
            CancellationToken cancellationToken = default);

        Task<BaseResponse<VaccinationRecordResponse>> CreateVaccinationRecordAsync(
            Guid medicalRecordId,
            CreateVaccinationRecordRequest model);

        Task<BaseResponse<VaccinationRecordResponse>> UpdateVaccinationRecordAsync(
            Guid recordId,
            UpdateVaccinationRecordRequest model);

        Task<BaseResponse<bool>> DeleteVaccinationRecordAsync(Guid recordId);
    }
}
