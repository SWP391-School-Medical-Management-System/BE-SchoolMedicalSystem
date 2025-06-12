using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationResponse;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

public interface IStudentMedicationService
{
    Task<BaseListResponse<StudentMedicationResponse>> GetStudentMedicationsAsync(
        int pageIndex, int pageSize, string searchTerm, string orderBy,
        Guid? studentId = null, Guid? parentId = null, StudentMedicationStatus? status = null,
        bool? expiringSoon = null, bool? requiresAdministration = null,
        CancellationToken cancellationToken = default);

    Task<BaseResponse<StudentMedicationResponse>> GetStudentMedicationByIdAsync(Guid medicationId);
    Task<BaseResponse<StudentMedicationResponse>> CreateStudentMedicationAsync(CreateStudentMedicationRequest model);

    Task<BaseResponse<StudentMedicationResponse>> UpdateStudentMedicationAsync(Guid medicationId,
        UpdateStudentMedicationRequest model);

    Task<BaseResponse<bool>> DeleteStudentMedicationAsync(Guid medicationId);

    Task<BaseResponse<StudentMedicationResponse>> ApproveStudentMedicationAsync(Guid medicationId,
        ApproveStudentMedicationRequest request);

    Task<BaseListResponse<StudentMedicationResponse>> GetPendingApprovalsAsync(int pageIndex, int pageSize,
        CancellationToken cancellationToken = default);

    Task<BaseResponse<StudentMedicationResponse>> UpdateMedicationStatusAsync(Guid medicationId,
        UpdateMedicationStatusRequest request);

    Task<BaseResponse<List<StudentMedicationResponse>>> GetExpiredMedicationsAsync();
    Task<BaseResponse<List<StudentMedicationResponse>>> GetExpiringSoonMedicationsAsync(int days = 7);

    Task<BaseListResponse<StudentMedicationResponse>> GetMyChildrenMedicationsAsync(int pageIndex, int pageSize,
        StudentMedicationStatus? status = null, CancellationToken cancellationToken = default);
}