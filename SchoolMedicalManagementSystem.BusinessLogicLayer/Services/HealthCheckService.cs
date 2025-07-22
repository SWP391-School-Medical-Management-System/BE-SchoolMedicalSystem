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

                var responses = healthChecks.Select(hc =>
                {
                    var response = _mapper.Map<HealthCheckResponse>(hc);
                    response.TotalStudents = hc.HealthCheckConsents?.Where(c => !c.IsDeleted).Select(c => c.StudentId).Distinct().Count() ?? 0;
                    response.ApprovedStudents = hc.HealthCheckConsents?.Where(c => c.Status == "Confirmed" && !c.IsDeleted).Select(c => c.StudentId).Distinct().Count() ?? 0;
                    return response;
                }).ToList();

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
                    TotalConsents = consents.Count,
                    ConfirmedConsents = consents.Count(c => c.Status == "Confirmed"),
                    PendingConsents = consents.Count(c => c.Status == "Pending"),
                    DeclinedConsents = consents.Count(c => c.Status == "Declined"),
                    ItemNurseAssignments = itemNurseAssignments
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

                var consents = healthCheck.HealthCheckConsents.Where(c => c.Status == "Pending").ToList();
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

                return BaseResponse<bool>.SuccessResult(true, "Chốt danh sách thành công.");
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

                    var existingAssignments = await _unitOfWork.GetRepositoryByEntity<HealthCheckAssignment>().GetQueryable()
                        .Where(a => a.HealthCheckId == request.HealthCheckId && !a.IsDeleted)
                        .ToListAsync(cancellationToken);

                    foreach (var assignmentRequest in request.Assignments)
                    {
                        // Kiểm tra HealthCheckItemId có thuộc buổi khám không
                        var itemExists = await _unitOfWork.GetRepositoryByEntity<HealthCheckItemAssignment>().GetQueryable()
                            .AnyAsync(hia => hia.HealthCheckId == request.HealthCheckId && hia.HealthCheckItemId == assignmentRequest.HealthCheckItemId && !hia.IsDeleted, cancellationToken);
                        if (!itemExists)
                        {
                            _logger.LogWarning("Hạng mục kiểm tra {HealthCheckItemId} không thuộc buổi khám {HealthCheckId}", assignmentRequest.HealthCheckItemId, request.HealthCheckId);
                            return BaseResponse<bool>.ErrorResult($"Hạng mục kiểm tra {assignmentRequest.HealthCheckItemId} không thuộc buổi khám này.");
                        }

                        // Kiểm tra y tá
                        var nurse = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                            .FirstOrDefaultAsync(u => u.Id == assignmentRequest.NurseId && u.UserRoles.Any(ur => ur.Role.Name == "SCHOOLNURSE") && !u.IsDeleted, cancellationToken);
                        if (nurse == null)
                        {
                            _logger.LogWarning("Y tá {NurseId} không tồn tại hoặc không có vai SCHOOLNURSE", assignmentRequest.NurseId);
                            return BaseResponse<bool>.ErrorResult($"Y tá {assignmentRequest.NurseId} không tồn tại hoặc không có vai SCHOOLNURSE.");
                        }

                        var existingAssignment = existingAssignments.FirstOrDefault(a => a.HealthCheckItemId == assignmentRequest.HealthCheckItemId);
                        if (existingAssignment != null)
                        {
                            // Cập nhật phân công hiện tại
                            existingAssignment.NurseId = assignmentRequest.NurseId;
                            existingAssignment.AssignedDate = DateTime.UtcNow;
                            existingAssignment.LastUpdatedDate = DateTime.UtcNow;
                            await _unitOfWork.GetRepositoryByEntity<HealthCheckAssignment>().UpdateAsync(existingAssignment);
                            _logger.LogInformation("Cập nhật phân công y tá {NurseId} cho hạng mục {HealthCheckItemId} trong buổi khám {HealthCheckId}", assignmentRequest.NurseId, assignmentRequest.HealthCheckItemId, request.HealthCheckId);
                        }
                        else
                        {
                            // Thêm phân công mới
                            var newAssignment = new HealthCheckAssignment
                            {
                                Id = Guid.NewGuid(),
                                HealthCheckId = request.HealthCheckId,
                                HealthCheckItemId = assignmentRequest.HealthCheckItemId,
                                NurseId = assignmentRequest.NurseId,
                                AssignedDate = DateTime.UtcNow,
                                CreatedDate = DateTime.UtcNow,
                                IsDeleted = false
                            };
                            await _unitOfWork.GetRepositoryByEntity<HealthCheckAssignment>().AddAsync(newAssignment);
                            _logger.LogInformation("Thêm phân công y tá {NurseId} cho hạng mục {HealthCheckItemId} trong buổi khám {HealthCheckId}", assignmentRequest.NurseId, assignmentRequest.HealthCheckItemId, request.HealthCheckId);
                        }
                    }

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    var cacheKey = _cacheService.GenerateCacheKey("healthcheck_detail", request.HealthCheckId.ToString());
                    await _cacheService.RemoveAsync(cacheKey);
                    await _cacheService.RemoveByPrefixAsync(HEALTHCHECK_LIST_PREFIX);
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

                    // Lấy tất cả các phân công hiện tại
                    var existingAssignments = await _unitOfWork.GetRepositoryByEntity<HealthCheckAssignment>().GetQueryable()
                        .Where(a => a.HealthCheckId == healthCheckId && !a.IsDeleted)
                        .ToListAsync(cancellationToken);

                    // Xóa mềm tất cả các phân công hiện tại
                    foreach (var assignment in existingAssignments)
                    {
                        assignment.IsDeleted = true;
                        assignment.LastUpdatedDate = DateTime.UtcNow;
                        await _unitOfWork.GetRepositoryByEntity<HealthCheckAssignment>().UpdateAsync(assignment);
                        _logger.LogInformation("Xóa mềm phân công y tá cho hạng mục {HealthCheckItemId} trong buổi khám {HealthCheckId}", assignment.HealthCheckItemId, healthCheckId);
                    }

                    // Thêm các phân công mới từ request
                    foreach (var assignmentRequest in request.Assignments)
                    {
                        // Kiểm tra HealthCheckItemId có thuộc buổi khám không
                        var itemExists = await _unitOfWork.GetRepositoryByEntity<HealthCheckItemAssignment>().GetQueryable()
                            .AnyAsync(hia => hia.HealthCheckId == healthCheckId && hia.HealthCheckItemId == assignmentRequest.HealthCheckItemId && !hia.IsDeleted, cancellationToken);
                        if (!itemExists)
                        {
                            _logger.LogWarning("Hạng mục kiểm tra {HealthCheckItemId} không thuộc buổi khám {HealthCheckId}", assignmentRequest.HealthCheckItemId, healthCheckId);
                            return BaseResponse<bool>.ErrorResult($"Hạng mục kiểm tra {assignmentRequest.HealthCheckItemId} không thuộc buổi khám này.");
                        }

                        // Kiểm tra y tá
                        var nurse = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                            .FirstOrDefaultAsync(u => u.Id == assignmentRequest.NurseId && u.UserRoles.Any(ur => ur.Role.Name == "SCHOOLNURSE") && !u.IsDeleted, cancellationToken);
                        if (nurse == null)
                        {
                            _logger.LogWarning("Y tá {NurseId} không tồn tại hoặc không có vai SCHOOLNURSE", assignmentRequest.NurseId);
                            return BaseResponse<bool>.ErrorResult($"Y tá {assignmentRequest.NurseId} không tồn tại hoặc không có vai SCHOOLNURSE.");
                        }

                        // Thêm phân công mới
                        var newAssignment = new HealthCheckAssignment
                        {
                            Id = Guid.NewGuid(),
                            HealthCheckId = healthCheckId,
                            HealthCheckItemId = assignmentRequest.HealthCheckItemId,
                            NurseId = assignmentRequest.NurseId,
                            AssignedDate = DateTime.UtcNow,
                            CreatedDate = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        await _unitOfWork.GetRepositoryByEntity<HealthCheckAssignment>().AddAsync(newAssignment);
                        _logger.LogInformation("Thêm phân công y tá {NurseId} cho hạng mục {HealthCheckItemId} trong buổi khám {HealthCheckId}", assignmentRequest.NurseId, assignmentRequest.HealthCheckItemId, healthCheckId);
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

                    return BaseResponse<bool>.SuccessResult(true, "Tái phân công y tá cho tất cả các hạng mục thành công.");
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
                await _cacheService.InvalidateTrackingSetAsync(HEALTHCHECK_CACHE_SET);
                await Task.WhenAll(
                    _cacheService.RemoveByPrefixAsync(HEALTHCHECK_CACHE_PREFIX),
                    _cacheService.RemoveByPrefixAsync(HEALTHCHECK_LIST_PREFIX),
                    _cacheService.RemoveByPrefixAsync("parent_consent_status"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi làm mới cache cho buổi khám.");
            }
        }

        #endregion
    }
}