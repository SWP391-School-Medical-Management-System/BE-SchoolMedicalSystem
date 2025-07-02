using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalConditionRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalCondition;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

public interface IMedicalConditionService
{
    Task<BaseListResponse<MedicalConditionResponse>> GetMedicalConditionsAsync(
        int pageIndex,
        int pageSize,
        string searchTerm,
        string orderBy,
        Guid? medicalRecordId = null,
        MedicalConditionType? type = null,
        string severity = null,
        CancellationToken cancellationToken = default);

    Task<BaseResponse<MedicalConditionResponse>> GetMedicalConditionByIdAsync(Guid conditionId);

    Task<BaseListResponse<MedicalConditionResponse>> GetMedicalConditionsByRecordIdAsync(
        Guid medicalRecordId,
        MedicalConditionType? type = null,
        CancellationToken cancellationToken = default);

    Task<BaseListResponse<MedicalConditionResponse>> GetAllMedicalConditionByStudentIdAsync(
    Guid studentId,
    int pageIndex = 1,
    int pageSize = 10,
    MedicalConditionType? type = null,
    CancellationToken cancellationToken = default);

    Task<BaseResponse<MedicalConditionResponse>> CreateMedicalConditionAsync(CreateMedicalConditionRequest model);

    Task<BaseResponse<MedicalConditionResponse>> UpdateMedicalConditionAsync(Guid conditionId,
        UpdateMedicalConditionRequest model);

    Task<BaseResponse<bool>> DeleteMedicalConditionAsync(Guid conditionId);
}