using AutoMapper;
using Azure.Core;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Helpers;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicationStockResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationAdministrationResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts.IAuthService;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.StudentMedication;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;
using System.Text.Json;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services;

public class StudentMedicationService : IStudentMedicationService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IAuthService _authService;
    private readonly ILogger<StudentMedicationService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IValidator<CreateStudentMedicationRequest> _createValidator;
    private readonly IValidator<UpdateStudentMedicationRequest> _updateValidator;
    private readonly IValidator<ApproveStudentMedicationRequest> _approveValidator;
    private readonly IValidator<RejectStudentMedicationRequest> _rejectValidator;

    private const string MEDICATION_CACHE_PREFIX = "student_medication";
    private const string MEDICATION_LIST_PREFIX = "student_medications_list";
    private const string MEDICATION_STOCK_PREFIX = "medication_stock";
    private const string MEDICATION_CACHE_SET = "student_medication_cache_keys";
    private const string STOCK_CACHE_SET = "medication_stock_cache_keys";
    private const string STATISTICS_PREFIX = "medication_statistics";
    private const string MEDICATION_REQUEST_PREFIX = "student_medication_requests";
    private const string USER_PROFILE_PREFIX = "user_profile";
    private const string STUDENT_LIST_PREFIX = "student_list";
    private const string PARENT_LIST_PREFIX = "parent_list";
    private const string CLASS_ENROLLMENT_PREFIX = "class_enrollment";
    private const string USER_PROFILE_CACHE_SET = "user_profile_cache_keys";
    private const string STUDENT_LIST_CACHE_SET = "student_list_cache_keys";
    private const string PARENT_LIST_CACHE_SET = "parent_list_cache_keys";
    private const string CLASS_ENROLLMENT_CACHE_SET = "class_enrollment_cache_keys";

    public StudentMedicationService(
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<StudentMedicationService> logger,
        IHttpContextAccessor httpContextAccessor,
        IAuthService authService,
        IValidator<CreateStudentMedicationRequest> createValidator,
        IValidator<UpdateStudentMedicationRequest> updateValidator,
        IValidator<ApproveStudentMedicationRequest> approveValidator,
        IValidator<RejectStudentMedicationRequest> rejectValidator)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _authService = authService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _approveValidator = approveValidator;
        _rejectValidator = rejectValidator;
    }

    #region Basic CRUD Operations

    public async Task<BaseListResponse<StudentMedicationListResponse>> GetStudentMedicationsAsync
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
    )
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

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<StudentMedicationListResponse>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<StudentMedication>().GetQueryable()
                .AsSplitQuery()
                .Include(sm => sm.Student)
                .Include(sm => sm.Parent)
                .Include(sm => sm.ApprovedBy)
                .Include(sm => sm.Administrations.Where(a => !a.IsDeleted))
                .Include(sm => sm.Schedules.Where(s => !s.IsDeleted))
                .Where(sm => !sm.IsDeleted)
                .AsQueryable();

            query = ApplyStudentMedicationFilters(query, searchTerm, studentId, parentId, status, expiringSoon,
                requiresAdministration);
            query = ApplyStudentMedicationOrdering(query, orderBy);

            var totalCount = await query.CountAsync(cancellationToken);
            var medications = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = _mapper.Map<List<StudentMedicationListResponse>>(medications);

            var result = BaseListResponse<StudentMedicationListResponse>.SuccessResult(
                responses, totalCount, pageSize, pageIndex,
                "Lấy danh sách thuốc học sinh thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICATION_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving student medications");
            return BaseListResponse<StudentMedicationListResponse>.ErrorResult("Lỗi lấy danh sách thuốc học sinh.");
        }
    }

    public async Task<BaseResponse<StudentMedicationDetailResponse>> GetStudentMedicationByIdAsync(Guid medicationId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BaseResponse<StudentMedicationDetailResponse>.ErrorResult(
                    "Không thể xác định người dùng hiện tại.");
            }

            var cacheKey = _cacheService.GenerateCacheKey(MEDICATION_CACHE_PREFIX, "detail", medicationId.ToString());
            var cachedResponse = await _cacheService.GetAsync<BaseResponse<StudentMedicationDetailResponse>>(cacheKey);

            if (cachedResponse != null)
            {
                var canAccess = await ValidateAccessPermissionAsync(currentUserId, medicationId);
                if (!canAccess)
                {
                    return BaseResponse<StudentMedicationDetailResponse>.ErrorResult(
                        "Bạn không có quyền xem thông tin thuốc này.");
                }

                _logger.LogDebug("Cache hit for medication detail: {MedicationId}", medicationId);
                return cachedResponse;
            }

            var medicationRepo = _unitOfWork.GetRepositoryByEntity<StudentMedication>();
            var medication = await medicationRepo.GetQueryable()
                .AsSplitQuery()
                .Include(sm => sm.Student)
                .Include(sm => sm.Parent)
                .Include(sm => sm.ApprovedBy)
                .Include(sm => sm.Administrations.Where(a => !a.IsDeleted))
                .Include(sm => sm.Schedules.Where(s => !s.IsDeleted))
                .Include(sm => sm.StockHistory.Where(s => !s.IsDeleted))
                .Where(sm => sm.Id == medicationId && !sm.IsDeleted)
                .FirstOrDefaultAsync();

            if (medication == null)
            {
                return BaseResponse<StudentMedicationDetailResponse>.ErrorResult("Không tìm thấy thuốc học sinh.");
            }

            var currentUser = await GetCurrentUserWithRoles();
            if (currentUser == null)
            {
                return BaseResponse<StudentMedicationDetailResponse>.ErrorResult(
                    "Không thể xác định quyền của người dùng.");
            }

            var hasPermission = await ValidateUserAccessToMedicationAsync(currentUser, medication);
            if (!hasPermission)
            {
                return BaseResponse<StudentMedicationDetailResponse>.ErrorResult(
                    "Bạn không có quyền xem thông tin thuốc này.");
            }

            var medicationResponse = _mapper.Map<StudentMedicationDetailResponse>(medication);

            var response = BaseResponse<StudentMedicationDetailResponse>.SuccessResult(
                medicationResponse, "Lấy thông tin thuốc học sinh thành công.");

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICATION_CACHE_SET);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student medication by ID: {MedicationId}", medicationId);
            return BaseResponse<StudentMedicationDetailResponse>.ErrorResult(
                $"Lỗi lấy thông tin thuốc học sinh: {ex.Message}");
        }
    }

    public async Task<BaseListResponse<StudentMedicationListResponse>> GetAllMedicationsByNurseOrStudentAsync(
    int pageIndex,
    int pageSize,
    Guid? nurseId = null,
    Guid? studentId = null,
    StudentMedicationStatus? status = null,
    CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate cache key based on parameters
            var cacheKey = _cacheService.GenerateCacheKey(
                MEDICATION_LIST_PREFIX,
                "nurse_or_student",
                pageIndex.ToString(),
                pageSize.ToString(),
                nurseId?.ToString() ?? "",
                studentId?.ToString() ?? "",
                status?.ToString() ?? "");

            // Check cache for existing result
            var cachedResult = await _cacheService.GetAsync<BaseListResponse<StudentMedicationListResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Cache hit for medications by nurse or student");
                return cachedResult;
            }

            // Build query
            var query = _unitOfWork.GetRepositoryByEntity<StudentMedication>().GetQueryable()
                .AsSplitQuery()
                .Include(sm => sm.Student)
                .Include(sm => sm.Parent)
                .Include(sm => sm.ApprovedBy)
                .Include(sm => sm.Administrations.Where(a => !a.IsDeleted))
                .Include(sm => sm.Schedules.Where(s => !s.IsDeleted))
                .Where(sm => !sm.IsDeleted)
                .AsQueryable();

            // Apply filters based on NurseID, StudentID, and Status
            if (nurseId.HasValue)
            {
                query = query.Where(sm => sm.ApprovedById == nurseId.Value);
            }

            if (studentId.HasValue)
            {
                query = query.Where(sm => sm.StudentId == studentId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(sm => sm.Status == status.Value);
            }

            // If both nurseId and studentId are null, return error to prevent fetching all records
            if (!nurseId.HasValue && !studentId.HasValue)
            {
                return BaseListResponse<StudentMedicationListResponse>.ErrorResult(
                    "Vui lòng cung cấp ít nhất một trong hai tham số NurseID hoặc StudentID.");
            }

            // Order by submission date descending
            query = query.OrderByDescending(sm => sm.SubmittedAt ?? sm.CreatedDate);

            // Get total count and paginated results
            var totalCount = await query.CountAsync(cancellationToken);
            var medications = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            // Map to response model
            var responses = _mapper.Map<List<StudentMedicationListResponse>>(medications);

            // Create success response
            var result = BaseListResponse<StudentMedicationListResponse>.SuccessResult(
                responses, totalCount, pageSize, pageIndex,
                "Lấy danh sách thuốc theo y tá hoặc học sinh thành công.");

            // Cache the result
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICATION_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medications by nurse or student");
            return BaseListResponse<StudentMedicationListResponse>.ErrorResult(
                "Lỗi lấy danh sách thuốc theo y tá hoặc học sinh.");
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

            var student = await ValidateStudentAsync(model.StudentId);
            if (student == null)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult(
                    "Không tìm thấy học sinh hoặc người dùng không phải là học sinh.");
            }

            var parentValidation = await ValidateParentPermissionsAsync(currentUserId, model.StudentId);
            if (!parentValidation.IsValid)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult(parentValidation.ErrorMessage);
            }

            var medication = _mapper.Map<StudentMedication>(model);

            // Xử lý TimesOfDay và SpecificTimes
            if (model.TimesOfDay != null && model.TimesOfDay.Count > 0)
            {
                medication.TimesOfDay = JsonSerializer.Serialize(model.TimesOfDay);
            }

            if (model.SpecificTimes != null && model.SpecificTimes.Count > 0)
            {
                medication.SpecificTimes = JsonSerializer.Serialize(model.SpecificTimes);
            }

            SetAdditionalMedicationProperties(medication, currentUserId, parentValidation.ParentRoleName);

            var medicationRepo = _unitOfWork.GetRepositoryByEntity<StudentMedication>();
            await medicationRepo.AddAsync(medication);

            await CreateInitialStockRecordAsync(medication);
            await _unitOfWork.SaveChangesAsync();

            await CreateMedicationRequestNotificationAsync(medication);
            await InvalidateAllCachesAsync();

            medication = await medicationRepo.GetQueryable()
                .AsSplitQuery()
                .Include(sm => sm.Student)
                .Include(sm => sm.Parent)
                .Include(sm => sm.ApprovedBy)
                .Include(sm => sm.Administrations)
                .Include(sm => sm.StockHistory)
                .FirstOrDefaultAsync(sm => sm.Id == medication.Id);

            var medicationResponse = _mapper.Map<StudentMedicationResponse>(medication);

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

    public async Task<BaseListResponse<StudentMedicationResponse>> CreateBulkStudentMedicationsAsync(
    CreateBulkStudentMedicationRequest request)
    {
        try
        {
            if (request == null || request.Medications == null || !request.Medications.Any())
            {
                _logger.LogWarning("Invalid medication list for studentId: {StudentId}", request.StudentId);
                return BaseListResponse<StudentMedicationResponse>.ErrorResult("Danh sách thuốc không hợp lệ.");
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                _logger.LogWarning("Unable to identify current user for creating bulk medications");
                return BaseListResponse<StudentMedicationResponse>.ErrorResult("Không thể xác định người dùng hiện tại.");
            }

            var student = await ValidateStudentAsync(request.StudentId);
            if (student == null)
            {
                _logger.LogWarning("Student not found or not a student for studentId: {StudentId}", request.StudentId);
                return BaseListResponse<StudentMedicationResponse>.ErrorResult(
                    "Không tìm thấy học sinh hoặc người dùng không phải là học sinh.");
            }

            var parentValidation = await ValidateParentPermissionsAsync(currentUserId, request.StudentId);
            if (!parentValidation.IsValid)
            {
                _logger.LogWarning("Parent validation failed for studentId: {StudentId}, parentId: {ParentId}, error: {Error}",
                    request.StudentId, currentUserId, parentValidation.ErrorMessage);
                return BaseListResponse<StudentMedicationResponse>.ErrorResult(parentValidation.ErrorMessage);
            }

            var medicationRepo = _unitOfWork.GetRepositoryByEntity<StudentMedication>();
            var stockRepo = _unitOfWork.GetRepositoryByEntity<MedicationStock>();
            var requestRepo = _unitOfWork.GetRepositoryByEntity<StudentMedicationRequest>();
            var parentRoleName = parentValidation.ParentRoleName;
            var medications = new List<StudentMedication>();
            var responses = new List<StudentMedicationResponse>();

            // Lấy thông tin phụ huynh
            var parentRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var parent = await parentRepo.GetQueryable()
                .FirstOrDefaultAsync(u => u.Id == currentUserId && !u.IsDeleted);

            // Tạo StudentMedicationRequest
            var medicationRequest = new StudentMedicationRequest
            {
                Id = Guid.NewGuid(),
                StudentId = request.StudentId,
                ParentId = currentUserId,
                Status = StudentMedicationStatus.PendingApproval,
                SubmittedAt = DateTime.UtcNow,
                CreatedBy = parentRoleName,
                CreatedDate = DateTime.UtcNow,
                Priority = request.Priority,
                StudentName = student.FullName,
                StudentCode = student.StudentCode,
                ParentName = parent?.FullName ?? "",
                Code = await GenerateStudentMedicationRequestCodeAsync()
            };
            await requestRepo.AddAsync(medicationRequest);

            foreach (var medicationDetails in request.Medications)
            {
                var medication = _mapper.Map<StudentMedication>(medicationDetails);
                medication.Id = Guid.NewGuid();
                medication.StudentId = request.StudentId;
                medication.ParentId = currentUserId;
                medication.Status = StudentMedicationStatus.PendingApproval;
                medication.SubmittedAt = DateTime.UtcNow;
                medication.CreatedBy = parentRoleName;
                medication.CreatedDate = DateTime.UtcNow;
                medication.StudentMedicationRequestId = medicationRequest.Id;
                medication.QuantityUnit = medicationDetails.QuantityUnit;
                medication.StartDate = medicationDetails.StartDate;
                medication.FrequencyCount = medicationDetails.FrequencyCount;

                // Xử lý TimesOfDay
                if (medicationDetails.TimesOfDay != null && medicationDetails.TimesOfDay.Any())
                {
                    medication.TimesOfDay = JsonSerializer.Serialize(medicationDetails.TimesOfDay);
                }

                // Không kiểm tra định dạng dosage nếu là Bottle
                if (medication.QuantityUnit == QuantityUnitEnum.Bottle)
                {
                    medication.TotalDay = int.MaxValue; // Đặt TotalDay là "Future"
                }
                else
                {
                    // Tính TotalDay cho các đơn vị khác (Tablet, Pack)
                    int dosageQuantity = 1;
                    if (!string.IsNullOrEmpty(medicationDetails.Dosage))
                    {
                        var parts = medicationDetails.Dosage.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0 && int.TryParse(parts[0], out dosageQuantity) && dosageQuantity > 0)
                        {
                            double totalDay = (double)medicationDetails.QuantitySent / (dosageQuantity * (medicationDetails.FrequencyCount ?? 1));
                            medication.TotalDay = (int)Math.Ceiling(totalDay);
                        }
                        else
                        {
                            medication.TotalDay = 0;
                            _logger.LogWarning("Liều lượng không hợp lệ: {Dosage} cho thuốc {MedicationId}", medicationDetails.Dosage, medication.Id);
                        }
                    }
                }

                // Tính TotalDoses
                var totalDoses = CalculateTotalDosesFromDosage(
                    medicationDetails.Dosage,
                    medicationDetails.QuantitySent,
                    medicationDetails.FrequencyCount);
                medication.TotalDoses = totalDoses;
                medication.RemainingDoses = totalDoses;

                await medicationRepo.AddAsync(medication);
                medications.Add(medication);

                var initialStock = new MedicationStock
                {
                    Id = Guid.NewGuid(),
                    StudentMedicationId = medication.Id,
                    QuantityAdded = medicationDetails.QuantitySent,
                    QuantityUnit = medicationDetails.QuantityUnit,
                    ExpiryDate = medicationDetails.ExpiryDate,
                    DateAdded = DateTime.UtcNow,
                    Notes = "Thuốc ban đầu từ phụ huynh",
                    IsInitialStock = true,
                    CreatedBy = parentRoleName,
                    CreatedDate = DateTime.UtcNow
                };

                await stockRepo.AddAsync(initialStock);

                var medicationResponse = _mapper.Map<StudentMedicationResponse>(medication);
                medicationResponse.Id = medication.Id;
                medicationResponse.RequestId = medication.StudentMedicationRequestId;
                medicationResponse.StudentName = student.FullName;
                medicationResponse.StudentCode = student.StudentCode;
                medicationResponse.ParentName = parent?.FullName ?? "";
                medicationResponse.StatusDisplayName = medication.Status.ToString();
                medicationResponse.PriorityDisplayName = medication.Priority.ToString();
                medicationResponse.Code = medicationRequest.Code;
                responses.Add(medicationResponse);
            }

            // Lưu thay đổi
            medicationRequest.MedicationsDetails = medications;
            await _unitOfWork.SaveChangesAsync();

            // Xóa cache cụ thể sau khi tạo yêu cầu thuốc
            var medicationCacheKey = _cacheService.GenerateCacheKey(
                MEDICATION_REQUEST_PREFIX,
                "1", // pageIndex mặc định
                "10", // pageSize mặc định
                request.StudentId.ToString(),
                currentUserId.ToString(),
                StudentMedicationStatus.PendingApproval.ToString()
            );
            await _cacheService.RemoveAsync(medicationCacheKey);
            _logger.LogDebug("Đã xóa cache yêu cầu thuốc cụ thể: {CacheKey}", medicationCacheKey);

            // Xóa cache danh sách yêu cầu thuốc chung
            await _cacheService.RemoveByPrefixAsync(MEDICATION_REQUEST_PREFIX);
            _logger.LogDebug("Đã xóa cache danh sách yêu cầu thuốc với prefix: {Prefix}", MEDICATION_REQUEST_PREFIX);

            // Xóa cache liên quan đến học sinh và phụ huynh
            await _cacheService.RemoveByPrefixAsync(STUDENT_LIST_PREFIX);
            _logger.LogDebug("Đã xóa cache danh sách học sinh với prefix: {Prefix}", STUDENT_LIST_PREFIX);
            await _cacheService.RemoveByPrefixAsync(PARENT_LIST_PREFIX);
            _logger.LogDebug("Đã xóa cache danh sách phụ huynh với prefix: {Prefix}", PARENT_LIST_PREFIX);

            // Gọi InvalidateAllCachesAsync để xóa các cache liên quan
            await InvalidateAllCachesAsync();

            await CreateBulkMedicationRequestNotificationAsync(medications);
            await _authService.InvalidateUserCacheAsync(student.Username, student.Email, student.Id);

            _logger.LogInformation("Created {Count} bulk student medications with request ID {RequestId} for student {StudentId}",
                request.Medications.Count, medicationRequest.Id, request.StudentId);

            return BaseListResponse<StudentMedicationResponse>.SuccessResult(
                responses, responses.Count, request.Medications.Count, 1,
                "Gửi yêu cầu thuốc hàng loạt cho học sinh thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bulk student medications for student {StudentId}", request.StudentId);
            return BaseListResponse<StudentMedicationResponse>.ErrorResult($"Lỗi gửi yêu cầu thuốc hàng loạt: {ex.Message}");
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
                .Include(sm => sm.Schedules)
                .Include(sm => sm.StockHistory)
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
            if (!string.IsNullOrEmpty(model.DoctorName))
                medication.DoctorName = model.DoctorName;
            if (!string.IsNullOrEmpty(model.Hospital))
                medication.Hospital = model.Hospital;
            if (model.PrescriptionDate.HasValue)
                medication.PrescriptionDate = model.PrescriptionDate.Value;
            if (!string.IsNullOrEmpty(model.PrescriptionNumber))
                medication.PrescriptionNumber = model.PrescriptionNumber;
            if (model.QuantitySent.HasValue && model.QuantitySent.Value > 0)
            {
                medication.QuantitySent = model.QuantitySent.Value;

                await UpdateStockRecordAsync(medication, model.QuantitySent.Value);
            }

            if (!string.IsNullOrEmpty(model.QuantityUnit))
            {
                if (Enum.TryParse<QuantityUnitEnum>(model.QuantityUnit, true, out var unit))
                {
                    medication.QuantityUnit = unit;
                }
                else
                {
                    return BaseResponse<StudentMedicationResponse>.ErrorResult("Đơn vị số lượng không hợp lệ.");
                }
            }
            await UpdateStockRecordAsync(medication, model.QuantitySent ?? medication.QuantitySent);

            if (model.SpecialNotes != null)
                medication.SpecialNotes = model.SpecialNotes;
            if (model.EmergencyContactInstructions != null)
                medication.EmergencyContactInstructions = model.EmergencyContactInstructions;
            if (model.Priority.HasValue)
                medication.Priority = model.Priority.Value;

            // Cập nhật TimesOfDay và SpecificTimes nếu có
            if (model.TimesOfDay != null && model.TimesOfDay.Count > 0)
            {
                medication.TimesOfDay = JsonSerializer.Serialize(model.TimesOfDay);
            }

            if (model.SpecificTimes != null && model.SpecificTimes.Count > 0)
            {
                medication.SpecificTimes = JsonSerializer.Serialize(model.SpecificTimes);
            }

            medication.LastUpdatedBy = parentRoleName;
            medication.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            await InvalidateAllCachesAsync();

            var medicationResponse = _mapper.Map<StudentMedicationResponse>(medication);

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

    public async Task<BaseResponse<StudentMedicationResponse>> AddMoreMedicationAsync(
        AddMoreMedicationRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult("Không thể xác định người dùng hiện tại.");
            }

            var medicationRepo = _unitOfWork.GetRepositoryByEntity<StudentMedication>();
            var medication = await medicationRepo.GetQueryable()
                .Include(sm => sm.Student)
                .Include(sm => sm.Parent)
                .Include(sm => sm.StockHistory)
                .FirstOrDefaultAsync(sm => sm.Id == request.StudentMedicationId && !sm.IsDeleted);

            if (medication == null)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult("Không tìm thấy thuốc.");
            }

            var parentValidation = await ValidateParentPermissionsAsync(currentUserId, medication.StudentId);
            if (!parentValidation.IsValid)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult(parentValidation.ErrorMessage);
            }

            if (medication.Status != StudentMedicationStatus.Active && 
                medication.Status != StudentMedicationStatus.Approved)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult(
                    "Chỉ có thể thêm thuốc cho thuốc đã được phê duyệt hoặc đang hoạt động.");
            }

            // Tạo stock record mới
            var stockRepo = _unitOfWork.GetRepositoryByEntity<MedicationStock>();
            var newStock = new MedicationStock
            {
                Id = Guid.NewGuid(),
                StudentMedicationId = medication.Id,
                QuantityAdded = request.AdditionalQuantity,
                QuantityUnit = medication.QuantityUnit ?? QuantityUnitEnum.Tablet, 
                ExpiryDate = request.NewExpiryDate,
                DateAdded = DateTime.Now,
                Notes = request.Notes,
                IsInitialStock = false,
                CreatedBy = parentValidation.ParentRoleName,
                CreatedDate = DateTime.Now
            };

            await stockRepo.AddAsync(newStock);

            // Cập nhật tổng số lượng thuốc
            medication.QuantitySent += request.AdditionalQuantity;
            
            // Tính toán lại TotalDoses và RemainingDoses
            await RecalculateMedicationDosesAsync(medication);

            medication.LastUpdatedBy = parentValidation.ParentRoleName;
            medication.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            await CreateAdditionalMedicationNotificationAsync(medication, request.AdditionalQuantity);
            await InvalidateAllCachesAsync();

            var medicationResponse = _mapper.Map<StudentMedicationResponse>(medication);

            _logger.LogInformation("Added {Quantity} more medication for {MedicationId} - New TotalDoses: {TotalDoses}, New RemainingDoses: {RemainingDoses}",
                request.AdditionalQuantity, medication.Id, medication.TotalDoses, medication.RemainingDoses);

            return BaseResponse<StudentMedicationResponse>.SuccessResult(
                medicationResponse,
                $"Đã thêm {request.AdditionalQuantity} {medication.QuantityUnit} thuốc thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding more medication: {MedicationId}", request.StudentMedicationId);
            return BaseResponse<StudentMedicationResponse>.ErrorResult($"Lỗi thêm thuốc: {ex.Message}");
        }
    }

    public async Task<BaseResponse<StudentMedicationResponse>> UpdateMedicationManagementAsync(
        Guid medicationId, UpdateMedicationManagementRequest request)
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
                    "Chỉ School Nurse mới có quyền cập nhật cài đặt quản lý thuốc.");
            }

            var medicationRepo = _unitOfWork.GetRepositoryByEntity<StudentMedication>();
            var medication = await medicationRepo.GetQueryable()
                .Include(sm => sm.Student)
                .Include(sm => sm.Parent)
                .Include(sm => sm.Schedules)
                .FirstOrDefaultAsync(sm => sm.Id == medicationId && !sm.IsDeleted);

            if (medication == null)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult("Không tìm thấy thuốc học sinh.");
            }

            var schoolNurseRoleName = await GetSchoolNurseRoleName();

            if (request.TotalDoses.HasValue)
                medication.TotalDoses = request.TotalDoses.Value;
            if (request.RemainingDoses.HasValue)
                medication.RemainingDoses = request.RemainingDoses.Value;
            if (request.MinStockThreshold.HasValue)
                medication.MinStockThreshold = request.MinStockThreshold.Value;
            if (request.SkipOnAbsence.HasValue)
                medication.SkipOnAbsence = request.SkipOnAbsence.Value;
            if (request.RequireNurseConfirmation.HasValue)
                medication.RequireNurseConfirmation = request.RequireNurseConfirmation.Value;
            if (request.AutoGenerateSchedule.HasValue)
                medication.AutoGenerateSchedule = request.AutoGenerateSchedule.Value;
            if (request.SkipWeekends.HasValue)
                medication.SkipWeekends = request.SkipWeekends.Value;
            if (!string.IsNullOrEmpty(request.SpecificTimes))
                medication.SpecificTimes = request.SpecificTimes;
            if (!string.IsNullOrEmpty(request.SkipDates))
                medication.SkipDates = request.SkipDates;

            if (!string.IsNullOrEmpty(request.ManagementNotes))
                medication.ManagementNotes = request.ManagementNotes;

            medication.LastUpdatedBy = schoolNurseRoleName;
            medication.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            await InvalidateAllCachesAsync();

            var medicationResponse = _mapper.Map<StudentMedicationResponse>(medication);

            _logger.LogInformation("Updated medication management settings for {MedicationId}", medicationId);

            return BaseResponse<StudentMedicationResponse>.SuccessResult(
                medicationResponse, "Cập nhật cài đặt quản lý thuốc thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating medication management: {MedicationId}", medicationId);
            return BaseResponse<StudentMedicationResponse>.ErrorResult($"Lỗi cập nhật cài đặt quản lý: {ex.Message}");
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

    public async Task<BaseListResponse<StudentMedicationRequestResponse>> GetAllStudentMedicationRequestAsync(
    int pageIndex,
    int pageSize,
    Guid? studentId = null,
    Guid? parentId = null,
    StudentMedicationStatus? status = null,
    CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(
                "student_medication_requests",
                pageIndex.ToString(),
                pageSize.ToString(),
                studentId?.ToString() ?? "",
                parentId?.ToString() ?? "",
                status?.ToString() ?? ""
            );

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<StudentMedicationRequestResponse>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<StudentMedicationRequest>().GetQueryable()
                .AsSplitQuery()
                .Include(r => r.Student)
                .Include(r => r.Parent)
                .Include(r => r.ApprovedBy)
                .Where(r => !r.IsDeleted)
                .AsQueryable();

            query = ApplyStudentMedicationRequestFilters(query, studentId, parentId, status);

            var totalCount = await query.CountAsync(cancellationToken);
            var requests = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = _mapper.Map<List<StudentMedicationRequestResponse>>(requests);

            var result = BaseListResponse<StudentMedicationRequestResponse>.SuccessResult(
                responses, totalCount, pageSize, pageIndex,
                "Lấy danh sách yêu cầu thuốc thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICATION_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all student medication requests");
            return BaseListResponse<StudentMedicationRequestResponse>.ErrorResult("Lỗi lấy danh sách yêu cầu thuốc.");
        }
    }

    public async Task<BaseResponse<StudentMedicationRequestDetailResponse>> GetStudentMedicationRequestByIdAsync(
        Guid requestId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BaseResponse<StudentMedicationRequestDetailResponse>.ErrorResult(
                    "Không thể xác định người dùng hiện tại.");
            }

            var cacheKey = _cacheService.GenerateCacheKey("student_medication_request_detail", requestId.ToString());
            var cachedResponse = await _cacheService.GetAsync<BaseResponse<StudentMedicationRequestDetailResponse>>(cacheKey);

            if (cachedResponse != null)
            {
                var canAccess = await ValidateRequestAccessPermissionAsync(currentUserId, requestId);
                if (!canAccess)
                {
                    return BaseResponse<StudentMedicationRequestDetailResponse>.ErrorResult(
                        "Bạn không có quyền xem thông tin yêu cầu này.");
                }
                return cachedResponse;
            }

            var requestRepo = _unitOfWork.GetRepositoryByEntity<StudentMedicationRequest>();
            var request = await requestRepo.GetQueryable()
                .AsSplitQuery()
                .Include(r => r.Student)
                .Include(r => r.Parent)
                .Include(r => r.ApprovedBy)
                .Include(r => r.MedicationsDetails)
                    .ThenInclude(m => m.Student)
                .Include(r => r.MedicationsDetails)
                    .ThenInclude(m => m.Parent)
                .Include(r => r.MedicationsDetails)
                    .ThenInclude(m => m.ApprovedBy)
                .Include(r => r.MedicationsDetails)
                    .ThenInclude(m => m.StockHistory)
                .Where(r => r.Id == requestId && !r.IsDeleted)
                .FirstOrDefaultAsync();

            if (request == null)
            {
                return BaseResponse<StudentMedicationRequestDetailResponse>.ErrorResult("Không tìm thấy yêu cầu thuốc.");
            }

            var currentUser = await GetCurrentUserWithRoles();
            if (currentUser == null)
            {
                return BaseResponse<StudentMedicationRequestDetailResponse>.ErrorResult(
                    "Không thể xác định quyền của người dùng.");
            }

            var hasPermission = await ValidateUserAccessToRequestAsync(currentUser, request);
            if (!hasPermission)
            {
                return BaseResponse<StudentMedicationRequestDetailResponse>.ErrorResult(
                    "Bạn không có quyền xem thông tin yêu cầu này.");
            }

            var requestResponse = _mapper.Map<StudentMedicationRequestDetailResponse>(request);

            var response = BaseResponse<StudentMedicationRequestDetailResponse>.SuccessResult(
                requestResponse, "Lấy thông tin chi tiết yêu cầu thuốc thành công.");

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICATION_CACHE_SET);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông tin yêu cầu thuốc: {RequestId}", requestId);
            return BaseResponse<StudentMedicationRequestDetailResponse>.ErrorResult(
                $"Lỗi lấy thông tin yêu cầu thuốc: {ex.Message}");
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
                return BaseResponse<StudentMedicationResponse>.ErrorResult("Chỉ School Nurse mới có quyền phê duyệt thuốc.");
            }

            var requestRepo = _unitOfWork.GetRepositoryByEntity<StudentMedicationRequest>();
            var medicationRequest = await requestRepo.GetQueryable()
                .Include(r => r.MedicationsDetails)
                .FirstOrDefaultAsync(r => r.Id == medicationId && !r.IsDeleted);

            if (medicationRequest == null)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult("Không tìm thấy yêu cầu thuốc.");
            }

            if (medicationRequest.Status != StudentMedicationStatus.PendingApproval)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult("Chỉ có thể phê duyệt yêu cầu đang chờ duyệt.");
            }

            medicationRequest.ApprovedById = currentUserId;
            medicationRequest.ApprovedAt = DateTime.Now;
            medicationRequest.Status = request.IsApproved ? StudentMedicationStatus.Approved : StudentMedicationStatus.Rejected;
            medicationRequest.LastUpdatedBy = await GetSchoolNurseRoleName();
            medicationRequest.LastUpdatedDate = DateTime.Now;

            if (request.IsApproved)
            {
                foreach (var medication in medicationRequest.MedicationsDetails)
                {
                    medication.Status = StudentMedicationStatus.Approved;
                    medication.ApprovedById = currentUserId; // Cập nhật ApprovedById
                    _logger.LogInformation("Medication {MedicationId} set to APPROVED, ApprovedById set to {ApprovedById}", medication.Id, currentUserId);
                }
            }
            else
            {
                foreach (var medication in medicationRequest.MedicationsDetails)
                {
                    medication.Status = StudentMedicationStatus.Rejected;
                    medication.ApprovedById = currentUserId; // Cập nhật ApprovedById
                    medication.RejectionReason = request.Notes;
                    _logger.LogInformation("Medication {MedicationId} set to REJECTED, ApprovedById set to {ApprovedById}", medication.Id, currentUserId);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await InvalidateAllCachesAsync();

            var medicationResponse = _mapper.Map<StudentMedicationResponse>(medicationRequest.MedicationsDetails.FirstOrDefault());
            return BaseResponse<StudentMedicationResponse>.SuccessResult(medicationResponse, request.IsApproved ? "Phê duyệt thành công." : "Từ chối thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving medication request: {MedicationId}", medicationId);
            return BaseResponse<StudentMedicationResponse>.ErrorResult($"Lỗi phê duyệt: {ex.Message}");
        }
    }

    public async Task<BaseResponse<List<StudentMedicationResponse>>> UpdateQuantityReceivedAsync(
    Guid requestId,
    UpdateQuantityReceivedRequest request,
    CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BaseResponse<List<StudentMedicationResponse>>.ErrorResult("Không thể xác định người dùng hiện tại.");
            }

            var currentUser = await ValidateSchoolNursePermissions(currentUserId);
            if (currentUser == null)
            {
                return BaseResponse<List<StudentMedicationResponse>>.ErrorResult(
                    "Chỉ School Nurse mới có quyền cập nhật số lượng thuốc nhận được.");
            }

            // Lấy danh sách StudentMedication theo requestId
            var medicationRepo = _unitOfWork.GetRepositoryByEntity<StudentMedication>();
            var medications = await medicationRepo.GetQueryable()
                .Include(sm => sm.Student)
                .Include(sm => sm.Parent)
                .Include(sm => sm.StockHistory)
                .Where(sm => sm.StudentMedicationRequestId == requestId && !sm.IsDeleted)
                .ToListAsync(cancellationToken);

            if (!medications.Any())
            {
                return BaseResponse<List<StudentMedicationResponse>>.ErrorResult("Không tìm thấy thuốc nào thuộc yêu cầu này.");
            }

            // Kiểm tra danh sách MedicationId trong request
            var requestMedicationIds = request.Medications.Select(m => m.MedicationId).ToList();
            var invalidIds = requestMedicationIds.Except(medications.Select(m => m.Id)).ToList();
            if (invalidIds.Any())
            {
                return BaseResponse<List<StudentMedicationResponse>>.ErrorResult(
                    $"Một số ID thuốc không thuộc yêu cầu này: {string.Join(", ", invalidIds)}.");
            }

            var schoolNurseRoleName = await GetSchoolNurseRoleName();
            var stockRepo = _unitOfWork.GetRepositoryByEntity<MedicationStock>();
            var updatedMedications = new List<StudentMedication>();

            foreach (var medUpdate in request.Medications)
            {
                var medication = medications.FirstOrDefault(m => m.Id == medUpdate.MedicationId);
                if (medication == null)
                {
                    continue; // Bỏ qua nếu không tìm thấy (đã kiểm tra ở trên)
                }

                if (medUpdate.QuantityReceived < 0)
                {
                    return BaseResponse<List<StudentMedicationResponse>>.ErrorResult(
                        $"Số lượng nhận được của thuốc {medUpdate.MedicationId} phải lớn hơn hoặc bằng 0.");
                }

                // Cập nhật QuantityReceive và trạng thái Received
                medication.QuantityReceive = medUpdate.QuantityReceived;
                medication.Received = medUpdate.QuantityReceived > 0 ? ReceivedMedication.Received : ReceivedMedication.NotReceive;
                medication.LastUpdatedBy = schoolNurseRoleName;
                medication.LastUpdatedDate = DateTime.Now;

                // Cập nhật stock record
                var newStock = new MedicationStock
                {
                    Id = Guid.NewGuid(),
                    StudentMedicationId = medication.Id,
                    QuantityAdded = medUpdate.QuantityReceived,
                    QuantityUnit = medication.QuantityUnit ?? QuantityUnitEnum.Tablet,
                    ExpiryDate = medication.ExpiryDate,
                    DateAdded = DateTime.Now,
                    Notes = medUpdate.Notes ?? "Cập nhật số lượng thuốc nhận được từ phụ huynh",
                    IsInitialStock = false,
                    CreatedBy = schoolNurseRoleName,
                    CreatedDate = DateTime.Now,
                    LastUpdatedBy = schoolNurseRoleName,
                    LastUpdatedDate = DateTime.Now
                };
                await stockRepo.AddAsync(newStock);

                // Cập nhật TotalDoses và RemainingDoses
                await RecalculateMedicationDosesAsync(medication);

                medicationRepo.Update(medication);
                updatedMedications.Add(medication);

                _logger.LogInformation("Cập nhật QuantityReceived cho thuốc {MedicationId}: QuantityReceive={QuantityReceived}, Received={ReceivedStatus}",
                    medUpdate.MedicationId, medUpdate.QuantityReceived, medication.Received);
            }

            // Lưu tất cả thay đổi
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await InvalidateAllCachesAsync();

            var medicationResponses = updatedMedications.Select(m => _mapper.Map<StudentMedicationResponse>(m)).ToList();

            return BaseResponse<List<StudentMedicationResponse>>.SuccessResult(
                medicationResponses, "Cập nhật số lượng thuốc nhận được thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật QuantityReceived cho yêu cầu {RequestId}", requestId);
            return BaseResponse<List<StudentMedicationResponse>>.ErrorResult(
                $"Lỗi cập nhật số lượng thuốc nhận được: {ex.Message}");
        }
    }

    public async Task<BaseListResponse<PendingApprovalResponse>> GetPendingApprovalsAsync
    (
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(
                MEDICATION_LIST_PREFIX, "pending_approvals", pageIndex.ToString(), pageSize.ToString());

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<PendingApprovalResponse>>(cacheKey);
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

            var responses = _mapper.Map<List<PendingApprovalResponse>>(medications);

            var result = BaseListResponse<PendingApprovalResponse>.SuccessResult(
                responses, totalCount, pageSize, pageIndex,
                "Lấy danh sách yêu cầu chờ phê duyệt thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICATION_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending approvals");
            return BaseListResponse<PendingApprovalResponse>.ErrorResult("Lỗi lấy danh sách chờ phê duyệt.");
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
                .Include(sm => sm.Schedules)
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
                var pendingSchedules = medication.Schedules?
                    .Where(s => !s.IsDeleted && s.Status == MedicationScheduleStatus.Pending)
                    .ToList();

                if (pendingSchedules?.Any() == true)
                {
                    foreach (var schedule in pendingSchedules)
                    {
                        schedule.Status = MedicationScheduleStatus.Cancelled;
                        schedule.Notes = "Thuốc đã ngưng sử dụng";
                        schedule.LastUpdatedBy = schoolNurseRoleName;
                        schedule.LastUpdatedDate = DateTime.Now;
                    }

                    _logger.LogInformation(
                        "Cancelled {Count} pending schedules for discontinued medication {MedicationId}",
                        pendingSchedules.Count, medicationId);
                }

                await CreateStatusChangeNotificationAsync(medication, oldStatus, request.Status, request.Reason);
            }
            else if (request.Status == StudentMedicationStatus.Active && oldStatus != StudentMedicationStatus.Active)
            {
                _logger.LogInformation(
                    "Medication {MedicationId} status changed to ACTIVE - will be picked up by background service",
                    medicationId);
            }
            else if (request.Status == StudentMedicationStatus.Completed)
            {
                var remainingSchedules = medication.Schedules?
                    .Where(s => !s.IsDeleted && s.Status == MedicationScheduleStatus.Pending)
                    .ToList();

                if (remainingSchedules?.Any() == true)
                {
                    foreach (var schedule in remainingSchedules)
                    {
                        schedule.Status = MedicationScheduleStatus.Cancelled;
                        schedule.Notes = "Thuốc đã hoàn thành điều trị";
                        schedule.LastUpdatedBy = schoolNurseRoleName;
                        schedule.LastUpdatedDate = DateTime.Now;
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await InvalidateAllCachesAsync();

            var medicationResponse = _mapper.Map<StudentMedicationResponse>(medication);

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

    public async Task<BaseResponse<StudentMedicationResponse>> RejectStudentMedicationAsync(
        Guid medicationId, RejectStudentMedicationRequest request)
    {
        try
        {
            // Kiểm tra validation với _rejectValidator
            var validationResult = await _rejectValidator.ValidateAsync(request);
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
                    "Chỉ School Nurse, Admin hoặc Manager mới có quyền từ chối yêu cầu thuốc.");
            }

            var requestRepo = _unitOfWork.GetRepositoryByEntity<StudentMedicationRequest>();
            var medicationRequest = await requestRepo.GetQueryable()
                .Include(r => r.MedicationsDetails)
                .Include(r => r.Student)
                .Include(r => r.Parent)
                .Include(r => r.ApprovedBy)
                .FirstOrDefaultAsync(r => r.Id == medicationId && !r.IsDeleted);

            if (medicationRequest == null)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult("Không tìm thấy yêu cầu thuốc.");
            }

            if (medicationRequest.Status != StudentMedicationStatus.PendingApproval)
            {
                return BaseResponse<StudentMedicationResponse>.ErrorResult(
                    "Chỉ có thể từ chối yêu cầu đang chờ duyệt.");
            }

            var schoolNurseRoleName = await GetSchoolNurseRoleName();

            medicationRequest.ApprovedById = currentUserId;
            medicationRequest.ApprovedAt = DateTime.Now;
            medicationRequest.Status = StudentMedicationStatus.Rejected;
            medicationRequest.RejectionReason = request.RejectionReason;
            medicationRequest.LastUpdatedBy = schoolNurseRoleName;
            medicationRequest.LastUpdatedDate = DateTime.Now;

            foreach (var medication in medicationRequest.MedicationsDetails)
            {
                medication.Status = StudentMedicationStatus.Rejected;
                medication.ApprovedById = currentUserId;
                medication.RejectionReason = request.RejectionReason;
                medication.LastUpdatedBy = schoolNurseRoleName;
                medication.LastUpdatedDate = DateTime.Now;
                _logger.LogInformation("Medication {MedicationId} set to REJECTED, ApprovedById set to {ApprovedById}", medication.Id, currentUserId);
            }

            await _unitOfWork.SaveChangesAsync();

            await CreateApprovalNotificationRequestAsync(medicationRequest, false, request.Notes);
            await InvalidateAllCachesAsync();

            var medicationResponse = _mapper.Map<StudentMedicationResponse>(medicationRequest.MedicationsDetails.FirstOrDefault());

            _logger.LogInformation("Medication request {MedicationRequestId} rejected by {NurseId}", medicationId, currentUserId);

            return BaseResponse<StudentMedicationResponse>.SuccessResult(
                medicationResponse, "Từ chối yêu cầu thuốc thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting medication request: {MedicationRequestId}", medicationId);
            return BaseResponse<StudentMedicationResponse>.ErrorResult($"Lỗi từ chối yêu cầu thuốc: {ex.Message}");
        }
    }

    public async Task<BaseListResponse<MedicationAdministrationResponse>> GetAdministrationHistoryAsync
    (
        Guid medicationId,
        int pageIndex,
        int pageSize,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(
                MEDICATION_LIST_PREFIX, "administration_history", medicationId.ToString(),
                pageIndex.ToString(), pageSize.ToString(),
                fromDate?.ToString("yyyy-MM-dd") ?? "",
                toDate?.ToString("yyyy-MM-dd") ?? "");

            var cachedResult =
                await _cacheService.GetAsync<BaseListResponse<MedicationAdministrationResponse>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<MedicationAdministration>().GetQueryable()
                .Include(ma => ma.AdministeredBy)
                .Include(ma => ma.StudentMedication).ThenInclude(sm => sm.Student)
                .Where(ma => ma.StudentMedicationId == medicationId && !ma.IsDeleted);

            if (fromDate.HasValue)
            {
                query = query.Where(ma => ma.AdministeredAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(ma => ma.AdministeredAt <= toDate.Value.AddDays(1));
            }

            query = query.OrderByDescending(ma => ma.AdministeredAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var administrations = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = _mapper.Map<List<MedicationAdministrationResponse>>(administrations);

            var result = BaseListResponse<MedicationAdministrationResponse>.SuccessResult(
                responses, totalCount, pageSize, pageIndex,
                "Lấy lịch sử cho uống thuốc thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting administration history: {MedicationId}", medicationId);
            return BaseListResponse<MedicationAdministrationResponse>.ErrorResult("Lỗi lấy lịch sử cho uống thuốc.");
        }
    }

    #endregion

    #region Parent Specific Methods

    public async Task<BaseListResponse<ParentMedicationResponse>> GetMyChildrenMedicationsAsync
    (
        int pageIndex,
        int pageSize,
        StudentMedicationStatus? status = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BaseListResponse<ParentMedicationResponse>.ErrorResult(
                    "Không thể xác định người dùng hiện tại.");
            }

            var cacheKey = _cacheService.GenerateCacheKey(
                MEDICATION_LIST_PREFIX,
                "my_children",
                currentUserId.ToString(),
                pageIndex.ToString(),
                pageSize.ToString(),
                status?.ToString() ?? "");

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<ParentMedicationResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Cache hit for parent medications");
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<StudentMedication>().GetQueryable()
                .Include(sm => sm.Student)
                .Include(sm => sm.Parent)
                .Include(sm => sm.ApprovedBy)
                .Include(sm => sm.Administrations.Where(a => !a.IsDeleted))
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

            var responses = _mapper.Map<List<ParentMedicationResponse>>(medications);

            var result = BaseListResponse<ParentMedicationResponse>.SuccessResult(
                responses, totalCount, pageSize, pageIndex,
                "Lấy danh sách thuốc con em thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICATION_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my children medications");
            return BaseListResponse<ParentMedicationResponse>.ErrorResult("Lỗi lấy danh sách thuốc con em.");
        }
    }

    #endregion

    #region Parent Medication Stock Methods

    public async Task<BaseListResponse<MedicationStockResponse>> GetMyMedicationStockHistoryAsync
    (
        int pageIndex,
        int pageSize,
        Guid? studentId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BaseListResponse<MedicationStockResponse>.ErrorResult(
                    "Không thể xác định người dùng hiện tại.");
            }

            var cacheKey = _cacheService.GenerateCacheKey(
                MEDICATION_STOCK_PREFIX,
                "my_history",
                currentUserId.ToString(),
                pageIndex.ToString(),
                pageSize.ToString(),
                studentId?.ToString() ?? "",
                fromDate?.ToString("yyyy-MM-dd") ?? "",
                toDate?.ToString("yyyy-MM-dd") ?? "");

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<MedicationStockResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Cache hit for my medication stock history");
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<MedicationStock>()
                .GetQueryable()
                .Include(ms => ms.StudentMedication).ThenInclude(sm => sm.Student)
                .Include(ms => ms.StudentMedication).ThenInclude(sm => sm.Parent)
                .Where(ms => ms.StudentMedication.ParentId == currentUserId &&
                             !ms.IsDeleted &&
                             !ms.StudentMedication.IsDeleted);

            query = ApplyMedicationStockFilters(query, studentId, null, null, fromDate, toDate, null, null);

            query = query.OrderByDescending(ms => ms.DateAdded);

            var totalCount = await query.CountAsync(cancellationToken);
            var stockRecords = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = _mapper.Map<List<MedicationStockResponse>>(stockRecords);

            var result = BaseListResponse<MedicationStockResponse>.SuccessResult(
                responses, totalCount, pageSize, pageIndex,
                "Lấy lịch sử gửi thuốc của bạn thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            await _cacheService.AddToTrackingSetAsync(cacheKey, STOCK_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my medication stock history for parent");
            return BaseListResponse<MedicationStockResponse>.ErrorResult("Lỗi lấy lịch sử gửi thuốc của bạn.");
        }
    }

    public async Task<BaseListResponse<MedicationStockResponse>> GetMedicationStockHistoryAsync
    (
        Guid studentMedicationId,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BaseListResponse<MedicationStockResponse>.ErrorResult(
                    "Không thể xác định người dùng hiện tại.");
            }

            var medication = await _unitOfWork.GetRepositoryByEntity<StudentMedication>()
                .GetQueryable()
                .FirstOrDefaultAsync(sm => sm.Id == studentMedicationId &&
                                           sm.ParentId == currentUserId &&
                                           !sm.IsDeleted);

            if (medication == null)
            {
                return BaseListResponse<MedicationStockResponse>.ErrorResult(
                    "Không tìm thấy thuốc hoặc bạn không có quyền xem.");
            }

            var cacheKey = _cacheService.GenerateCacheKey(
                MEDICATION_STOCK_PREFIX,
                "history",
                studentMedicationId.ToString(),
                pageIndex.ToString(),
                pageSize.ToString());

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<MedicationStockResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Cache hit for medication stock history");
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<MedicationStock>()
                .GetQueryable()
                .Include(ms => ms.StudentMedication).ThenInclude(sm => sm.Student)
                .Include(ms => ms.StudentMedication).ThenInclude(sm => sm.Parent)
                .Where(ms => ms.StudentMedicationId == studentMedicationId && !ms.IsDeleted)
                .OrderByDescending(ms => ms.DateAdded);

            var totalCount = await query.CountAsync(cancellationToken);
            var stockRecords = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = _mapper.Map<List<MedicationStockResponse>>(stockRecords);

            var result = BaseListResponse<MedicationStockResponse>.SuccessResult(
                responses, totalCount, pageSize, pageIndex,
                "Lấy lịch sử gửi thuốc thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            await _cacheService.AddToTrackingSetAsync(cacheKey, STOCK_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting medication stock history: {MedicationId}", studentMedicationId);
            return BaseListResponse<MedicationStockResponse>.ErrorResult("Lỗi lấy lịch sử gửi thuốc.");
        }
    }

    #endregion

    #region School Nurse MedicationStock Methods

    public async Task<BaseListResponse<MedicationStockResponse>> GetAllMedicationStockHistoryAsync
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
    )
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BaseListResponse<MedicationStockResponse>.ErrorResult(
                    "Không thể xác định người dùng hiện tại.");
            }

            var currentUser = await ValidateSchoolNursePermissions(currentUserId);
            if (currentUser == null)
            {
                return BaseListResponse<MedicationStockResponse>.ErrorResult(
                    "Chỉ School Nurse mới có quyền xem tất cả lịch sử gửi thuốc.");
            }

            var cacheKey = _cacheService.GenerateCacheKey(
                MEDICATION_STOCK_PREFIX,
                "all_history",
                pageIndex.ToString(),
                pageSize.ToString(),
                studentId?.ToString() ?? "",
                parentId?.ToString() ?? "",
                medicationId?.ToString() ?? "",
                fromDate?.ToString("yyyy-MM-dd") ?? "",
                toDate?.ToString("yyyy-MM-dd") ?? "",
                isExpired?.ToString() ?? "",
                lowStock?.ToString() ?? "");

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<MedicationStockResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Cache hit for all medication stock history");
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<MedicationStock>()
                .GetQueryable()
                .Include(ms => ms.StudentMedication).ThenInclude(sm => sm.Student)
                .Include(ms => ms.StudentMedication).ThenInclude(sm => sm.Parent)
                .Where(ms => !ms.IsDeleted && !ms.StudentMedication.IsDeleted);

            query = ApplyMedicationStockFilters(query, studentId, parentId, medicationId, fromDate, toDate, isExpired,
                lowStock);

            query = query.OrderByDescending(ms => ms.DateAdded);

            var totalCount = await query.CountAsync(cancellationToken);
            var stockRecords = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = _mapper.Map<List<MedicationStockResponse>>(stockRecords);

            var result = BaseListResponse<MedicationStockResponse>.SuccessResult(
                responses, totalCount, pageSize, pageIndex,
                "Lấy tất cả lịch sử gửi thuốc thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            await _cacheService.AddToTrackingSetAsync(cacheKey, STOCK_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all medication stock history for nurse");
            return BaseListResponse<MedicationStockResponse>.ErrorResult("Lỗi lấy lịch sử gửi thuốc.");
        }
    }

    public async Task<BaseResponse<StudentMedicationUsageHistoryResponse>> AdministerMedicationAsync(
         Guid medicationId,
         AdministerMedicationRequest request,
         CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
                return BaseResponse<StudentMedicationUsageHistoryResponse>.ErrorResult("Không thể xác định người dùng hiện tại.");

            var currentUser = await ValidateSchoolNursePermissions(currentUserId);
            if (currentUser == null)
                return BaseResponse<StudentMedicationUsageHistoryResponse>.ErrorResult("Chỉ y tá trường học mới có quyền ghi nhận việc sử dụng thuốc.");

            var medicationRepo = _unitOfWork.GetRepositoryByEntity<StudentMedication>();
            var medication = await medicationRepo.GetQueryable()
                .Include(sm => sm.Student)
                .Include(sm => sm.UsageHistory)
                .FirstOrDefaultAsync(sm => sm.Id == medicationId && !sm.IsDeleted, cancellationToken);

            if (medication == null)
                return BaseResponse<StudentMedicationUsageHistoryResponse>.ErrorResult("Không tìm thấy thuốc của học sinh.");

            if (medication.Status != StudentMedicationStatus.Active && medication.Status != StudentMedicationStatus.Approved)
                return BaseResponse<StudentMedicationUsageHistoryResponse>.ErrorResult("Chỉ có thể ghi nhận sử dụng cho thuốc đã được phê duyệt hoặc đang hoạt động.");

            if (medication.StartDate > DateTime.Now)
                return BaseResponse<StudentMedicationUsageHistoryResponse>.ErrorResult("Không thể ghi nhận sử dụng thuốc trước ngày bắt đầu.");

            if ((request.Status == StatusUsage.Skipped || request.Status == StatusUsage.Missed) && string.IsNullOrEmpty(request.Note))
                return BaseResponse<StudentMedicationUsageHistoryResponse>.ErrorResult("Ghi chú là bắt buộc khi thuốc không được sử dụng.");

            // Phân tích TimesOfDay
            List<string> timesOfDay = new List<string>();
            if (!string.IsNullOrEmpty(medication.TimesOfDay))
            {
                try
                {
                    timesOfDay = System.Text.Json.JsonSerializer.Deserialize<List<string>>(medication.TimesOfDay);
                    if (timesOfDay == null || timesOfDay.Any(t => string.IsNullOrEmpty(t)))
                    {
                        _logger.LogWarning("TimesOfDay không hợp lệ hoặc chứa giá trị rỗng: {TimesOfDay} cho thuốc {MedicationId}", medication.TimesOfDay, medicationId);
                        timesOfDay = new List<string> { "Morning" };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi phân tích TimesOfDay: {TimesOfDay} cho thuốc {MedicationId}", medication.TimesOfDay, medicationId);
                    timesOfDay = new List<string> { "Morning" };
                    _logger.LogInformation("Sử dụng TimesOfDay mặc định: [\"Morning\"] cho thuốc {MedicationId}", medicationId);
                }
            }
            else
            {
                _logger.LogInformation("TimesOfDay rỗng, sử dụng giá trị mặc định: [\"Morning\"] cho thuốc {MedicationId}", medicationId);
                timesOfDay = new List<string> { "Morning" };
            }

            // Tính số liều mỗi ngày
            int dosesPerDay = medication.FrequencyCount ?? 1;
            if (dosesPerDay <= 0)
                return BaseResponse<StudentMedicationUsageHistoryResponse>.ErrorResult("Số lần uống thuốc mỗi ngày không hợp lệ.");

            // Xử lý AdministeredTime và AdministeredPeriod
            DateTime? administeredTime = null;
            string? administeredPeriod = null;
            bool isMakeupDose = request.IsMakeupDose;
            if (request.AdministeredTime.HasValue) // Kiểm tra cho cả Used, Skipped, Missed
            {
                // Chuyển đổi sang múi giờ Việt Nam (+7)
                var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                administeredTime = TimeZoneInfo.ConvertTimeFromUtc(request.AdministeredTime.Value.ToUniversalTime(), vietnamTimeZone);

                // Xác định AdministeredPeriod
                administeredPeriod = GetAdministeredPeriod(administeredTime.Value);

                // Kiểm tra thời gian có nằm trong TimesOfDay (chỉ áp dụng cho Used)
                if (request.Status == StatusUsage.Used && timesOfDay.Any())
                {
                    bool isValidTime = false;
                    foreach (var timeOfDay in timesOfDay)
                    {
                        var (start, end) = GetTimeRange(timeOfDay);
                        if (administeredTime.Value.TimeOfDay >= start && administeredTime.Value.TimeOfDay <= end)
                        {
                            isValidTime = true;
                            break;
                        }
                    }
                    if (!isValidTime)
                    {
                        _logger.LogInformation("Giờ uống thuốc {AdministeredTime} không nằm trong khung giờ {TimesOfDay}, nhưng vẫn được ghi nhận.", administeredTime, string.Join(", ", timesOfDay));
                    }
                }
            }

            // Đếm số liều đã uống trong ngày (không bao gồm liều bù)
            var usageCountToday = medication.UsageHistory
                .Count(uh => uh.UsageDate.HasValue &&
                             uh.UsageDate.Value.Date == DateTime.Today &&
                             uh.Status == StatusUsage.Used &&
                             !uh.IsDeleted);

            // Kiểm tra giới hạn liều trong ngày (chỉ áp dụng với liều thường, không áp dụng với liều bù)
            if (request.Status == StatusUsage.Used && !isMakeupDose && usageCountToday >= dosesPerDay)
            {
                return BaseResponse<StudentMedicationUsageHistoryResponse>.ErrorResult(
                    $"Học sinh đã uống đủ {dosesPerDay} liều cho hôm nay rồi. " +
                    $"Vui lòng xác nhận liều bù bằng cách đặt isMakeupDose = true.");
            }

            // Tính toán liều lượng cho lần uống này
            int dosageQuantity = GetDosageQuantity(request.DosageUsed);
            int currentDosageQuantity = dosageQuantity == 0 ? 1 : dosageQuantity;
            int prescribedDosageQuantity = 1;

            if (request.Status == StatusUsage.Used)
            {
                // Validate prescribed dosage
                if (!string.IsNullOrEmpty(medication.Dosage))
                {
                    prescribedDosageQuantity = GetDosageQuantity(medication.Dosage);
                    if (prescribedDosageQuantity <= 0)
                    {
                        _logger.LogWarning("Liều lượng không hợp lệ trong StudentMedication: {Dosage} cho thuốc {MedicationId}", medication.Dosage, medicationId);
                        return BaseResponse<StudentMedicationUsageHistoryResponse>.ErrorResult("Liều lượng trong StudentMedication không hợp lệ.");
                    }
                }

                // Validate dosage used matches prescribed dosage, đặc biệt cho Bottle
                if (!string.IsNullOrEmpty(request.DosageUsed))
                {
                    if (currentDosageQuantity <= 0)
                        return BaseResponse<StudentMedicationUsageHistoryResponse>.ErrorResult("Liều lượng sử dụng không hợp lệ. Vui lòng nhập theo định dạng: 'số + khoảng trắng + đơn vị' (VD: '1 viên').");

                    if (medication.QuantityUnit == QuantityUnitEnum.Bottle)
                    {
                        // Đối với Bottle, DosageUsed phải khớp với Dosage đã nhập
                        if (request.DosageUsed != medication.Dosage)
                        {
                            return BaseResponse<StudentMedicationUsageHistoryResponse>.ErrorResult(
                                $"Liều lượng sử dụng ({request.DosageUsed}) phải khớp với liều lượng quy định: {medication.Dosage} cho thuốc dạng chai.");
                        }
                        // Không trừ QuantityReceive cho Bottle
                        currentDosageQuantity = prescribedDosageQuantity; // Đảm bảo khớp
                    }
                    else
                    {
                        // Đối với Tablet hoặc Pack, kiểm tra khớp với liều lượng quy định
                        if (currentDosageQuantity != prescribedDosageQuantity)
                            return BaseResponse<StudentMedicationUsageHistoryResponse>.ErrorResult($"Liều lượng sử dụng không hợp lệ. Phải khớp với liều lượng quy định: {medication.Dosage}.");
                    }
                }
                else
                {
                    // Nếu không cung cấp dosage, dùng dosage quy định
                    currentDosageQuantity = prescribedDosageQuantity;
                }

                int totalUsageCountToday = medication.UsageHistory
                .Count(uh => uh.UsageDate.HasValue &&
                             uh.UsageDate.Value.Date == DateTime.Today &&
                             uh.Status == StatusUsage.Used &&
                             !uh.IsDeleted);

                if (!isMakeupDose && totalUsageCountToday >= dosesPerDay)
                {
                    return BaseResponse<StudentMedicationUsageHistoryResponse>.ErrorResult(
                        $"Học sinh đã uống đủ {dosesPerDay} liều cho hôm nay rồi. " +
                        $"Vui lòng xác nhận liều bù bằng cách đặt isMakeupDose = true.");
                }

                // Kiểm tra giới hạn liều trong ngày cho Tablet/Pack (không áp dụng cho Bottle)
                if (medication.QuantityUnit != QuantityUnitEnum.Bottle)
                {
                    int maxDosagePerDay = medication.QuantitySent / medication.TotalDay;
                    int totalDosageUsedToday = medication.UsageHistory
                        .Where(uh => uh.UsageDate.HasValue &&
                                     uh.UsageDate.Value.Date == DateTime.Today &&
                                     uh.Status == StatusUsage.Used &&
                                     !uh.IsDeleted)
                        .Sum(uh => GetDosageQuantity(uh.DosageUse));

                    if (!isMakeupDose && totalDosageUsedToday + currentDosageQuantity > maxDosagePerDay)
                    {
                        return BaseResponse<StudentMedicationUsageHistoryResponse>.ErrorResult(
                            $"Không thể uống liều thường. Tổng số viên trong ngày sẽ vượt quá giới hạn " +
                            $"({totalDosageUsedToday + currentDosageQuantity}/{maxDosagePerDay} viên).");
                    }
                }
            }

            // Nếu là liều bù, ghi log hoặc xử lý đặc biệt
            if (isMakeupDose)
            {
                _logger.LogInformation("Học sinh uống liều bù. Tổng số liều hôm nay: {TotalDoses} cho thuốc {MedicationId}",
                    usageCountToday + 1, medicationId);
            }

            var schoolNurseRoleName = await GetSchoolNurseRoleName();

            var usageHistory = new StudentMedicationUsageHistory
            {
                Id = Guid.NewGuid(),
                StudentMedicationId = medicationId,
                StudentId = medication.StudentId,
                UsageDate = DateTime.Now,
                AdministeredTime = request.AdministeredTime.HasValue ? administeredTime : null, // Lưu AdministeredTime cho cả Used, Skipped, Missed
                AdministeredPeriod = request.AdministeredTime.HasValue ? administeredPeriod : null, // Lưu AdministeredPeriod cho cả Used, Skipped, Missed
                DosageUse = request.DosageUsed ?? medication.Dosage,
                Status = request.Status,
                Reason = isMakeupDose ? "Liều uống bù" : null,
                Note = request.Note,
                AdministeredBy = currentUserId,
                CreatedAt = DateTime.Now,
                CreatedBy = schoolNurseRoleName,
                CreatedDate = DateTime.Now
            };

            if (request.Status == StatusUsage.Used)
            {
                if (medication.QuantityUnit == QuantityUnitEnum.Tablet || medication.QuantityUnit == QuantityUnitEnum.Pack)
                {
                    if (medication.QuantityReceive >= currentDosageQuantity)
                    {
                        medication.QuantityReceive -= currentDosageQuantity;
                        medicationRepo.Update(medication);
                    }
                    else
                        return BaseResponse<StudentMedicationUsageHistoryResponse>.ErrorResult($"Không đủ thuốc trong kho. Yêu cầu: {currentDosageQuantity}, Còn lại: {medication.QuantityReceive}.");
                }
                // Không trừ QuantityReceive cho Bottle (đã kiểm tra DosageUse ở trên)

                if (medication.QuantityReceive == 0 && (medication.QuantityUnit == QuantityUnitEnum.Tablet || medication.QuantityUnit == QuantityUnitEnum.Pack))
                {
                    medication.Status = StudentMedicationStatus.Completed;
                    medication.LastUpdatedBy = schoolNurseRoleName;
                    medication.LastUpdatedDate = DateTime.Now;
                    medicationRepo.Update(medication);
                }
            }

            if (medication.QuantityReceive <= medication.MinStockThreshold && !medication.LowStockAlertSent && (medication.QuantityUnit == QuantityUnitEnum.Tablet || medication.QuantityUnit == QuantityUnitEnum.Pack))
            {
                await SendLowStockNotificationAsync(medication);
                medication.LowStockAlertSent = true;
                medicationRepo.Update(medication);
            }

            await _unitOfWork.GetRepositoryByEntity<StudentMedicationUsageHistory>().AddAsync(usageHistory);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _authService.InvalidateUserCacheAsync(medication.Student.Username, medication.Student.Email, medication.StudentId);

            var usageHistoryResponse = _mapper.Map<StudentMedicationUsageHistoryResponse>(usageHistory);
            usageHistoryResponse.QuantityReceive = medication.QuantityReceive;

            _logger.LogInformation("Ghi nhận sử dụng thuốc {MedicationId} với trạng thái {Status}", medicationId, request.Status);

            return BaseResponse<StudentMedicationUsageHistoryResponse>.SuccessResult(
                usageHistoryResponse, "Ghi nhận việc sử dụng thuốc thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi ghi nhận sử dụng thuốc {MedicationId}", medicationId);
            return BaseResponse<StudentMedicationUsageHistoryResponse>.ErrorResult($"Lỗi ghi nhận việc sử dụng thuốc: {ex.Message}");
        }
    }

    public async Task<BaseListResponse<MedicationStockResponse>> GetMedicationStockByIdForNurseAsync
    (
        Guid studentMedicationId,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BaseListResponse<MedicationStockResponse>.ErrorResult(
                    "Không thể xác định người dùng hiện tại.");
            }

            var currentUser = await ValidateSchoolNursePermissions(currentUserId);
            if (currentUser == null)
            {
                return BaseListResponse<MedicationStockResponse>.ErrorResult(
                    "Chỉ School Nurse mới có quyền xem lịch sử gửi thuốc này.");
            }

            var medication = await _unitOfWork.GetRepositoryByEntity<StudentMedication>()
                .GetQueryable()
                .FirstOrDefaultAsync(sm => sm.Id == studentMedicationId && !sm.IsDeleted);

            if (medication == null)
            {
                return BaseListResponse<MedicationStockResponse>.ErrorResult("Không tìm thấy thuốc.");
            }

            var cacheKey = _cacheService.GenerateCacheKey(
                MEDICATION_STOCK_PREFIX,
                "nurse_history",
                studentMedicationId.ToString(),
                pageIndex.ToString(),
                pageSize.ToString());

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<MedicationStockResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Cache hit for nurse medication stock history");
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<MedicationStock>()
                .GetQueryable()
                .Include(ms => ms.StudentMedication).ThenInclude(sm => sm.Student)
                .Include(ms => ms.StudentMedication).ThenInclude(sm => sm.Parent)
                .Where(ms => ms.StudentMedicationId == studentMedicationId && !ms.IsDeleted)
                .OrderByDescending(ms => ms.DateAdded);

            var totalCount = await query.CountAsync(cancellationToken);
            var stockRecords = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = _mapper.Map<List<MedicationStockResponse>>(stockRecords);

            var result = BaseListResponse<MedicationStockResponse>.SuccessResult(
                responses, totalCount, pageSize, pageIndex,
                "Lấy lịch sử gửi thuốc thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15));
            await _cacheService.AddToTrackingSetAsync(cacheKey, STOCK_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting medication stock history for nurse: {MedicationId}",
                studentMedicationId);
            return BaseListResponse<MedicationStockResponse>.ErrorResult("Lỗi lấy lịch sử gửi thuốc.");
        }
    }



    #endregion

    #region Helper Methods

    private int GetDosageQuantity(string dosageUse)
    {
        if (string.IsNullOrEmpty(dosageUse))
            return 1; // Default quantity

        var parts = dosageUse.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0 && int.TryParse(parts[0], out int quantity) && quantity > 0)
            return quantity;

        return 1; // Default quantity if parsing fails
    }

    private string GetAdministeredPeriod(DateTime administeredTime)
    {
        var hour = administeredTime.Hour;
        if (hour >= 6 && hour < 11) return "Buổi sáng";
        if (hour >= 11 && hour < 14) return "Buổi trưa";
        if (hour >= 14 && hour < 18) return "Buổi chiều";
        if (hour >= 18 && hour < 22) return "Buổi tối";
        return "Khuya";
    }

    private async Task<string> GenerateStudentMedicationCodeAsync()
    {
        var today = DateTime.Now.ToString("yyyyMMdd");
        var prefix = "SM-"; // Prefix cho Student Medication
        var repo = _unitOfWork.GetRepositoryByEntity<StudentMedicationRequest>();
        var lastCode = await repo.GetQueryable()
            .Where(m => m.Code != null && m.Code.StartsWith(prefix + today))
            .OrderByDescending(m => m.Code)
            .Select(m => m.Code)
            .FirstOrDefaultAsync();

        int sequenceNumber = 1;
        if (!string.IsNullOrEmpty(lastCode))
        {
            var numberPart = lastCode.Substring(lastCode.Length - 4); 
            if (int.TryParse(numberPart, out int lastNumber))
            {
                sequenceNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{today}-{sequenceNumber:D4}"; 
    }

    private (TimeSpan start, TimeSpan end) GetTimeRange(string timeOfDay)
    {
        if (string.IsNullOrEmpty(timeOfDay))
            return (TimeSpan.Zero, TimeSpan.Zero);

        return timeOfDay.ToLower() switch
        {
            "buổi sáng" => (TimeSpan.FromHours(7), TimeSpan.FromHours(11)),
            "buổi trưa" => (TimeSpan.FromHours(11).Add(TimeSpan.FromMinutes(1)), TimeSpan.FromHours(15)),
            "buổi chiều" => (TimeSpan.FromHours(11).Add(TimeSpan.FromMinutes(1)), TimeSpan.FromHours(15)),
            "buổi tối" => (TimeSpan.FromHours(15).Add(TimeSpan.FromMinutes(1)), TimeSpan.FromHours(19)),
            _ => (TimeSpan.Zero, TimeSpan.Zero) // Trường hợp không xác định
        };
    }

    private int CalculateTotalDosesFromDosage(string dosage, int quantitySent, int? frequencyCount)
    {
        try
        {
            if (string.IsNullOrEmpty(dosage) || quantitySent <= 0)
                return 0;

            // Giả sử dosage có định dạng như "1 viên" hoặc "200 mg"
            if (int.TryParse(dosage.Split(' ')[0], out var dosePerTime))
            {
                if (frequencyCount.HasValue && frequencyCount.Value > 0)
                {
                    // Tổng liều = Số lượng gửi / Liều mỗi lần * Số lần/ngày
                    return (quantitySent / dosePerTime) * frequencyCount.Value;
                }
                return quantitySent / dosePerTime;
            }
            return quantitySent; // Mặc định trả về quantitySent nếu không phân tích được dosage
        }
        catch
        {
            return quantitySent;
        }
    }

    private double ParseDosage(string dosage)
    {
        try
        {
            if (string.IsNullOrEmpty(dosage))
                return 0;

            var numericMatch = System.Text.RegularExpressions.Regex.Match(dosage, @"(\d+(?:\.\d+)?)");
            if (numericMatch.Success && double.TryParse(numericMatch.Value, out var dosageValue))
            {
                return dosageValue;
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing dosage '{Dosage}', returning 0", dosage);
            return 0;
        }
    }

    private async Task SendLowStockNotificationAsync(StudentMedication medication)
    {
        try
        {
            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = $"Cảnh báo số lượng thuốc thấp - {medication.Student?.FullName}",
                Content = $"Thuốc '{medication.MedicationName}' cho học sinh {medication.Student?.FullName} " +
                          $"({medication.Student?.StudentCode}) còn lại {medication.QuantityReceive} {medication.QuantityUnit}. " +
                          $"Vui lòng bổ sung thêm thuốc.",
                NotificationType = NotificationType.General,
                SenderId = null,
                RecipientId = medication.ParentId,
                RequiresConfirmation = false,
                IsRead = false,
                CreatedDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(7)
            };

            await notificationRepo.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Sent low stock notification for medication {MedicationId}", medication.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending low stock notification for medication {MedicationId}", medication.Id);
        }
    }

    private async Task<string> GenerateStudentMedicationRequestCodeAsync()
    {
        try
        {
            _logger.LogDebug("Generating StudentMedicationRequest code");
            var lastRequest = await _unitOfWork.GetRepositoryByEntity<StudentMedicationRequest>()
                .GetQueryable()
                .OrderByDescending(r => r.CreatedDate)
                .FirstOrDefaultAsync();

            if (lastRequest == null)
            {
                _logger.LogDebug("No existing request found, returning default code REQ-000001");
                return "REQ-000001"; // Mã mặc định cho request đầu tiên
            }

            var lastCodeNumber = int.Parse(lastRequest.Code.Split('-')[1]);
            var newCode = $"REQ-{lastCodeNumber + 1:D6}";
            _logger.LogDebug("Generated new code: {NewCode}", newCode);
            return newCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating StudentMedicationRequest code");
            throw; // Ném lại exception để xử lý ở cấp cao hơn
        }
    }

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

    private async Task CreateInitialStockRecordAsync(StudentMedication medication)
    {
        try
        {
            var stockRepo = _unitOfWork.GetRepositoryByEntity<MedicationStock>();
            var initialStock = new MedicationStock
            {
                Id = Guid.NewGuid(),
                StudentMedicationId = medication.Id,
                QuantityAdded = medication.QuantitySent,
                QuantityUnit = medication.QuantityUnit ?? QuantityUnitEnum.Tablet, // Giá trị mặc định nếu null
                ExpiryDate = medication.ExpiryDate,
                DateAdded = DateTime.Now,
                Notes = "Thuốc ban đầu từ phụ huynh",
                IsInitialStock = true,
                CreatedBy = medication.CreatedBy,
                CreatedDate = DateTime.Now
            };

            await stockRepo.AddAsync(initialStock);
            
            // Tính toán TotalDoses ngay khi tạo stock record đầu tiên
            var totalDoses = CalculateTotalDosesFromDosage(medication.Dosage, medication.QuantitySent);
            medication.TotalDoses = totalDoses;
            medication.RemainingDoses = totalDoses;
            
            _logger.LogInformation("Created initial stock record for medication {MedicationId}: QuantitySent={QuantitySent}, TotalDoses={TotalDoses}",
                medication.Id, medication.QuantitySent, totalDoses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating initial stock record for medication {MedicationId}", medication.Id);
        }
    }

    private async Task UpdateStockRecordAsync(StudentMedication medication, int newQuantity)
    {
        var latestStock = medication.StockHistory?
            .OrderByDescending(s => s.DateAdded)
            .FirstOrDefault();

        if (latestStock != null && latestStock.IsInitialStock)
        {
            latestStock.QuantityAdded = newQuantity;
            latestStock.Notes = $"Cập nhật số lượng: {newQuantity} {medication.QuantityUnit}";
            latestStock.LastUpdatedBy = "PARENT";
            latestStock.LastUpdatedDate = DateTime.Now;
        }
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
    private IQueryable<StudentMedicationRequest> ApplyStudentMedicationRequestFilters(
    IQueryable<StudentMedicationRequest> query,
    Guid? studentId,
    Guid? parentId,
    StudentMedicationStatus? status)
    {
        if (studentId.HasValue)
        {
            query = query.Where(r => r.StudentId == studentId.Value);
        }

        if (parentId.HasValue)
        {
            query = query.Where(r => r.ParentId == parentId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return query.OrderByDescending(r => r.SubmittedAt ?? r.CreatedDate);
    }

    private IQueryable<StudentMedication> ApplyStudentMedicationFilters(
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

    private IQueryable<StudentMedication> ApplyStudentMedicationOrdering(IQueryable<StudentMedication> query,
        string orderBy)
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
            "priority" => query.OrderBy(sm => sm.Priority),
            "priority_desc" => query.OrderByDescending(sm => sm.Priority),
            _ => query.OrderByDescending(sm => sm.SubmittedAt ?? sm.CreatedDate)
        };
    }

    private IQueryable<MedicationStock> ApplyMedicationStockFilters(
        IQueryable<MedicationStock> query,
        Guid? studentId,
        Guid? parentId,
        Guid? medicationId,
        DateTime? fromDate,
        DateTime? toDate,
        bool? isExpired,
        bool? lowStock)
    {
        if (studentId.HasValue)
            query = query.Where(ms => ms.StudentMedication.StudentId == studentId.Value);

        if (parentId.HasValue)
            query = query.Where(ms => ms.StudentMedication.ParentId == parentId.Value);

        if (medicationId.HasValue)
            query = query.Where(ms => ms.StudentMedicationId == medicationId.Value);

        if (fromDate.HasValue)
            query = query.Where(ms => ms.DateAdded >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(ms => ms.DateAdded <= toDate.Value.AddDays(1));

        if (isExpired.HasValue)
        {
            if (isExpired.Value)
                query = query.Where(ms => ms.ExpiryDate < DateTime.Today);
            else
                query = query.Where(ms => ms.ExpiryDate >= DateTime.Today);
        }

        if (lowStock.HasValue && lowStock.Value)
        {
            query = query.Where(ms => ms.StudentMedication.RemainingDoses <= ms.StudentMedication.MinStockThreshold);
        }

        return query;
    }

    private async Task InvalidateAllCachesAsync()
    {
        try
        {
            _logger.LogDebug("Starting cache invalidation for medication requests, users, students, parents, and related data");
            // Xóa toàn bộ tracking set
            await Task.WhenAll(
                _cacheService.InvalidateTrackingSetAsync(MEDICATION_CACHE_SET),
                _cacheService.InvalidateTrackingSetAsync(USER_PROFILE_CACHE_SET),
                _cacheService.InvalidateTrackingSetAsync(STUDENT_LIST_CACHE_SET),
                _cacheService.InvalidateTrackingSetAsync(PARENT_LIST_CACHE_SET),
                _cacheService.InvalidateTrackingSetAsync(CLASS_ENROLLMENT_CACHE_SET),
                _cacheService.InvalidateTrackingSetAsync("vaccination_session_cache_keys"),
                _cacheService.InvalidateTrackingSetAsync("nurse_assignment_status_cache_keys"),
                _cacheService.InvalidateTrackingSetAsync("student_session_cache_keys"),
                _cacheService.InvalidateTrackingSetAsync("session_detail_cache_keys"),
                _cacheService.InvalidateTrackingSetAsync("parent_consent_status_cache_keys"),
                _cacheService.InvalidateTrackingSetAsync("student_vaccination_result_cache_keys")
            );

            // Xóa cache theo prefix
            await Task.WhenAll(
                _cacheService.RemoveByPrefixAsync(MEDICATION_REQUEST_PREFIX),
                _cacheService.RemoveByPrefixAsync(USER_PROFILE_PREFIX),
                _cacheService.RemoveByPrefixAsync(STUDENT_LIST_PREFIX),
                _cacheService.RemoveByPrefixAsync(PARENT_LIST_PREFIX),
                _cacheService.RemoveByPrefixAsync(CLASS_ENROLLMENT_PREFIX),
                _cacheService.RemoveByPrefixAsync("vaccination_session"),
                _cacheService.RemoveByPrefixAsync("vaccination_sessions_list"),
                _cacheService.RemoveByPrefixAsync("nurse_assignment_status"),
                _cacheService.RemoveByPrefixAsync("student_sessions"),
                _cacheService.RemoveByPrefixAsync("session_detail"),
                _cacheService.RemoveByPrefixAsync("parent_consent_status"),
                _cacheService.RemoveByPrefixAsync("student_vaccination_result")
            );
            _logger.LogDebug("Completed cache invalidation for medication requests, users, students, parents, and related data");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in cache invalidation for medication requests, users, students, parents, and related data");
        }
    }

    private async Task<bool> ValidateRequestAccessPermissionAsync(Guid currentUserId, Guid requestId)
    {
        try
        {
            var currentUser = await GetCurrentUserWithRoles();
            if (currentUser == null) return false;

            var userRoles = currentUser.UserRoles.Select(ur => ur.Role.Name.ToUpper()).ToList();

            if (userRoles.Any(role => new[] { "SCHOOLNURSE" }.Contains(role)))
            {
                return true;
            }

            var request = await _unitOfWork.GetRepositoryByEntity<StudentMedicationRequest>()
                .GetQueryable()
                .Where(r => r.Id == requestId && !r.IsDeleted)
                .Select(r => new { r.ParentId, r.StudentId })
                .FirstOrDefaultAsync();

            if (request == null) return false;

            if (userRoles.Contains("PARENT"))
            {
                return request.ParentId == currentUserId;
            }

            if (userRoles.Contains("STUDENT"))
            {
                return request.StudentId == currentUserId;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating request access permission");
            return false;
        }
    }

    private async Task<bool> ValidateUserAccessToRequestAsync(ApplicationUser currentUser, StudentMedicationRequest request)
    {
        try
        {
            var userRoles = currentUser.UserRoles.Select(ur => ur.Role.Name.ToUpper()).ToList();

            if (userRoles.Any(role => new[] { "SCHOOLNURSE" }.Contains(role)))
            {
                return true;
            }

            if (userRoles.Contains("PARENT"))
            {
                return request.ParentId == currentUser.Id;
            }

            if (userRoles.Contains("STUDENT"))
            {
                return request.StudentId == currentUser.Id;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user access to request");
            return false;
        }
    }

    private async Task<bool> ValidateUserAccessToMedicationAsync(ApplicationUser currentUser,
        StudentMedication medication)
    {
        try
        {
            var userRoles = currentUser.UserRoles.Select(ur => ur.Role.Name.ToUpper()).ToList();

            if (userRoles.Any(role => new[] { "SCHOOLNURSE" }.Contains(role)))
            {
                return true;
            }

            // PARENT chỉ có thể xem thuốc của con em mình
            if (userRoles.Contains("PARENT"))
            {
                return medication.ParentId == currentUser.Id;
            }

            // STUDENT chỉ có thể xem thuốc của chính mình
            if (userRoles.Contains("STUDENT"))
            {
                return medication.StudentId == currentUser.Id;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user access to medication");
            return false;
        }
    }

    private async Task<bool> ValidateAccessPermissionAsync(Guid currentUserId, Guid medicationId)
    {
        try
        {
            var currentUser = await GetCurrentUserWithRoles();
            if (currentUser == null) return false;

            var userRoles = currentUser.UserRoles.Select(ur => ur.Role.Name.ToUpper()).ToList();

            if (userRoles.Any(role => new[] { "SCHOOLNURSE" }.Contains(role)))
            {
                return true;
            }

            var medication = await _unitOfWork.GetRepositoryByEntity<StudentMedication>()
                .GetQueryable()
                .Where(sm => sm.Id == medicationId && !sm.IsDeleted)
                .Select(sm => new { sm.ParentId, sm.StudentId })
                .FirstOrDefaultAsync();

            if (medication == null) return false;

            if (userRoles.Contains("PARENT"))
            {
                return medication.ParentId == currentUserId;
            }

            if (userRoles.Contains("STUDENT"))
            {
                return medication.StudentId == currentUserId;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating access permission");
            return false;
        }
    }

    private async Task<ApplicationUser> ValidateStudentAsync(Guid studentId)
    {
        var studentRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
        var student = await studentRepo.GetQueryable()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == studentId && !u.IsDeleted);

        if (student == null || !student.UserRoles.Any(ur => ur.Role.Name == "STUDENT"))
        {
            return null;
        }

        return student;
    }

    private async Task<(bool IsValid, string ErrorMessage, string ParentRoleName)> ValidateParentPermissionsAsync(
        Guid currentUserId, Guid studentId)
    {
        var currentUser = await GetCurrentUserWithRoles();
        var parentRoleName = await GetParentRoleName();

        if (currentUser != null && currentUser.UserRoles.Any(ur => ur.Role.Name == "PARENT"))
        {
            var student = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>()
                .GetQueryable()
                .FirstOrDefaultAsync(u => u.Id == studentId && !u.IsDeleted);

            if (student?.ParentId != currentUserId)
            {
                return (false, "Bạn chỉ có thể gửi thuốc cho con em mình.", parentRoleName);
            }
        }

        return (true, string.Empty, parentRoleName);
    }

    private void SetAdditionalMedicationProperties(StudentMedication medication, Guid currentUserId,
        string parentRoleName)
    {
        medication.Id = Guid.NewGuid();
        medication.ParentId = currentUserId;
        // TotalDoses và RemainingDoses sẽ được tính toán sau khi tạo stock record
        medication.MinStockThreshold = 3;
        medication.LowStockAlertSent = false;
        medication.SkipOnAbsence = true;
        medication.AutoGenerateSchedule = true;
        medication.RequireNurseConfirmation = false;
        medication.Status = StudentMedicationStatus.PendingApproval;
        medication.SubmittedAt = DateTime.Now;
        medication.CreatedBy = parentRoleName;
        medication.CreatedDate = DateTime.Now;
    }

    #endregion

    #region Notification Methods

    private async Task CreateMedicationRequestNotificationAsync(StudentMedication medication)
    {
        try
        {
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

    private async Task CreateBulkMedicationRequestNotificationAsync(List<StudentMedication> medications)
    {
        try
        {
            var nurses = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>()
                .GetQueryable()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "SCHOOLNURSE") &&
                            u.IsActive && !u.IsDeleted)
                .ToListAsync();

            if (!nurses.Any() || !medications.Any()) return;

            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();
            var notifications = new List<Notification>();

            foreach (var nurse in nurses)
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = $"Yêu cầu thuốc hàng loạt mới - {medications[0].Student?.FullName}",
                    Content = $"Phụ huynh đã gửi {medications.Count} yêu cầu thuốc cho học sinh " +
                              $"{medications[0].Student?.FullName} ({medications[0].Student?.StudentCode}). " +
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

            _logger.LogInformation("Created {Count} bulk medication request notifications", notifications.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bulk medication request notifications");
        }
    }

    private async Task CreateApprovalNotificationAsync(StudentMedication medication, bool isApproved, string notes)
    {
        try
        {
            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();

            var title = isApproved
                ? $"Thuốc được phê duyệt - {medication.Student?.FullName}"
                : $"Thuốc bị từ chối - {medication.Student?.FullName}";

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

    private async Task CreateApprovalNotificationRequestAsync(StudentMedicationRequest medicationRequest, bool isApproved, string notes)
    {
        try
        {
            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();

            var title = isApproved
                ? $"Yêu cầu thuốc được phê duyệt - {medicationRequest.StudentName}"
                : $"Yêu cầu thuốc bị từ chối - {medicationRequest.StudentName}";

            // Tạo nội dung thông báo dựa trên danh sách MedicationsDetails
            var content = isApproved
                ? $"Yêu cầu thuốc cho con em {medicationRequest.StudentName} ({medicationRequest.StudentCode}) đã được phê duyệt. " +
                  $"Danh sách thuốc: {string.Join(", ", medicationRequest.MedicationsDetails.Select(m => m.MedicationName))}. " +
                  $"Nhà trường sẽ cho con em uống thuốc theo đúng hướng dẫn từ {medicationRequest.MedicationsDetails.Min(m => m.StartDate)?.ToString("dd/MM/yyyy") ?? "N/A"} " +
                  $"đến {medicationRequest.MedicationsDetails.Max(m => m.EndDate)?.ToString("dd/MM/yyyy") ?? "N/A"}."
                : $"Yêu cầu thuốc cho con em {medicationRequest.StudentName} ({medicationRequest.StudentCode}) đã bị từ chối. " +
                  $"Lý do: {medicationRequest.RejectionReason ?? "Không có lý do cụ thể"}";

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
                SenderId = medicationRequest.ApprovedById,
                RecipientId = medicationRequest.ParentId,
                RequiresConfirmation = false,
                IsRead = false,
                CreatedDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(30)
            };

            await notificationRepo.AddAsync(notification);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Created approval notification for medication request {RequestId}", medicationRequest.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating approval notification for request {RequestId}", medicationRequest.Id);
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

    private async Task CreateAdditionalMedicationNotificationAsync(StudentMedication medication, int additionalQuantity)
    {
        try
        {
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
                    Title = $"Phụ huynh gửi thêm thuốc - {medication.Student?.FullName}",
                    Content = $"Phụ huynh đã gửi thêm {additionalQuantity} {medication.QuantityUnit} " +
                              $"thuốc '{medication.MedicationName}' cho học sinh {medication.Student?.FullName} " +
                              $"({medication.Student?.StudentCode}). " +
                              $"Tổng số lượng hiện tại: {medication.QuantitySent} {medication.QuantityUnit}.",
                    NotificationType = NotificationType.General,
                    SenderId = medication.ParentId,
                    RecipientId = nurse.Id,
                    RequiresConfirmation = false,
                    IsRead = false,
                    CreatedDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(3)
                };

                notifications.Add(notification);
            }

            await notificationRepo.AddRangeAsync(notifications);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Created {Count} additional medication notifications", notifications.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating additional medication notifications");
        }
    }

    #endregion

    #region Student Medication Usage History

    public async Task<BaseListResponse<StudentMedicationUsageHistoryResponse>> GetStudentMedicationUsageHistoryAsync(
        Guid studentId,
        int pageIndex,
        int pageSize,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        StatusUsage? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BaseListResponse<StudentMedicationUsageHistoryResponse>.ErrorResult(
                    "Không thể xác định người dùng hiện tại.");
            }

            // Validate student exists
            var student = await ValidateStudentAsync(studentId);
            if (student == null)
            {
                return BaseListResponse<StudentMedicationUsageHistoryResponse>.ErrorResult(
                    "Không tìm thấy học sinh hoặc người dùng không phải là học sinh.");
            }

            // Validate access permission
            var currentUser = await GetCurrentUserWithRoles();
            if (currentUser == null)
            {
                return BaseListResponse<StudentMedicationUsageHistoryResponse>.ErrorResult(
                    "Không thể xác định quyền của người dùng.");
            }

            var userRoles = currentUser.UserRoles.Select(ur => ur.Role.Name.ToUpper()).ToList();
            if (!userRoles.Any(role => new[] { "SCHOOLNURSE", "ADMIN", "MANAGER" }.Contains(role)) &&
                currentUser.Id != studentId && currentUser.Id != student.ParentId)
            {
                return BaseListResponse<StudentMedicationUsageHistoryResponse>.ErrorResult(
                    "Bạn không có quyền xem lịch sử sử dụng thuốc của học sinh này.");
            }

            // Generate cache key
            var cacheKey = _cacheService.GenerateCacheKey(
                MEDICATION_LIST_PREFIX,
                "student_usage_history",
                studentId.ToString(),
                pageIndex.ToString(),
                pageSize.ToString(),
                fromDate?.ToString("yyyy-MM-dd") ?? "",
                toDate?.ToString("yyyy-MM-dd") ?? "",
                status?.ToString() ?? "");

            // Check cache
            var cachedResult = await _cacheService.GetAsync<BaseListResponse<StudentMedicationUsageHistoryResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Cache hit for student medication usage history: {StudentId}", studentId);
                return cachedResult;
            }

            // Build query
            var query = _unitOfWork.GetRepositoryByEntity<StudentMedicationUsageHistory>()
                .GetQueryable()
                .Include(uh => uh.StudentMedication)
                    .ThenInclude(sm => sm.Student)
                .Include(uh => uh.Nurse!) // Sửa từ AdministeredBy thành Nurse
                .Where(uh => uh.StudentId == studentId && !uh.IsDeleted);

            // Apply filters
            if (fromDate.HasValue)
            {
                query = query.Where(uh => uh.UsageDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(uh => uh.UsageDate <= toDate.Value.AddDays(1));
            }

            if (status.HasValue)
            {
                query = query.Where(uh => uh.Status == status.Value);
            }

            // Order by usage date descending
            query = query.OrderByDescending(uh => uh.UsageDate);

            // Get total count and paginated results
            var totalCount = await query.CountAsync(cancellationToken);
            var usageHistory = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            // Map to response model
            var responses = _mapper.Map<List<StudentMedicationUsageHistoryResponse>>(usageHistory);

            // Create success response
            var result = BaseListResponse<StudentMedicationUsageHistoryResponse>.SuccessResult(
                responses, totalCount, pageSize, pageIndex,
                "Lấy lịch sử sử dụng thuốc của học sinh thành công.");

            // Cache the result
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICATION_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medication usage history for student {StudentId}", studentId);
            return BaseListResponse<StudentMedicationUsageHistoryResponse>.ErrorResult(
                "Lỗi lấy lịch sử sử dụng thuốc của học sinh.");
        }
    }

    public async Task<BaseListResponse<StudentMedicationUsageHistoryResponse>> GetMedicationUsageHistoryAsync(
    Guid studentMedicationId,
    int pageIndex,
    int pageSize,
    DateTime? fromDate = null,
    DateTime? toDate = null,
    StatusUsage? status = null,
    CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return BaseListResponse<StudentMedicationUsageHistoryResponse>.ErrorResult(
                    "Không thể xác định người dùng hiện tại.");
            }

            // Validate medication exists
            var medication = await _unitOfWork.GetRepositoryByEntity<StudentMedication>()
                .GetQueryable()
                .Include(sm => sm.Student)
                    .ThenInclude(s => s.StudentClasses)
                        .ThenInclude(sc => sc.SchoolClass)
                .Where(sm => sm.Id == studentMedicationId && !sm.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (medication == null)
            {
                return BaseListResponse<StudentMedicationUsageHistoryResponse>.ErrorResult(
                    "Không tìm thấy thuốc học sinh.");
            }

            // Validate access permission
            var currentUser = await GetCurrentUserWithRoles();
            if (currentUser == null)
            {
                return BaseListResponse<StudentMedicationUsageHistoryResponse>.ErrorResult(
                    "Không thể xác định quyền của người dùng.");
            }

            var userRoles = currentUser.UserRoles.Select(ur => ur.Role.Name.ToUpper()).ToList();
            if (!userRoles.Any(role => new[] { "SCHOOLNURSE", "ADMIN", "MANAGER" }.Contains(role)) &&
                currentUser.Id != medication.StudentId && currentUser.Id != medication.ParentId)
            {
                return BaseListResponse<StudentMedicationUsageHistoryResponse>.ErrorResult(
                    "Bạn không có quyền xem lịch sử sử dụng thuốc này.");
            }

            // Generate cache key
            var cacheKey = _cacheService.GenerateCacheKey(
                MEDICATION_LIST_PREFIX,
                "medication_usage_history",
                studentMedicationId.ToString(),
                pageIndex.ToString(),
                pageSize.ToString(),
                fromDate?.ToString("yyyy-MM-dd") ?? "",
                toDate?.ToString("yyyy-MM-dd") ?? "",
                status?.ToString() ?? "");

            // Check cache
            var cachedResult = await _cacheService.GetAsync<BaseListResponse<StudentMedicationUsageHistoryResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Cache hit for medication usage history: {StudentMedicationId}", studentMedicationId);
                return cachedResult;
            }

            // Build query
            var query = _unitOfWork.GetRepositoryByEntity<StudentMedicationUsageHistory>()
                .GetQueryable()
                .Include(uh => uh.StudentMedication)
                    .ThenInclude(sm => sm.Student)
                        .ThenInclude(s => s.StudentClasses)
                            .ThenInclude(sc => sc.SchoolClass)
                .Include(uh => uh.Nurse)
                .Where(uh => uh.StudentMedicationId == studentMedicationId && !uh.IsDeleted);

            // Apply filters
            if (fromDate.HasValue)
            {
                query = query.Where(uh => uh.UsageDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(uh => uh.UsageDate <= toDate.Value.AddDays(1));
            }

            if (status.HasValue)
            {
                query = query.Where(uh => uh.Status == status.Value);
            }

            // Order by usage date descending
            query = query.OrderByDescending(uh => uh.UsageDate);

            // Get total count and paginated results
            var totalCount = await query.CountAsync(cancellationToken);
            var usageHistory = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            // Map to response model
            var responses = _mapper.Map<List<StudentMedicationUsageHistoryResponse>>(usageHistory);

            foreach (var response in responses)
            {
                var history = usageHistory.FirstOrDefault(uh => uh.Id == response.Id);
                if (history?.StudentMedication?.Student != null)
                {
                    response.StudentName = history.StudentMedication.Student.FullName;
                    response.StudentCode = history.StudentMedication.Student.StudentCode;
                    // Lấy ClassName từ SchoolClass thông qua StudentClasses
                    response.ClassName = history.StudentMedication.Student.StudentClasses
                        .FirstOrDefault()?.SchoolClass?.Name ?? "";
                    response.MedicationName = history.StudentMedication.MedicationName;
                    response.DosageUse = history.DosageUse;
                    response.UsageDate = history.UsageDate;
                    response.AdministeredTime = history.AdministeredTime;
                    response.AdministeredPeriod = history.AdministeredPeriod;
                    response.Status = history.Status;
                    response.StatusDisplayName = history.Status.ToString();
                    response.AdministeredByName = history.Nurse != null ? history.Nurse.FullName : "";
                    response.Note = history.Note;
                    response.QuantityReceive = history.StudentMedication.QuantityReceive;
                }
            }

            // Create success response
            var result = BaseListResponse<StudentMedicationUsageHistoryResponse>.SuccessResult(
                responses, totalCount, pageSize, pageIndex,
                "Lấy lịch sử sử dụng thuốc thành công.");

            // Cache the result
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            await _cacheService.AddToTrackingSetAsync(cacheKey, MEDICATION_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medication usage history for medication {StudentMedicationId}", studentMedicationId);
            return BaseListResponse<StudentMedicationUsageHistoryResponse>.ErrorResult(
                "Lỗi lấy lịch sử sử dụng thuốc.");
        }
    }

    #endregion


    private async Task CalculateAndUpdateMedicationDosesAsync(StudentMedication medication)
    {
        try
        {
            // Tính tổng số lượng thuốc đã gửi từ StockHistory
            var totalQuantitySent = medication.StockHistory?
                .Where(s => !s.IsDeleted)
                .Sum(s => s.QuantityAdded) ?? 0;

            if (totalQuantitySent == 0)
            {
                // Nếu chưa có stock history, sử dụng QuantitySent từ medication
                totalQuantitySent = medication.QuantitySent;
            }

            // Tính toán TotalDoses dựa trên dosage và quantity
            var totalDoses = CalculateTotalDosesFromDosage(medication.Dosage, totalQuantitySent);
            
            // Cập nhật TotalDoses và RemainingDoses
            medication.TotalDoses = totalDoses;
            medication.RemainingDoses = totalDoses;
            
            _logger.LogInformation("Calculated doses for medication {MedicationId}: QuantitySent={QuantitySent}, Dosage={Dosage}, TotalDoses={TotalDoses}",
                medication.Id, totalQuantitySent, medication.Dosage, totalDoses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating medication doses for {MedicationId}", medication.Id);
            // Fallback: set default values
            medication.TotalDoses = medication.QuantitySent;
            medication.RemainingDoses = medication.QuantitySent;
        }
    }

    private int CalculateTotalDosesFromDosage(string dosage, int quantity)
    {
        try
        {
            if (string.IsNullOrEmpty(dosage) || quantity <= 0)
                return quantity;

            // Parse dosage string to extract numeric value
            var dosageLower = dosage.ToLower().Trim();
            
            // Handle common dosage patterns
            if (dosageLower.Contains("1 viên") || dosageLower.Contains("1 tablet") || dosageLower.Contains("1 pill"))
                return quantity;
            
            if (dosageLower.Contains("2 viên") || dosageLower.Contains("2 tablet") || dosageLower.Contains("2 pill"))
                return quantity / 2;
            
            if (dosageLower.Contains("1/2 viên") || dosageLower.Contains("0.5 viên") || dosageLower.Contains("half"))
                return quantity * 2;
            
            if (dosageLower.Contains("1.5 viên") || dosageLower.Contains("1.5 tablet"))
                return (int)(quantity / 1.5);
            
            // Extract numeric value using regex
            var numericMatch = System.Text.RegularExpressions.Regex.Match(dosage, @"(\d+(?:\.\d+)?)");
            if (numericMatch.Success && double.TryParse(numericMatch.Value, out var dosageValue))
            {
                if (dosageValue > 0)
                    return (int)(quantity / dosageValue);
            }
            
            // Default: assume 1 unit per dose
            return quantity;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing dosage '{Dosage}', using default calculation", dosage);
            return quantity;
        }
    }

    private async Task RecalculateMedicationDosesAsync(StudentMedication medication)
    {
        try
        {
            // Tính tổng số lượng thuốc đã gửi từ StockHistory
            var totalQuantitySent = medication.StockHistory?
                .Where(s => !s.IsDeleted)
                .Sum(s => s.QuantityAdded) ?? 0;

            if (totalQuantitySent == 0)
            {
                totalQuantitySent = medication.QuantitySent;
            }

            // Tính toán TotalDoses mới
            var newTotalDoses = CalculateTotalDosesFromDosage(medication.Dosage, totalQuantitySent);
            
            // Tính số liều đã sử dụng
            var usedDoses = medication.TotalDoses - medication.RemainingDoses;
            
            // Cập nhật TotalDoses và RemainingDoses
            medication.TotalDoses = newTotalDoses;
            medication.RemainingDoses = Math.Max(0, newTotalDoses - usedDoses);
            
            _logger.LogInformation("Recalculated doses for medication {MedicationId}: New TotalDoses={TotalDoses}, New RemainingDoses={RemainingDoses}, UsedDoses={UsedDoses}",
                medication.Id, newTotalDoses, medication.RemainingDoses, usedDoses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating medication doses for {MedicationId}", medication.Id);
        }
    }
}