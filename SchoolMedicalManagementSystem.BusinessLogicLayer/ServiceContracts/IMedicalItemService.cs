using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalItemRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalItemUsageRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalItemResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalItemUsageResponse;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

public interface IMedicalItemService
{
    #region Medical Item CRUD Operations

    Task<BaseListResponse<MedicalItemResponse>> GetMedicalItemsAsync(
        int pageIndex,
        int pageSize,
        string searchTerm,
        string orderBy,
        string? type = null,
        bool? lowStock = null,
        bool? expiringSoon = null,
        bool? expired = null,
        string? alertsOnly = null,
        int? expiryDays = 30,
        MedicalItemApprovalStatus? approvalStatus = null,
        PriorityLevel? priority = null,
        bool? urgentOnly = null,
        CancellationToken cancellationToken = default);

    Task<BaseResponse<MedicalItemResponse>> GetMedicalItemByIdAsync(Guid itemId);
    Task<BaseResponse<MedicalItemResponse>> CreateMedicalItemAsync(CreateMedicalItemRequest model);
    Task<BaseResponse<MedicalItemResponse>> UpdateMedicalItemAsync(Guid itemId, UpdateMedicalItemRequest model);
    Task<BaseResponse<bool>> DeleteMedicalItemAsync(Guid itemId);

    #endregion
    
    #region Approval methods

    Task<BaseListResponse<MedicalItemResponse>> GetPendingApprovalsAsync(
        int pageIndex = 1,
        int pageSize = 10,
        string searchTerm = "",
        string orderBy = null,
        PriorityLevel? priority = null,
        bool? urgentOnly = null,
        CancellationToken cancellationToken = default);

    Task<BaseResponse<MedicalItemResponse>> RejectMedicalItemAsync(
        Guid itemId, RejectMedicalItemRequest model);

    Task<BaseResponse<MedicalItemResponse>> ApproveMedicalItemAsync(
        Guid itemId, ApproveMedicalItemRequest model);

    #endregion

    #region Stock Management

    Task<BaseResponse<MedicalItemStockSummaryResponse>> GetStockSummaryAsync();

    Task<BaseResponse<bool>> UpdateStockQuantityAsync(Guid itemId, int newQuantity, string reason);

    #endregion

    #region Medical Item Usage Management

    Task<BaseResponse<MedicalItemUsageResponse>> RecordMedicalItemUsageAsync(CreateMedicalItemUsageRequest model);

    Task<BaseResponse<MedicalItemUsageResponse>> CorrectMedicalItemUsageAsync(
        Guid originalUsageId,
        CorrectMedicalItemUsageRequest request);

    Task<BaseResponse<MedicalItemUsageResponse>> ReturnMedicalItemAsync(
        Guid originalUsageId,
        ReturnMedicalItemRequest request);

    Task<BaseListResponse<MedicalItemUsageResponse>> GetUsageHistoryAsync(
        int pageIndex,
        int pageSize,
        string searchTerm,
        string orderBy,
        Guid? medicalItemId = null,
        Guid? studentId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    Task<BaseListResponse<MedicalItemUsageResponse>> GetUsageHistoryByStudentAsync(
        Guid studentId,
        int pageIndex,
        int pageSize,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    #endregion
}