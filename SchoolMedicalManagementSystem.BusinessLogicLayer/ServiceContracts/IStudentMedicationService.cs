using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicationStockResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationAdministrationResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationResponse;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

public interface IStudentMedicationService
{
    // Basic CRUD Operations
    Task<BaseListResponse<StudentMedicationListResponse>> GetStudentMedicationsAsync
    (
        int pageIndex,
        int pageSize,
        string searchTerm,
        string orderBy,
        Guid? studentId = null,
        Guid? parentId = null,
        StudentMedicationStatus? status = null,
        bool? expiringSoon = null,
        bool? requiresAdministration = null,
        CancellationToken cancellationToken = default
    );

    Task<BaseResponse<StudentMedicationDetailResponse>> GetStudentMedicationByIdAsync(Guid medicationId);

    Task<BaseListResponse<StudentMedicationListResponse>> GetAllMedicationsByNurseOrStudentAsync(
        int pageIndex,
        int pageSize,
        Guid? nurseId = null,
        Guid? studentId = null,
        CancellationToken cancellationToken = default);

    Task<BaseResponse<StudentMedicationResponse>> CreateStudentMedicationAsync(CreateStudentMedicationRequest model);

    Task<BaseListResponse<StudentMedicationResponse>> CreateBulkStudentMedicationsAsync(CreateBulkStudentMedicationRequest request);

    Task<BaseResponse<StudentMedicationResponse>> UpdateStudentMedicationAsync(Guid medicationId,
        UpdateStudentMedicationRequest model);

    Task<BaseResponse<StudentMedicationResponse>> AddMoreMedicationAsync(
        AddMoreMedicationRequest request);

    Task<BaseResponse<StudentMedicationResponse>> UpdateMedicationManagementAsync(
        Guid medicationId, UpdateMedicationManagementRequest request);

    Task<BaseResponse<bool>> DeleteStudentMedicationAsync(Guid medicationId);

    Task<BaseListResponse<StudentMedicationRequestResponse>> GetAllStudentMedicationRequestAsync(
    int pageIndex,
    int pageSize,
    Guid? studentId = null,
    Guid? parentId = null,
    StudentMedicationStatus? status = null,
    CancellationToken cancellationToken = default);

    Task<BaseResponse<StudentMedicationRequestDetailResponse>> GetStudentMedicationRequestByIdAsync(Guid requestId);

    // Approval Workflow
    Task<BaseResponse<StudentMedicationResponse>> ApproveStudentMedicationAsync(Guid medicationId,
        ApproveStudentMedicationRequest request);

    Task<BaseResponse<StudentMedicationResponse>> RejectStudentMedicationAsync(Guid medicationId,
        RejectStudentMedicationRequest request);

    Task<BaseListResponse<PendingApprovalResponse>> GetPendingApprovalsAsync
    (
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default
    );

    // Status Management

    Task<BaseResponse<List<StudentMedicationResponse>>> UpdateQuantityReceivedAsync(
            Guid requestId,
            UpdateQuantityReceivedRequest request,
            CancellationToken cancellationToken = default);

    Task<BaseResponse<StudentMedicationResponse>> UpdateMedicationStatusAsync(Guid medicationId,
        UpdateMedicationStatusRequest request);

    // Parent Specific Methods
    Task<BaseListResponse<ParentMedicationResponse>> GetMyChildrenMedicationsAsync
    (
        int pageIndex,
        int pageSize,
        StudentMedicationStatus? status = null,
        CancellationToken cancellationToken = default
    );

    Task<BaseListResponse<MedicationAdministrationResponse>> GetAdministrationHistoryAsync(Guid medicationId,
        int pageIndex,
        int pageSize,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default
    );

    // Parent Medication Stock Methods
    Task<BaseListResponse<MedicationStockResponse>> GetMyMedicationStockHistoryAsync
    (
        int pageIndex,
        int pageSize,
        Guid? studentId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default
    );

    Task<BaseListResponse<MedicationStockResponse>> GetMedicationStockHistoryAsync
    (
        Guid studentMedicationId,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default
    );

    // School Nurse MedicationStock Methods
    Task<BaseListResponse<MedicationStockResponse>> GetAllMedicationStockHistoryAsync
    (
        int pageIndex,
        int pageSize,
        Guid? studentId = null,
        Guid? parentId = null,
        Guid? medicationId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        bool? isExpired = null,
        bool? lowStock = null,
        CancellationToken cancellationToken = default
    );

    Task<BaseListResponse<MedicationStockResponse>> GetMedicationStockByIdForNurseAsync
    (
        Guid studentMedicationId,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default
    );

    Task<BaseResponse<StudentMedicationUsageHistoryResponse>> AdministerMedicationAsync(
        Guid medicationId,
        AdministerMedicationRequest request,
        CancellationToken cancellationToken = default);

}