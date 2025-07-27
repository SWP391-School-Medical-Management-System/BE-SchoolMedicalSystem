using System.Security.Claims;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Helpers;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalItemRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalItemUsageRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalItemResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalItemUsageResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services;

public class MedicalItemService : IMedicalItemService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<MedicalItemService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IValidator<CreateMedicalItemRequest> _createMedicalItemValidator;
    private readonly IValidator<UpdateMedicalItemRequest> _updateMedicalItemValidator;
    private readonly IValidator<CreateMedicalItemUsageRequest> _createUsageValidator;
    private readonly INotificationService _notificationService;

    private const string MEDICAL_ITEM_CACHE_PREFIX = "medical_item";
    private const string MEDICAL_ITEM_LIST_PREFIX = "medical_items_list";
    private const string MEDICAL_ITEM_USAGE_CACHE_PREFIX = "medical_item_usage";
    private const string MEDICAL_ITEM_USAGE_LIST_PREFIX = "medical_item_usages_list";
    private const string MEDICAL_ITEM_CACHE_SET = "medical_item_cache_keys";
    private const string STATISTICS_PREFIX = "statistics";
    private const string NOTIFICATION_PREFIX = "notification";
    private const string NOTIFICATIONS_LIST_PREFIX = "notifications_list";

    public MedicalItemService(
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<MedicalItemService> logger,
        IHttpContextAccessor httpContextAccessor,
        IValidator<CreateMedicalItemRequest> createMedicalItemValidator,
        IValidator<UpdateMedicalItemRequest> updateMedicalItemValidator,
        IValidator<CreateMedicalItemUsageRequest> createUsageValidator,
        INotificationService notificationService)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _createMedicalItemValidator = createMedicalItemValidator;
        _updateMedicalItemValidator = updateMedicalItemValidator;
        _createUsageValidator = createUsageValidator;
        _notificationService = notificationService;
    }

    #region Medical Item CRUD Operations

    public async Task<BaseListResponse<MedicalItemResponse>> GetMedicalItemsAsync(
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
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(
                MEDICAL_ITEM_LIST_PREFIX,
                pageIndex.ToString(),
                pageSize.ToString(),
                searchTerm ?? "",
                orderBy ?? "",
                type ?? "",
                lowStock?.ToString() ?? "",
                expiringSoon?.ToString() ?? "",
                expired?.ToString() ?? "",
                alertsOnly ?? "",
                expiryDays?.ToString() ?? "30",
                approvalStatus?.ToString() ?? "",
                priority?.ToString() ?? "",
                urgentOnly?.ToString() ?? ""
            );

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<MedicalItemResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Medical items list found in cache with key: {CacheKey}", cacheKey);
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<MedicalItem>().GetQueryable()
                .Include(mi => mi.RequestedBy)
                .Include(mi => mi.ApprovedBy)
                .Where(mi => !mi.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrEmpty(alertsOnly))
            {
                query = ApplyAlertsFilter(query, alertsOnly, expiryDays ?? 30);
            }
            else
            {
                query = ApplyMedicalItemFilters(query, searchTerm, type, lowStock, expiringSoon, expired,
                    expiryDays ?? 30, approvalStatus, priority, urgentOnly);
            }

            if (!string.IsNullOrEmpty(alertsOnly))
            {
                query = ApplyAlertsOrdering(query, alertsOnly);
            }
            else
            {
                query = ApplyMedicalItemOrdering(query, orderBy);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var medicalItems = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = medicalItems.Select(MapToMedicalItemResponse).ToList();

            var message = GetSuccessMessage(alertsOnly, expiryDays, approvalStatus);

            var result = BaseListResponse<MedicalItemResponse>.SuccessResult(
                responses,
                totalCount,
                pageSize,
                pageIndex,
                message);

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICAL_ITEM_CACHE_SET);
            _logger.LogDebug("Cached medical items list with key: {CacheKey}", cacheKey);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medical items");
            return BaseListResponse<MedicalItemResponse>.ErrorResult("Lỗi lấy danh sách thuốc và vật tư y tế.");
        }
    }

    public async Task<BaseResponse<MedicalItemResponse>> GetMedicalItemByIdAsync(Guid itemId)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(MEDICAL_ITEM_CACHE_PREFIX, itemId.ToString());
            var cachedResponse = await _cacheService.GetAsync<BaseResponse<MedicalItemResponse>>(cacheKey);

            if (cachedResponse != null)
            {
                _logger.LogDebug("Medical item found in cache with key: {CacheKey}", cacheKey);
                return cachedResponse;
            }

            var itemRepo = _unitOfWork.GetRepositoryByEntity<MedicalItem>();
            var medicalItem = await itemRepo.GetQueryable()
                .Include(mi => mi.RequestedBy)
                .Include(mi => mi.ApprovedBy)
                .Where(mi => mi.Id == itemId && !mi.IsDeleted)
                .FirstOrDefaultAsync();

            if (medicalItem == null)
            {
                return BaseResponse<MedicalItemResponse>.ErrorResult("Không tìm thấy thuốc hoặc vật tư y tế.");
            }

            var itemResponse = MapToMedicalItemResponse(medicalItem);

            var response = BaseResponse<MedicalItemResponse>.SuccessResult(
                itemResponse, "Lấy thông tin thuốc/vật tư y tế thành công.");

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICAL_ITEM_CACHE_SET);
            _logger.LogDebug("Cached medical item with key: {CacheKey}", cacheKey);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting medical item by ID: {ItemId}", itemId);
            return BaseResponse<MedicalItemResponse>.ErrorResult($"Lỗi lấy thông tin thuốc/vật tư y tế: {ex.Message}");
        }
    }

    public async Task<BaseResponse<MedicalItemResponse>> CreateMedicalItemAsync(CreateMedicalItemRequest model)
    {
        try
        {
            var validationResult = await _createMedicalItemValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BaseResponse<MedicalItemResponse>.ErrorResult(errors);
            }

            var currentUserId = GetCurrentUserId();
            var schoolNurseRoleName = await GetSchoolNurseRoleName();

            var medicalItem = _mapper.Map<MedicalItem>(model);
            medicalItem.Id = Guid.NewGuid();
            medicalItem.RequestedById = currentUserId;
            medicalItem.RequestedAt = DateTime.Now;
            medicalItem.ApprovalStatus = MedicalItemApprovalStatus.Pending;
            medicalItem.CreatedBy = schoolNurseRoleName;
            medicalItem.CreatedDate = DateTime.Now;

            var itemRepo = _unitOfWork.GetRepositoryByEntity<MedicalItem>();
            await itemRepo.AddAsync(medicalItem);
            await _unitOfWork.SaveChangesAsync();

            await CreateApprovalRequestNotificationAsync(medicalItem);

            // Xóa cache cụ thể
            var itemCacheKey = _cacheService.GenerateCacheKey(MEDICAL_ITEM_CACHE_PREFIX, medicalItem.Id.ToString());
            await _cacheService.RemoveAsync(itemCacheKey);
            _logger.LogDebug("Đã xóa cache cụ thể cho medical item detail: {CacheKey}", itemCacheKey);
            await _cacheService.RemoveByPrefixAsync(MEDICAL_ITEM_LIST_PREFIX);
            _logger.LogDebug("Đã xóa cache danh sách medical items với prefix: {Prefix}", MEDICAL_ITEM_LIST_PREFIX);
            await _cacheService.RemoveByPrefixAsync(NOTIFICATION_PREFIX);
            _logger.LogDebug("Đã xóa cache notification với prefix: {Prefix}", NOTIFICATION_PREFIX);
            await _cacheService.RemoveByPrefixAsync(NOTIFICATIONS_LIST_PREFIX);
            _logger.LogDebug("Đã xóa cache notifications list với prefix: {Prefix}", NOTIFICATIONS_LIST_PREFIX);

            await InvalidateAllCachesAsync();

            medicalItem = await itemRepo.GetQueryable()
                .Include(mi => mi.RequestedBy)
                .Include(mi => mi.ApprovedBy)
                .FirstOrDefaultAsync(mi => mi.Id == medicalItem.Id);

            var itemResponse = MapToMedicalItemResponse(medicalItem);

            _logger.LogInformation("Created medical item {ItemId} and sent for approval", medicalItem.Id);

            return BaseResponse<MedicalItemResponse>.SuccessResult(
                itemResponse,
                "Tạo thuốc/vật tư y tế thành công. Yêu cầu đã được gửi đến Manager để phê duyệt.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating medical item");
            return BaseResponse<MedicalItemResponse>.ErrorResult($"Lỗi thêm thuốc/vật tư y tế: {ex.Message}");
        }
    }

    public async Task<BaseResponse<MedicalItemResponse>> UpdateMedicalItemAsync(
    Guid itemId,
    UpdateMedicalItemRequest model)
    {
        try
        {
            var validationResult = await _updateMedicalItemValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BaseResponse<MedicalItemResponse>.ErrorResult(errors);
            }

            var itemRepo = _unitOfWork.GetRepositoryByEntity<MedicalItem>();
            var medicalItem = await itemRepo.GetQueryable()
                .Include(mi => mi.RequestedBy)
                .Include(mi => mi.ApprovedBy)
                .FirstOrDefaultAsync(mi => mi.Id == itemId && !mi.IsDeleted);

            if (medicalItem == null)
            {
                return BaseResponse<MedicalItemResponse>.ErrorResult("Không tìm thấy thuốc hoặc vật tư y tế.");
            }

            var currentUserId = GetCurrentUserId();
            var userRoles = await GetCurrentUserRolesAsync();

            if (!userRoles.Contains("SCHOOLNURSE"))
            {
                return BaseResponse<MedicalItemResponse>.ErrorResult(
                    "Chỉ Y tá trường học mới có quyền cập nhật thuốc/vật tư y tế.");
            }

            // Nếu School Nurse cập nhật và trạng thái là Approved hoặc Rejected, chuyển về Pending
            if (userRoles.Contains("SCHOOLNURSE") && !userRoles.Contains("MANAGER") &&
                (medicalItem.ApprovalStatus == MedicalItemApprovalStatus.Approved ||
                 medicalItem.ApprovalStatus == MedicalItemApprovalStatus.Rejected))
            {
                medicalItem.ApprovalStatus = MedicalItemApprovalStatus.Pending;
                medicalItem.RequestedAt = DateTime.Now;
                medicalItem.ApprovedById = null;
                medicalItem.ApprovedAt = null;
                medicalItem.RejectedAt = null;
                medicalItem.RejectionReason = null;
                _logger.LogInformation("Trạng thái MedicalItem {ItemId} chuyển về Pending để chờ phê duyệt lại bởi School Nurse {UserId}", itemId, currentUserId);
            }

            var schoolNurseRoleName = await GetSchoolNurseRoleName();
            var managerRoleName = await GetManagerRoleName();
            var oldQuantity = medicalItem.Quantity;

            _mapper.Map(model, medicalItem);
            medicalItem.LastUpdatedBy = userRoles.Contains("MANAGER") ? managerRoleName : schoolNurseRoleName;
            medicalItem.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();

            if (medicalItem.ApprovalStatus == MedicalItemApprovalStatus.Pending &&
                userRoles.Contains("SCHOOLNURSE"))
            {
                await CreateApprovalRequestNotificationAsync(medicalItem);
            }
            else if (medicalItem.ApprovalStatus == MedicalItemApprovalStatus.Approved)
            {
                if (oldQuantity != model.Quantity)
                {
                    await CreateStockChangeNotificationAsync(medicalItem, oldQuantity, model.Quantity,
                        "Cập nhật thông tin thuốc/vật tư");
                }

                await CreateStockAlertNotificationsAsync(medicalItem);
            }

            // Xóa cache cụ thể
            var itemCacheKey = _cacheService.GenerateCacheKey(MEDICAL_ITEM_CACHE_PREFIX, itemId.ToString());
            await _cacheService.RemoveAsync(itemCacheKey);
            _logger.LogDebug("Đã xóa cache cụ thể cho medical item detail: {CacheKey}", itemCacheKey);
            await _cacheService.RemoveByPrefixAsync(MEDICAL_ITEM_LIST_PREFIX);
            _logger.LogDebug("Đã xóa cache danh sách medical items với prefix: {Prefix}", MEDICAL_ITEM_LIST_PREFIX);
            await _cacheService.RemoveByPrefixAsync(NOTIFICATION_PREFIX);
            _logger.LogDebug("Đã xóa cache notification với prefix: {Prefix}", NOTIFICATION_PREFIX);
            await _cacheService.RemoveByPrefixAsync(NOTIFICATIONS_LIST_PREFIX);
            _logger.LogDebug("Đã xóa cache notifications list với prefix: {Prefix}", NOTIFICATIONS_LIST_PREFIX);

            await InvalidateAllCachesAsync();

            var itemResponse = MapToMedicalItemResponse(medicalItem);

            var message = medicalItem.ApprovalStatus == MedicalItemApprovalStatus.Pending
                ? "Cập nhật thuốc/vật tư thành công. Yêu cầu đã được gửi lại để phê duyệt."
                : "Cập nhật thuốc/vật tư y tế thành công.";

            _logger.LogInformation(
                "Updated medical item {ItemId} by user {UserId}, status: {Status}",
                itemId, currentUserId, medicalItem.ApprovalStatus);

            return BaseResponse<MedicalItemResponse>.SuccessResult(itemResponse, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating medical item: {ItemId}", itemId);
            return BaseResponse<MedicalItemResponse>.ErrorResult($"Lỗi cập nhật thuốc/vật tư y tế: {ex.Message}");
        }
    }

    public async Task<BaseResponse<bool>> DeleteMedicalItemAsync(Guid itemId)
    {
        try
        {
            var itemRepo = _unitOfWork.GetRepositoryByEntity<MedicalItem>();
            var medicalItem = await itemRepo.GetQueryable()
                .FirstOrDefaultAsync(mi => mi.Id == itemId && !mi.IsDeleted);

            if (medicalItem == null)
            {
                return BaseResponse<bool>.ErrorResult("Không tìm thấy thuốc hoặc vật tư y tế.");
            }

            var hasUsage = await _unitOfWork.GetRepositoryByEntity<MedicalItemUsage>().GetQueryable()
                .AnyAsync(miu => miu.MedicalItemId == itemId && !miu.IsDeleted);

            if (hasUsage)
            {
                return BaseResponse<bool>.ErrorResult("Không thể xóa thuốc/vật tư y tế đã có lịch sử sử dụng.");
            }

            var schoolNurseRoleName = await GetSchoolNurseRoleName();

            medicalItem.IsDeleted = true;
            medicalItem.LastUpdatedBy = schoolNurseRoleName;
            medicalItem.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();

            // Xóa cache cụ thể
            var itemCacheKey = _cacheService.GenerateCacheKey(MEDICAL_ITEM_CACHE_PREFIX, itemId.ToString());
            await _cacheService.RemoveAsync(itemCacheKey);
            _logger.LogDebug("Đã xóa cache cụ thể cho medical item detail: {CacheKey}", itemCacheKey);
            await _cacheService.RemoveByPrefixAsync(MEDICAL_ITEM_LIST_PREFIX);
            _logger.LogDebug("Đã xóa cache danh sách medical items với prefix: {Prefix}", MEDICAL_ITEM_LIST_PREFIX);

            await InvalidateAllCachesAsync();

            _logger.LogInformation("Deleted medical item {ItemId}", itemId);

            return BaseResponse<bool>.SuccessResult(true, "Xóa thuốc/vật tư y tế thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting medical item: {ItemId}", itemId);
            return BaseResponse<bool>.ErrorResult($"Lỗi xóa thuốc/vật tư y tế: {ex.Message}");
        }
    }

    public async Task<BaseResponse<MedicalItemResponse>> ApproveMedicalItemAsync(
        Guid itemId, ApproveMedicalItemRequest model)
    {
        try
        {
            var itemRepo = _unitOfWork.GetRepositoryByEntity<MedicalItem>();
            var medicalItem = await itemRepo.GetQueryable()
                .Include(mi => mi.RequestedBy)
                .FirstOrDefaultAsync(mi => mi.Id == itemId && !mi.IsDeleted);

            if (medicalItem == null)
            {
                return BaseResponse<MedicalItemResponse>.ErrorResult("Không tìm thấy thuốc hoặc vật tư y tế.");
            }

            if (medicalItem.ApprovalStatus != MedicalItemApprovalStatus.Pending)
            {
                return BaseResponse<MedicalItemResponse>.ErrorResult(
                    "Thuốc/vật tư này không ở trạng thái chờ phê duyệt.");
            }

            var currentUserId = GetCurrentUserId();
            var managerRoleName = await GetManagerRoleName();

            medicalItem.ApprovalStatus = MedicalItemApprovalStatus.Approved;
            medicalItem.ApprovedById = currentUserId;
            medicalItem.ApprovedAt = DateTime.Now;
            medicalItem.LastUpdatedBy = managerRoleName;
            medicalItem.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();

            await CreateApprovalNotificationAsync(medicalItem, true, model.ApprovalNotes);

            await CreateStockAlertNotificationsAsync(medicalItem);

            // Xóa cache cụ thể
            var itemCacheKey = _cacheService.GenerateCacheKey(MEDICAL_ITEM_CACHE_PREFIX, itemId.ToString());
            await _cacheService.RemoveAsync(itemCacheKey);
            _logger.LogDebug("Đã xóa cache cụ thể cho medical item detail: {CacheKey}", itemCacheKey);
            await _cacheService.RemoveByPrefixAsync(MEDICAL_ITEM_LIST_PREFIX);
            _logger.LogDebug("Đã xóa cache danh sách medical items với prefix: {Prefix}", MEDICAL_ITEM_LIST_PREFIX);
            await _cacheService.RemoveByPrefixAsync(NOTIFICATION_PREFIX);
            _logger.LogDebug("Đã xóa cache notification với prefix: {Prefix}", NOTIFICATION_PREFIX);
            await _cacheService.RemoveByPrefixAsync(NOTIFICATIONS_LIST_PREFIX);
            _logger.LogDebug("Đã xóa cache notifications list với prefix: {Prefix}", NOTIFICATIONS_LIST_PREFIX);

            await InvalidateAllCachesAsync();

            var itemResponse = MapToMedicalItemResponse(medicalItem);

            _logger.LogInformation("Approved medical item {ItemId} by manager {ManagerId}", itemId, currentUserId);

            return BaseResponse<MedicalItemResponse>.SuccessResult(
                itemResponse, "Phê duyệt thuốc/vật tư y tế thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving medical item: {ItemId}", itemId);
            return BaseResponse<MedicalItemResponse>.ErrorResult($"Lỗi phê duyệt thuốc/vật tư y tế: {ex.Message}");
        }
    }

    public async Task<BaseResponse<MedicalItemResponse>> RejectMedicalItemAsync(
        Guid itemId, RejectMedicalItemRequest model)
    {
        try
        {
            var itemRepo = _unitOfWork.GetRepositoryByEntity<MedicalItem>();
            var medicalItem = await itemRepo.GetQueryable()
                .Include(mi => mi.RequestedBy)
                .FirstOrDefaultAsync(mi => mi.Id == itemId && !mi.IsDeleted);

            if (medicalItem == null)
            {
                return BaseResponse<MedicalItemResponse>.ErrorResult("Không tìm thấy thuốc hoặc vật tư y tế.");
            }

            if (medicalItem.ApprovalStatus != MedicalItemApprovalStatus.Pending)
            {
                return BaseResponse<MedicalItemResponse>.ErrorResult(
                    "Thuốc/vật tư này không ở trạng thái chờ phê duyệt.");
            }

            var currentUserId = GetCurrentUserId();
            var managerRoleName = await GetManagerRoleName();

            medicalItem.ApprovalStatus = MedicalItemApprovalStatus.Rejected;
            medicalItem.ApprovedById = currentUserId;
            medicalItem.RejectedAt = DateTime.Now;
            medicalItem.RejectionReason = model.RejectionReason;
            medicalItem.LastUpdatedBy = managerRoleName;
            medicalItem.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();

            await CreateApprovalNotificationAsync(medicalItem, false, model.RejectionReason);

            // Xóa cache cụ thể
            var itemCacheKey = _cacheService.GenerateCacheKey(MEDICAL_ITEM_CACHE_PREFIX, itemId.ToString());
            await _cacheService.RemoveAsync(itemCacheKey);
            _logger.LogDebug("Đã xóa cache cụ thể cho medical item detail: {CacheKey}", itemCacheKey);
            await _cacheService.RemoveByPrefixAsync(MEDICAL_ITEM_LIST_PREFIX);
            _logger.LogDebug("Đã xóa cache danh sách medical items với prefix: {Prefix}", MEDICAL_ITEM_LIST_PREFIX);
            await _cacheService.RemoveByPrefixAsync(NOTIFICATION_PREFIX);
            _logger.LogDebug("Đã xóa cache notification với prefix: {Prefix}", NOTIFICATION_PREFIX);
            await _cacheService.RemoveByPrefixAsync(NOTIFICATIONS_LIST_PREFIX);
            _logger.LogDebug("Đã xóa cache notifications list với prefix: {Prefix}", NOTIFICATIONS_LIST_PREFIX);

            await InvalidateAllCachesAsync();

            var itemResponse = MapToMedicalItemResponse(medicalItem);

            _logger.LogInformation("Rejected medical item {ItemId} by manager {ManagerId}. Reason: {Reason}",
                itemId, currentUserId, model.RejectionReason);

            return BaseResponse<MedicalItemResponse>.SuccessResult(
                itemResponse, "Từ chối thuốc/vật tư y tế thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting medical item: {ItemId}", itemId);
            return BaseResponse<MedicalItemResponse>.ErrorResult($"Lỗi từ chối thuốc/vật tư y tế: {ex.Message}");
        }
    }

    public async Task<BaseListResponse<MedicalItemResponse>> GetPendingApprovalsAsync(
        int pageIndex = 1,
        int pageSize = 10,
        string searchTerm = "",
        string orderBy = null,
        PriorityLevel? priority = null,
        bool? urgentOnly = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(
                "pending_approvals",
                pageIndex.ToString(),
                pageSize.ToString(),
                searchTerm ?? "",
                orderBy ?? "",
                priority?.ToString() ?? "",
                urgentOnly?.ToString() ?? ""
            );

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<MedicalItemResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Pending approvals found in cache with key: {CacheKey}", cacheKey);
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<MedicalItem>().GetQueryable()
                .Include(mi => mi.RequestedBy)
                .Include(mi => mi.ApprovedBy)
                .Where(mi => !mi.IsDeleted && mi.ApprovalStatus == MedicalItemApprovalStatus.Pending)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(mi =>
                    mi.Name.ToLower().Contains(searchTerm) ||
                    (mi.Description != null && mi.Description.ToLower().Contains(searchTerm)) ||
                    (mi.Justification != null && mi.Justification.ToLower().Contains(searchTerm)));
            }

            if (priority.HasValue)
            {
                query = query.Where(mi => mi.Priority == priority.Value);
            }

            if (urgentOnly == true)
            {
                query = query.Where(mi => mi.IsUrgent);
            }

            query = orderBy?.ToLower() switch
            {
                "name" => query.OrderBy(mi => mi.Name),
                "name_desc" => query.OrderByDescending(mi => mi.Name),
                "type" => query.OrderBy(mi => mi.Type),
                "type_desc" => query.OrderByDescending(mi => mi.Type),
                "requested_date" => query.OrderBy(mi => mi.RequestedAt),
                "requested_date_desc" => query.OrderByDescending(mi => mi.RequestedAt),
                _ => query.OrderByDescending(mi => mi.IsUrgent)
                    .ThenByDescending(mi => mi.Priority)
                    .ThenBy(mi => mi.RequestedAt)
            };

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = items.Select(MapToMedicalItemResponse).ToList();

            var result = BaseListResponse<MedicalItemResponse>.SuccessResult(
                responses,
                totalCount,
                pageSize,
                pageIndex,
                "Lấy danh sách chờ phê duyệt thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICAL_ITEM_CACHE_SET);
            _logger.LogDebug("Cached pending approvals with key: {CacheKey}", cacheKey);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending approvals");
            return BaseListResponse<MedicalItemResponse>.ErrorResult("Lỗi lấy danh sách chờ phê duyệt.");
        }
    }

    #endregion

    #region Stock Management

    public async Task<BaseResponse<MedicalItemStockSummaryResponse>> GetStockSummaryAsync()
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(STATISTICS_PREFIX, "stock_summary");
            var cachedResult = await _cacheService.GetAsync<BaseResponse<MedicalItemStockSummaryResponse>>(cacheKey);

            if (cachedResult != null)
            {
                _logger.LogDebug("Stock summary found in cache with key: {CacheKey}", cacheKey);
                return cachedResult;
            }

            var itemRepo = _unitOfWork.GetRepositoryByEntity<MedicalItem>();
            var items = await itemRepo.GetQueryable()
                .Where(mi => !mi.IsDeleted)
                .ToListAsync();

            var summary = new MedicalItemStockSummaryResponse
            {
                TotalItems = items.Count,
                LowStockItems = items.Count(i => i.Quantity <= 10),
                ExpiredItems = items.Count(i => i.ExpiryDate.HasValue && i.ExpiryDate.Value <= DateTime.Now),
                ExpiringSoonItems = items.Count(i => i.ExpiryDate.HasValue &&
                                                     i.ExpiryDate.Value <= DateTime.Now.AddDays(30) &&
                                                     i.ExpiryDate.Value > DateTime.Now),
                MedicationCount = items.Count(i => i.Type == "Medication"),
                SupplyCount = items.Count(i => i.Type == "Supply")
            };

            var result = BaseResponse<MedicalItemStockSummaryResponse>.SuccessResult(
                summary, "Lấy tổng quan tồn kho thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICAL_ITEM_CACHE_SET);
            _logger.LogDebug("Cached stock summary with key: {CacheKey}", cacheKey);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock summary");
            return BaseResponse<MedicalItemStockSummaryResponse>.ErrorResult("Lỗi lấy tổng quan tồn kho.");
        }
    }

    public async Task<BaseResponse<bool>> UpdateStockQuantityAsync(Guid itemId, int newQuantity, string reason)
    {
        try
        {
            var itemRepo = _unitOfWork.GetRepositoryByEntity<MedicalItem>();
            var medicalItem = await itemRepo.GetQueryable()
                .FirstOrDefaultAsync(mi => mi.Id == itemId && !mi.IsDeleted);

            if (medicalItem == null)
            {
                return BaseResponse<bool>.ErrorResult("Không tìm thấy thuốc hoặc vật tư y tế.");
            }

            var oldQuantity = medicalItem.Quantity;
            var schoolNurseRoleName = await GetSchoolNurseRoleName();

            medicalItem.Quantity = newQuantity;
            medicalItem.LastUpdatedBy = schoolNurseRoleName;
            medicalItem.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();

            await CreateStockChangeNotificationAsync(medicalItem, oldQuantity, newQuantity, reason);
            await CreateStockAlertNotificationsAsync(medicalItem);

            // Xóa cache cụ thể
            var itemCacheKey = _cacheService.GenerateCacheKey(MEDICAL_ITEM_CACHE_PREFIX, itemId.ToString());
            await _cacheService.RemoveAsync(itemCacheKey);
            _logger.LogDebug("Đã xóa cache cụ thể cho medical item detail: {CacheKey}", itemCacheKey);
            await _cacheService.RemoveByPrefixAsync(MEDICAL_ITEM_LIST_PREFIX);
            _logger.LogDebug("Đã xóa cache danh sách medical items với prefix: {Prefix}", MEDICAL_ITEM_LIST_PREFIX);
            await _cacheService.RemoveByPrefixAsync(NOTIFICATION_PREFIX);
            _logger.LogDebug("Đã xóa cache notification với prefix: {Prefix}", NOTIFICATION_PREFIX);
            await _cacheService.RemoveByPrefixAsync(NOTIFICATIONS_LIST_PREFIX);
            _logger.LogDebug("Đã xóa cache notifications list với prefix: {Prefix}", NOTIFICATIONS_LIST_PREFIX);

            await InvalidateAllCachesAsync();

            _logger.LogInformation(
                "Updated stock quantity for item {ItemId} from {OldQuantity} to {NewQuantity}, reason: {Reason}",
                itemId, oldQuantity, newQuantity, reason);

            return BaseResponse<bool>.SuccessResult(true, "Cập nhật số lượng tồn kho thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock quantity for item: {ItemId}", itemId);
            return BaseResponse<bool>.ErrorResult($"Lỗi cập nhật số lượng tồn kho: {ex.Message}");
        }
    }

    #endregion

    #region Medical Item Usage Management

    public async Task<BaseResponse<MedicalItemUsageResponse>> RecordMedicalItemUsageAsync(
        CreateMedicalItemUsageRequest model)
    {
        try
        {
            var validationResult = await _createUsageValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BaseResponse<MedicalItemUsageResponse>.ErrorResult(errors);
            }

            var itemRepo = _unitOfWork.GetRepositoryByEntity<MedicalItem>();
            var medicalItem = await itemRepo.GetQueryable()
                .FirstOrDefaultAsync(mi => mi.Id == model.MedicalItemId && !mi.IsDeleted);

            if (medicalItem == null)
            {
                return BaseResponse<MedicalItemUsageResponse>.ErrorResult("Không tìm thấy thuốc/vật tư y tế.");
            }

            if (medicalItem.ApprovalStatus != MedicalItemApprovalStatus.Approved)
            {
                var statusText = GetApprovalStatusDisplayName(medicalItem.ApprovalStatus);
                return BaseResponse<MedicalItemUsageResponse>.ErrorResult(
                    $"Không thể sử dụng thuốc/vật tư y tế chưa được phê duyệt. Trạng thái hiện tại: {statusText}");
            }

            if (medicalItem.Quantity < model.Quantity)
            {
                return BaseResponse<MedicalItemUsageResponse>.ErrorResult(
                    $"Số lượng sử dụng ({model.Quantity}) vượt quá tồn kho hiện tại ({medicalItem.Quantity}).");
            }

            var healthEventRepo = _unitOfWork.GetRepositoryByEntity<HealthEvent>();
            var healthEvent = await healthEventRepo.GetQueryable()
                .Include(he => he.Student)
                .FirstOrDefaultAsync(he => he.Id == model.HealthEventId && !he.IsDeleted);

            if (healthEvent == null)
            {
                return BaseResponse<MedicalItemUsageResponse>.ErrorResult("Không tìm thấy sự kiện y tế.");
            }

            var currentUserId = GetCurrentUserId();
            var schoolNurseRoleName = await GetSchoolNurseRoleName();

            var usage = _mapper.Map<MedicalItemUsage>(model);
            usage.Id = Guid.NewGuid();
            usage.UsedById = currentUserId;
            usage.UsedAt = DateTime.Now;
            usage.CreatedBy = schoolNurseRoleName;
            usage.CreatedDate = DateTime.Now;

            var usageRepo = _unitOfWork.GetRepositoryByEntity<MedicalItemUsage>();
            await usageRepo.AddAsync(usage);

            medicalItem.Quantity -= (int)model.Quantity;
            medicalItem.LastUpdatedBy = schoolNurseRoleName;
            medicalItem.LastUpdatedDate = DateTime.Now;

            await CreateMedicationUsageNotificationAsync(healthEvent.Student, medicalItem, usage);

            await _unitOfWork.SaveChangesAsync();

            // Xóa cache cụ thể
            var itemCacheKey = _cacheService.GenerateCacheKey(MEDICAL_ITEM_CACHE_PREFIX, model.MedicalItemId.ToString());
            await _cacheService.RemoveAsync(itemCacheKey);
            _logger.LogDebug("Đã xóa cache cụ thể cho medical item detail: {CacheKey}", itemCacheKey);
            await _cacheService.RemoveByPrefixAsync(MEDICAL_ITEM_LIST_PREFIX);
            _logger.LogDebug("Đã xóa cache danh sách medical items với prefix: {Prefix}", MEDICAL_ITEM_LIST_PREFIX);
            await _cacheService.RemoveByPrefixAsync(MEDICAL_ITEM_USAGE_LIST_PREFIX);
            _logger.LogDebug("Đã xóa cache danh sách medical item usages với prefix: {Prefix}", MEDICAL_ITEM_USAGE_LIST_PREFIX);
            await _cacheService.RemoveByPrefixAsync(NOTIFICATION_PREFIX);
            _logger.LogDebug("Đã xóa cache notification với prefix: {Prefix}", NOTIFICATION_PREFIX);
            await _cacheService.RemoveByPrefixAsync(NOTIFICATIONS_LIST_PREFIX);
            _logger.LogDebug("Đã xóa cache notifications list với prefix: {Prefix}", NOTIFICATIONS_LIST_PREFIX);

            await InvalidateAllCachesAsync();

            usage = await usageRepo.GetQueryable()
                .Include(miu => miu.MedicalItem)
                .Include(miu => miu.HealthEvent)
                .ThenInclude(he => he.Student)
                .Include(miu => miu.UsedBy)
                .FirstOrDefaultAsync(miu => miu.Id == usage.Id);

            var usageResponse = MapToUsageResponse(usage);

            _logger.LogInformation("Recorded medical item usage {UsageId} for item {ItemId}, student {StudentId}",
                usage.Id, model.MedicalItemId, healthEvent.Student.Id);

            return BaseResponse<MedicalItemUsageResponse>.SuccessResult(
                usageResponse, "Ghi nhận sử dụng thuốc/vật tư y tế thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording medical item usage");
            return BaseResponse<MedicalItemUsageResponse>.ErrorResult(
                $"Lỗi ghi nhận sử dụng thuốc/vật tư y tế: {ex.Message}");
        }
    }

    public async Task<BaseResponse<MedicalItemUsageResponse>> CorrectMedicalItemUsageAsync(
        Guid originalUsageId,
        CorrectMedicalItemUsageRequest request)
    {
        try
        {
            var validationResult = await _createUsageValidator.ValidateAsync(request.CorrectedData);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BaseResponse<MedicalItemUsageResponse>.ErrorResult(errors);
            }

            var usageRepo = _unitOfWork.GetRepositoryByEntity<MedicalItemUsage>();
            var originalUsage = await usageRepo.GetQueryable()
                .Include(miu => miu.MedicalItem)
                .Include(miu => miu.HealthEvent)
                .ThenInclude(he => he.Student)
                .FirstOrDefaultAsync(miu => miu.Id == originalUsageId && !miu.IsDeleted);

            if (originalUsage == null)
            {
                return BaseResponse<MedicalItemUsageResponse>.ErrorResult("Không tìm thấy bản ghi sử dụng gốc.");
            }

            var itemRepo = _unitOfWork.GetRepositoryByEntity<MedicalItem>();
            var medicalItem = await itemRepo.GetQueryable()
                .FirstOrDefaultAsync(mi => mi.Id == request.CorrectedData.MedicalItemId && !mi.IsDeleted);

            if (medicalItem == null)
            {
                return BaseResponse<MedicalItemUsageResponse>.ErrorResult("Không tìm thấy thuốc/vật tư y tế.");
            }

            var quantityDifference = request.CorrectedData.Quantity - originalUsage.Quantity;

            if (quantityDifference > 0 && medicalItem.Quantity < quantityDifference)
            {
                return BaseResponse<MedicalItemUsageResponse>.ErrorResult(
                    $"Số lượng điều chỉnh thêm ({quantityDifference}) vượt quá tồn kho hiện tại ({medicalItem.Quantity}).");
            }

            var currentUserId = GetCurrentUserId();
            var schoolNurseRoleName = await GetSchoolNurseRoleName();

            originalUsage.IsDeleted = true;
            originalUsage.LastUpdatedBy = schoolNurseRoleName;
            originalUsage.LastUpdatedDate = DateTime.Now;

            medicalItem.Quantity += (int)originalUsage.Quantity;

            var correctedUsage = _mapper.Map<MedicalItemUsage>(request.CorrectedData);
            correctedUsage.Id = Guid.NewGuid();
            correctedUsage.UsedById = currentUserId;
            correctedUsage.UsedAt = DateTime.Now;
            correctedUsage.CreatedBy = schoolNurseRoleName;
            correctedUsage.CreatedDate = DateTime.Now;
            correctedUsage.Notes = $"ĐIỀU CHỈNH: {request.CorrectionReason}. Thay thế cho usage {originalUsageId}. " +
                                   $"Dữ liệu gốc: {originalUsage.Quantity} {originalUsage.MedicalItem?.Unit ?? "đơn vị"}. " +
                                   $"{correctedUsage.Notes ?? ""}".Trim();

            await usageRepo.AddAsync(correctedUsage);

            medicalItem.Quantity -= (int)request.CorrectedData.Quantity;
            medicalItem.LastUpdatedBy = schoolNurseRoleName;
            medicalItem.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();

            // Xóa cache cụ thể
            var itemCacheKey = _cacheService.GenerateCacheKey(MEDICAL_ITEM_CACHE_PREFIX, request.CorrectedData.MedicalItemId.ToString());
            await _cacheService.RemoveAsync(itemCacheKey);
            _logger.LogDebug("Đã xóa cache cụ thể cho medical item detail: {CacheKey}", itemCacheKey);
            await _cacheService.RemoveByPrefixAsync(MEDICAL_ITEM_LIST_PREFIX);
            _logger.LogDebug("Đã xóa cache danh sách medical items với prefix: {Prefix}", MEDICAL_ITEM_LIST_PREFIX);
            await _cacheService.RemoveByPrefixAsync(MEDICAL_ITEM_USAGE_LIST_PREFIX);
            _logger.LogDebug("Đã xóa cache danh sách medical item usages với prefix: {Prefix}", MEDICAL_ITEM_USAGE_LIST_PREFIX);

            await InvalidateAllCachesAsync();

            correctedUsage = await usageRepo.GetQueryable()
                .Include(miu => miu.MedicalItem)
                .Include(miu => miu.HealthEvent)
                .ThenInclude(he => he.Student)
                .Include(miu => miu.UsedBy)
                .FirstOrDefaultAsync(miu => miu.Id == correctedUsage.Id);

            var usageResponse = MapToUsageResponse(correctedUsage);

            _logger.LogInformation(
                "Corrected medical item usage {OriginalUsageId} → {NewUsageId}. Reason: {Reason}",
                originalUsageId, correctedUsage.Id, request.CorrectionReason);

            return BaseResponse<MedicalItemUsageResponse>.SuccessResult(
                usageResponse, "Điều chỉnh sử dụng thuốc/vật tư y tế thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error correcting medical item usage: {OriginalUsageId}", originalUsageId);
            return BaseResponse<MedicalItemUsageResponse>.ErrorResult(
                $"Lỗi điều chỉnh sử dụng thuốc/vật tư y tế: {ex.Message}");
        }
    }

    public async Task<BaseResponse<MedicalItemUsageResponse>> ReturnMedicalItemAsync(
        Guid originalUsageId,
        ReturnMedicalItemRequest request)
    {
        try
        {
            var usageRepo = _unitOfWork.GetRepositoryByEntity<MedicalItemUsage>();
            var originalUsage = await usageRepo.GetQueryable()
                .Include(miu => miu.MedicalItem)
                .Include(miu => miu.HealthEvent)
                .ThenInclude(he => he.Student)
                .FirstOrDefaultAsync(miu => miu.Id == originalUsageId && !miu.IsDeleted);

            if (originalUsage == null)
            {
                return BaseResponse<MedicalItemUsageResponse>.ErrorResult("Không tìm thấy bản ghi sử dụng gốc.");
            }

            if (request.ReturnQuantity <= 0 || request.ReturnQuantity > originalUsage.Quantity)
            {
                return BaseResponse<MedicalItemUsageResponse>.ErrorResult(
                    $"Số lượng hoàn trả không hợp lệ. Phải từ 0 đến {originalUsage.Quantity}.");
            }

            var currentUserId = GetCurrentUserId();
            var schoolNurseRoleName = await GetSchoolNurseRoleName();

            var returnUsage = new MedicalItemUsage
            {
                Id = Guid.NewGuid(),
                MedicalItemId = originalUsage.MedicalItemId,
                HealthEventId = originalUsage.HealthEventId,
                Quantity = -request.ReturnQuantity,
                Notes =
                    $"HOÀN TRẢ: {request.ReturnReason}. Hoàn trả {request.ReturnQuantity} từ usage {originalUsageId}.",
                UsedAt = DateTime.Now,
                UsedById = currentUserId,
                CreatedBy = schoolNurseRoleName,
                CreatedDate = DateTime.Now
            };

            await usageRepo.AddAsync(returnUsage);

            originalUsage.MedicalItem.Quantity += (int)request.ReturnQuantity;
            originalUsage.MedicalItem.LastUpdatedBy = schoolNurseRoleName;
            originalUsage.MedicalItem.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();

            // Xóa cache cụ thể
            var itemCacheKey = _cacheService.GenerateCacheKey(MEDICAL_ITEM_CACHE_PREFIX, originalUsage.MedicalItemId.ToString());
            await _cacheService.RemoveAsync(itemCacheKey);
            _logger.LogDebug("Đã xóa cache cụ thể cho medical item detail: {CacheKey}", itemCacheKey);
            await _cacheService.RemoveByPrefixAsync(MEDICAL_ITEM_LIST_PREFIX);
            _logger.LogDebug("Đã xóa cache danh sách medical items với prefix: {Prefix}", MEDICAL_ITEM_LIST_PREFIX);
            await _cacheService.RemoveByPrefixAsync(MEDICAL_ITEM_USAGE_LIST_PREFIX);
            _logger.LogDebug("Đã xóa cache danh sách medical item usages với prefix: {Prefix}", MEDICAL_ITEM_USAGE_LIST_PREFIX);

            await InvalidateAllCachesAsync();

            returnUsage = await usageRepo.GetQueryable()
                .Include(miu => miu.MedicalItem)
                .Include(miu => miu.HealthEvent)
                .ThenInclude(he => he.Student)
                .Include(miu => miu.UsedBy)
                .FirstOrDefaultAsync(miu => miu.Id == returnUsage.Id);

            var usageResponse = MapToUsageResponse(returnUsage);

            _logger.LogInformation(
                "Created return entry for usage {OriginalUsageId}. Returned quantity: {ReturnQuantity}",
                originalUsageId, request.ReturnQuantity);

            return BaseResponse<MedicalItemUsageResponse>.SuccessResult(
                usageResponse, "Hoàn trả thuốc/vật tư y tế thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error returning medical item for usage: {OriginalUsageId}", originalUsageId);
            return BaseResponse<MedicalItemUsageResponse>.ErrorResult($"Lỗi hoàn trả thuốc/vật tư y tế: {ex.Message}");
        }
    }

    public async Task<BaseListResponse<MedicalItemUsageResponse>> GetUsageHistoryAsync(
        int pageIndex,
        int pageSize,
        string searchTerm,
        string orderBy,
        Guid? medicalItemId = null,
        Guid? studentId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(
                MEDICAL_ITEM_USAGE_LIST_PREFIX,
                pageIndex.ToString(),
                pageSize.ToString(),
                searchTerm ?? "",
                orderBy ?? "",
                medicalItemId?.ToString() ?? "",
                studentId?.ToString() ?? "",
                fromDate?.ToString("yyyy-MM-dd") ?? "",
                toDate?.ToString("yyyy-MM-dd") ?? ""
            );

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<MedicalItemUsageResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Medical item usages list found in cache with key: {CacheKey}", cacheKey);
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<MedicalItemUsage>().GetQueryable()
                .Include(miu => miu.MedicalItem)
                .Include(miu => miu.HealthEvent)
                .ThenInclude(he => he.Student)
                .Include(miu => miu.UsedBy)
                .Where(miu => !miu.IsDeleted)
                .AsQueryable();

            query = ApplyUsageFilters(query, searchTerm, medicalItemId, studentId, fromDate, toDate);
            query = ApplyUsageOrdering(query, orderBy);

            var totalCount = await query.CountAsync(cancellationToken);
            var usages = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = usages.Select(MapToUsageResponse).ToList();

            var result = BaseListResponse<MedicalItemUsageResponse>.SuccessResult(
                responses,
                totalCount,
                pageSize,
                pageIndex,
                "Lấy lịch sử sử dụng thuốc/vật tư thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICAL_ITEM_CACHE_SET);
            _logger.LogDebug("Cached medical item usage history with key: {CacheKey}", cacheKey);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medical item usage history");
            return BaseListResponse<MedicalItemUsageResponse>.ErrorResult("Lỗi lấy lịch sử sử dụng thuốc/vật tư.");
        }
    }

    public async Task<BaseListResponse<MedicalItemUsageResponse>> GetUsageHistoryByStudentAsync(
        Guid studentId,
        int pageIndex,
        int pageSize,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(
                MEDICAL_ITEM_USAGE_LIST_PREFIX,
                "by_student",
                studentId.ToString(),
                pageIndex.ToString(),
                pageSize.ToString(),
                fromDate?.ToString("yyyy-MM-dd") ?? "",
                toDate?.ToString("yyyy-MM-dd") ?? ""
            );

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<MedicalItemUsageResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Usage history by student found in cache with key: {CacheKey}", cacheKey);
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<MedicalItemUsage>().GetQueryable()
                .Include(miu => miu.MedicalItem)
                .Include(miu => miu.HealthEvent)
                .ThenInclude(he => he.Student)
                .Include(miu => miu.UsedBy)
                .Where(miu => miu.HealthEvent.Student.Id == studentId && !miu.IsDeleted)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                query = query.Where(miu => miu.UsedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(miu => miu.UsedAt <= toDate.Value.AddDays(1));
            }

            query = query.OrderByDescending(miu => miu.UsedAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var usages = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = usages.Select(MapToUsageResponse).ToList();

            var result = BaseListResponse<MedicalItemUsageResponse>.SuccessResult(
                responses,
                totalCount,
                pageSize,
                pageIndex,
                "Lấy lịch sử sử dụng theo học sinh thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICAL_ITEM_CACHE_SET);
            _logger.LogDebug("Cached usage history by student with key: {CacheKey}", cacheKey);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage history by student: {StudentId}", studentId);
            return BaseListResponse<MedicalItemUsageResponse>.ErrorResult("Lỗi lấy lịch sử sử dụng theo học sinh.");
        }
    }

    #endregion

    #region Helper Methods

    private async Task<List<string>> GetCurrentUserRolesAsync()
    {
        try
        {
            return await UserHelper.GetCurrentUserRolesFromDatabaseAsync(_httpContextAccessor.HttpContext, _unitOfWork);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user roles from database");
            return new List<string>();
        }
    }

    private async Task<bool> HasRoleAsync(string roleName)
    {
        try
        {
            return await UserHelper.HasRoleAsync(_httpContextAccessor.HttpContext, _unitOfWork, roleName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user role: {RoleName}", roleName);
            return false;
        }
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(UserHelper.GetCurrentUserId(_httpContextAccessor.HttpContext));
    }

    private async Task<string> GetManagerRoleName()
    {
        try
        {
            var managerRole = await _unitOfWork.GetRepositoryByEntity<Role>().GetQueryable()
                .FirstOrDefaultAsync(m => m.Name == "MANAGER");
            return managerRole?.Name ?? "MANAGER";
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting manager role name");
            return "MANAGER";
        }
    }

    private async Task<string> GetSchoolNurseRoleName()
    {
        try
        {
            var schoolNurseRole = await _unitOfWork.GetRepositoryByEntity<Role>().GetQueryable()
                .FirstOrDefaultAsync(r => r.Name == "SCHOOLNURSE");
            return schoolNurseRole?.Name ?? "SCHOOLNURSE";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting school nurse role name");
            return "SCHOOLNURSE";
        }
    }

    private MedicalItemResponse MapToMedicalItemResponse(MedicalItem medicalItem)
    {
        var response = _mapper.Map<MedicalItemResponse>(medicalItem);

        response.FormDisplayName = GetFormDisplayName(medicalItem.Form);
        response.IsExpiringSoon = medicalItem.ExpiryDate.HasValue &&
                                  medicalItem.ExpiryDate.Value <= DateTime.Now.AddDays(30) &&
                                  medicalItem.ExpiryDate.Value > DateTime.Now;
        response.IsExpired = medicalItem.ExpiryDate.HasValue && medicalItem.ExpiryDate.Value <= DateTime.Now;
        response.IsLowStock = medicalItem.Quantity <= 10;
        response.StatusText = GetStatusText(medicalItem);

        response.Status = medicalItem.ApprovalStatus.ToString();
        response.StatusDisplayName = GetApprovalStatusDisplayName(medicalItem.ApprovalStatus);
        response.Priority = medicalItem.Priority.ToString();
        response.PriorityDisplayName = GetPriorityDisplayName(medicalItem.Priority);

        if (medicalItem.RequestedBy != null)
        {
            response.RequestedByName = medicalItem.RequestedBy.FullName;
            response.RequestedByStaffCode = medicalItem.RequestedBy.StaffCode;
        }

        if (medicalItem.ApprovedBy != null)
        {
            response.ApprovedByName = medicalItem.ApprovedBy.FullName;
            response.ApprovedByStaffCode = medicalItem.ApprovedBy.StaffCode;
        }

        var currentUserRoles = UserHelper.GetCurrentUserRoles(_httpContextAccessor.HttpContext);
        var hasManagerRole = currentUserRoles.Any(r => r.Equals("MANAGER", StringComparison.OrdinalIgnoreCase));

        response.CanApprove = hasManagerRole &&
                              medicalItem.ApprovalStatus == MedicalItemApprovalStatus.Pending;

        response.CanReject = hasManagerRole &&
                             medicalItem.ApprovalStatus == MedicalItemApprovalStatus.Pending;

        response.CanUse = medicalItem.ApprovalStatus == MedicalItemApprovalStatus.Approved;

        return response;
    }

    private MedicalItemUsageResponse MapToUsageResponse(MedicalItemUsage usage)
    {
        var response = _mapper.Map<MedicalItemUsageResponse>(usage);

        if (usage.MedicalItem != null)
        {
            response.MedicalItemName = usage.MedicalItem.Name;
            response.MedicalItemType = usage.MedicalItem.Type;
            response.Unit = usage.MedicalItem.Unit;
        }

        if (usage.HealthEvent != null)
        {
            response.HealthEventDescription = usage.HealthEvent.Description;

            if (usage.HealthEvent.Student != null)
            {
                response.StudentName = usage.HealthEvent.Student.FullName;
                response.StudentCode = usage.HealthEvent.Student.StudentCode;
            }
        }

        if (usage.UsedBy != null)
        {
            response.UsedByName = usage.UsedBy.FullName;
        }

        return response;
    }

    private string GetPriorityDisplayName(PriorityLevel priority)
    {
        return priority switch
        {
            PriorityLevel.Low => "Thấp",
            PriorityLevel.Normal => "Bình thường",
            PriorityLevel.High => "Cao",
            PriorityLevel.Critical => "Khẩn cấp",
            _ => priority.ToString()
        };
    }

    private string GetApprovalStatusDisplayName(MedicalItemApprovalStatus status)
    {
        return status switch
        {
            MedicalItemApprovalStatus.Pending => "Chờ phê duyệt",
            MedicalItemApprovalStatus.Approved => "Đã phê duyệt",
            MedicalItemApprovalStatus.Rejected => "Bị từ chối",
            _ => status.ToString()
        };
    }

    private string GetFormDisplayName(MedicationForm? form)
    {
        if (!form.HasValue) return "";

        return form.Value switch
        {
            MedicationForm.Tablet => "Viên",
            MedicationForm.Syrup => "Siro",
            MedicationForm.Injection => "Tiêm",
            MedicationForm.Cream => "Kem",
            MedicationForm.Drops => "Nhỏ giọt",
            MedicationForm.Inhaler => "Hít",
            MedicationForm.Other => "Khác",
            _ => form.ToString()
        };
    }

    private string GetStatusText(MedicalItem item)
    {
        var statuses = new List<string>();

        if (item.ExpiryDate.HasValue && item.ExpiryDate.Value <= DateTime.Now)
        {
            statuses.Add("Hết hạn");
        }
        else if (item.ExpiryDate.HasValue && item.ExpiryDate.Value <= DateTime.Now.AddDays(30))
        {
            statuses.Add("Gần hết hạn");
        }

        if (item.Quantity <= 10)
        {
            statuses.Add("Tồn kho thấp");
        }

        if (item.Quantity == 0)
        {
            statuses.Add("Hết hàng");
        }

        return statuses.Any() ? string.Join(", ", statuses) : "Bình thường";
    }

    private IQueryable<MedicalItem> ApplyMedicalItemFilters(
        IQueryable<MedicalItem> query,
        string searchTerm,
        string? type,
        bool? lowStock,
        bool? expiringSoon,
        bool? expired,
        int expiryDays = 30,
        MedicalItemApprovalStatus? approvalStatus = null,
        PriorityLevel? priority = null,
        bool? urgentOnly = null)
    {
        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(mi =>
                mi.Name.ToLower().Contains(searchTerm) ||
                (mi.Description != null && mi.Description.ToLower().Contains(searchTerm)) ||
                (mi.Dosage != null && mi.Dosage.ToLower().Contains(searchTerm)) ||
                (mi.Justification != null &&
                 mi.Justification.ToLower().Contains(searchTerm)));
        }

        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(mi => mi.Type == type);
        }

        if (lowStock == true)
        {
            query = query.Where(mi => mi.Quantity <= 10);
        }

        if (expiringSoon == true)
        {
            var cutoffDate = DateTime.Now.AddDays(expiryDays);
            query = query.Where(mi => mi.ExpiryDate.HasValue &&
                                      mi.ExpiryDate.Value <= cutoffDate &&
                                      mi.ExpiryDate.Value > DateTime.Now);
        }

        if (expired == true)
        {
            query = query.Where(mi => mi.ExpiryDate.HasValue && mi.ExpiryDate.Value <= DateTime.Now);
        }

        if (approvalStatus.HasValue)
        {
            query = query.Where(mi => mi.ApprovalStatus == approvalStatus.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(mi => mi.Priority == priority.Value);
        }

        if (urgentOnly == true)
        {
            query = query.Where(mi => mi.IsUrgent);
        }

        return query;
    }

    private IQueryable<MedicalItem> ApplyAlertsFilter(IQueryable<MedicalItem> query, string alertsOnly, int expiryDays)
    {
        return alertsOnly.ToLower() switch
        {
            "low_stock" => query.Where(mi => mi.Quantity <= 10),
            "expired" => query.Where(mi => mi.ExpiryDate.HasValue && mi.ExpiryDate.Value <= DateTime.Now),
            "expiring_soon" => query.Where(mi => mi.ExpiryDate.HasValue &&
                                                 mi.ExpiryDate.Value <= DateTime.Now.AddDays(expiryDays) &&
                                                 mi.ExpiryDate.Value > DateTime.Now),
            "all_alerts" => query.Where(mi =>
                mi.Quantity <= 10 ||
                (mi.ExpiryDate.HasValue && mi.ExpiryDate.Value <= DateTime.Now.AddDays(expiryDays))),
            "critical" => query.Where(mi =>
                mi.Quantity <= 5 ||
                (mi.ExpiryDate.HasValue && mi.ExpiryDate.Value <= DateTime.Now) ||
                (mi.ExpiryDate.HasValue && mi.ExpiryDate.Value <= DateTime.Now.AddDays(7))),
            "out_of_stock" => query.Where(mi => mi.Quantity == 0),
            _ => query
        };
    }

    private IQueryable<MedicalItem> ApplyAlertsOrdering(IQueryable<MedicalItem> query, string alertsOnly)
    {
        return alertsOnly.ToLower() switch
        {
            "low_stock" or "out_of_stock" => query.OrderBy(mi => mi.Quantity).ThenBy(mi => mi.ExpiryDate),
            "expired" or "expiring_soon" => query.OrderBy(mi => mi.ExpiryDate).ThenBy(mi => mi.Quantity),
            "critical" => query.OrderBy(mi => mi.Quantity).ThenBy(mi => mi.ExpiryDate),
            "all_alerts" => query.OrderBy(mi => mi.Quantity).ThenBy(mi => mi.ExpiryDate),
            _ => query.OrderByDescending(mi => mi.CreatedDate)
        };
    }

    private string GetSuccessMessage(string alertsOnly, int? expiryDays,
        MedicalItemApprovalStatus? approvalStatus = null)
    {
        if (!string.IsNullOrEmpty(alertsOnly))
        {
            return alertsOnly.ToLower() switch
            {
                "low_stock" => "Lấy danh sách thuốc/vật tư tồn kho thấp thành công.",
                "expired" => "Lấy danh sách thuốc/vật tư hết hạn thành công.",
                "expiring_soon" => $"Lấy danh sách thuốc/vật tư gần hết hạn trong {expiryDays} ngày thành công.",
                "all_alerts" => "Lấy danh sách cảnh báo tồn kho thành công.",
                "critical" => "Lấy danh sách cảnh báo nghiêm trọng thành công.",
                "out_of_stock" => "Lấy danh sách thuốc/vật tư hết hàng thành công.",
                _ => "Lấy danh sách thuốc và vật tư y tế thành công."
            };
        }

        if (approvalStatus.HasValue)
        {
            return approvalStatus.Value switch
            {
                MedicalItemApprovalStatus.Pending => "Lấy danh sách thuốc/vật tư chờ phê duyệt thành công.",
                MedicalItemApprovalStatus.Approved => "Lấy danh sách thuốc/vật tư đã phê duyệt thành công.",
                MedicalItemApprovalStatus.Rejected => "Lấy danh sách thuốc/vật tư bị từ chối thành công.",
                _ => "Lấy danh sách thuốc và vật tư y tế thành công."
            };
        }

        return "Lấy danh sách thuốc và vật tư y tế thành công.";
    }

    private IQueryable<MedicalItem> ApplyMedicalItemOrdering(IQueryable<MedicalItem> query, string orderBy)
    {
        return orderBy?.ToLower() switch
        {
            "name" => query.OrderBy(mi => mi.Name),
            "name_desc" => query.OrderByDescending(mi => mi.Name),
            "type" => query.OrderBy(mi => mi.Type),
            "type_desc" => query.OrderByDescending(mi => mi.Type),
            "quantity" => query.OrderBy(mi => mi.Quantity),
            "quantity_desc" => query.OrderByDescending(mi => mi.Quantity),
            "expirydate" => query.OrderBy(mi => mi.ExpiryDate),
            "expirydate_desc" => query.OrderByDescending(mi => mi.ExpiryDate),
            "createdate" => query.OrderBy(mi => mi.CreatedDate),
            "createdate_desc" => query.OrderByDescending(mi => mi.CreatedDate),
            _ => query.OrderByDescending(mi => mi.CreatedDate)
        };
    }

    private IQueryable<MedicalItemUsage> ApplyUsageFilters(
        IQueryable<MedicalItemUsage> query,
        string searchTerm,
        Guid? medicalItemId,
        Guid? studentId,
        DateTime? fromDate,
        DateTime? toDate)
    {
        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(miu =>
                miu.MedicalItem.Name.ToLower().Contains(searchTerm) ||
                miu.HealthEvent.Student.FullName.ToLower().Contains(searchTerm) ||
                miu.HealthEvent.Student.StudentCode.ToLower().Contains(searchTerm) ||
                (miu.Notes != null && miu.Notes.ToLower().Contains(searchTerm)));
        }

        if (medicalItemId.HasValue)
        {
            query = query.Where(miu => miu.MedicalItemId == medicalItemId.Value);
        }

        if (studentId.HasValue)
        {
            query = query.Where(miu => miu.HealthEvent.Student.Id == studentId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(miu => miu.UsedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(miu => miu.UsedAt <= toDate.Value.AddDays(1));
        }

        return query;
    }

    private IQueryable<MedicalItemUsage> ApplyUsageOrdering(IQueryable<MedicalItemUsage> query, string orderBy)
    {
        return orderBy?.ToLower() switch
        {
            "itemname" => query.OrderBy(miu => miu.MedicalItem.Name),
            "itemname_desc" => query.OrderByDescending(miu => miu.MedicalItem.Name),
            "studentname" => query.OrderBy(miu => miu.HealthEvent.Student.FullName),
            "studentname_desc" => query.OrderByDescending(miu => miu.HealthEvent.Student.FullName),
            "quantity" => query.OrderBy(miu => miu.Quantity),
            "quantity_desc" => query.OrderByDescending(miu => miu.Quantity),
            "usedat" => query.OrderBy(miu => miu.UsedAt),
            "usedat_desc" => query.OrderByDescending(miu => miu.UsedAt),
            "createdate" => query.OrderBy(miu => miu.CreatedDate),
            "createdate_desc" => query.OrderByDescending(miu => miu.CreatedDate),
            _ => query.OrderByDescending(miu => miu.UsedAt)
        };
    }

    private async Task InvalidateAllCachesAsync()
    {
        try
        {
            _logger.LogDebug("Starting comprehensive cache invalidation for medical items, usages, and related entities");
            var keysBefore = await _cacheService.GetKeysByPatternAsync("*medical_item*");
            _logger.LogDebug("Cache keys before invalidation: {Keys}", string.Join(", ", keysBefore));

            await Task.WhenAll(
                _cacheService.InvalidateTrackingSetAsync(MEDICAL_ITEM_CACHE_SET),
                _cacheService.RemoveByPrefixAsync(MEDICAL_ITEM_CACHE_PREFIX),
                _cacheService.RemoveByPrefixAsync(MEDICAL_ITEM_LIST_PREFIX),
                _cacheService.RemoveByPrefixAsync(MEDICAL_ITEM_USAGE_CACHE_PREFIX),
                _cacheService.RemoveByPrefixAsync(MEDICAL_ITEM_USAGE_LIST_PREFIX),
                _cacheService.RemoveByPrefixAsync(STATISTICS_PREFIX),
                _cacheService.RemoveByPrefixAsync(NOTIFICATION_PREFIX),
                _cacheService.RemoveByPrefixAsync(NOTIFICATIONS_LIST_PREFIX)
            );

            var keysAfter = await _cacheService.GetKeysByPatternAsync("*medical_item*");
            _logger.LogDebug("Cache keys after invalidation: {Keys}", string.Join(", ", keysAfter));
            _logger.LogDebug("Completed comprehensive cache invalidation for medical items, usages, and related entities");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in comprehensive cache invalidation for medical items and usages");
        }
    }

    #endregion

    #region Notification Methods

    private async Task CreateApprovalRequestNotificationAsync(MedicalItem medicalItem)
    {
        try
        {
            var managerRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var managers = await managerRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "MANAGER") &&
                            !u.IsDeleted && u.IsActive)
                .ToListAsync();

            if (!managers.Any()) return;

            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();
            var notifications = new List<Notification>();

            var priorityText = GetPriorityDisplayName(medicalItem.Priority);
            var urgentText = medicalItem.IsUrgent ? " (KHẨN CẤP)" : "";

            foreach (var manager in managers)
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = $"Yêu cầu phê duyệt thuốc/vật tư y tế{urgentText}",
                    Content = $"Y tá {medicalItem.RequestedBy?.FullName ?? "N/A"} yêu cầu thêm: {medicalItem.Name} " +
                              $"(Loại: {medicalItem.Type}, Số lượng: {medicalItem.Quantity} {medicalItem.Unit}) " +
                              $"- Độ ưu tiên: {priorityText}. " +
                              $"Lý do: {medicalItem.Justification}",
                    NotificationType = NotificationType.General,
                    SenderId = medicalItem.RequestedById,
                    RecipientId = manager.Id,
                    RequiresConfirmation = false,
                    IsRead = false,
                    IsConfirmed = false,
                    CreatedDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(7)
                };

                notifications.Add(notification);
            }

            await notificationRepo.AddRangeAsync(notifications);
            await _unitOfWork.SaveChangesAsync();

            // Xóa cache liên quan đến notification
            await _cacheService.RemoveByPrefixAsync(NOTIFICATION_PREFIX);
            _logger.LogDebug("Đã xóa cache notification với prefix: {Prefix}", NOTIFICATION_PREFIX);
            await _cacheService.RemoveByPrefixAsync(NOTIFICATIONS_LIST_PREFIX);
            _logger.LogDebug("Đã xóa cache notifications list với prefix: {Prefix}", NOTIFICATIONS_LIST_PREFIX);

            _logger.LogInformation(
                "Created approval request notifications for item {ItemId} to {ManagerCount} managers",
                medicalItem.Id, managers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating approval request notifications for item {ItemId}", medicalItem.Id);
        }
    }

    private async Task CreateApprovalNotificationAsync(MedicalItem medicalItem, bool isApproved, string notes)
    {
        try
        {
            if (!medicalItem.RequestedById.HasValue) return;

            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();

            var title = isApproved
                ? $"Thuốc/vật tư được phê duyệt - {medicalItem.Name}"
                : $"Thuốc/vật tư bị từ chối - {medicalItem.Name}";

            var content = isApproved
                ? $"Yêu cầu thêm {medicalItem.Name} đã được Manager phê duyệt. " +
                  $"Thuốc/vật tư đã có thể sử dụng trong hệ thống. " +
                  (string.IsNullOrEmpty(notes) ? "" : $". Ghi chú: {notes}")
                : $"Yêu cầu thêm {medicalItem.Name} đã bị từ chối. " +
                  $"Lý do: {notes}";

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = title,
                Content = content,
                NotificationType = NotificationType.General,
                SenderId = medicalItem.ApprovedById,
                RecipientId = medicalItem.RequestedById.Value,
                RequiresConfirmation = false,
                IsRead = false,
                IsConfirmed = false,
                CreatedDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(7)
            };

            await notificationRepo.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            // Xóa cache liên quan đến notification
            await _cacheService.RemoveByPrefixAsync(NOTIFICATION_PREFIX);
            _logger.LogDebug("Đã xóa cache notification với prefix: {Prefix}", NOTIFICATION_PREFIX);
            await _cacheService.RemoveByPrefixAsync(NOTIFICATIONS_LIST_PREFIX);
            _logger.LogDebug("Đã xóa cache notifications list với prefix: {Prefix}", NOTIFICATIONS_LIST_PREFIX);

            _logger.LogInformation("Created approval notification for item {ItemId}, approved: {IsApproved}",
                medicalItem.Id, isApproved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating approval notification for item {ItemId}", medicalItem.Id);
        }
    }

    private async Task CreateStockAlertNotificationsAsync(MedicalItem medicalItem)
    {
        try
        {
            var alerts = new List<string>();

            if (medicalItem.Quantity <= 5)
            {
                alerts.Add(
                    $"CẢNH BÁO: {medicalItem.Name} sắp hết hàng (còn {medicalItem.Quantity} {medicalItem.Unit})");
            }
            else if (medicalItem.Quantity <= 10)
            {
                alerts.Add(
                    $"THÔNG BÁO: {medicalItem.Name} tồn kho thấp (còn {medicalItem.Quantity} {medicalItem.Unit})");
            }

            if (medicalItem.ExpiryDate.HasValue)
            {
                var daysUntilExpiry = (medicalItem.ExpiryDate.Value - DateTime.Now).TotalDays;
                if (daysUntilExpiry <= 0)
                {
                    alerts.Add(
                        $"CẢNH BÁO: {medicalItem.Name} đã hết hạn sử dụng từ {medicalItem.ExpiryDate:dd/MM/yyyy}");
                }
                else if (daysUntilExpiry <= 7)
                {
                    alerts.Add(
                        $"CẢNH BÁO: {medicalItem.Name} sẽ hết hạn trong {(int)daysUntilExpiry} ngày ({medicalItem.ExpiryDate:dd/MM/yyyy})");
                }
                else if (daysUntilExpiry <= 30)
                {
                    alerts.Add(
                        $"THÔNG BÁO: {medicalItem.Name} sẽ hết hạn trong {(int)daysUntilExpiry} ngày ({medicalItem.ExpiryDate:dd/MM/yyyy})");
                }
            }

            if (!alerts.Any()) return;

            var nurseRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var nurses = await nurseRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "SCHOOLNURSE") &&
                            !u.IsDeleted && u.IsActive)
                .ToListAsync();

            if (!nurses.Any()) return;

            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();
            var notifications = new List<Notification>();

            foreach (var nurse in nurses)
            {
                foreach (var alert in alerts)
                {
                    var notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        Title = "Cảnh báo tồn kho thuốc/vật tư y tế",
                        Content = alert,
                        NotificationType = NotificationType.General,
                        SenderId = null,
                        RecipientId = nurse.Id,
                        RequiresConfirmation = false,
                        IsRead = false,
                        IsConfirmed = false,
                        CreatedDate = DateTime.Now,
                        EndDate = DateTime.Now.AddDays(30)
                    };

                    notifications.Add(notification);
                }
            }

            await notificationRepo.AddRangeAsync(notifications);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Created {Count} stock alert notifications for item {ItemId}",
                notifications.Count, medicalItem.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating stock alert notifications for item {ItemId}", medicalItem.Id);
        }
    }

    private async Task CreateStockChangeNotificationAsync(
        MedicalItem medicalItem,
        int oldQuantity,
        int newQuantity,
        string reason = null)
    {
        try
        {
            var nurseRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var nurses = await nurseRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "SCHOOLNURSE") &&
                            !u.IsDeleted && u.IsActive)
                .ToListAsync();

            if (!nurses.Any()) return;

            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();
            var notifications = new List<Notification>();

            var changeType = newQuantity > oldQuantity ? "Nhập kho" : "Xuất kho";
            var quantityChange = Math.Abs(newQuantity - oldQuantity);

            foreach (var nurse in nurses)
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = $"Thông báo thay đổi tồn kho - {medicalItem.Name}",
                    Content = $"{changeType}: {medicalItem.Name} - " +
                              $"Thay đổi: {quantityChange} {medicalItem.Unit} " +
                              $"(Từ {oldQuantity} → {newQuantity} {medicalItem.Unit})" +
                              (string.IsNullOrEmpty(reason) ? "" : $". Lý do: {reason}"),
                    NotificationType = NotificationType.General,
                    SenderId = null,
                    RecipientId = nurse.Id,
                    RequiresConfirmation = false,
                    IsRead = false,
                    IsConfirmed = false,
                    CreatedDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(7)
                };

                notifications.Add(notification);
            }

            await notificationRepo.AddRangeAsync(notifications);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Created stock change notifications for item {ItemId}: {OldQuantity} → {NewQuantity}",
                medicalItem.Id, oldQuantity, newQuantity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating stock change notification for item {ItemId}", medicalItem.Id);
        }
    }

    private async Task CreateMedicationUsageNotificationAsync(
        ApplicationUser student,
        MedicalItem medicalItem,
        MedicalItemUsage usage)
    {
        try
        {
            if (!student.ParentId.HasValue || medicalItem.Type != "Medication")
            {
                return;
            }

            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = $"Thông báo sử dụng thuốc - {student.FullName}",
                Content = $"Con em Quý phụ huynh ({student.FullName} - {student.StudentCode}) " +
                          $"đã được sử dụng thuốc: {medicalItem.Name} " +
                          $"với liều lượng: {usage.Quantity} {medicalItem.Unit}. " +
                          $"Ghi chú: {usage.Notes ?? "Không có"}.",
                NotificationType = NotificationType.General,
                SenderId = null,
                RecipientId = student.ParentId.Value,
                RequiresConfirmation = false,
                IsRead = false,
                IsConfirmed = false,
                CreatedDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(7)
            };

            await notificationRepo.AddAsync(notification);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Created medication usage notification for parent {ParentId}, student {StudentId}",
                student.ParentId.Value, student.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating medication usage notification for student {StudentId}", student.Id);
        }
    }

    #endregion
}