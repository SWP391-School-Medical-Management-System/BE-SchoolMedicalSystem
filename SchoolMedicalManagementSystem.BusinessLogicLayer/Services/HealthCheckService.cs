using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthCheckRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthCheckResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccinationSessionResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthCheckResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthCheckItemResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalRecordResponse;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using HealthCheckResult = SchoolMedicalManagementSystem.DataAccessLayer.Entities.HealthCheckResult;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services
{
    public class HealthCheckService : IHealthCheckService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly ILogger<HealthCheckService> _logger;
        private readonly IValidator<CreateWholeHealthCheckRequest> _createWholeHealthCheckValidator;
        private readonly IValidator<UpdateHealthCheckRequest> _updateHealthCheckValidator;
        private readonly IValidator<ParentApproveHealthCheckRequest> _parentApproveValidator;
        private readonly IValidator<AssignNurseToHealthCheckRequest> _assignNurseValidator;
        private readonly IValidator<ReAssignNurseToHealthCheckRequest> _reAssignNurseValidator;
        private readonly IValidator<SaveVisionCheckRequest> _saveVisionCheckValidator;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private const string HEALTHCHECK_CACHE_PREFIX = "healthcheck_session";
        private const string HEALTHCHECK_LIST_PREFIX = "healthcheck_sessions_list";
        private const string HEALTHCHECK_CACHE_SET = "healthcheck_session_cache_keys";

        public HealthCheckService(
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ILogger<HealthCheckService> logger,
            IValidator<CreateWholeHealthCheckRequest> createWholeHealthCheckValidator,
            IValidator<UpdateHealthCheckRequest> updateHealthCheckValidator,
            IValidator<ParentApproveHealthCheckRequest> parentApproveValidator,
            IValidator<AssignNurseToHealthCheckRequest> assignNurseValidator,
            IValidator<ReAssignNurseToHealthCheckRequest> reAssignNurseValidator,
            IValidator<SaveVisionCheckRequest> saveVisionCheckValidator,
            IEmailService emailService,
            IHttpContextAccessor httpContextAccessor)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _logger = logger;
            _createWholeHealthCheckValidator = createWholeHealthCheckValidator;
            _updateHealthCheckValidator = updateHealthCheckValidator;
            _parentApproveValidator = parentApproveValidator;
            _assignNurseValidator = assignNurseValidator;
            _reAssignNurseValidator = reAssignNurseValidator;
            _emailService = emailService;
            _saveVisionCheckValidator = saveVisionCheckValidator;
            _httpContextAccessor = httpContextAccessor;
        }

        #region CRUD HealthCheck

        public async Task<BaseListResponse<HealthCheckResponse>> GetHealthChecksAsync(
         int pageIndex, int pageSize, string searchTerm, string orderBy, Guid? nurseId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = _cacheService.GenerateCacheKey(HEALTHCHECK_LIST_PREFIX, pageIndex.ToString(), pageSize.ToString(), searchTerm ?? "", orderBy ?? "", nurseId?.ToString() ?? "all");
                var cachedResult = await _cacheService.GetAsync<BaseListResponse<HealthCheckResponse>>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogDebug("Danh sách buổi khám được tìm thấy trong cache.");
                    return cachedResult;
                }

                var query = _unitOfWork.GetRepositoryByEntity<HealthCheck>().GetQueryable()
                    .Include(hc => hc.HealthCheckClasses)
                        .ThenInclude(hcc => hcc.SchoolClass)
                    .Include(hc => hc.HealthCheckAssignments)
                    .Include(hc => hc.HealthCheckConsents)
                    .Where(hc => !hc.IsDeleted)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(hc => hc.Title.ToLower().Contains(searchTerm) || hc.Location.ToLower().Contains(searchTerm));
                }

                if (nurseId.HasValue)
                {
                    query = query.Where(hc => hc.HealthCheckAssignments.Any(a => a.NurseId == nurseId.Value));
                }

                query = ApplyHealthCheckOrdering(query, orderBy);
                var totalCount = await query.CountAsync(cancellationToken);
                var healthChecks = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

                var responses = _mapper.Map<List<HealthCheckResponse>>(healthChecks);
                foreach (var response in responses)
                {
                    var hc = healthChecks.FirstOrDefault(h => h.Id == response.Id);
                    if (hc != null)
                    {
                        response.ApprovedStudents = hc.HealthCheckConsents.Count(hcs => hcs.Status == "Confirmed" && !hcs.IsDeleted);
                        response.TotalStudents = hc.HealthCheckConsents.Count(hcs => !hcs.IsDeleted);
                    }
                }

                var result = BaseListResponse<HealthCheckResponse>.SuccessResult(responses, totalCount, pageSize, pageIndex, "Lấy danh sách buổi khám thành công.");
                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
                await _cacheService.AddToTrackingSetAsync(cacheKey, HEALTHCHECK_CACHE_SET);
                await InvalidateAllCachesAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách buổi khám.");
                return BaseListResponse<HealthCheckResponse>.ErrorResult("Lỗi lấy danh sách buổi khám.");
            }
        }

        public async Task<BaseResponse<HealthCheckDetailResponse>> GetHealthCheckDetailAsync(Guid healthCheckId, CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = _cacheService.GenerateCacheKey("healthcheck_detail", healthCheckId.ToString());
                var cachedResult = await _cacheService.GetAsync<BaseResponse<HealthCheckDetailResponse>>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogDebug("Chi tiết buổi khám được tìm thấy trong cache.");
                    return cachedResult;
                }

                var healthCheck = await _unitOfWork.GetRepositoryByEntity<HealthCheck>().GetQueryable()
                    .Include(hc => hc.HealthCheckClasses).ThenInclude(hcc => hcc.SchoolClass)
                    .Include(hc => hc.HealthCheckItemAssignments).ThenInclude(hia => hia.HealthCheckItem)
                    .Include(hc => hc.HealthCheckAssignments).ThenInclude(ha => ha.Nurse)
                    .FirstOrDefaultAsync(hc => hc.Id == healthCheckId && !hc.IsDeleted, cancellationToken);

                if (healthCheck == null)
                {
                    _logger.LogWarning("Buổi khám không tồn tại: {HealthCheckId}", healthCheckId);
                    return BaseResponse<HealthCheckDetailResponse>.ErrorResult("Buổi khám không tồn tại.");
                }

                var consents = await _unitOfWork.GetRepositoryByEntity<HealthCheckConsent>().GetQueryable()
                    .Where(c => c.HealthCheckId == healthCheckId && !c.IsDeleted)
                    .ToListAsync(cancellationToken);

                var itemNurseAssignments = healthCheck.HealthCheckItemAssignments
                    .Where(hia => !hia.IsDeleted)
                    .Select(hia => new ItemNurseAssignmentHealthCheck
                    {
                        HealthCheckItemId = hia.HealthCheckItemId,
                        HealthCheckItemName = hia.HealthCheckItem?.Name ?? "Unknown Item",
                        NurseId = healthCheck.HealthCheckAssignments.FirstOrDefault(a => a.HealthCheckItemId == hia.HealthCheckItemId && !a.IsDeleted)?.NurseId,
                        NurseName = healthCheck.HealthCheckAssignments.FirstOrDefault(a => a.HealthCheckItemId == hia.HealthCheckItemId && !a.IsDeleted)?.Nurse?.FullName ?? "Chưa phân công"
                    }).ToList();

                // Lấy danh sách HealthCheckItems
                var healthCheckItems = healthCheck.HealthCheckItemAssignments
                    .Where(hia => !hia.IsDeleted)
                    .Select(hia => new HealthCheckItemResponseDetail
                    {
                        Id = hia.HealthCheckItemId,
                        Name = hia.HealthCheckItem?.Name ?? "Unknown Item",
                        Description = hia.HealthCheckItem?.Description ?? "",
                        Category = hia.HealthCheckItem != null ? hia.HealthCheckItem.Categories.ToString() : ""
                    }).DistinctBy(i => i.Id).ToList();

                // Tính toán consents, xử lý trạng thái WaitingForParentConsent
                var totalConsents = consents.Count;
                var confirmedConsents = consents.Count(c => string.Equals(c.Status, "Confirmed", StringComparison.OrdinalIgnoreCase));
                var pendingConsents = consents.Count(c => string.Equals(c.Status, "WaitingForParentConsent", StringComparison.OrdinalIgnoreCase));
                var declinedConsents = consents.Count(c => string.Equals(c.Status, "Declined", StringComparison.OrdinalIgnoreCase));

                // Log để kiểm tra các giá trị Status
                var statusValues = consents.Select(c => c.Status ?? "null").Distinct();
                _logger.LogDebug("Status values found in consents: {Statuses}", string.Join(", ", statusValues));

                var response = new HealthCheckDetailResponse
                {
                    Id = healthCheck.Id,
                    Title = healthCheck.Title,
                    Description = healthCheck.Description,
                    ResponsibleOrganizationName = healthCheck.ResponsibleOrganizationName,
                    Location = healthCheck.Location,
                    ScheduledDate = healthCheck.ScheduledDate,
                    StartTime = healthCheck.StartTime,
                    EndTime = healthCheck.EndTime,
                    Status = healthCheck.Status,
                    Notes = healthCheck.Notes,
                    ClassIds = healthCheck.HealthCheckClasses.Select(c => c.ClassId).ToList(),
                    TotalConsents = totalConsents,
                    ConfirmedConsents = confirmedConsents,
                    PendingConsents = pendingConsents,
                    DeclinedConsents = declinedConsents,
                    ItemNurseAssignments = itemNurseAssignments,
                    HealthCheckItems = healthCheckItems
                };

                var result = BaseResponse<HealthCheckDetailResponse>.SuccessResult(response, "Lấy chi tiết buổi khám thành công.");
                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
                await _cacheService.AddToTrackingSetAsync(cacheKey, "healthcheck_detail_cache_keys");
                await InvalidateAllCachesAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết buổi khám: {HealthCheckId}", healthCheckId);
                return BaseResponse<HealthCheckDetailResponse>.ErrorResult("Lỗi lấy chi tiết buổi khám.");
            }
        }

        public async Task<BaseResponse<CreateWholeHealthCheckResponse>> CreateWholeHealthCheckAsync(CreateWholeHealthCheckRequest model)
        {
            try
            {
                var validationResult = await _createWholeHealthCheckValidator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning("Validation failed for CreateWholeHealthCheckRequest: {Errors}", errors);
                    return BaseResponse<CreateWholeHealthCheckResponse>.ErrorResult(errors);
                }

                // Kiểm tra HealthCheckItemIds
                var healthCheckItems = await _unitOfWork.GetRepositoryByEntity<HealthCheckItem>().GetQueryable()
                    .Where(item => model.HealthCheckItemIds.Contains(item.Id) && !item.IsDeleted)
                    .ToListAsync();
                if (healthCheckItems.Count != model.HealthCheckItemIds.Count)
                {
                    var invalidItemIds = model.HealthCheckItemIds.Except(healthCheckItems.Select(i => i.Id)).ToList();
                    _logger.LogWarning("Một hoặc nhiều HealthCheckItemIds không tồn tại hoặc đã bị xóa: {InvalidItemIds}", string.Join(", ", invalidItemIds));
                    return BaseResponse<CreateWholeHealthCheckResponse>.ErrorResult($"Một hoặc nhiều hạng mục kiểm tra không tồn tại: {string.Join(", ", invalidItemIds)}");
                }

                // Kiểm tra ClassIds
                var classIds = model.ClassIds.Distinct().ToList();
                _logger.LogInformation("Checking ClassIds: {ClassIds}", string.Join(", ", classIds));
                var validClasses = await _unitOfWork.GetRepositoryByEntity<SchoolClass>().GetQueryable()
                    .Where(c => classIds.Contains(c.Id) && !c.IsDeleted)
                    .ToListAsync();
                _logger.LogInformation("Found {Count} valid classes: {ValidClassIds}", validClasses.Count, string.Join(", ", validClasses.Select(c => c.Id)));
                if (validClasses.Count != classIds.Count)
                {
                    var invalidClassIds = classIds.Except(validClasses.Select(c => c.Id)).ToList();
                    _logger.LogWarning("Một hoặc nhiều ClassIds không tồn tại hoặc đã bị xóa: {InvalidClassIds}", string.Join(", ", invalidClassIds));
                    return BaseResponse<CreateWholeHealthCheckResponse>.ErrorResult($"Một hoặc nhiều lớp học không tồn tại hoặc đã bị xóa: {string.Join(", ", invalidClassIds)}");
                }

                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("uid")?.Value;
                if (!Guid.TryParse(userIdClaim, out var nurseId))
                {
                    _logger.LogError("Không thể xác định ID người dùng từ claims.");
                    throw new UnauthorizedAccessException("Không thể xác định ID người dùng.");
                }

                var healthCheck = new HealthCheck
                {
                    Id = Guid.NewGuid(),
                    Title = model.Title,
                    Description = model.Description,
                    ResponsibleOrganizationName = model.ResponsibleOrganizationName,
                    Location = model.Location,
                    ScheduledDate = model.ScheduledDate,
                    StartTime = model.StartTime,
                    EndTime = model.EndTime,
                    Notes = model.Notes,
                    Status = "PendingApproval",
                    CreatedById = nurseId,
                    CreatedDate = DateTime.UtcNow,
                    IsDeleted = false,
                    Code = $"HCHECK-{Guid.NewGuid().ToString().Substring(0, 8)}"
                };

                await _unitOfWork.GetRepositoryByEntity<HealthCheck>().AddAsync(healthCheck);

                // Thêm HealthCheckClass
                foreach (var classId in classIds)
                {
                    _logger.LogInformation("Adding HealthCheckClass for HealthCheckId: {HealthCheckId}, ClassId: {ClassId}", healthCheck.Id, classId);
                    await _unitOfWork.GetRepositoryByEntity<HealthCheckClass>().AddAsync(new HealthCheckClass
                    {
                        Id = Guid.NewGuid(),
                        HealthCheckId = healthCheck.Id,
                        ClassId = classId,
                        CreatedDate = DateTime.UtcNow,
                        IsDeleted = false
                    });
                }

                // Thêm HealthCheckItemAssignment
                foreach (var healthCheckItemId in model.HealthCheckItemIds.Distinct())
                {
                    _logger.LogInformation("Adding HealthCheckItemAssignment for HealthCheckId: {HealthCheckId}, HealthCheckItemId: {HealthCheckItemId}", healthCheck.Id, healthCheckItemId);
                    await _unitOfWork.GetRepositoryByEntity<HealthCheckItemAssignment>().AddAsync(new HealthCheckItemAssignment
                    {
                        Id = Guid.NewGuid(),
                        HealthCheckId = healthCheck.Id,
                        HealthCheckItemId = healthCheckItemId,
                        CreatedDate = DateTime.UtcNow,
                        IsDeleted = false
                    });
                }

                // Tìm học sinh thuộc các lớp được chọn
                var students = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                    .Where(u => u.StudentClasses.Any(sc => classIds.Contains(sc.ClassId)) && u.UserRoles.Any(ur => ur.Role.Name == "STUDENT") && !u.IsDeleted)
                    .ToListAsync();

                foreach (var student in students)
                {
                    if (student?.ParentId == null)
                    {
                        _logger.LogWarning("Học sinh null hoặc không có phụ huynh cho health check ID: {HealthCheckId}, student ID: {StudentId}", healthCheck.Id, student?.Id);
                        continue;
                    }

                    var parent = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                        .FirstOrDefaultAsync(u => u.Id == student.ParentId && u.UserRoles.Any(ur => ur.Role.Name == "PARENT") && !u.IsDeleted);
                    if (parent == null)
                    {
                        _logger.LogWarning("Không tìm thấy phụ huynh cho học sinh ID: {StudentId}", student.Id);
                        continue;
                    }

                    var consent = new HealthCheckConsent
                    {
                        Id = Guid.NewGuid(),
                        HealthCheckId = healthCheck.Id,
                        StudentId = student.Id,
                        ParentId = parent.Id,
                        Status = "Pending",
                        CreatedDate = DateTime.UtcNow,
                        IsDeleted = false,
                        ConsentFormUrl = $"https://yourdomain.com/consent/{Guid.NewGuid()}"
                    };
                    _logger.LogInformation("Adding HealthCheckConsent for HealthCheckId: {HealthCheckId}, StudentId: {StudentId}, ParentId: {ParentId}", healthCheck.Id, student.Id, parent.Id);
                    await _unitOfWork.GetRepositoryByEntity<HealthCheckConsent>().AddAsync(consent);
                }

                _logger.LogInformation("Saving changes to database for HealthCheckId: {HealthCheckId}", healthCheck.Id);
                await _unitOfWork.SaveChangesAsync();
                await _cacheService.RemoveByPrefixAsync(HEALTHCHECK_LIST_PREFIX);
                await InvalidateAllCachesAsync();

                var response = _mapper.Map<CreateWholeHealthCheckResponse>(healthCheck);
                response.ClassIds = healthCheck.HealthCheckClasses.Select(c => c.ClassId).ToList();
                response.HealthCheckItemIds = healthCheck.HealthCheckItemAssignments.Select(a => a.HealthCheckItemId).ToList();

                _logger.LogInformation("Tạo buổi khám với ID: {HealthCheckId}", healthCheck.Id);
                return BaseResponse<CreateWholeHealthCheckResponse>.SuccessResult(response, "Tạo buổi khám, liên kết lớp và hạng mục kiểm tra thành công.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu thay đổi vào database: {Message}", ex.InnerException?.Message ?? ex.Message);
                return BaseResponse<CreateWholeHealthCheckResponse>.ErrorResult($"Lỗi tạo buổi khám: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo buổi khám.");
                return BaseResponse<CreateWholeHealthCheckResponse>.ErrorResult($"Lỗi tạo buổi khám: {ex.Message}");
            }
        }

        public async Task<BaseResponse<HealthCheckResponse>> UpdateHealthCheckAsync(Guid healthCheckId, UpdateHealthCheckRequest model)
        {
            try
            {
                var validationResult = await _updateHealthCheckValidator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return BaseResponse<HealthCheckResponse>.ErrorResult(errors);
                }

                var healthCheck = await _unitOfWork.GetRepositoryByEntity<HealthCheck>().GetQueryable()
                    .FirstOrDefaultAsync(hc => hc.Id == healthCheckId && !hc.IsDeleted);
                if (healthCheck == null)
                {
                    return BaseResponse<HealthCheckResponse>.ErrorResult("Không tìm thấy buổi khám.");
                }

                _mapper.Map(model, healthCheck);
                healthCheck.LastUpdatedBy = "SCHOOLNURSE";
                healthCheck.LastUpdatedDate = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();
                var cacheKey = _cacheService.GenerateCacheKey("healthcheck_detail", healthCheckId.ToString());
                await _cacheService.RemoveAsync(cacheKey);
                await _cacheService.RemoveByPrefixAsync(HEALTHCHECK_LIST_PREFIX);
                await InvalidateAllCachesAsync();

                var response = _mapper.Map<HealthCheckResponse>(healthCheck);
                return BaseResponse<HealthCheckResponse>.SuccessResult(response, "Cập nhật buổi khám thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật buổi khám: {HealthCheckId}", healthCheckId);
                return BaseResponse<HealthCheckResponse>.ErrorResult($"Lỗi cập nhật buổi khám: {ex.Message}");
            }
        }

        public async Task<BaseResponse<bool>> DeleteHealthCheckAsync(Guid healthCheckId)
        {
            try
            {
                var healthCheck = await _unitOfWork.GetRepositoryByEntity<HealthCheck>().GetQueryable()
                    .FirstOrDefaultAsync(hc => hc.Id == healthCheckId && !hc.IsDeleted);
                if (healthCheck == null)
                {
                    return BaseResponse<bool>.ErrorResult("Không tìm thấy buổi khám.");
                }

                healthCheck.IsDeleted = true;
                healthCheck.LastUpdatedBy = "SCHOOLNURSE";
                healthCheck.LastUpdatedDate = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();
                var cacheKey = _cacheService.GenerateCacheKey("healthcheck_detail", healthCheckId.ToString());
                await _cacheService.RemoveAsync(cacheKey);
                await _cacheService.RemoveByPrefixAsync(HEALTHCHECK_LIST_PREFIX);
                await InvalidateAllCachesAsync();

                return BaseResponse<bool>.SuccessResult(true, "Xóa buổi khám thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa buổi khám: {HealthCheckId}", healthCheckId);
                return BaseResponse<bool>.ErrorResult($"Lỗi xóa buổi khám: {ex.Message}");
            }
        }

        #endregion

        #region Process HealthCheck

        public async Task<BaseResponse<bool>> ApproveHealthCheckAsync(Guid healthCheckId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var healthCheck = await _unitOfWork.GetRepositoryByEntity<HealthCheck>().GetQueryable()
                        .Include(hc => hc.HealthCheckClasses).ThenInclude(hcc => hcc.SchoolClass)
                        .FirstOrDefaultAsync(hc => hc.Id == healthCheckId && !hc.IsDeleted, cancellationToken);

                    if (healthCheck == null || healthCheck.Status != "PendingApproval")
                    {
                        return BaseResponse<bool>.ErrorResult("Buổi khám không tồn tại hoặc không đang chờ duyệt.");
                    }

                    var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("uid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var managerId))
                    {
                        throw new UnauthorizedAccessException("Không thể xác định ID người dùng.");
                    }

                    healthCheck.ApprovedById = managerId;
                    healthCheck.ApprovedDate = DateTime.UtcNow;
                    healthCheck.Status = "WaitingForParentConsent";
                    healthCheck.LastUpdatedBy = managerId.ToString();
                    healthCheck.LastUpdatedDate = DateTime.UtcNow;

                    var consents = await _unitOfWork.GetRepositoryByEntity<HealthCheckConsent>().GetQueryable()
                        .Where(c => c.HealthCheckId == healthCheckId && !c.IsDeleted && c.Status == "Pending")
                        .ToListAsync(cancellationToken);

                    foreach (var consent in consents)
                    {
                        consent.Status = "WaitingForParentConsent";
                        var student = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetById(consent.StudentId);
                        var parent = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetById(consent.ParentId);

                        if (student != null && parent != null)
                        {
                            var emailBody = GenerateConsentEmail(student, healthCheck, parent, consent.Id);
                            await _emailService.SendEmailAsync(parent.Email, "Yêu cầu đồng ý kiểm tra sức khỏe", emailBody);
                        }
                    }

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    var cacheKey = _cacheService.GenerateCacheKey("healthcheck_detail", healthCheckId.ToString());
                    await _cacheService.RemoveAsync(cacheKey);
                    await _cacheService.RemoveByPrefixAsync(HEALTHCHECK_LIST_PREFIX);
                    await InvalidateAllCachesAsync();

                    return BaseResponse<bool>.SuccessResult(true, "Duyệt buổi khám và gửi email thành công.");
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi duyệt buổi khám: {HealthCheckId}", healthCheckId);
                return BaseResponse<bool>.ErrorResult($"Lỗi duyệt buổi khám: {ex.Message}");
            }
        }

        public async Task<BaseResponse<bool>> DeclineHealthCheckAsync(Guid healthCheckId, string reason, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var healthCheck = await _unitOfWork.GetRepositoryByEntity<HealthCheck>().GetQueryable()
                        .Include(hc => hc.HealthCheckConsents)
                        .FirstOrDefaultAsync(hc => hc.Id == healthCheckId && !hc.IsDeleted, cancellationToken);

                    if (healthCheck == null)
                    {
                        return BaseResponse<bool>.ErrorResult("Buổi khám không tồn tại.");
                    }

                    if (healthCheck.Status == "Scheduled" || healthCheck.Status == "Completed")
                    {
                        return BaseResponse<bool>.ErrorResult("Buổi khám đã được lên lịch hoặc hoàn thành, không thể từ chối.");
                    }

                    if (string.IsNullOrWhiteSpace(reason))
                    {
                        return BaseResponse<bool>.ErrorResult("Lý do từ chối là bắt buộc.");
                    }

                    var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("uid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var managerId))
                    {
                        throw new UnauthorizedAccessException("Không thể xác định ID quản lý.");
                    }

                    healthCheck.Status = "Declined";
                    healthCheck.DeclineReason = reason;
                    healthCheck.LastUpdatedBy = managerId.ToString();
                    healthCheck.LastUpdatedDate = DateTime.UtcNow;

                    foreach (var consent in healthCheck.HealthCheckConsents.Where(c => c.Status == "Pending"))
                    {
                        consent.Status = "Declined";
                        consent.ResponseDate = DateTime.UtcNow;
                        await _unitOfWork.GetRepositoryByEntity<HealthCheckConsent>().UpdateAsync(consent);
                    }

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    var cacheKey = _cacheService.GenerateCacheKey("healthcheck_detail", healthCheckId.ToString());
                    await _cacheService.RemoveAsync(cacheKey);
                    await _cacheService.RemoveByPrefixAsync(HEALTHCHECK_LIST_PREFIX);
                    foreach (var consent in healthCheck.HealthCheckConsents)
                    {
                        var consentCacheKey = _cacheService.GenerateCacheKey("parent_consent_status", healthCheckId.ToString(), consent.StudentId.ToString());
                        await _cacheService.RemoveAsync(consentCacheKey);
                    }
                    await InvalidateAllCachesAsync();

                    return BaseResponse<bool>.SuccessResult(true, "Buổi khám đã được từ chối thành công.");
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi từ chối buổi khám: {HealthCheckId}", healthCheckId);
                return BaseResponse<bool>.ErrorResult($"Lỗi khi từ chối buổi khám: {ex.Message}");
            }
        }

        public async Task<BaseResponse<bool>> FinalizeHealthCheckAsync(Guid healthCheckId)
        {
            try
            {
                var healthCheck = await _unitOfWork.GetRepositoryByEntity<HealthCheck>().GetQueryable()
                    .Include(hc => hc.HealthCheckConsents)
                    .FirstOrDefaultAsync(hc => hc.Id == healthCheckId && !hc.IsDeleted);

                if (healthCheck == null || healthCheck.Status != "WaitingForParentConsent")
                {
                    return BaseResponse<bool>.ErrorResult("Buổi khám không tồn tại hoặc không đang chờ chốt.");
                }

                var consents = healthCheck.HealthCheckConsents
                .Where(c => !string.Equals(c.Status, "Confirmed", StringComparison.OrdinalIgnoreCase))
                .ToList();
                foreach (var consent in consents)
                {
                    consent.Status = "Declined";
                    consent.ResponseDate = DateTime.UtcNow;
                    await _unitOfWork.GetRepositoryByEntity<HealthCheckConsent>().UpdateAsync(consent);
                }

                healthCheck.Status = "Scheduled";
                await _unitOfWork.SaveChangesAsync();

                var cacheKey = _cacheService.GenerateCacheKey("healthcheck_detail", healthCheckId.ToString());
                await _cacheService.RemoveAsync(cacheKey);
                await _cacheService.RemoveByPrefixAsync(HEALTHCHECK_LIST_PREFIX);
                foreach (var consent in consents)
                {
                    var consentCacheKey = _cacheService.GenerateCacheKey("parent_consent_status", healthCheckId.ToString(), consent.StudentId.ToString());
                    await _cacheService.RemoveAsync(consentCacheKey);
                }
                await InvalidateAllCachesAsync();

                return new BaseResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Chốt danh sách thành công. Một số yêu cầu chưa phản hồi đã được tự động từ chối."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chốt buổi khám: {HealthCheckId}", healthCheckId);
                return BaseResponse<bool>.ErrorResult($"Lỗi chốt buổi khám: {ex.Message}");
            }
        }

        public async Task<BaseResponse<bool>> ParentApproveAsync(Guid healthCheckId, Guid studentId, ParentApproveHealthCheckRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var validationResult = await _parentApproveValidator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return BaseResponse<bool>.ErrorResult(errors);
                }

                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var consents = await _unitOfWork.GetRepositoryByEntity<HealthCheckConsent>().GetQueryable()
                        .Include(c => c.Parent)
                        .Include(c => c.Student)
                        .Include(c => c.HealthCheck)
                        .Where(c => c.HealthCheckId == healthCheckId && !c.IsDeleted && c.ParentId != null)
                        .ToListAsync(cancellationToken);

                    var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("uid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var parentId))
                    {
                        throw new UnauthorizedAccessException("Không thể xác định ID phụ huynh.");
                    }

                    var consentToUpdate = consents.FirstOrDefault(c => c.ParentId == parentId && c.StudentId == studentId && c.Status == "WaitingForParentConsent");
                    if (consentToUpdate == null)
                    {
                        return BaseResponse<bool>.ErrorResult("Không tìm thấy yêu cầu đồng ý đang chờ xử lý.");
                    }

                    consentToUpdate.Status = request.Status;
                    consentToUpdate.ResponseDate = DateTime.UtcNow;

                    await _unitOfWork.GetRepositoryByEntity<HealthCheckConsent>().UpdateAsync(consentToUpdate);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    var cacheKey = _cacheService.GenerateCacheKey("healthcheck_detail", healthCheckId.ToString());
                    await _cacheService.RemoveAsync(cacheKey);
                    await _cacheService.RemoveByPrefixAsync(HEALTHCHECK_LIST_PREFIX);
                    var consentCacheKey = _cacheService.GenerateCacheKey("parent_consent_status", healthCheckId.ToString(), studentId.ToString());
                    await _cacheService.RemoveAsync(consentCacheKey);
                    await InvalidateAllCachesAsync();

                    var emailBody = GenerateConfirmationEmail(consentToUpdate);
                    await _emailService.SendEmailAsync(consentToUpdate.Parent.Email, $"Xác nhận {request.Status} yêu cầu kiểm tra sức khỏe", emailBody);

                    return BaseResponse<bool>.SuccessResult(true, $"Yêu cầu đồng ý đã được {request.Status.ToLower()}.");
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý yêu cầu đồng ý cho buổi khám: {HealthCheckId}, học sinh: {StudentId}", healthCheckId, studentId);
                return BaseResponse<bool>.ErrorResult($"Lỗi khi xử lý yêu cầu đồng ý: {ex.Message}");
            }
        }

        public async Task<BaseResponse<bool>> AssignNurseToHealthCheckAsync(
    AssignNurseToHealthCheckRequest request,
    CancellationToken cancellationToken = default)
        {
            try
            {
                var validationResult = await _assignNurseValidator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning("Validation failed for AssignNurseToHealthCheckRequest: {Errors}", errors);
                    return BaseResponse<bool>.ErrorResult(errors);
                }

                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var healthCheck = await _unitOfWork.GetRepositoryByEntity<HealthCheck>().GetQueryable()
                        .FirstOrDefaultAsync(hc => hc.Id == request.HealthCheckId && !hc.IsDeleted, cancellationToken);
                    if (healthCheck == null)
                    {
                        _logger.LogWarning("Buổi khám không tồn tại: {HealthCheckId}", request.HealthCheckId);
                        return BaseResponse<bool>.ErrorResult("Buổi khám không tồn tại.");
                    }

                    if (healthCheck.Status != "WaitingForParentConsent" && healthCheck.Status != "Scheduled")
                    {
                        _logger.LogWarning("Buổi khám {HealthCheckId} không ở trạng thái hợp lệ để phân công y tá (Status: {Status})", request.HealthCheckId, healthCheck.Status);
                        return BaseResponse<bool>.ErrorResult("Buổi khám không ở trạng thái hợp lệ để phân công y tá (phải là WaitingForParentConsent hoặc Scheduled).");
                    }

                    // Lấy danh sách HealthCheckItemId từ request
                    var requestItemIds = request.Assignments.Select(a => a.HealthCheckItemId).Distinct().ToList();
                    var requestNurseIds = request.Assignments.Select(a => a.NurseId).Distinct().ToList();

                    // Lấy tất cả phân công hiện tại cho HealthCheckId
                    var existingAssignments = await _unitOfWork.GetRepositoryByEntity<HealthCheckAssignment>().GetQueryable()
                        .Include(a => a.HealthCheckItems)
                        .Where(a => a.HealthCheckId == request.HealthCheckId && !a.IsDeleted)
                        .ToListAsync(cancellationToken);

                    // Xóa các HealthCheckItemId trong request khỏi các y tá khác
                    foreach (var assignment in existingAssignments)
                    {
                        var itemsToRemove = assignment.HealthCheckItems
                            .Where(hci => requestItemIds.Contains(hci.Id))
                            .ToList();
                        if (itemsToRemove.Any())
                        {
                            foreach (var item in itemsToRemove)
                            {
                                assignment.HealthCheckItems.Remove(item);
                            }
                            assignment.LastUpdatedDate = DateTime.UtcNow;
                            if (!assignment.HealthCheckItems.Any())
                            {
                                assignment.IsDeleted = true;
                            }
                            await _unitOfWork.GetRepositoryByEntity<HealthCheckAssignment>().UpdateAsync(assignment);
                            _logger.LogInformation("Xóa mềm hoặc cập nhật phân công y tá {NurseId} cho các hạng mục {ItemIds} trong buổi khám {HealthCheckId}",
                                assignment.NurseId, string.Join(", ", itemsToRemove.Select(i => i.Id)), request.HealthCheckId);
                        }
                    }

                    // Nhóm các HealthCheckItemId theo NurseId từ request
                    var assignmentsByNurse = request.Assignments
                        .GroupBy(a => a.NurseId)
                        .ToDictionary(g => g.Key, g => g.Select(a => a.HealthCheckItemId).ToList());

                    // Thêm hoặc cập nhật phân công mới
                    foreach (var nurseId in assignmentsByNurse.Keys)
                    {
                        // Kiểm tra y tá
                        var nurse = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                            .FirstOrDefaultAsync(u => u.Id == nurseId && u.UserRoles.Any(ur => ur.Role.Name == "SCHOOLNURSE") && !u.IsDeleted, cancellationToken);
                        if (nurse == null)
                        {
                            _logger.LogWarning("Y tá {NurseId} không tồn tại hoặc không có vai SCHOOLNURSE", nurseId);
                            return BaseResponse<bool>.ErrorResult($"Y tá {nurseId} không tồn tại hoặc không có vai SCHOOLNURSE.");
                        }

                        // Kiểm tra các HealthCheckItemId
                        var itemIds = assignmentsByNurse[nurseId];
                        var healthCheckItems = await _unitOfWork.GetRepositoryByEntity<HealthCheckItem>().GetQueryable()
                            .Where(hci => itemIds.Contains(hci.Id) && !hci.IsDeleted)
                            .ToListAsync(cancellationToken);

                        // Kiểm tra xem tất cả HealthCheckItemId có thuộc buổi khám không
                        foreach (var itemId in itemIds)
                        {
                            var itemExists = await _unitOfWork.GetRepositoryByEntity<HealthCheckItemAssignment>().GetQueryable()
                                .AnyAsync(hia => hia.HealthCheckId == request.HealthCheckId && hia.HealthCheckItemId == itemId && !hia.IsDeleted, cancellationToken);
                            if (!itemExists)
                            {
                                _logger.LogWarning("Hạng mục kiểm tra {HealthCheckItemId} không thuộc buổi khám {HealthCheckId}", itemId, request.HealthCheckId);
                                return BaseResponse<bool>.ErrorResult($"Hạng mục kiểm tra {itemId} không thuộc buổi khám này.");
                            }
                        }

                        // Tìm phân công hiện tại của y tá
                        var existingNurseAssignment = existingAssignments
                            .FirstOrDefault(a => a.NurseId == nurseId && !a.IsDeleted);

                        if (existingNurseAssignment != null)
                        {
                            // Cập nhật phân công hiện tại
                            foreach (var item in healthCheckItems)
                            {
                                if (!existingNurseAssignment.HealthCheckItems.Any(i => i.Id == item.Id))
                                {
                                    existingNurseAssignment.HealthCheckItems.Add(item);
                                }
                            }
                            existingNurseAssignment.LastUpdatedDate = DateTime.UtcNow;
                            await _unitOfWork.GetRepositoryByEntity<HealthCheckAssignment>().UpdateAsync(existingNurseAssignment);
                            _logger.LogInformation("Cập nhật phân công y tá {NurseId} với {ItemCount} hạng mục trong buổi khám {HealthCheckId}: {ItemNames}",
                                nurseId, healthCheckItems.Count, request.HealthCheckId,
                                string.Join(", ", healthCheckItems.Select(hci => hci.Name ?? "Chưa xác định")));
                        }
                        else
                        {
                            // Thêm phân công mới
                            var newAssignment = new HealthCheckAssignment
                            {
                                Id = Guid.NewGuid(),
                                HealthCheckId = request.HealthCheckId,
                                NurseId = nurseId,
                                HealthCheckItemId = Guid.Empty,
                                HealthCheckItems = healthCheckItems.Any() ? healthCheckItems : new List<HealthCheckItem>(),
                                AssignedDate = DateTime.UtcNow,
                                CreatedDate = DateTime.UtcNow,
                                IsDeleted = false
                            };
                            await _unitOfWork.GetRepositoryByEntity<HealthCheckAssignment>().AddAsync(newAssignment);
                            _logger.LogInformation("Thêm phân công y tá {NurseId} cho {ItemCount} hạng mục trong buổi khám {HealthCheckId}: {ItemNames}",
                                nurseId, healthCheckItems.Count, request.HealthCheckId,
                                string.Join(", ", healthCheckItems.Select(hci => hci.Name ?? "Chưa xác định")));
                        }
                    }

                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    // Xóa cache
                    var cacheKey = _cacheService.GenerateCacheKey("healthcheck_detail", request.HealthCheckId.ToString());
                    await _cacheService.RemoveAsync(cacheKey);
                    _logger.LogDebug("Đã xóa cache cụ thể cho healthcheck detail: {CacheKey}", cacheKey);
                    await _cacheService.RemoveByPrefixAsync(HEALTHCHECK_LIST_PREFIX);
                    _logger.LogDebug("Đã xóa cache danh sách healthchecks với prefix: {Prefix}", HEALTHCHECK_LIST_PREFIX);
                    var nurseAssignmentCacheKey = _cacheService.GenerateCacheKey("nurse_assignment_status", request.HealthCheckId.ToString());
                    await _cacheService.RemoveAsync(nurseAssignmentCacheKey);
                    _logger.LogDebug("Đã xóa cache nurse assignment statuses: {CacheKey}", nurseAssignmentCacheKey);

                    await InvalidateAllCachesAsync();

                    return BaseResponse<bool>.SuccessResult(true, "Phân công y tá thành công.");
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi phân công y tá cho buổi khám: {HealthCheckId}", request.HealthCheckId);
                return BaseResponse<bool>.ErrorResult($"Lỗi khi phân công y tá: {ex.Message}");
            }
        }

        public async Task<BaseResponse<bool>> ReassignNurseToHealthCheckAsync(
    Guid healthCheckId, ReAssignNurseToHealthCheckRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var validationResult = await _reAssignNurseValidator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning("Validation failed for ReAssignNurseToHealthCheckRequest: {Errors}", errors);
                    return BaseResponse<bool>.ErrorResult(errors);
                }

                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var healthCheck = await _unitOfWork.GetRepositoryByEntity<HealthCheck>().GetQueryable()
                        .FirstOrDefaultAsync(hc => hc.Id == healthCheckId && !hc.IsDeleted, cancellationToken);
                    if (healthCheck == null)
                    {
                        _logger.LogWarning("Buổi khám không tồn tại: {HealthCheckId}", healthCheckId);
                        return BaseResponse<bool>.ErrorResult("Buổi khám không tồn tại.");
                    }

                    if (healthCheck.Status != "WaitingForParentConsent" && healthCheck.Status != "Scheduled")
                    {
                        _logger.LogWarning("Buổi khám {HealthCheckId} không ở trạng thái hợp lệ để tái phân công y tá (Status: {Status})", healthCheckId, healthCheck.Status);
                        return BaseResponse<bool>.ErrorResult("Buổi khám không ở trạng thái hợp lệ để tái phân công y tá (phải là WaitingForParentConsent hoặc Scheduled).");
                    }

                    // Lấy danh sách HealthCheckItemId từ request
                    var requestItemIds = request.Assignments.Select(a => a.HealthCheckItemId).Distinct().ToList();
                    var requestNurseIds = request.Assignments.Select(a => a.NurseId).Distinct().ToList();

                    // Lấy tất cả phân công hiện tại cho HealthCheckId
                    var existingAssignments = await _unitOfWork.GetRepositoryByEntity<HealthCheckAssignment>().GetQueryable()
                        .Include(a => a.HealthCheckItems)
                        .Where(a => a.HealthCheckId == healthCheckId && !a.IsDeleted)
                        .ToListAsync(cancellationToken);

                    // Xóa các HealthCheckItemId trong request khỏi các y tá khác
                    foreach (var assignment in existingAssignments)
                    {
                        if (!requestNurseIds.Contains(assignment.NurseId))
                        {
                            var itemsToRemove = assignment.HealthCheckItems
                                .Where(hci => requestItemIds.Contains(hci.Id))
                                .ToList();
                            if (itemsToRemove.Any())
                            {
                                foreach (var item in itemsToRemove)
                                {
                                    assignment.HealthCheckItems.Remove(item);
                                }
                                assignment.LastUpdatedDate = DateTime.UtcNow;
                                if (!assignment.HealthCheckItems.Any())
                                {
                                    assignment.IsDeleted = true;
                                }
                                await _unitOfWork.GetRepositoryByEntity<HealthCheckAssignment>().UpdateAsync(assignment);
                                _logger.LogInformation("Xóa mềm hoặc cập nhật phân công y tá {NurseId} cho các hạng mục {ItemIds} trong buổi khám {HealthCheckId}",
                                    assignment.NurseId, string.Join(", ", itemsToRemove.Select(i => i.Id)), healthCheckId);
                            }
                        }
                    }

                    // Nhóm các HealthCheckItemId theo NurseId từ request
                    var assignmentsByNurse = request.Assignments
                        .GroupBy(a => a.NurseId)
                        .ToDictionary(g => g.Key, g => g.Select(a => a.HealthCheckItemId).ToList());

                    // Thêm hoặc cập nhật phân công mới
                    foreach (var nurseId in assignmentsByNurse.Keys)
                    {
                        // Kiểm tra y tá
                        var nurse = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                            .FirstOrDefaultAsync(u => u.Id == nurseId && u.UserRoles.Any(ur => ur.Role.Name == "SCHOOLNURSE") && !u.IsDeleted, cancellationToken);
                        if (nurse == null)
                        {
                            _logger.LogWarning("Y tá {NurseId} không tồn tại hoặc không có vai SCHOOLNURSE", nurseId);
                            return BaseResponse<bool>.ErrorResult($"Y tá {nurseId} không tồn tại hoặc không có vai SCHOOLNURSE.");
                        }

                        // Kiểm tra các HealthCheckItemId
                        var itemIds = assignmentsByNurse[nurseId];
                        var healthCheckItems = await _unitOfWork.GetRepositoryByEntity<HealthCheckItem>().GetQueryable()
                            .Where(hci => itemIds.Contains(hci.Id) && !hci.IsDeleted)
                            .ToListAsync(cancellationToken);

                        // Kiểm tra xem tất cả HealthCheckItemId có thuộc buổi khám không
                        foreach (var itemId in itemIds)
                        {
                            var itemExists = await _unitOfWork.GetRepositoryByEntity<HealthCheckItemAssignment>().GetQueryable()
                                .AnyAsync(hia => hia.HealthCheckId == healthCheckId && hia.HealthCheckItemId == itemId && !hia.IsDeleted, cancellationToken);
                            if (!itemExists)
                            {
                                _logger.LogWarning("Hạng mục kiểm tra {HealthCheckItemId} không thuộc buổi khám {HealthCheckId}", itemId, healthCheckId);
                                return BaseResponse<bool>.ErrorResult($"Hạng mục kiểm tra {itemId} không thuộc buổi khám này.");
                            }
                        }

                        // Tìm phân công hiện tại của y tá
                        var existingNurseAssignment = existingAssignments
                            .FirstOrDefault(a => a.NurseId == nurseId && !a.IsDeleted);

                        if (existingNurseAssignment != null)
                        {
                            // Cập nhật phân công hiện tại
                            foreach (var item in healthCheckItems)
                            {
                                if (!existingNurseAssignment.HealthCheckItems.Any(i => i.Id == item.Id))
                                {
                                    existingNurseAssignment.HealthCheckItems.Add(item);
                                }
                            }
                            existingNurseAssignment.LastUpdatedDate = DateTime.UtcNow;
                            await _unitOfWork.GetRepositoryByEntity<HealthCheckAssignment>().UpdateAsync(existingNurseAssignment);
                            _logger.LogInformation("Cập nhật phân công y tá {NurseId} với {ItemCount} hạng mục trong buổi khám {HealthCheckId}: {ItemNames}",
                                nurseId, healthCheckItems.Count, healthCheckId,
                                string.Join(", ", healthCheckItems.Select(hci => hci.Name ?? "Chưa xác định")));
                        }
                        else
                        {
                            // Thêm phân công mới
                            var newAssignment = new HealthCheckAssignment
                            {
                                Id = Guid.NewGuid(),
                                HealthCheckId = healthCheckId,
                                NurseId = nurseId,
                                HealthCheckItemId = Guid.Empty,
                                HealthCheckItems = healthCheckItems.Any() ? healthCheckItems : new List<HealthCheckItem>(),
                                AssignedDate = DateTime.UtcNow,
                                CreatedDate = DateTime.UtcNow,
                                IsDeleted = false
                            };
                            await _unitOfWork.GetRepositoryByEntity<HealthCheckAssignment>().AddAsync(newAssignment);
                            _logger.LogInformation("Thêm phân công y tá {NurseId} cho {ItemCount} hạng mục trong buổi khám {HealthCheckId}: {ItemNames}",
                                nurseId, healthCheckItems.Count, healthCheckId,
                                string.Join(", ", healthCheckItems.Select(hci => hci.Name ?? "Chưa xác định")));
                        }
                    }

                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    // Xóa cache
                    var cacheKey = _cacheService.GenerateCacheKey("healthcheck_detail", healthCheckId.ToString());
                    await _cacheService.RemoveAsync(cacheKey);
                    _logger.LogDebug("Đã xóa cache cụ thể cho healthcheck detail: {CacheKey}", cacheKey);
                    await _cacheService.RemoveByPrefixAsync(HEALTHCHECK_LIST_PREFIX);
                    _logger.LogDebug("Đã xóa cache danh sách healthchecks với prefix: {Prefix}", HEALTHCHECK_LIST_PREFIX);
                    var nurseAssignmentCacheKey = _cacheService.GenerateCacheKey("nurse_assignment_status", healthCheckId.ToString());
                    await _cacheService.RemoveAsync(nurseAssignmentCacheKey);
                    _logger.LogDebug("Đã xóa cache nurse assignment statuses: {CacheKey}", nurseAssignmentCacheKey);

                    await InvalidateAllCachesAsync();

                    return BaseResponse<bool>.SuccessResult(true, "Tái phân công y tá cho các hạng mục được chỉ định thành công.");
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tái phân công y tá cho buổi khám {HealthCheckId}. InnerException: {InnerException}", healthCheckId, ex.InnerException?.ToString() ?? "Không có InnerException");
                return BaseResponse<bool>.ErrorResult($"Lỗi khi tái phân công y tá: {ex.Message}");
            }
        }

        public async Task<BaseResponse<bool>> CompleteHealthCheckAsync(Guid healthCheckId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var healthCheck = await _unitOfWork.GetRepositoryByEntity<HealthCheck>().GetQueryable()
                        .Include(hc => hc.HealthCheckConsents)
                        .FirstOrDefaultAsync(hc => hc.Id == healthCheckId && !hc.IsDeleted, cancellationToken);

                    if (healthCheck == null)
                    {
                        return BaseResponse<bool>.ErrorResult("Buổi khám không tồn tại.");
                    }

                    if (healthCheck.Status != "Scheduled")
                    {
                        return BaseResponse<bool>.ErrorResult("Buổi khám phải ở trạng thái Scheduled để hoàn tất.");
                    }

                    var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("uid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var managerId))
                    {
                        throw new UnauthorizedAccessException("Không thể xác định ID quản lý.");
                    }

                    healthCheck.Status = "Completed";
                    healthCheck.LastUpdatedBy = managerId.ToString();
                    healthCheck.LastUpdatedDate = DateTime.UtcNow;

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    var cacheKey = _cacheService.GenerateCacheKey("healthcheck_detail", healthCheckId.ToString());
                    await _cacheService.RemoveAsync(cacheKey);
                    await _cacheService.RemoveByPrefixAsync(HEALTHCHECK_LIST_PREFIX);
                    await InvalidateAllCachesAsync();

                    return BaseResponse<bool>.SuccessResult(true, "Hoàn tất buổi khám thành công.");
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hoàn tất buổi khám: {HealthCheckId}", healthCheckId);
                return BaseResponse<bool>.ErrorResult($"Lỗi hoàn tất buổi khám: {ex.Message}");
            }
        }

        public async Task<BaseListResponse<StudentConsentStatusHealthCheckResponse>> GetAllStudentConsentStatusAsync(
            Guid healthCheckId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Lấy thông tin buổi khám
                var healthCheck = await _unitOfWork.GetRepositoryByEntity<HealthCheck>().GetQueryable()
                    .Include(hc => hc.HealthCheckClasses)
                    .ThenInclude(hcc => hcc.SchoolClass)
                    .FirstOrDefaultAsync(hc => hc.Id == healthCheckId && !hc.IsDeleted, cancellationToken);

                if (healthCheck == null)
                {
                    _logger.LogWarning("Buổi khám không tồn tại: {HealthCheckId}", healthCheckId);
                    return BaseListResponse<StudentConsentStatusHealthCheckResponse>.ErrorResult("Buổi khám không tồn tại.");
                }

                // Lấy danh sách đồng ý
                var consents = await _unitOfWork.GetRepositoryByEntity<HealthCheckConsent>().GetQueryable()
                    .Include(c => c.Student)
                    .Include(c => c.Parent)
                    .Where(c => c.HealthCheckId == healthCheckId && !c.IsDeleted)
                    .ToListAsync(cancellationToken);

                // Lấy danh sách học sinh thuộc các lớp liên quan
                var classIds = healthCheck.HealthCheckClasses.Select(hcc => hcc.ClassId).ToList();
                var studentsQuery = _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                    .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "STUDENT") && !u.IsDeleted)
                    .Include(u => u.StudentClasses);

                var students = await studentsQuery
                    .Where(u => u.StudentClasses.Any(sc => classIds.Contains(sc.ClassId)))
                    .ToListAsync(cancellationToken);

                // Nhóm học sinh theo lớp
                var classStudentGroups = from student in students
                                         from studentClass in student.StudentClasses
                                         where classIds.Contains(studentClass.ClassId)
                                         group student by new { studentClass.ClassId, studentClass.SchoolClass.Name } into g
                                         select new StudentConsentStatusHealthCheckResponse
                                         {
                                             ClassId = g.Key.ClassId,
                                             ClassName = g.Key.Name,
                                             TotalStudents = g.Count(),
                                             PendingCount = 0,
                                             ConfirmedCount = 0,
                                             DeclinedCount = 0,
                                             Students = g.Select(student => new StudentConsentDetailHealthResponse
                                             {
                                                 StudentId = student.Id,
                                                 StudentName = student.FullName,
                                                 Status = consents.FirstOrDefault(c => c.StudentId == student.Id)?.Status ?? "Pending",
                                                 ResponseDate = consents.FirstOrDefault(c => c.StudentId == student.Id)?.ResponseDate,                                               
                                             }).ToList()
                                         };

                // Tính toán thống kê cho mỗi lớp
                var classResponses = classStudentGroups.ToList();
                foreach (var classResponse in classResponses)
                {
                    classResponse.PendingCount = classResponse.Students.Count(s => s.Status == "Pending");
                    classResponse.ConfirmedCount = classResponse.Students.Count(s => s.Status == "Confirmed");
                    classResponse.DeclinedCount = classResponse.Students.Count(s => s.Status == "Declined");
                }

                // Tính toán tổng thống kê cho toàn bộ buổi khám
                var totalStudents = classResponses.Sum(cr => cr.TotalStudents);
                var pendingCount = classResponses.Sum(cr => cr.PendingCount);
                var confirmedCount = classResponses.Sum(cr => cr.ConfirmedCount);
                var declinedCount = classResponses.Sum(cr => cr.DeclinedCount);

                // Ghi log
                _logger.LogInformation("Lấy danh sách học sinh cho buổi khám {HealthCheckId}. Tổng: {Total}, Đang chờ: {Pending}, Đồng ý: {Confirmed}, Từ chối: {Declined}",
                    healthCheckId, totalStudents, pendingCount, confirmedCount, declinedCount);

                // Trả về kết quả
                return BaseListResponse<StudentConsentStatusHealthCheckResponse>.SuccessResult(
                    classResponses,
                    totalStudents,
                    classResponses.Count,
                    1,
                    "Lấy danh sách học sinh và trạng thái đồng ý thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách học sinh cho buổi khám: {HealthCheckId}", healthCheckId);
                return BaseListResponse<StudentConsentStatusHealthCheckResponse>.ErrorResult("Lỗi lấy danh sách học sinh và trạng thái đồng ý.");
            }
        }

        public async Task<BaseListResponse<HealthCheckNurseAssignmentStatusResponse>> GetHealthCheckNurseAssignmentsAsync(
            Guid healthCheckId,
            CancellationToken cancellationToken = default)
                {
                    try
                    {
                        var cacheKey = _cacheService.GenerateCacheKey(
                            "health_check_nurse_assignment_status",
                            healthCheckId.ToString()
                        );

                        // Kiểm tra cache (để nguyên đoạn bị comment để bạn có thể bật lại nếu cần)
                        // var cachedResult = await _cacheService.GetAsync<BaseListResponse<HealthCheckNurseAssignmentStatusResponse>>(cacheKey);
                        // if (cachedResult != null)
                        // {
                        //     _logger.LogDebug("Health check nurse assignment statuses found in cache with key: {CacheKey}", cacheKey);
                        //     return cachedResult;
                        // }

                        var healthCheck = await _unitOfWork.GetRepositoryByEntity<HealthCheck>().GetQueryable()
                            .FirstOrDefaultAsync(hc => hc.Id == healthCheckId && !hc.IsDeleted, cancellationToken);

                        if (healthCheck == null)
                        {
                            _logger.LogWarning("Health check not found for id: {Id}", healthCheckId);
                            return BaseListResponse<HealthCheckNurseAssignmentStatusResponse>.ErrorResult("Buổi khám không tồn tại.");
                        }

                        var nurses = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                            .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "SCHOOLNURSE") && !u.IsDeleted)
                            .ToListAsync(cancellationToken);

                        // Truy vấn HealthCheckAssignment để lấy thông tin phân công y tá
                        var assignments = await _unitOfWork.GetRepositoryByEntity<HealthCheckAssignment>().GetQueryable()
                            .Include(a => a.HealthCheckItems) // Include để lấy danh sách HealthCheckItem
                            .Where(a => a.HealthCheckId == healthCheckId && !a.IsDeleted)
                            .ToListAsync(cancellationToken);

                        _logger.LogDebug("Found {AssignmentCount} assignments for healthCheckId: {HealthCheckId}",
                            assignments.Count, healthCheckId);

                        var responses = nurses.Select(nurse =>
                        {
                            // Lấy danh sách phân công cho y tá cụ thể
                            var nurseAssignments = assignments.Where(a => a.NurseId == nurse.Id).ToList();
                            var isAssigned = nurseAssignments.Any();

                            // Lấy danh sách HealthCheckItem từ các phân công của y tá
                            var assignedItems = nurseAssignments
                                .SelectMany(a => a.HealthCheckItems) // Lấy tất cả HealthCheckItem từ collection
                                .Where(hci => hci != null)
                                .ToList();

                            _logger.LogDebug("Nurse {NurseId} has {ItemCount} assigned items", nurse.Id, assignedItems.Count);

                            return new HealthCheckNurseAssignmentStatusResponse
                            {
                                NurseId = nurse.Id,
                                NurseName = nurse.FullName,
                                IsAssigned = isAssigned,
                                AssignedHealthCheckItemIds = assignedItems.Select(hci => hci.Id).ToList(),
                                AssignedHealthCheckItemNames = assignedItems.Select(hci => hci.Name ?? "Chưa xác định").ToList()
                            };
                        }).ToList();

                        var result = BaseListResponse<HealthCheckNurseAssignmentStatusResponse>.SuccessResult(
                            responses,
                            responses.Count,
                            responses.Count,
                            1,
                            "Lấy danh sách trạng thái phân công y tá cho buổi khám thành công.");

                        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
                        await _cacheService.AddToTrackingSetAsync(cacheKey, "health_check_nurse_assignment_status_cache_keys");
                        await InvalidateAllCachesAsync();
                        _logger.LogDebug("Cached health check nurse assignment statuses with key: {CacheKey}", cacheKey);

                        return result;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error retrieving health check nurse assignment statuses for id: {Id}", healthCheckId);
                        return BaseListResponse<HealthCheckNurseAssignmentStatusResponse>.ErrorResult("Lỗi lấy trạng thái phân công y tá.");
                    }
                }

        public async Task<BaseListResponse<HealthCheckResponse>> GetHealthCheckByStudentIdAsync(
            Guid studentId,
            int pageIndex,
            int pageSize,
            string searchTerm = "",
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Kiểm tra phân trang
                if (pageIndex < 1 || pageSize < 1)
                {
                    _logger.LogWarning("Invalid pagination parameters: pageIndex={PageIndex}, pageSize={PageSize}", pageIndex, pageSize);
                    return BaseListResponse<HealthCheckResponse>.ErrorResult("Thông tin phân trang không hợp lệ.");
                }

                // Generate cache key
                var cacheKey = _cacheService.GenerateCacheKey(
                    "health_check_by_student",
                    studentId.ToString(),
                    pageIndex.ToString(),
                    pageSize.ToString(),
                    searchTerm);

                // Check cache
                var cachedResult = await _cacheService.GetAsync<BaseListResponse<HealthCheckResponse>>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogDebug("Cache hit for health checks by studentId: {StudentId}", studentId);
                    return cachedResult;
                }

                // Build query
                var query = _unitOfWork.GetRepositoryByEntity<HealthCheck>()
                    .GetQueryable()
                    .Include(hc => hc.HealthCheckClasses) // Sửa từ Classes thành HealthCheckClasses
                        .ThenInclude(hcc => hcc.SchoolClass)
                    .Include(hc => hc.HealthCheckConsents) // Sử dụng HealthCheckConsents thay vì HealthCheckStudents
                    .Where(hc => !hc.IsDeleted &&
                                 hc.HealthCheckConsents.Any(hcs => hcs.StudentId == studentId && !hcs.IsDeleted));

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.Trim().ToLower();
                    query = query.Where(hc => hc.Title.ToLower().Contains(searchTerm) ||
                                             hc.Description.ToLower().Contains(searchTerm) ||
                                             hc.ResponsibleOrganizationName.ToLower().Contains(searchTerm));
                }

                // Order by ScheduledDate descending
                query = query.OrderByDescending(hc => hc.ScheduledDate);

                // Get total count
                var totalCount = await query.CountAsync(cancellationToken);

                // Paginate results
                var healthChecks = await query
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                // Map to response model
                var responses = _mapper.Map<List<HealthCheckResponse>>(healthChecks);
                foreach (var response in responses)
                {
                    var hc = healthChecks.FirstOrDefault(h => h.Id == response.Id);
                    if (hc != null)
                    {
                        response.ApprovedStudents = hc.HealthCheckConsents.Count(hcs => hcs.Status == "Confirmed" && !hcs.IsDeleted);
                        response.TotalStudents = hc.HealthCheckConsents.Count(hcs => !hcs.IsDeleted);
                    }
                }

                // Create success response
                var result = BaseListResponse<HealthCheckResponse>.SuccessResult(
                    responses,
                    totalCount,
                    pageSize,
                    pageIndex,
                    "Lấy danh sách buổi khám theo học sinh thành công.");

                // Cache the result
                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
                await _cacheService.AddToTrackingSetAsync(cacheKey, "health_check_cache_keys");
                await InvalidateAllCachesAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving health checks for studentId: {StudentId}", studentId);
                return BaseListResponse<HealthCheckResponse>.ErrorResult("Lỗi lấy danh sách buổi khám.");
            }
        }

        #endregion

        #region HealthCheck Flow

        public async Task<BaseResponse<VisionRecordResponseHealth>> SaveLeftEyeCheckAsync(
             Guid healthCheckId, SaveVisionCheckRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                // Kiểm tra request không null
                if (request == null)
                {
                    _logger.LogError("Request is null in SaveLeftEyeCheckAsync: HealthCheckId={HealthCheckId}", healthCheckId);
                    return BaseResponse<VisionRecordResponseHealth>.ErrorResult("Dữ liệu yêu cầu không hợp lệ.");
                }

                // Kiểm tra validator không null
                if (_saveVisionCheckValidator == null)
                {
                    _logger.LogError("Validator is null in SaveLeftEyeCheckAsync: HealthCheckId={HealthCheckId}", healthCheckId);
                    return BaseResponse<VisionRecordResponseHealth>.ErrorResult("Cấu hình validator không hợp lệ.");
                }

                var validationResult = await _saveVisionCheckValidator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning("Validation failed for SaveVisionCheckRequest: {Errors}", errors);
                    return BaseResponse<VisionRecordResponseHealth>.ErrorResult(errors);
                }

                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Kiểm tra buổi khám
                    var healthCheck = await _unitOfWork.GetRepositoryByEntity<HealthCheck>().GetQueryable()
                        .FirstOrDefaultAsync(hc => hc.Id == healthCheckId && !hc.IsDeleted && hc.Status == "Scheduled", cancellationToken);
                    if (healthCheck == null)
                    {
                        _logger.LogWarning("Buổi khám không tồn tại hoặc không ở trạng thái Scheduled: {HealthCheckId}", healthCheckId);
                        return BaseResponse<VisionRecordResponseHealth>.ErrorResult("Buổi khám không tồn tại hoặc không ở trạng thái Scheduled.");
                    }

                    // Kiểm tra học sinh
                    var student = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                        .FirstOrDefaultAsync(u => u.Id == request.StudentId && u.UserRoles.Any(ur => ur.Role.Name == "STUDENT") && !u.IsDeleted, cancellationToken);
                    if (student == null)
                    {
                        _logger.LogWarning("Học sinh không tồn tại: {StudentId}", request.StudentId);
                        return BaseResponse<VisionRecordResponseHealth>.ErrorResult("Học sinh không tồn tại.");
                    }

                    // Kiểm tra HealthCheckItem
                    var itemAssignment = await _unitOfWork.GetRepositoryByEntity<HealthCheckItemAssignment>().GetQueryable()
                        .Include(hia => hia.HealthCheckItem)
                        .FirstOrDefaultAsync(hia => hia.HealthCheckId == healthCheckId && hia.HealthCheckItemId == request.HealthCheckItemId && !hia.IsDeleted, cancellationToken);
                    if (itemAssignment == null || itemAssignment.HealthCheckItem == null || itemAssignment.HealthCheckItem.Categories != HealthCheckItemName.Vision)
                    {
                        _logger.LogWarning("Hạng mục kiểm tra không hợp lệ, không tồn tại hoặc không thuộc buổi khám: HealthCheckId={HealthCheckId}, HealthCheckItemId={HealthCheckItemId}", healthCheckId, request.HealthCheckItemId);
                        return BaseResponse<VisionRecordResponseHealth>.ErrorResult("Hạng mục kiểm tra không hợp lệ, không tồn tại hoặc không thuộc buổi khám.");
                    }

                    // Kiểm tra y tá
                    var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("uid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var nurseId))
                    {
                        _logger.LogError("Không thể xác định ID y tá.");
                        return BaseResponse<VisionRecordResponseHealth>.ErrorResult("Không thể xác định ID y tá.");
                    }

                    // Kiểm tra xem y tá có được phân công cho hạng mục này không
                    var assignment = await _unitOfWork.GetRepositoryByEntity<HealthCheckAssignment>().GetQueryable()
                        .FirstOrDefaultAsync(a => a.HealthCheckId == healthCheckId && a.HealthCheckItemId == request.HealthCheckItemId && a.NurseId == nurseId && !a.IsDeleted, cancellationToken);
                    if (assignment == null)
                    {
                        _logger.LogWarning("Y tá {NurseId} không được phân công cho hạng mục {HealthCheckItemId}", nurseId, request.HealthCheckItemId);
                        return BaseResponse<VisionRecordResponseHealth>.ErrorResult("Y tá không được phân công cho hạng mục này.");
                    }

                    // Tìm hoặc tạo VisionRecord
                    var medicalRecord = await _unitOfWork.GetRepositoryByEntity<MedicalRecord>().GetQueryable()
                        .FirstOrDefaultAsync(mr => mr.UserId == request.StudentId && !mr.IsDeleted, cancellationToken);
                    if (medicalRecord == null)
                    {
                        _logger.LogWarning("Học sinh chưa có hồ sơ y tế: {StudentId}", request.StudentId);
                        return BaseResponse<VisionRecordResponseHealth>.ErrorResult("Học sinh chưa có hồ sơ y tế.");
                    }

                    var visionRecord = await _unitOfWork.GetRepositoryByEntity<VisionRecord>().GetQueryable()
                .FirstOrDefaultAsync(vr => vr.MedicalRecordId == medicalRecord.Id && vr.HealthCheckId == healthCheckId && !vr.IsDeleted, cancellationToken);

                    // Tìm giá trị RightEye từ bản ghi cũ (nếu có) -- THAY ĐỔI
                    decimal? previousRightEye = null;
                    if (visionRecord == null) // Chỉ tìm bản ghi cũ nếu không có VisionRecord cho buổi khám hiện tại
                    {
                        var previousVisionRecord = await _unitOfWork.GetRepositoryByEntity<VisionRecord>().GetQueryable()
                            .Where(vr => vr.MedicalRecordId == medicalRecord.Id && !vr.IsDeleted)
                            .OrderByDescending(vr => vr.CheckDate) // Lấy bản ghi gần nhất
                            .FirstOrDefaultAsync(cancellationToken);
                        previousRightEye = previousVisionRecord?.RightEye;
                    }
                    else
                    {
                        previousRightEye = visionRecord.RightEye; // Giữ giá trị RightEye hiện tại nếu VisionRecord đã tồn tại
                    }

                    if (visionRecord == null)
                    {
                        visionRecord = new VisionRecord
                        {
                            Id = Guid.NewGuid(),
                            MedicalRecordId = medicalRecord.Id,
                            HealthCheckId = healthCheckId,
                            LeftEye = request.Value,
                            RightEye = previousRightEye, // Sử dụng giá trị RightEye từ bản ghi cũ -- THAY ĐỔI
                            CheckDate = DateTime.UtcNow,
                            Comments = request.Comments,
                            RecordedBy = nurseId,
                            CreatedDate = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        await _unitOfWork.GetRepositoryByEntity<VisionRecord>().AddAsync(visionRecord);
                    }
                    else
                    {
                        visionRecord.LeftEye = request.Value;
                        visionRecord.Comments = request.Comments ?? visionRecord.Comments;
                        visionRecord.CheckDate = DateTime.UtcNow;
                        visionRecord.RecordedBy = nurseId;
                        visionRecord.LastUpdatedDate = DateTime.UtcNow;
                        await _unitOfWork.GetRepositoryByEntity<VisionRecord>().UpdateAsync(visionRecord);
                    }

                    // Cập nhật hoặc tạo HealthCheckResult
                    var healthCheckResult = await _unitOfWork.GetRepositoryByEntity<HealthCheckResult>().GetQueryable()
                        .FirstOrDefaultAsync(hcr => hcr.HealthCheckId == healthCheckId && hcr.UserId == request.StudentId && !hcr.IsDeleted, cancellationToken);
                    if (healthCheckResult == null)
                    {
                        healthCheckResult = new HealthCheckResult
                        {
                            Id = Guid.NewGuid(),
                            UserId = request.StudentId,
                            HealthCheckId = healthCheckId,
                            OverallAssessment = "Chưa đánh giá",
                            Recommendations = "Không có khuyến nghị",
                            HasAbnormality = false,
                            CreatedDate = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        await _unitOfWork.GetRepositoryByEntity<HealthCheckResult>().AddAsync(healthCheckResult);
                    }

                    // Cập nhật hoặc tạo HealthCheckResultItem
                    var resultItem = await _unitOfWork.GetRepositoryByEntity<HealthCheckResultItem>().GetQueryable()
                        .FirstOrDefaultAsync(ri => ri.HealthCheckResultId == healthCheckResult.Id && ri.HealthCheckItemId == request.HealthCheckItemId && !ri.IsDeleted, cancellationToken);

                    // Chuyển đổi decimal? sang double? để gán và so sánh
                    double? requestValueAsDouble = request.Value.HasValue ? Convert.ToDouble(request.Value.Value) : null;

                    // Kiểm tra MinValue và MaxValue trước khi so sánh
                    bool isNormal = false;
                    if (requestValueAsDouble.HasValue && itemAssignment.HealthCheckItem.MinValue.HasValue && itemAssignment.HealthCheckItem.MaxValue.HasValue)
                    {
                        isNormal = itemAssignment.HealthCheckItem.MinValue.Value <= requestValueAsDouble.Value && requestValueAsDouble.Value <= itemAssignment.HealthCheckItem.MaxValue.Value;
                    }
                    else
                    {
                        _logger.LogWarning("MinValue hoặc MaxValue không được thiết lập cho HealthCheckItemId: {HealthCheckItemId}", request.HealthCheckItemId);
                    }

                    if (resultItem == null)
                    {
                        resultItem = new HealthCheckResultItem
                        {
                            Id = Guid.NewGuid(),
                            HealthCheckResultId = healthCheckResult.Id,
                            HealthCheckItemId = request.HealthCheckItemId,
                            Value = requestValueAsDouble,
                            IsNormal = isNormal,
                            Notes = request.Comments,
                            CreatedDate = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        await _unitOfWork.GetRepositoryByEntity<HealthCheckResultItem>().AddAsync(resultItem);
                    }
                    else
                    {
                        resultItem.Value = requestValueAsDouble;
                        resultItem.IsNormal = isNormal;
                        resultItem.Notes = request.Comments ?? resultItem.Notes;
                        resultItem.LastUpdatedDate = DateTime.UtcNow;
                        await _unitOfWork.GetRepositoryByEntity<HealthCheckResultItem>().UpdateAsync(resultItem);
                    }

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await InvalidateAllCachesAsync();

                    var response = _mapper.Map<VisionRecordResponseHealth>(visionRecord);
                    if (response == null)
                    {
                        _logger.LogError("Lỗi khi ánh xạ VisionRecord sang VisionRecordResponseHealth: VisionRecordId={VisionRecordId}", visionRecord.Id);
                        return BaseResponse<VisionRecordResponseHealth>.ErrorResult("Lỗi khi ánh xạ dữ liệu kết quả kiểm tra mắt trái.");
                    }

                    return BaseResponse<VisionRecordResponseHealth>.SuccessResult(response, "Lưu kết quả kiểm tra mắt trái thành công.");
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu kết quả kiểm tra mắt trái: HealthCheckId={HealthCheckId}, StudentId={StudentId}", healthCheckId, request?.StudentId);
                return BaseResponse<VisionRecordResponseHealth>.ErrorResult($"Lỗi khi lưu kết quả kiểm tra mắt trái: {ex.Message}");
            }
        }

        public async Task<BaseResponse<VisionRecordResponseHealth>> SaveRightEyeCheckAsync(
             Guid healthCheckId, SaveVisionCheckRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                // Kiểm tra request không null
                if (request == null)
                {
                    _logger.LogError("Request is null in SaveRightEyeCheckAsync: HealthCheckId={HealthCheckId}", healthCheckId);
                    return BaseResponse<VisionRecordResponseHealth>.ErrorResult("Dữ liệu yêu cầu không hợp lệ.");
                }

                // Kiểm tra validator không null
                if (_saveVisionCheckValidator == null)
                {
                    _logger.LogError("Validator is null in SaveRightEyeCheckAsync: HealthCheckId={HealthCheckId}", healthCheckId);
                    return BaseResponse<VisionRecordResponseHealth>.ErrorResult("Cấu hình validator không hợp lệ.");
                }

                var validationResult = await _saveVisionCheckValidator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning("Validation failed for SaveVisionCheckRequest: {Errors}", errors);
                    return BaseResponse<VisionRecordResponseHealth>.ErrorResult(errors);
                }

                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Kiểm tra buổi khám
                    var healthCheck = await _unitOfWork.GetRepositoryByEntity<HealthCheck>().GetQueryable()
                        .FirstOrDefaultAsync(hc => hc.Id == healthCheckId && !hc.IsDeleted && hc.Status == "Scheduled", cancellationToken);
                    if (healthCheck == null)
                    {
                        _logger.LogWarning("Buổi khám không tồn tại hoặc không ở trạng thái Scheduled: {HealthCheckId}", healthCheckId);
                        return BaseResponse<VisionRecordResponseHealth>.ErrorResult("Buổi khám không tồn tại hoặc không ở trạng thái Scheduled.");
                    }

                    // Kiểm tra học sinh
                    var student = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                        .FirstOrDefaultAsync(u => u.Id == request.StudentId && u.UserRoles.Any(ur => ur.Role.Name == "STUDENT") && !u.IsDeleted, cancellationToken);
                    if (student == null)
                    {
                        _logger.LogWarning("Học sinh không tồn tại: {StudentId}", request.StudentId);
                        return BaseResponse<VisionRecordResponseHealth>.ErrorResult("Học sinh không tồn tại.");
                    }

                    // Kiểm tra HealthCheckItem
                    var itemAssignment = await _unitOfWork.GetRepositoryByEntity<HealthCheckItemAssignment>().GetQueryable()
                        .Include(hia => hia.HealthCheckItem)
                        .FirstOrDefaultAsync(hia => hia.HealthCheckId == healthCheckId && hia.HealthCheckItemId == request.HealthCheckItemId && !hia.IsDeleted, cancellationToken);
                    if (itemAssignment == null || itemAssignment.HealthCheckItem == null || itemAssignment.HealthCheckItem.Categories != HealthCheckItemName.Vision)
                    {
                        _logger.LogWarning("Hạng mục kiểm tra không hợp lệ, không tồn tại hoặc không thuộc buổi khám: HealthCheckId={HealthCheckId}, HealthCheckItemId={HealthCheckItemId}", healthCheckId, request.HealthCheckItemId);
                        return BaseResponse<VisionRecordResponseHealth>.ErrorResult("Hạng mục kiểm tra không hợp lệ, không tồn tại hoặc không thuộc buổi khám.");
                    }

                    // Kiểm tra y tá
                    var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("uid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var nurseId))
                    {
                        _logger.LogError("Không thể xác định ID y tá.");
                        return BaseResponse<VisionRecordResponseHealth>.ErrorResult("Không thể xác định ID y tá.");
                    }

                    // Kiểm tra xem y tá có được phân công cho hạng mục này không
                    var assignment = await _unitOfWork.GetRepositoryByEntity<HealthCheckAssignment>().GetQueryable()
                        .FirstOrDefaultAsync(a => a.HealthCheckId == healthCheckId && a.HealthCheckItemId == request.HealthCheckItemId && a.NurseId == nurseId && !a.IsDeleted, cancellationToken);
                    if (assignment == null)
                    {
                        _logger.LogWarning("Y tá {NurseId} không được phân công cho hạng mục {HealthCheckItemId}", nurseId, request.HealthCheckItemId);
                        return BaseResponse<VisionRecordResponseHealth>.ErrorResult("Y tá không được phân công cho hạng mục này.");
                    }

                    // Tìm hoặc tạo VisionRecord
                    var medicalRecord = await _unitOfWork.GetRepositoryByEntity<MedicalRecord>().GetQueryable()
                        .FirstOrDefaultAsync(mr => mr.UserId == request.StudentId && !mr.IsDeleted, cancellationToken);
                    if (medicalRecord == null)
                    {
                        _logger.LogWarning("Học sinh chưa có hồ sơ y tế: {StudentId}", request.StudentId);
                        return BaseResponse<VisionRecordResponseHealth>.ErrorResult("Học sinh chưa có hồ sơ y tế.");
                    }

                    var visionRecord = await _unitOfWork.GetRepositoryByEntity<VisionRecord>().GetQueryable()
                        .FirstOrDefaultAsync(vr => vr.MedicalRecordId == medicalRecord.Id && vr.HealthCheckId == healthCheckId && !vr.IsDeleted, cancellationToken);
                    decimal? previousLeftEye = null;
                    if (visionRecord == null) // Chỉ tìm bản ghi cũ nếu không có VisionRecord cho buổi khám hiện tại
                    {
                        var previousVisionRecord = await _unitOfWork.GetRepositoryByEntity<VisionRecord>().GetQueryable()
                            .Where(vr => vr.MedicalRecordId == medicalRecord.Id && !vr.IsDeleted)
                            .OrderByDescending(vr => vr.CheckDate) // Lấy bản ghi gần nhất
                            .FirstOrDefaultAsync(cancellationToken);
                        previousLeftEye = previousVisionRecord?.LeftEye;
                    }
                    else
                    {
                        previousLeftEye = visionRecord.LeftEye; // Giữ giá trị LeftEye hiện tại nếu VisionRecord đã tồn tại
                    }

                    if (visionRecord == null)
                    {
                        visionRecord = new VisionRecord
                        {
                            Id = Guid.NewGuid(),
                            MedicalRecordId = medicalRecord.Id,
                            HealthCheckId = healthCheckId,
                            LeftEye = previousLeftEye, 
                            RightEye = request.Value,
                            CheckDate = DateTime.UtcNow,
                            Comments = request.Comments,
                            RecordedBy = nurseId,
                            CreatedDate = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        await _unitOfWork.GetRepositoryByEntity<VisionRecord>().AddAsync(visionRecord);
                    }
                    else
                    {
                        visionRecord.RightEye = request.Value;
                        visionRecord.Comments = request.Comments ?? visionRecord.Comments;
                        visionRecord.CheckDate = DateTime.UtcNow;
                        visionRecord.RecordedBy = nurseId;
                        visionRecord.LastUpdatedDate = DateTime.UtcNow;
                        await _unitOfWork.GetRepositoryByEntity<VisionRecord>().UpdateAsync(visionRecord);
                    }

                    // Cập nhật hoặc tạo HealthCheckResult
                    var healthCheckResult = await _unitOfWork.GetRepositoryByEntity<HealthCheckResult>().GetQueryable()
                        .FirstOrDefaultAsync(hcr => hcr.HealthCheckId == healthCheckId && hcr.UserId == request.StudentId && !hcr.IsDeleted, cancellationToken);
                    if (healthCheckResult == null)
                    {
                        healthCheckResult = new HealthCheckResult
                        {
                            Id = Guid.NewGuid(),
                            UserId = request.StudentId,
                            HealthCheckId = healthCheckId,
                            OverallAssessment = "Chưa đánh giá",
                            Recommendations = "Không có khuyến nghị",
                            HasAbnormality = false,
                            CreatedDate = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        await _unitOfWork.GetRepositoryByEntity<HealthCheckResult>().AddAsync(healthCheckResult);
                    }

                    // Cập nhật hoặc tạo HealthCheckResultItem
                    var resultItem = await _unitOfWork.GetRepositoryByEntity<HealthCheckResultItem>().GetQueryable()
                        .FirstOrDefaultAsync(ri => ri.HealthCheckResultId == healthCheckResult.Id && ri.HealthCheckItemId == request.HealthCheckItemId && !ri.IsDeleted, cancellationToken);

                    // Chuyển đổi decimal? sang double? để gán và so sánh
                    double? requestValueAsDouble = request.Value.HasValue ? Convert.ToDouble(request.Value.Value) : null;

                    // Kiểm tra MinValue và MaxValue trước khi so sánh
                    bool isNormal = false;
                    if (requestValueAsDouble.HasValue && itemAssignment.HealthCheckItem.MinValue.HasValue && itemAssignment.HealthCheckItem.MaxValue.HasValue)
                    {
                        isNormal = itemAssignment.HealthCheckItem.MinValue.Value <= requestValueAsDouble.Value && requestValueAsDouble.Value <= itemAssignment.HealthCheckItem.MaxValue.Value;
                    }
                    else
                    {
                        _logger.LogWarning("MinValue hoặc MaxValue không được thiết lập cho HealthCheckItemId: {HealthCheckItemId}", request.HealthCheckItemId);
                    }

                    if (resultItem == null)
                    {
                        resultItem = new HealthCheckResultItem
                        {
                            Id = Guid.NewGuid(),
                            HealthCheckResultId = healthCheckResult.Id,
                            HealthCheckItemId = request.HealthCheckItemId,
                            Value = requestValueAsDouble,
                            IsNormal = isNormal,
                            Notes = request.Comments,
                            CreatedDate = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        await _unitOfWork.GetRepositoryByEntity<HealthCheckResultItem>().AddAsync(resultItem);
                    }
                    else
                    {
                        resultItem.Value = requestValueAsDouble;
                        resultItem.IsNormal = isNormal;
                        resultItem.Notes = request.Comments ?? resultItem.Notes;
                        resultItem.LastUpdatedDate = DateTime.UtcNow;
                        await _unitOfWork.GetRepositoryByEntity<HealthCheckResultItem>().UpdateAsync(resultItem);
                    }

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await InvalidateAllCachesAsync();

                    var response = _mapper.Map<VisionRecordResponseHealth>(visionRecord);
                    if (response == null)
                    {
                        _logger.LogError("Lỗi khi ánh xạ VisionRecord sang VisionRecordResponseHealth: VisionRecordId={VisionRecordId}", visionRecord.Id);
                        return BaseResponse<VisionRecordResponseHealth>.ErrorResult("Lỗi khi ánh xạ dữ liệu kết quả kiểm tra mắt phải.");
                    }

                    return BaseResponse<VisionRecordResponseHealth>.SuccessResult(response, "Lưu kết quả kiểm tra mắt phải thành công.");
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu kết quả kiểm tra mắt phải: HealthCheckId={HealthCheckId}, StudentId={StudentId}", healthCheckId, request?.StudentId);
                return BaseResponse<VisionRecordResponseHealth>.ErrorResult($"Lỗi khi lưu kết quả kiểm tra mắt phải: {ex.Message}");
            }
        }


        //public async Task<BaseResponse<HearingRecordResponse>> SaveHearingCheckAsync(
        //    Guid healthCheckId,
        //    SaveHearingCheckRequest request,
        //    CancellationToken cancellationToken = default)
        //{
        //    try
        //    {
        //        var validationResult = await _saveHearingCheckValidator.ValidateAsync(request, cancellationToken);
        //        if (!validationResult.IsValid)
        //        {
        //            string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
        //            _logger.LogWarning("Validation failed for SaveHearingCheckRequest: {Errors}", errors);
        //            return BaseResponse<HearingRecordResponse>.ErrorResult(errors);
        //        }

        //        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        //        {
        //            // Kiểm tra buổi khám
        //            var healthCheck = await _unitOfWork.GetRepositoryByEntity<HealthCheck>().GetQueryable()
        //                .FirstOrDefaultAsync(hc => hc.Id == healthCheckId && !hc.IsDeleted && hc.Status == "Scheduled", cancellationToken);
        //            if (healthCheck == null)
        //            {
        //                _logger.LogWarning("Buổi khám không tồn tại hoặc không ở trạng thái Scheduled: {HealthCheckId}", healthCheckId);
        //                return BaseResponse<HearingRecordResponse>.ErrorResult("Buổi khám không tồn tại hoặc không ở trạng thái Scheduled.");
        //            }

        //            // Kiểm tra quyền y tá
        //            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("uid")?.Value;
        //            if (!Guid.TryParse(userIdClaim, out var nurseId))
        //            {
        //                _logger.LogError("Không thể xác định ID y tá từ claims.");
        //                throw new UnauthorizedAccessException("Không thể xác định ID y tá.");
        //            }

        //            // Kiểm tra y tá được phân công cho hạng mục kiểm tra thính lực
        //            var itemAssignment = await _unitOfWork.GetRepositoryByEntity<HealthCheckItemAssignment>().GetQueryable()
        //                .Include(hia => hia.HealthCheckItem)
        //                .FirstOrDefaultAsync(hia => hia.HealthCheckId == healthCheckId
        //                    && hia.HealthCheckItem.Categories == HealthCheckItemName.Hearing
        //                    && !hia.IsDeleted, cancellationToken);
        //            if (itemAssignment == null)
        //            {
        //                _logger.LogWarning("Hạng mục kiểm tra thính lực không thuộc buổi khám: {HealthCheckId}", healthCheckId);
        //                return BaseResponse<HearingRecordResponse>.ErrorResult("Hạng mục kiểm tra thính lực không thuộc buổi khám này.");
        //            }

        //            var nurseAssignment = await _unitOfWork.GetRepositoryByEntity<HealthCheckAssignment>().GetQueryable()
        //                .FirstOrDefaultAsync(a => a.HealthCheckId == healthCheckId
        //                    && a.HealthCheckItemId == itemAssignment.HealthCheckItemId
        //                    && a.NurseId == nurseId
        //                    && !a.IsDeleted, cancellationToken);
        //            if (nurseAssignment == null)
        //            {
        //                _logger.LogWarning("Y tá {NurseId} không được phân công cho hạng mục kiểm tra thính lực trong buổi khám: {HealthCheckId}", nurseId, healthCheckId);
        //                return BaseResponse<HearingRecordResponse>.ErrorResult("Bạn không được phân công thực hiện kiểm tra thính lực cho buổi khám này.");
        //            }

        //            // Kiểm tra học sinh và consent
        //            var consent = await _unitOfWork.GetRepositoryByEntity<HealthCheckConsent>().GetQueryable()
        //                .FirstOrDefaultAsync(c => c.HealthCheckId == healthCheckId
        //                    && c.StudentId == request.StudentId
        //                    && c.Status == "Confirmed"
        //                    && !c.IsDeleted, cancellationToken);
        //            if (consent == null)
        //            {
        //                _logger.LogWarning("Học sinh {StudentId} không được phép tham gia buổi khám: {HealthCheckId}", request.StudentId, healthCheckId);
        //                return BaseResponse<HearingRecordResponse>.ErrorResult("Học sinh không được phép tham gia buổi khám này.");
        //            }

        //            // Tạo hoặc cập nhật HealthCheckResult
        //            var healthCheckResult = await _unitOfWork.GetRepositoryByEntity<HealthCheckResult>().GetQueryable()
        //                .FirstOrDefaultAsync(hcr => hcr.HealthCheckId == healthCheckId
        //                    && hcr.UserId == request.StudentId
        //                    && !hcr.IsDeleted, cancellationToken);
        //            if (healthCheckResult == null)
        //            {
        //                healthCheckResult = new HealthCheckResult
        //                {
        //                    Id = Guid.NewGuid(),
        //                    HealthCheckId = healthCheckId,
        //                    UserId = request.StudentId,
        //                    CreatedDate = DateTime.UtcNow,
        //                    IsDeleted = false
        //                };
        //                await _unitOfWork.GetRepositoryByEntity<HealthCheckResult>().AddAsync(healthCheckResult);
        //            }

        //            // Tạo bản ghi HearingRecord
        //            var hearingRecord = new HearingRecord
        //            {
        //                Id = Guid.NewGuid(),
        //                MedicalRecordId = (await _unitOfWork.GetRepositoryByEntity<MedicalRecord>().GetQueryable()
        //                    .FirstOrDefaultAsync(mr => mr.UserId == request.StudentId && !mr.IsDeleted, cancellationToken))?.Id
        //                    ?? throw new Exception("Học sinh không có hồ sơ y tế."),
        //                HealthCheckId = healthCheckId,
        //                LeftEar = request.LeftEar,
        //                RightEar = request.RightEar,
        //                CheckDate = DateTime.UtcNow,
        //                Comments = request.Comments,
        //                RecordedBy = nurseId,
        //                CreatedDate = DateTime.UtcNow,
        //                IsDeleted = false
        //            };
        //            await _unitOfWork.GetRepositoryByEntity<HearingRecord>().AddAsync(hearingRecord);

        //            // Tạo bản ghi HealthCheckResultItem
        //            var resultItem = new HealthCheckResultItem
        //            {
        //                Id = Guid.NewGuid(),
        //                HealthCheckResultId = healthCheckResult.Id,
        //                HealthCheckItemId = itemAssignment.HealthCheckItemId,
        //                IsNormal = request.LeftEar == "Normal" && request.RightEar == "Normal", // Ví dụ điều kiện bình thường
        //                Notes = request.Comments,
        //                CreatedDate = DateTime.UtcNow,
        //                IsDeleted = false
        //            };
        //            await _unitOfWork.GetRepositoryByEntity<HealthCheckResultItem>().AddAsync(resultItem);

        //            // Cập nhật HasAbnormality trong HealthCheckResult
        //            healthCheckResult.HasAbnormality |= !resultItem.IsNormal;
        //            await _unitOfWork.GetRepositoryByEntity<HealthCheckResult>().UpdateAsync(healthCheckResult);

        //            await _unitOfWork.SaveChangesAsync(cancellationToken);
        //            await InvalidateHealthCheckCachesAsync(healthCheckId);

        //            var response = _mapper.Map<HearingRecordResponse>(hearingRecord);
        //            return BaseResponse<HearingRecordResponse>.SuccessResult(response, "Lưu kết quả kiểm tra thính lực thành công.");
        //        }, cancellationToken);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Lỗi khi lưu kết quả kiểm tra thính lực cho buổi khám: {HealthCheckId}, học sinh: {StudentId}", healthCheckId, request.StudentId);
        //        return BaseResponse<HearingRecordResponse>.ErrorResult($"Lỗi khi lưu kết quả kiểm tra thính lực: {ex.Message}");
        //    }
        //}

        #endregion

        #region Helper Methods

        private IQueryable<HealthCheck> ApplyHealthCheckOrdering(IQueryable<HealthCheck> query, string orderBy)
        {
            return orderBy?.ToLower() switch
            {
                "title" => query.OrderBy(hc => hc.Title),
                "title_desc" => query.OrderByDescending(hc => hc.Title),
                "location" => query.OrderBy(hc => hc.Location),
                "location_desc" => query.OrderByDescending(hc => hc.Location),
                "scheduleddate" => query.OrderBy(hc => hc.ScheduledDate),
                "scheduleddate_desc" => query.OrderByDescending(hc => hc.ScheduledDate),
                _ => query.OrderByDescending(hc => hc.ScheduledDate)
            };
        }

        private string GenerateConsentEmail(ApplicationUser student, HealthCheck healthCheck, ApplicationUser parent, Guid consentId)
        {
            if (student == null || healthCheck == null || parent == null)
            {
                throw new ArgumentNullException("Thông tin học sinh, buổi khám, hoặc phụ huynh không được null.");
            }

            return $@"
                <h2>Yêu cầu đồng ý kiểm tra sức khỏe</h2>
                <p>Thông tin học sinh:</p>
                <ul>
                    <li>Họ và tên: {student.FullName ?? "Không có tên"}</li>
                    <li>Mã học sinh: {student.StudentCode ?? "Không có mã"}</li>
                </ul>
                <p>Thông tin kiểm tra sức khỏe:</p>
                <ul>
                    <li>Tiêu đề: {healthCheck.Title ?? "Không xác định"}</li>
                    <li>Địa điểm: {healthCheck.Location ?? "Không xác định"}</li>
                    <li>Thời gian: {healthCheck.ScheduledDate:dd/MM/yyyy} {healthCheck.StartTime:HH:mm} - {healthCheck.EndTime:HH:mm}</li>
                </ul>
                <p>Phụ huynh: {parent.FullName ?? "Không có tên"} ({parent.Email ?? "Không có email"})</p>
                <p>Vui lòng phản hồi tại: <a href='https://yourdomain.com/consent?healthCheckId={healthCheck.Id}&consentId={consentId}'>Link đồng ý</a></p>";
        }

        private string GenerateConfirmationEmail(HealthCheckConsent consent)
        {
            var action = consent.Status == "Confirmed" ? "đồng ý" : "từ chối";
            var deadline = consent.HealthCheck?.ScheduledDate.AddDays(-3) ?? DateTime.UtcNow;
            return $@"<h2>Xác nhận {action} kiểm tra sức khỏe</h2>
                <p>Kính gửi {consent.Parent?.FullName ?? "Phụ huynh không xác định"},</p>
                <p>Chúng tôi đã nhận được phản hồi của bạn về yêu cầu kiểm tra sức khỏe cho học sinh {consent.Student?.FullName ?? "Học sinh không xác định"}.</p>
                <p>Trạng thái: <strong>{action}</strong></p>
                <p>Buổi khám: {consent.HealthCheck?.Title ?? "Buổi khám không xác định"}</p>
                <p>Thời hạn phản hồi: <strong>{deadline:dd/MM/yyyy HH:mm}</strong></p>
                <p>Thời gian phản hồi: {consent.ResponseDate?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa có"}</p>";
        }

        private async Task InvalidateAllCachesAsync()
        {
            try
            {
                // Invalidate specific tracking sets
                await Task.WhenAll(
                    _cacheService.InvalidateTrackingSetAsync(HEALTHCHECK_CACHE_SET),
                    _cacheService.InvalidateTrackingSetAsync("healthcheck_detail_cache_keys"),
                    _cacheService.InvalidateTrackingSetAsync("health_check_nurse_assignment_status_cache_keys"),
                    _cacheService.InvalidateTrackingSetAsync("health_check_cache_keys")
                );

                // Invalidate caches by prefixes
                await Task.WhenAll(
                    _cacheService.RemoveByPrefixAsync(HEALTHCHECK_CACHE_PREFIX),
                    _cacheService.RemoveByPrefixAsync(HEALTHCHECK_LIST_PREFIX),
                    _cacheService.RemoveByPrefixAsync("parent_consent_status"),
                    _cacheService.RemoveByPrefixAsync("healthcheck_detail"),
                    _cacheService.RemoveByPrefixAsync("nurse_assignment_status"),
                    _cacheService.RemoveByPrefixAsync("health_check_by_student")
                );

                _logger.LogDebug("All relevant caches invalidated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while invalidating all caches for health checks.");
            }
        }

        #endregion
    }
}