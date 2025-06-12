using System.Security.Claims;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Helpers;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services;

public class StudentMedicationService : IStudentMedicationService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<StudentMedicationService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IValidator<CreateStudentMedicationRequest> _createValidator;
    private readonly IValidator<UpdateStudentMedicationRequest> _updateValidator;
    private readonly IValidator<ApproveStudentMedicationRequest> _approveValidator;

    private const string MEDICATION_CACHE_PREFIX = "student_medication";
    private const string MEDICATION_LIST_PREFIX = "student_medications_list";
    private const string MEDICATION_CACHE_SET = "student_medication_cache_keys";
    private const string STATISTICS_PREFIX = "medication_statistics";

    public StudentMedicationService(
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<StudentMedicationService> logger,
        IHttpContextAccessor httpContextAccessor,
        IValidator<CreateStudentMedicationRequest> createValidator,
        IValidator<UpdateStudentMedicationRequest> updateValidator,
        IValidator<ApproveStudentMedicationRequest> approveValidator)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _approveValidator = approveValidator;
    }

    #region Basic CRUD Operations

    public async Task<BaseListResponse<StudentMedicationResponse>> GetStudentMedicationsAsync
    (
        int pageIndex, int pageSize, string searchTerm, string orderBy,
        Guid? studentId = null, Guid? parentId = null, StudentMedicationStatus? status = null,
        bool? expiringSoon = null, bool? requiresAdministration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(
                MEDICATION_LIST_PREFIX,
                pageIndex.ToString(),
                pageSize.ToString(),
                searchTerm ?? "",
                orderBy ?? "",
                studentId?.ToString() ?? "",
                parentId?.ToString() ?? "",
                status?.ToString() ?? "",
                expiringSoon?.ToString() ?? "",
                requiresAdministration?.ToString() ?? ""
            );

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<StudentMedicationResponse>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<StudentMedication>().GetQueryable()
                .Include(sm => sm.Student)
                .Include(sm => sm.Parent)
                .Include(sm => sm.ApprovedBy)
                .Include(sm => sm.Administrations)
                .Where(sm => !sm.IsDeleted)
                .AsQueryable();

            query = ApplyFilters(query, searchTerm, studentId, parentId, status, expiringSoon, requiresAdministration);
            query = ApplyOrdering(query, orderBy);

            var totalCount = await query.CountAsync(cancellationToken);
            var medications = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = medications.Select(MapToStudentMedicationResponse).ToList();

            var result = BaseListResponse<StudentMedicationResponse>.SuccessResult(
                responses, totalCount, pageSize, pageIndex,
                "Lấy danh sách thuốc học sinh thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICATION_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving student medications");
            return BaseListResponse<StudentMedicationResponse>.ErrorResult("Lỗi lấy danh sách thuốc học sinh.");
        }
    }

    public async Task<BaseResponse<StudentMedicationResponse>> GetStudentMedicationByIdAsync(Guid medicationId)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(MEDICATION_CACHE_PREFIX, medicationId.ToString());
            var cachedResponse = await _cacheService.GetAsync<BaseResponse<StudentMedicationResponse>>(cacheKey);

            if (cachedResponse != null)
            {
                return cachedResponse;
            }

            var medicationRepo = _unitOfWork.GetRepositoryByEntity<StudentMedication>();
            var medication = await medicationRepo.GetQueryable()
                .Include(sm => sm.Student)
                .Include(sm => sm.Parent)
                .Include(sm => sm.ApprovedBy)
                .Include(sm => sm.Administrations)
                .Where(sm => sm.Id == medicationId && !sm.IsDeleted)
                .FirstOrDefaultAsync();

            if (medication == null)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult("Không tìm thấy thuốc học sinh.");
            }

            var medicationResponse = MapToStudentMedicationResponse(medication);

            var response = BaseResponse<StudentMedicationResponse>.SuccessResult(
                medicationResponse, "Lấy thông tin thuốc học sinh thành công.");

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICATION_CACHE_SET);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student medication by ID: {MedicationId}", medicationId);
            return BaseResponse<StudentMedicationResponse>.ErrorResult(
                $"Lỗi lấy thông tin thuốc học sinh: {ex.Message}");
        }
    }

    public async Task<BaseResponse<StudentMedicationResponse>> CreateStudentMedicationAsync(
        CreateStudentMedicationRequest model)
    {
        try
        {
            var validationResult = await _createValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BaseResponse<StudentMedicationResponse>.ErrorResult(errors);
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult("Không thể xác định người dùng hiện tại.");
            }

            var studentRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var student = await studentRepo.GetQueryable()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == model.StudentId && !u.IsDeleted);

            if (student == null)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult("Không tìm thấy học sinh.");
            }

            if (!student.UserRoles.Any(ur => ur.Role.Name == "STUDENT"))
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult("Người dùng không phải là học sinh.");
            }

            var currentUser = await GetCurrentUserWithRoles();
            if (currentUser != null && currentUser.UserRoles.Any(ur => ur.Role.Name == "PARENT"))
            {
                if (student.ParentId != currentUserId)
                {
                    return BaseResponse<StudentMedicationResponse>.ErrorResult(
                        "Bạn chỉ có thể gửi thuốc cho con em mình.");
                }
            }

            var parentRoleName = await GetParentRoleName();

            var medication = _mapper.Map<StudentMedication>(model);
            medication.Id = Guid.NewGuid();
            medication.ParentId = currentUserId;
            medication.Status = StudentMedicationStatus.PendingApproval;
            medication.SubmittedAt = DateTime.Now;
            medication.CreatedBy = parentRoleName;
            medication.CreatedDate = DateTime.Now;

            var medicationRepo = _unitOfWork.GetRepositoryByEntity<StudentMedication>();
            await medicationRepo.AddAsync(medication);

            await _unitOfWork.SaveChangesAsync();

            await CreateMedicationRequestNotificationAsync(medication);

            await InvalidateAllCachesAsync();

            medication = await medicationRepo.GetQueryable()
                .Include(sm => sm.Student)
                .Include(sm => sm.Parent)
                .Include(sm => sm.ApprovedBy)
                .Include(sm => sm.Administrations)
                .FirstOrDefaultAsync(sm => sm.Id == medication.Id);

            var medicationResponse = MapToStudentMedicationResponse(medication);

            _logger.LogInformation("Created student medication {MedicationId} for student {StudentId}",
                medication.Id, model.StudentId);

            return BaseResponse<StudentMedicationResponse>.SuccessResult(
                medicationResponse, "Gửi yêu cầu thuốc cho học sinh thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student medication");
            return BaseResponse<StudentMedicationResponse>.ErrorResult($"Lỗi gửi yêu cầu thuốc: {ex.Message}");
        }
    }

    public async Task<BaseResponse<StudentMedicationResponse>> UpdateStudentMedicationAsync(
        Guid medicationId, UpdateStudentMedicationRequest model)
    {
        try
        {
            var validationResult = await _updateValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BaseResponse<StudentMedicationResponse>.ErrorResult(errors);
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult("Không thể xác định người dùng hiện tại.");
            }

            var medicationRepo = _unitOfWork.GetRepositoryByEntity<StudentMedication>();
            var medication = await medicationRepo.GetQueryable()
                .Include(sm => sm.Student)
                .Include(sm => sm.Parent)
                .FirstOrDefaultAsync(sm => sm.Id == medicationId && !sm.IsDeleted);

            if (medication == null)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult("Không tìm thấy yêu cầu thuốc.");
            }

            if (medication.ParentId != currentUserId)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult("Bạn chỉ có thể cập nhật yêu cầu của mình.");
            }

            if (medication.Status != StudentMedicationStatus.PendingApproval)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult(
                    "Chỉ có thể cập nhật yêu cầu đang chờ phê duyệt.");
            }

            var parentRoleName = await GetParentRoleName();

            if (!string.IsNullOrEmpty(model.MedicationName))
                medication.MedicationName = model.MedicationName;
            if (!string.IsNullOrEmpty(model.Dosage))
                medication.Dosage = model.Dosage;
            if (!string.IsNullOrEmpty(model.Instructions))
                medication.Instructions = model.Instructions;
            if (!string.IsNullOrEmpty(model.Frequency))
                medication.Frequency = model.Frequency;
            if (model.StartDate.HasValue)
                medication.StartDate = model.StartDate.Value;
            if (model.EndDate.HasValue)
                medication.EndDate = model.EndDate.Value;
            if (model.ExpiryDate.HasValue)
                medication.ExpiryDate = model.ExpiryDate.Value;
            if (!string.IsNullOrEmpty(model.Purpose))
                medication.Purpose = model.Purpose;
            if (model.SideEffects != null)
                medication.SideEffects = model.SideEffects;
            if (model.StorageInstructions != null)
                medication.StorageInstructions = model.StorageInstructions;

            medication.LastUpdatedBy = parentRoleName;
            medication.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            await InvalidateAllCachesAsync();

            var medicationResponse = MapToStudentMedicationResponse(medication);

            _logger.LogInformation("Updated student medication {MedicationId}", medicationId);

            return BaseResponse<StudentMedicationResponse>.SuccessResult(
                medicationResponse, "Cập nhật yêu cầu thuốc thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating student medication: {MedicationId}", medicationId);
            return BaseResponse<StudentMedicationResponse>.ErrorResult($"Lỗi cập nhật yêu cầu thuốc: {ex.Message}");
        }
    }

    public async Task<BaseResponse<bool>> DeleteStudentMedicationAsync(Guid medicationId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BaseResponse<bool>.ErrorResult("Không thể xác định người dùng hiện tại.");
            }

            var medicationRepo = _unitOfWork.GetRepositoryByEntity<StudentMedication>();
            var medication = await medicationRepo.GetQueryable()
                .FirstOrDefaultAsync(sm => sm.Id == medicationId && !sm.IsDeleted);

            if (medication == null)
            {
                return BaseResponse<bool>.ErrorResult("Không tìm thấy yêu cầu thuốc.");
            }

            if (medication.ParentId != currentUserId)
            {
                return BaseResponse<bool>.ErrorResult("Bạn chỉ có thể xóa yêu cầu của mình.");
            }

            if (medication.Status != StudentMedicationStatus.PendingApproval)
            {
                return BaseResponse<bool>.ErrorResult("Chỉ có thể xóa yêu cầu đang chờ phê duyệt.");
            }

            var parentRoleName = await GetParentRoleName();

            medication.IsDeleted = true;
            medication.LastUpdatedBy = parentRoleName;
            medication.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            await InvalidateAllCachesAsync();

            return BaseResponse<bool>.SuccessResult(true, "Xóa yêu cầu thuốc thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting student medication: {MedicationId}", medicationId);
            return BaseResponse<bool>.ErrorResult($"Lỗi xóa yêu cầu thuốc: {ex.Message}");
        }
    }

    #endregion

    #region Approval Workflow

    public async Task<BaseResponse<StudentMedicationResponse>> ApproveStudentMedicationAsync(
        Guid medicationId, ApproveStudentMedicationRequest request)
    {
        try
        {
            var validationResult = await _approveValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BaseResponse<StudentMedicationResponse>.ErrorResult(errors);
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult("Không thể xác định người dùng hiện tại.");
            }

            var currentUser = await ValidateSchoolNursePermissions(currentUserId);
            if (currentUser == null)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult(
                    "Chỉ School Nurse mới có quyền phê duyệt thuốc.");
            }

            var medicationRepo = _unitOfWork.GetRepositoryByEntity<StudentMedication>();
            var medication = await medicationRepo.GetQueryable()
                .Include(sm => sm.Student)
                .Include(sm => sm.Parent)
                .Include(sm => sm.ApprovedBy)
                .FirstOrDefaultAsync(sm => sm.Id == medicationId && !sm.IsDeleted);

            if (medication == null)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult("Không tìm thấy yêu cầu thuốc.");
            }

            if (medication.Status != StudentMedicationStatus.PendingApproval)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult(
                    "Chỉ có thể phê duyệt yêu cầu đang chờ duyệt.");
            }

            var schoolNurseRoleName = await GetSchoolNurseRoleName();

            medication.ApprovedById = currentUserId;
            medication.ApprovedAt = DateTime.Now;
            medication.Status =
                request.IsApproved ? StudentMedicationStatus.Approved : StudentMedicationStatus.Rejected;
            medication.RejectionReason = request.RejectionReason;
            medication.LastUpdatedBy = schoolNurseRoleName;
            medication.LastUpdatedDate = DateTime.Now;

            if (request.IsApproved && medication.StartDate <= DateTime.Today && medication.EndDate >= DateTime.Today)
            {
                medication.Status = StudentMedicationStatus.Active;
            }

            await _unitOfWork.SaveChangesAsync();

            await CreateApprovalNotificationAsync(medication, request.IsApproved, request.Notes);

            await InvalidateAllCachesAsync();

            var medicationResponse = MapToStudentMedicationResponse(medication);

            _logger.LogInformation("Medication {MedicationId} {Action} by {NurseId}",
                medicationId, request.IsApproved ? "approved" : "rejected", currentUserId);

            return BaseResponse<StudentMedicationResponse>.SuccessResult(
                medicationResponse,
                request.IsApproved ? "Phê duyệt thuốc thành công." : "Từ chối yêu cầu thuốc thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving student medication: {MedicationId}", medicationId);
            return BaseResponse<StudentMedicationResponse>.ErrorResult($"Lỗi phê duyệt thuốc: {ex.Message}");
        }
    }

    public async Task<BaseListResponse<StudentMedicationResponse>> GetPendingApprovalsAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(
                MEDICATION_LIST_PREFIX, "pending_approvals", pageIndex.ToString(), pageSize.ToString());

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<StudentMedicationResponse>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<StudentMedication>().GetQueryable()
                .Include(sm => sm.Student)
                .Include(sm => sm.Parent)
                .Where(sm => sm.Status == StudentMedicationStatus.PendingApproval && !sm.IsDeleted)
                .OrderBy(sm => sm.SubmittedAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var medications = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = medications.Select(MapToStudentMedicationResponse).ToList();

            var result = BaseListResponse<StudentMedicationResponse>.SuccessResult(
                responses, totalCount, pageSize, pageIndex,
                "Lấy danh sách yêu cầu chờ phê duyệt thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICATION_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending approvals");
            return BaseListResponse<StudentMedicationResponse>.ErrorResult("Lỗi lấy danh sách chờ phê duyệt.");
        }
    }

    #endregion

    #region Status Management

    public async Task<BaseResponse<StudentMedicationResponse>> UpdateMedicationStatusAsync(
        Guid medicationId, UpdateMedicationStatusRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult("Không thể xác định người dùng hiện tại.");
            }

            var currentUser = await ValidateSchoolNursePermissions(currentUserId);
            if (currentUser == null)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult(
                    "Chỉ School Nurse mới có quyền cập nhật trạng thái thuốc.");
            }

            var medicationRepo = _unitOfWork.GetRepositoryByEntity<StudentMedication>();
            var medication = await medicationRepo.GetQueryable()
                .Include(sm => sm.Student)
                .Include(sm => sm.Parent)
                .FirstOrDefaultAsync(sm => sm.Id == medicationId && !sm.IsDeleted);

            if (medication == null)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult("Không tìm thấy thuốc học sinh.");
            }

            var schoolNurseRoleName = await GetSchoolNurseRoleName();
            var oldStatus = medication.Status;

            medication.Status = request.Status;
            medication.LastUpdatedBy = schoolNurseRoleName;
            medication.LastUpdatedDate = DateTime.Now;

            if (request.Status == StudentMedicationStatus.Discontinued)
            {
                await CreateStatusChangeNotificationAsync(medication, oldStatus, request.Status, request.Reason);
            }

            await _unitOfWork.SaveChangesAsync();
            await InvalidateAllCachesAsync();

            var medicationResponse = MapToStudentMedicationResponse(medication);

            _logger.LogInformation("Updated medication {MedicationId} status from {OldStatus} to {NewStatus}",
                medicationId, oldStatus, request.Status);

            return BaseResponse<StudentMedicationResponse>.SuccessResult(
                medicationResponse, "Cập nhật trạng thái thuốc thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating medication status: {MedicationId}", medicationId);
            return BaseResponse<StudentMedicationResponse>.ErrorResult($"Lỗi cập nhật trạng thái thuốc: {ex.Message}");
        }
    }

    public async Task<BaseResponse<List<StudentMedicationResponse>>> GetExpiredMedicationsAsync()
    {
        try
        {
            var today = DateTime.Today;
            var cacheKey =
                _cacheService.GenerateCacheKey(MEDICATION_LIST_PREFIX, "expired", today.ToString("yyyy-MM-dd"));

            var cachedResult = await _cacheService.GetAsync<BaseResponse<List<StudentMedicationResponse>>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var medications = await _unitOfWork.GetRepositoryByEntity<StudentMedication>().GetQueryable()
                .Include(sm => sm.Student)
                .Include(sm => sm.Parent)
                .Where(sm => sm.ExpiryDate <= today &&
                             sm.Status != StudentMedicationStatus.Completed &&
                             !sm.IsDeleted)
                .ToListAsync();

            var responses = medications.Select(MapToStudentMedicationResponse).ToList();

            var result = BaseResponse<List<StudentMedicationResponse>>.SuccessResult(
                responses, "Lấy danh sách thuốc hết hạn thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(6));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expired medications");
            return BaseResponse<List<StudentMedicationResponse>>.ErrorResult("Lỗi lấy danh sách thuốc hết hạn.");
        }
    }

    public async Task<BaseResponse<List<StudentMedicationResponse>>> GetExpiringSoonMedicationsAsync(int days = 7)
    {
        try
        {
            var today = DateTime.Today;
            var expiryThreshold = today.AddDays(days);
            var cacheKey = _cacheService.GenerateCacheKey(
                MEDICATION_LIST_PREFIX, "expiring_soon", today.ToString("yyyy-MM-dd"), days.ToString());

            var cachedResult = await _cacheService.GetAsync<BaseResponse<List<StudentMedicationResponse>>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var medications = await _unitOfWork.GetRepositoryByEntity<StudentMedication>().GetQueryable()
                .Include(sm => sm.Student)
                .Include(sm => sm.Parent)
                .Where(sm => sm.ExpiryDate <= expiryThreshold &&
                             sm.ExpiryDate > today &&
                             sm.Status == StudentMedicationStatus.Active &&
                             !sm.IsDeleted)
                .ToListAsync();

            var responses = medications.Select(MapToStudentMedicationResponse).ToList();

            var result = BaseResponse<List<StudentMedicationResponse>>.SuccessResult(
                responses, $"Lấy danh sách thuốc gần hết hạn trong {days} ngày thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(2));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expiring soon medications");
            return BaseResponse<List<StudentMedicationResponse>>.ErrorResult("Lỗi lấy danh sách thuốc gần hết hạn.");
        }
    }

    #endregion

    #region Parent Specific Methods

    public async Task<BaseListResponse<StudentMedicationResponse>> GetMyChildrenMedicationsAsync(
        int pageIndex, int pageSize, StudentMedicationStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BaseListResponse<StudentMedicationResponse>.ErrorResult(
                    "Không thể xác định người dùng hiện tại.");
            }

            var cacheKey = _cacheService.GenerateCacheKey(
                MEDICATION_LIST_PREFIX, "my_children", currentUserId.ToString(),
                pageIndex.ToString(), pageSize.ToString(), status?.ToString() ?? "");

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<StudentMedicationResponse>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<StudentMedication>().GetQueryable()
                .Include(sm => sm.Student)
                .Include(sm => sm.Parent)
                .Include(sm => sm.ApprovedBy)
                .Include(sm => sm.Administrations)
                .Where(sm => sm.ParentId == currentUserId && !sm.IsDeleted);

            if (status.HasValue)
            {
                query = query.Where(sm => sm.Status == status.Value);
            }

            query = query.OrderByDescending(sm => sm.SubmittedAt ?? sm.CreatedDate);

            var totalCount = await query.CountAsync(cancellationToken);
            var medications = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = medications.Select(MapToStudentMedicationResponse).ToList();

            var result = BaseListResponse<StudentMedicationResponse>.SuccessResult(
                responses, totalCount, pageSize, pageIndex,
                "Lấy danh sách thuốc con em thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICATION_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my children medications");
            return BaseListResponse<StudentMedicationResponse>.ErrorResult("Lỗi lấy danh sách thuốc con em.");
        }
    }

    #endregion

    #region Helper Methods

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(UserHelper.GetCurrentUserId(_httpContextAccessor.HttpContext));
    }

    private async Task<ApplicationUser> GetCurrentUserWithRoles()
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty) return null;

            return await _unitOfWork.GetRepositoryByEntity<ApplicationUser>()
                .GetQueryable()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == currentUserId && !u.IsDeleted);
        }
        catch
        {
            return null;
        }
    }

    private async Task<ApplicationUser> ValidateSchoolNursePermissions(Guid userId)
    {
        try
        {
            var user = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>()
                .GetQueryable()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            if (user == null) return null;

            var validRoles = new[] { "SCHOOLNURSE", "ADMIN", "MANAGER" };
            return user.UserRoles.Any(ur => validRoles.Contains(ur.Role.Name.ToUpper())) ? user : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string> GetSchoolNurseRoleName()
    {
        try
        {
            var role = await _unitOfWork.GetRepositoryByEntity<Role>()
                .GetQueryable()
                .FirstOrDefaultAsync(r => r.Name == "SCHOOLNURSE");
            return role?.Name ?? "SCHOOLNURSE";
        }
        catch
        {
            return "SCHOOLNURSE";
        }
    }

    private async Task<string> GetParentRoleName()
    {
        try
        {
            var role = await _unitOfWork.GetRepositoryByEntity<Role>()
                .GetQueryable()
                .FirstOrDefaultAsync(r => r.Name == "PARENT");
            return role?.Name ?? "PARENT";
        }
        catch
        {
            return "PARENT";
        }
    }

    private StudentMedicationResponse MapToStudentMedicationResponse(StudentMedication medication)
    {
        var response = _mapper.Map<StudentMedicationResponse>(medication);

        // Status display name
        response.StatusDisplayName = GetStatusDisplayName(medication.Status);

        // Navigation data
        if (medication.Student != null)
        {
            response.StudentName = medication.Student.FullName;
            response.StudentCode = medication.Student.StudentCode;
        }

        if (medication.Parent != null)
        {
            response.ParentName = medication.Parent.FullName;
        }

        if (medication.ApprovedBy != null)
        {
            response.ApprovedByName = medication.ApprovedBy.FullName;
        }

        // Logic fields
        response.CanApprove = medication.Status == StudentMedicationStatus.PendingApproval;
        response.CanAdminister = medication.Status == StudentMedicationStatus.Active &&
                                 DateTime.Today >= medication.StartDate &&
                                 DateTime.Today <= medication.EndDate &&
                                 medication.ExpiryDate > DateTime.Today;

        var daysUntilExpiry = (medication.ExpiryDate - DateTime.Today).TotalDays;
        response.DaysUntilExpiry = (int)Math.Max(0, daysUntilExpiry);
        response.IsExpiringSoon = daysUntilExpiry <= 7 && daysUntilExpiry > 0;
        response.IsExpired = medication.ExpiryDate <= DateTime.Today;
        response.IsActive = medication.Status == StudentMedicationStatus.Active;

        // Administration count
        response.AdministrationCount = medication.Administrations?.Count(a => !a.IsDeleted) ?? 0;

        return response;
    }

    private string GetStatusDisplayName(StudentMedicationStatus status)
    {
        return status switch
        {
            StudentMedicationStatus.PendingApproval => "Chờ phê duyệt",
            StudentMedicationStatus.Approved => "Đã phê duyệt",
            StudentMedicationStatus.Rejected => "Bị từ chối",
            StudentMedicationStatus.Active => "Đang thực hiện",
            StudentMedicationStatus.Completed => "Hoàn thành",
            StudentMedicationStatus.Discontinued => "Ngưng sử dụng",
            _ => status.ToString()
        };
    }

    private IQueryable<StudentMedication> ApplyFilters(
        IQueryable<StudentMedication> query,
        string searchTerm,
        Guid? studentId,
        Guid? parentId,
        StudentMedicationStatus? status,
        bool? expiringSoon,
        bool? requiresAdministration)
    {
        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(sm =>
                sm.MedicationName.ToLower().Contains(searchTerm) ||
                sm.Student.FullName.ToLower().Contains(searchTerm) ||
                sm.Student.StudentCode.ToLower().Contains(searchTerm) ||
                sm.Purpose.ToLower().Contains(searchTerm));
        }

        if (studentId.HasValue)
        {
            query = query.Where(sm => sm.StudentId == studentId.Value);
        }

        if (parentId.HasValue)
        {
            query = query.Where(sm => sm.ParentId == parentId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(sm => sm.Status == status.Value);
        }

        if (expiringSoon == true)
        {
            var sevenDaysFromNow = DateTime.Today.AddDays(7);
            query = query.Where(sm => sm.ExpiryDate <= sevenDaysFromNow && sm.ExpiryDate > DateTime.Today);
        }

        if (requiresAdministration == true)
        {
            var today = DateTime.Today;
            query = query.Where(sm =>
                sm.Status == StudentMedicationStatus.Active &&
                sm.StartDate <= today &&
                sm.EndDate >= today);
        }

        return query;
    }

    private IQueryable<StudentMedication> ApplyOrdering(IQueryable<StudentMedication> query, string orderBy)
    {
        return orderBy?.ToLower() switch
        {
            "medicationname" => query.OrderBy(sm => sm.MedicationName),
            "medicationname_desc" => query.OrderByDescending(sm => sm.MedicationName),
            "studentname" => query.OrderBy(sm => sm.Student.FullName),
            "studentname_desc" => query.OrderByDescending(sm => sm.Student.FullName),
            "status" => query.OrderBy(sm => sm.Status),
            "status_desc" => query.OrderByDescending(sm => sm.Status),
            "submittedat" => query.OrderBy(sm => sm.SubmittedAt),
            "submittedat_desc" => query.OrderByDescending(sm => sm.SubmittedAt),
            "expirydate" => query.OrderBy(sm => sm.ExpiryDate),
            "expirydate_desc" => query.OrderByDescending(sm => sm.ExpiryDate),
            _ => query.OrderByDescending(sm => sm.SubmittedAt ?? sm.CreatedDate)
        };
    }

    private async Task InvalidateAllCachesAsync()
    {
        try
        {
            await Task.WhenAll(
                _cacheService.RemoveByPrefixAsync(MEDICATION_CACHE_PREFIX),
                _cacheService.RemoveByPrefixAsync(MEDICATION_LIST_PREFIX),
                _cacheService.RemoveByPrefixAsync(STATISTICS_PREFIX)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating caches");
        }
    }

    #endregion

    #region Notification Methods

    private async Task CreateMedicationRequestNotificationAsync(StudentMedication medication)
    {
        try
        {
            // Get all school nurses
            var nurses = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>()
                .GetQueryable()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "SCHOOLNURSE") &&
                            u.IsActive && !u.IsDeleted)
                .ToListAsync();

            if (!nurses.Any()) return;

            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();
            var notifications = new List<Notification>();

            foreach (var nurse in nurses)
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = $"Yêu cầu thuốc mới - {medication.Student?.FullName}",
                    Content = $"Phụ huynh đã gửi yêu cầu thuốc '{medication.MedicationName}' " +
                              $"cho học sinh {medication.Student?.FullName} ({medication.Student?.StudentCode}). " +
                              $"Mục đích: {medication.Purpose}. " +
                              $"Thời gian: {medication.StartDate:dd/MM/yyyy} - {medication.EndDate:dd/MM/yyyy}. " +
                              "Vui lòng xem xét và phê duyệt.",
                    NotificationType = NotificationType.General,
                    SenderId = null,
                    RecipientId = nurse.Id,
                    RequiresConfirmation = false,
                    IsRead = false,
                    CreatedDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(7)
                };

                notifications.Add(notification);
            }

            await notificationRepo.AddRangeAsync(notifications);
            
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Created {Count} medication request notifications", notifications.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating medication request notifications");
        }
    }

    private async Task CreateApprovalNotificationAsync(StudentMedication medication, bool isApproved, string notes)
    {
        try
        {
            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();

            var title = isApproved
                ? $"✅ Thuốc được phê duyệt - {medication.Student?.FullName}"
                : $"❌ Thuốc bị từ chối - {medication.Student?.FullName}";

            var content = isApproved
                ? $"Yêu cầu thuốc '{medication.MedicationName}' cho con em " +
                  $"{medication.Student?.FullName} ({medication.Student?.StudentCode}) đã được phê duyệt. " +
                  $"Nhà trường sẽ cho con em uống thuốc theo đúng hướng dẫn từ {medication.StartDate:dd/MM/yyyy} đến {medication.EndDate:dd/MM/yyyy}."
                : $"Yêu cầu thuốc '{medication.MedicationName}' cho con em " +
                  $"{medication.Student?.FullName} ({medication.Student?.StudentCode}) đã bị từ chối. " +
                  $"Lý do: {medication.RejectionReason}";

            if (!string.IsNullOrEmpty(notes))
            {
                content += $" Ghi chú: {notes}";
            }

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = title,
                Content = content,
                NotificationType = NotificationType.General,
                SenderId = medication.ApprovedById,
                RecipientId = medication.ParentId,
                RequiresConfirmation = false,
                IsRead = false,
                CreatedDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(30)
            };

            await notificationRepo.AddAsync(notification);
            
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Created approval notification for medication {MedicationId}", medication.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating approval notification");
        }
    }

    private async Task CreateStatusChangeNotificationAsync(
        StudentMedication medication, StudentMedicationStatus oldStatus, StudentMedicationStatus newStatus,
        string reason)
    {
        try
        {
            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();

            var statusName = GetStatusDisplayName(newStatus);
            var title = $"Thay đổi trạng thái thuốc - {medication.Student?.FullName}";
            var content = $"Trạng thái thuốc '{medication.MedicationName}' của con em " +
                          $"{medication.Student?.FullName} ({medication.Student?.StudentCode}) " +
                          $"đã được thay đổi thành: {statusName}";

            if (!string.IsNullOrEmpty(reason))
            {
                content += $". Lý do: {reason}";
            }

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = title,
                Content = content,
                NotificationType = NotificationType.General,
                SenderId = medication.ApprovedById,
                RecipientId = medication.ParentId,
                RequiresConfirmation = false,
                IsRead = false,
                CreatedDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(7)
            };

            await notificationRepo.AddAsync(notification);
            
            _logger.LogInformation("Created status change notification for medication {MedicationId}", medication.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating status change notification");
        }
    }

    #endregion
}