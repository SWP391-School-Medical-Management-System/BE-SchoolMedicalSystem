using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalRecordRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalRecordResponse;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

public interface IMedicalRecordService
{
    Task<BaseListResponse<MedicalRecordResponse>> GetMedicalRecordsAsync(
        int pageIndex,
        int pageSize,
        string searchTerm,
        string orderBy,
        string bloodType = null,
        bool? hasAllergies = null,
        bool? hasChronicDisease = null,
        bool? needsUpdate = null,
        CancellationToken cancellationToken = default);

    Task<BaseResponse<MedicalRecordDetailResponse>> GetMedicalRecordByIdAsync(Guid recordId);
    Task<BaseResponse<MedicalRecordDetailResponse>> GetMedicalRecordByStudentIdAsync(Guid studentId);
    Task<BaseResponse<MedicalRecordDetailResponse>> CreateMedicalRecordAsync(CreateMedicalRecordRequest model);

    Task<BaseResponse<MedicalRecordDetailResponse>> UpdateMedicalRecordAsync(Guid recordId,
        UpdateMedicalRecordRequest model);

    Task<BaseResponse<MedicalRecordDetailResponse>> UpdateMedicalRecordByParentAsync(
        Guid studentId,
        UpdateMedicalRecordByParentRequest model,
        Guid parentId);

    Task<BaseResponse<bool>> DeleteMedicalRecordAsync(Guid recordId);
}