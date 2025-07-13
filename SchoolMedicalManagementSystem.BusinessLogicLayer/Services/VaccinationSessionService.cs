using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using SchoolMedicalManagementSystem.DataAccessLayer.RepositoryContracts.IBaseUnitOfWork;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineSessionRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccinationSessionResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccineRecordResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.VaccineSession;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services
{
    public class VaccinationSessionService : IVaccinationSessionService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly ILogger<VaccinationSessionService> _logger;
        private readonly IValidator<CreateVaccinationSessionRequest> _createSessionValidator;
        private readonly IValidator<UpdateVaccinationSessionRequest> _updateSessionValidator;
        private readonly IValidator<CreateWholeVaccinationSessionRequest> _createWholeSessionValidator;
        private readonly IValidator<ParentApproveRequest> _parentApproveValidator;
        private readonly IValidator<AssignNurseToSessionRequest> _assignNurseValidator;
        private readonly IValidator<MarkStudentVaccinatedRequest> _markStudentVaccinatedValidator;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private const string SESSION_CACHE_PREFIX = "vaccination_session";
        private const string SESSION_LIST_PREFIX = "vaccination_sessions_list";
        private const string SESSION_CACHE_SET = "vaccination_session_cache_keys";

        public VaccinationSessionService(
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ILogger<VaccinationSessionService> logger,
            IValidator<CreateVaccinationSessionRequest> createSessionValidator,
            IValidator<UpdateVaccinationSessionRequest> updateSessionValidator,
            IValidator<CreateWholeVaccinationSessionRequest> createWholeSessionValidator,
            IValidator<ParentApproveRequest> parentApproveValidator,
            IValidator<AssignNurseToSessionRequest> assignNurseValidator,
            IValidator<MarkStudentVaccinatedRequest> markStudentVaccinatedValidator,
            IEmailService emailService,
            IHttpContextAccessor httpContextAccessor)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _logger = logger;
            _createSessionValidator = createSessionValidator;
            _updateSessionValidator = updateSessionValidator;
            _createWholeSessionValidator = createWholeSessionValidator;
            _parentApproveValidator = parentApproveValidator;
            _assignNurseValidator = assignNurseValidator;
            _markStudentVaccinatedValidator = markStudentVaccinatedValidator;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
        }

        #region CRUD Vaccination Session

        public async Task<BaseListResponse<VaccinationSessionResponse>> GetVaccinationSessionsAsync(
            int pageIndex,
            int pageSize,
            string searchTerm,
            string orderBy,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = _cacheService.GenerateCacheKey(
                    SESSION_LIST_PREFIX,
                    pageIndex.ToString(),
                    pageSize.ToString(),
                    searchTerm ?? "",
                    orderBy ?? ""
                );

                var cachedResult = await _cacheService.GetAsync<BaseListResponse<VaccinationSessionResponse>>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogDebug("Vaccination sessions list found in cache.");
                    return cachedResult;
                }

                var query = _unitOfWork.GetRepositoryByEntity<VaccinationSession>().GetQueryable()
                    .Include(vs => vs.VaccineType)
                    .Include(vs => vs.Classes)
                    .ThenInclude(vsc => vsc.SchoolClass)
                    .Where(vs => !vs.IsDeleted)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(vs =>
                        vs.Location.ToLower().Contains(searchTerm) ||
                        vs.SessionName.ToLower().Contains(searchTerm) ||
                        vs.VaccineType.Name.ToLower().Contains(searchTerm));
                }

                query = ApplySessionOrdering(query, orderBy);

                var totalCount = await query.CountAsync(cancellationToken);
                var sessions = await query
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                foreach (var session in sessions)
                {
                    _logger.LogDebug("Session {SessionId} has {ClassCount} classes", session.Id, session.Classes?.Count ?? 0);
                    if (session.Classes != null)
                    {
                        foreach (var classEntry in session.Classes)
                        {
                            _logger.LogDebug("Class {ClassId} in session {SessionId}: Name = {ClassName}", classEntry.ClassId, session.Id, classEntry.SchoolClass?.Name ?? "Null");
                        }
                    }
                }

                var responses = sessions.Select(session => _mapper.Map<VaccinationSessionResponse>(session)).ToList();

                var result = BaseListResponse<VaccinationSessionResponse>.SuccessResult(
                    responses,
                    totalCount,
                    pageSize,
                    pageIndex,
                    "Lấy danh sách buổi tiêm thành công.");

                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
                await _cacheService.AddToTrackingSetAsync(cacheKey, SESSION_CACHE_SET);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vaccination sessions.");
                return BaseListResponse<VaccinationSessionResponse>.ErrorResult("Lỗi lấy danh sách buổi tiêm.");
            }
        }

        public async Task<BaseListResponse<VaccinationSessionResponse>> GetSessionsByStudentIdAsync(
            Guid studentId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = _cacheService.GenerateCacheKey(
                    "student_sessions",
                    studentId.ToString()
                );

                var cachedResult = await _cacheService.GetAsync<BaseListResponse<VaccinationSessionResponse>>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogDebug("Student sessions found in cache for studentId: {StudentId}", studentId);
                    return cachedResult;
                }

                var consents = await _unitOfWork.GetRepositoryByEntity<VaccinationConsent>().GetQueryable()
                    .Include(c => c.Session)
                        .ThenInclude(s => s.VaccineType)
                    .Where(c => c.StudentId == studentId && !c.IsDeleted)
                    .ToListAsync(cancellationToken);

                var sessions = consents.Select(c => c.Session).Distinct().ToList();
                var responses = sessions.Select(MapToSessionResponse).ToList();

                var result = BaseListResponse<VaccinationSessionResponse>.SuccessResult(
                    responses,
                    responses.Count,
                    responses.Count,
                    1,
                    "Lấy danh sách buổi tiêm cho học sinh thành công.");

                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
                await _cacheService.AddToTrackingSetAsync(cacheKey, "student_session_cache_keys");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sessions for studentId: {StudentId}", studentId);
                return BaseListResponse<VaccinationSessionResponse>.ErrorResult("Lỗi lấy danh sách buổi tiêm.");
            }
        }

        public async Task<BaseResponse<VaccinationSessionDetailResponse>> GetSessionDetailAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = _cacheService.GenerateCacheKey(
                    "session_detail",
                    sessionId.ToString()
                );

                var cachedResult = await _cacheService.GetAsync<BaseResponse<VaccinationSessionDetailResponse>>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogDebug("Session detail found in cache for sessionId: {SessionId}", sessionId);
                    return cachedResult;
                }

                var session = await _unitOfWork.GetRepositoryByEntity<VaccinationSession>().GetQueryable()
                    .Include(vs => vs.Classes)
                        .ThenInclude(vsc => vsc.SchoolClass)
                    .Include(vs => vs.VaccineType)
                    .Include(vs => vs.Assignments)
                        .ThenInclude(va => va.Nurse)
                    .FirstOrDefaultAsync(vs => vs.Id == sessionId && !vs.IsDeleted, cancellationToken);

                if (session == null)
                {
                    return new BaseResponse<VaccinationSessionDetailResponse>
                    {
                        Success = false,
                        Message = "Buổi tiêm không tồn tại."
                    };
                }

                var consents = await _unitOfWork.GetRepositoryByEntity<VaccinationConsent>().GetQueryable()
                    .Where(c => c.SessionId == sessionId && !c.IsDeleted)
                    .ToListAsync(cancellationToken);

                var classNurseAssignments = new List<ClassNurseAssignment>();
                if (session.Assignments != null)
                {
                    classNurseAssignments = session.Assignments
                        .GroupBy(a => a.ClassId)
                        .Select(g =>
                        {
                            var classAssignment = session.Classes.FirstOrDefault(c => c.ClassId == g.Key);
                            string className = classAssignment != null && classAssignment.SchoolClass != null ? classAssignment.SchoolClass.Name : "Unknown Class";
                            var nurse = g.First().Nurse;
                            string nurseName = nurse != null ? nurse.FullName : "Unknown Nurse";
                            return new ClassNurseAssignment
                            {
                                ClassId = g.Key,
                                ClassName = className,
                                NurseId = g.First().NurseId,
                                NurseName = nurseName
                            };
                        }).ToList();
                }

                var response = new VaccinationSessionDetailResponse
                {
                    Id = session.Id,
                    VaccineTypeId = session.VaccineTypeId,
                    VaccineTypeName = session.VaccineTypeName,
                    Location = session.Location,
                    StartTime = session.StartTime,
                    EndTime = session.EndTime,
                    Status = session.Status,
                    SessionName = session.SessionName,
                    Notes = session.Notes,
                    ClassIds = session.Classes.Select(c => c.ClassId).ToList(),
                    TotalConsents = consents.Count,
                    ConfirmedConsents = consents.Count(c => c.Status == "Confirmed"),
                    PendingConsents = consents.Count(c => c.Status == "Pending"),
                    DeclinedConsents = consents.Count(c => c.Status == "Declined"),
                    SideEffect = session.SideEffect,
                    Contraindication = session.Contraindication,
                    ResponsibleOrganizationName = session.ResponsibleOrganizationName,
                    ClassNurseAssignments = classNurseAssignments
                };

                var result = new BaseResponse<VaccinationSessionDetailResponse>
                {
                    Success = true,
                    Data = response,
                    Message = "Lấy chi tiết buổi tiêm thành công."
                };

                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
                await _cacheService.AddToTrackingSetAsync(cacheKey, "session_detail_cache_keys");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving session detail for sessionId: {SessionId}", sessionId);
                return BaseResponse<VaccinationSessionDetailResponse>.ErrorResult("Lỗi lấy chi tiết buổi tiêm.");
            }
        }

        public async Task<BaseListResponse<ClassStudentConsentStatusResponse>> GetAllClassStudentConsentStatusAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var session = await _unitOfWork.GetRepositoryByEntity<VaccinationSession>().GetQueryable()
                    .Include(vs => vs.Classes)
                    .ThenInclude(vsc => vsc.SchoolClass)
                    .FirstOrDefaultAsync(vs => vs.Id == sessionId && !vs.IsDeleted, cancellationToken);

                if (session == null)
                {
                    return BaseListResponse<ClassStudentConsentStatusResponse>.ErrorResult("Buổi tiêm không tồn tại.");
                }

                var consents = await _unitOfWork.GetRepositoryByEntity<VaccinationConsent>().GetQueryable()
                    .Include(c => c.Student)
                    .Include(c => c.Session)
                    .Where(c => c.SessionId == sessionId && !c.IsDeleted)
                    .ToListAsync(cancellationToken);

                var classStudentsQuery = _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                    .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "STUDENT"))
                    .Include(u => u.StudentClasses);

                var classIds = session.Classes.Select(c => c.ClassId).ToList();
                var allClassStudents = await classStudentsQuery
                    .Where(u => u.StudentClasses.Any(sc => classIds.Contains(sc.ClassId)))
                    .ToListAsync(cancellationToken);

                var vaccinationRecords = await _unitOfWork.GetRepositoryByEntity<VaccinationRecord>().GetQueryable()
                    .Where(vr => vr.SessionId == sessionId && !vr.IsDeleted)
                    .ToDictionaryAsync(vr => vr.UserId, vr => vr.VaccinationStatus, cancellationToken);

                var responses = new List<ClassStudentConsentStatusResponse>();
                foreach (var classEntry in session.Classes.Where(c => !c.IsDeleted))
                {
                    var classId = classEntry.ClassId;
                    var classInfo = classEntry.SchoolClass;
                    var classStudents = allClassStudents.Where(u => u.StudentClasses.Any(sc => sc.ClassId == classId)).ToList();

                    var studentStatuses = (from student in classStudents
                                           join consent in consents on student.Id equals consent.StudentId into gj
                                           from consent in gj.DefaultIfEmpty()
                                           let vaccinationStatus = consent?.Status == "Confirmed" && vaccinationRecords.ContainsKey(student.Id)
                                               ? vaccinationRecords[student.Id] ?? "InProgress"
                                               : "InProgress"
                                           select new StudentConsentStatusResponse
                                           {
                                               StudentId = student.Id,
                                               StudentName = student.FullName,
                                               Status = consent?.Status ?? "Pending",
                                               ResponseDate = consent?.ResponseDate,
                                               VaccinationStatus = vaccinationStatus
                                           }).ToList();

                    var response = new ClassStudentConsentStatusResponse
                    {
                        ClassId = classId,
                        ClassName = classInfo.Name,
                        TotalStudents = classStudents.Count,
                        PendingCount = studentStatuses.Count(s => s.Status == "Pending"),
                        ConfirmedCount = studentStatuses.Count(s => s.Status == "Confirmed"),
                        DeclinedCount = studentStatuses.Count(s => s.Status == "Declined"),
                        Students = studentStatuses
                    };

                    responses.Add(response);
                }

                return BaseListResponse<ClassStudentConsentStatusResponse>.SuccessResult(
                    responses,
                    responses.Count,
                    responses.Count,
                    1,
                    "Lấy thông tin trạng thái học sinh cho tất cả các lớp thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all class student consent status for session {SessionId}", sessionId);
                return BaseListResponse<ClassStudentConsentStatusResponse>.ErrorResult("Lỗi lấy thông tin trạng thái học sinh.");
            }
        }

        public async Task<BaseListResponse<ClassStudentConsentStatusResponse>> GetClassStudentConsentStatusAsync(
         Guid sessionId,
         Guid classId,
         CancellationToken cancellationToken = default)
        {
            try
            {
                var session = await _unitOfWork.GetRepositoryByEntity<VaccinationSession>().GetQueryable()
                    .Include(vs => vs.Classes)
                    .ThenInclude(vsc => vsc.SchoolClass)
                    .FirstOrDefaultAsync(vs => vs.Id == sessionId && !vs.IsDeleted, cancellationToken);

                if (session == null)
                {
                    return BaseListResponse<ClassStudentConsentStatusResponse>.ErrorResult("Buổi tiêm không tồn tại.");
                }

                var classExists = session.Classes.Any(c => c.ClassId == classId && !c.IsDeleted);
                if (!classExists)
                {
                    return BaseListResponse<ClassStudentConsentStatusResponse>.ErrorResult("Lớp không thuộc buổi tiêm này.");
                }

                // Lấy danh sách consents liên quan đến buổi tiêm
                var consents = await _unitOfWork.GetRepositoryByEntity<VaccinationConsent>().GetQueryable()
                    .Include(c => c.Student)
                    .ThenInclude(s => s.StudentClasses) // Đảm bảo tải StudentClasses
                    .Include(c => c.Session)
                    .Where(c => c.SessionId == sessionId && !c.IsDeleted)
                    .ToListAsync(cancellationToken);

                // Lọc danh sách consents dựa trên học sinh thuộc classId, với kiểm tra null
                var classConsents = consents
                    .Where(c => c.Student != null && c.Student.StudentClasses != null && c.Student.StudentClasses.Any(sc => sc.ClassId == classId))
                    .ToList();

                var vaccinationRecords = await _unitOfWork.GetRepositoryByEntity<VaccinationRecord>().GetQueryable()
                    .Where(vr => vr.SessionId == sessionId && !vr.IsDeleted)
                    .ToDictionaryAsync(vr => vr.UserId, vr => vr.VaccinationStatus, cancellationToken);

                var studentStatuses = new List<StudentConsentStatusResponse>();
                var classInfo = session.Classes.First(c => c.ClassId == classId).SchoolClass;

                foreach (var consent in classConsents)
                {
                    if (consent.Student == null)
                    {
                        _logger.LogWarning("Consent {ConsentId} has null Student for session {SessionId}", consent.Id, sessionId);
                        continue; // Bỏ qua nếu Student là null
                    }

                    var vaccinationStatus = consent.Status == "Confirmed" && vaccinationRecords.ContainsKey(consent.StudentId)
                        ? vaccinationRecords[consent.StudentId] ?? "InProgress"
                        : "InProgress";

                    studentStatuses.Add(new StudentConsentStatusResponse
                    {
                        StudentId = consent.StudentId,
                        StudentName = consent.Student.FullName ?? "Unknown Student",
                        Status = consent.Status ?? "Pending",
                        ResponseDate = consent.ResponseDate,
                        VaccinationStatus = vaccinationStatus
                    });
                }

                var response = new ClassStudentConsentStatusResponse
                {
                    ClassId = classId,
                    ClassName = classInfo.Name,
                    TotalStudents = studentStatuses.Count, // Sử dụng số lượng consents hợp lệ
                    PendingCount = studentStatuses.Count(s => s.Status == "Pending"),
                    ConfirmedCount = studentStatuses.Count(s => s.Status == "Confirmed"),
                    DeclinedCount = studentStatuses.Count(s => s.Status == "Declined"),
                    Students = studentStatuses
                };

                var responses = new List<ClassStudentConsentStatusResponse> { response };

                return BaseListResponse<ClassStudentConsentStatusResponse>.SuccessResult(
                    responses,
                    1,
                    1,
                    1,
                    "Lấy thông tin trạng thái học sinh thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving class student consent status for session {SessionId}, class {ClassId}", sessionId, classId);
                return BaseListResponse<ClassStudentConsentStatusResponse>.ErrorResult("Lỗi lấy thông tin trạng thái học sinh.");
            }
        }

        public async Task<BaseResponse<VaccinationSessionResponse>> CreateVaccinationSessionAsync(
            CreateVaccinationSessionRequest model)
        {
            try
            {
                var validationResult = await _createSessionValidator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return new BaseResponse<VaccinationSessionResponse>
                    {
                        Success = false,
                        Message = errors
                    };
                }

                var nurseId = Guid.TryParse(_httpContextAccessor.HttpContext?.User?.Identity?.Name, out var id) ? id : Guid.Empty;

                var session = _mapper.Map<VaccinationSession>(model);
                session.Id = Guid.NewGuid();
                session.VaccineTypeId = model.VaccineTypeId;
                session.Status = "PendingApproval";
                session.CreatedById = nurseId != Guid.Empty ? nurseId : throw new UnauthorizedAccessException("Không thể xác định ID người dùng.");
                session.CreatedDate = DateTime.UtcNow;
                session.IsDeleted = false;
                session.Code = $"VSESSION-{Guid.NewGuid().ToString().Substring(0, 8)}";

                var vaccineType = await _unitOfWork.GetRepositoryByEntity<VaccinationType>().GetById(model.VaccineTypeId);
                session.VaccineTypeName = vaccineType?.Name ?? "Unknown";

                await _unitOfWork.GetRepositoryByEntity<VaccinationSession>().AddAsync(session);
                foreach (var classId in model.ClassIds)
                {
                    await _unitOfWork.GetRepositoryByEntity<VaccinationSessionClass>().AddAsync(new VaccinationSessionClass
                    {
                        Id = Guid.NewGuid(),
                        SessionId = session.Id,
                        ClassId = classId,
                        CreatedDate = DateTime.UtcNow
                    });
                }
                await _unitOfWork.SaveChangesAsync();

                // Xóa cache cụ thể cho danh sách sessions và session detail
                var sessionCacheKey = _cacheService.GenerateCacheKey("session_detail", session.Id.ToString());
                await _cacheService.RemoveAsync(sessionCacheKey);
                _logger.LogDebug("Đã xóa cache cụ thể cho session detail: {CacheKey}", sessionCacheKey);
                await _cacheService.RemoveByPrefixAsync(SESSION_LIST_PREFIX);
                _logger.LogDebug("Đã xóa cache danh sách sessions với prefix: {Prefix}", SESSION_LIST_PREFIX);

                await InvalidateAllCachesAsync();

                session = await _unitOfWork.GetRepositoryByEntity<VaccinationSession>().GetQueryable()
                    .FirstOrDefaultAsync(vs => vs.Id == session.Id);

                var response = MapToSessionResponse(session);

                _logger.LogInformation("Tạo buổi tiêm thành công với ID: {SessionId}", session.Id);
                return new BaseResponse<VaccinationSessionResponse>
                {
                    Success = true,
                    Data = response,
                    Message = "Tạo buổi tiêm thành công."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vaccination session.");
                return new BaseResponse<VaccinationSessionResponse>
                {
                    Success = false,
                    Message = $"Lỗi tạo buổi tiêm: {ex.Message}"
                };
            }
        }

        public async Task<BaseResponse<CreateWholeVaccinationSessionResponse>> CreateWholeVaccinationSessionAsync(
            CreateWholeVaccinationSessionRequest model)
        {
            try
            {
                var validationResult = await _createWholeSessionValidator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return new BaseResponse<CreateWholeVaccinationSessionResponse>
                    {
                        Success = false,
                        Message = errors
                    };
                }

                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("uid")?.Value;
                if (!Guid.TryParse(userIdClaim, out var nurseId))
                {
                    throw new UnauthorizedAccessException("Không thể xác định ID người dùng.");
                }

                var session = new VaccinationSession
                {
                    Id = Guid.NewGuid(),
                    VaccineTypeId = model.VaccineTypeId,
                    SessionName = model.SessionName,
                    ResponsibleOrganizationName = model.ResponsibleOrganizationName,
                    Location = model.Location,
                    StartDate = model.StartDate,
                    StartTime = model.StartTime,
                    EndTime = model.EndTime,
                    Posology = model.Posology,
                    SideEffect = model.SideEffect,
                    Contraindication = model.Contraindication,
                    Notes = model.Notes,
                    Status = "PendingApproval",
                    CreatedById = nurseId,
                    CreatedDate = DateTime.UtcNow,
                    IsDeleted = false,
                    Code = $"VSESSION-{Guid.NewGuid().ToString().Substring(0, 8)}"
                };

                var vaccineType = await _unitOfWork.GetRepositoryByEntity<VaccinationType>().GetById(model.VaccineTypeId);
                session.VaccineTypeName = vaccineType?.Name ?? "Unknown";

                await _unitOfWork.GetRepositoryByEntity<VaccinationSession>().AddAsync(session);

                foreach (var classId in model.ClassIds.Distinct())
                {
                    await _unitOfWork.GetRepositoryByEntity<VaccinationSessionClass>().AddAsync(new VaccinationSessionClass
                    {
                        Id = Guid.NewGuid(),
                        SessionId = session.Id,
                        ClassId = classId,
                        CreatedDate = DateTime.UtcNow,
                        IsDeleted = false
                    });
                }

                await _unitOfWork.SaveChangesAsync();

                // Xóa cache cụ thể cho danh sách sessions và session detail
                var sessionCacheKey = _cacheService.GenerateCacheKey("session_detail", session.Id.ToString());
                await _cacheService.RemoveAsync(sessionCacheKey);
                _logger.LogDebug("Đã xóa cache cụ thể cho session detail: {CacheKey}", sessionCacheKey);
                await _cacheService.RemoveByPrefixAsync(SESSION_LIST_PREFIX);
                _logger.LogDebug("Đã xóa cache danh sách sessions với prefix: {Prefix}", SESSION_LIST_PREFIX);

                await InvalidateAllCachesAsync();

                session = await _unitOfWork.GetRepositoryByEntity<VaccinationSession>().GetQueryable()
                    .Include(vs => vs.Classes)
                    .FirstOrDefaultAsync(vs => vs.Id == session.Id);

                var response = new CreateWholeVaccinationSessionResponse
                {
                    Id = session.Id,
                    VaccineTypeId = session.VaccineTypeId,
                    VaccineTypeName = session.VaccineTypeName,
                    SessionName = session.SessionName,
                    ResponsibleOrganizationName = session.ResponsibleOrganizationName,
                    Location = session.Location,
                    StartDate = session.StartDate,
                    StartTime = session.StartTime,
                    EndTime = session.EndTime,
                    Status = session.Status,
                    Posology = session.Posology,
                    SideEffect = session.SideEffect,
                    Contraindication = session.Contraindication,
                    Notes = session.Notes,
                    CreatedById = session.CreatedById,
                    CreatedDate = (DateTime)session.CreatedDate,
                    Code = session.Code,
                    ClassIds = session.Classes.Select(c => c.ClassId).ToList()
                };

                _logger.LogInformation("Tạo buổi tiêm với ID: {SessionId}", session.Id);
                return new BaseResponse<CreateWholeVaccinationSessionResponse>
                {
                    Success = true,
                    Data = response,
                    Message = "Tạo buổi tiêm và liên kết lớp thành công."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo buổi tiêm.");
                return new BaseResponse<CreateWholeVaccinationSessionResponse>
                {
                    Success = false,
                    Message = $"Lỗi tạo buổi tiêm: {ex.Message}"
                };
            }
        }

        public async Task<BaseResponse<VaccinationSessionResponse>> UpdateVaccinationSessionAsync(
            Guid sessionId,
            UpdateVaccinationSessionRequest model)
        {
            try
            {
                var validationResult = await _updateSessionValidator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return new BaseResponse<VaccinationSessionResponse>
                    {
                        Success = false,
                        Message = errors
                    };
                }

                var session = await _unitOfWork.GetRepositoryByEntity<VaccinationSession>().GetQueryable()
                    .Include(vs => vs.VaccineType)
                    .FirstOrDefaultAsync(vs => vs.Id == sessionId && !vs.IsDeleted);

                if (session == null)
                {
                    return new BaseResponse<VaccinationSessionResponse>
                    {
                        Success = false,
                        Message = "Không tìm thấy buổi tiêm."
                    };
                }

                _mapper.Map(model, session);
                session.LastUpdatedBy = "SCHOOLNURSE";
                session.LastUpdatedDate = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();

                // Xóa cache cụ thể cho session detail và danh sách sessions
                var sessionCacheKey = _cacheService.GenerateCacheKey("session_detail", sessionId.ToString());
                await _cacheService.RemoveAsync(sessionCacheKey);
                _logger.LogDebug("Đã xóa cache cụ thể cho session detail: {CacheKey}", sessionCacheKey);
                await _cacheService.RemoveByPrefixAsync(SESSION_LIST_PREFIX);
                _logger.LogDebug("Đã xóa cache danh sách sessions với prefix: {Prefix}", SESSION_LIST_PREFIX);

                await InvalidateAllCachesAsync();

                var response = MapToSessionResponse(session);

                return new BaseResponse<VaccinationSessionResponse>
                {
                    Success = true,
                    Data = response,
                    Message = "Cập nhật buổi tiêm thành công."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vaccination session: {SessionId}", sessionId);
                return new BaseResponse<VaccinationSessionResponse>
                {
                    Success = false,
                    Message = $"Lỗi cập nhật buổi tiêm: {ex.Message}"
                };
            }
        }

        public async Task<BaseResponse<bool>> DeleteVaccinationSessionAsync(Guid sessionId)
        {
            try
            {
                var session = await _unitOfWork.GetRepositoryByEntity<VaccinationSession>().GetQueryable()
                    .FirstOrDefaultAsync(vs => vs.Id == sessionId && !vs.IsDeleted);

                if (session == null)
                {
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "Không tìm thấy buổi tiêm."
                    };
                }

                session.IsDeleted = true;
                session.LastUpdatedBy = "SCHOOLNURSE";
                session.LastUpdatedDate = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();

                // Xóa cache cụ thể cho session detail và danh sách sessions
                var sessionCacheKey = _cacheService.GenerateCacheKey("session_detail", sessionId.ToString());
                await _cacheService.RemoveAsync(sessionCacheKey);
                _logger.LogDebug("Đã xóa cache cụ thể cho session detail: {CacheKey}", sessionCacheKey);
                await _cacheService.RemoveByPrefixAsync(SESSION_LIST_PREFIX);
                _logger.LogDebug("Đã xóa cache danh sách sessions với prefix: {Prefix}", SESSION_LIST_PREFIX);

                await InvalidateAllCachesAsync();

                return new BaseResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Xóa buổi tiêm thành công."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vaccination session: {SessionId}", sessionId);
                return new BaseResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Lỗi xóa buổi tiêm: {ex.Message}"
                };
            }
        }

        #endregion

        #region Process Vaccination Session

        public async Task<BaseResponse<bool>> ApproveSessionAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Bắt đầu duyệt buổi tiêm với ID: {SessionId}", sessionId);

                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var session = await _unitOfWork.GetRepositoryByEntity<VaccinationSession>().GetQueryable()
                        .Include(vs => vs.Classes)
                            .ThenInclude(vsc => vsc.SchoolClass)
                        .Include(vs => vs.VaccineType)
                        .FirstOrDefaultAsync(vs => vs.Id == sessionId && !vs.IsDeleted, cancellationToken);

                    if (session == null || session.Status != "PendingApproval")
                    {
                        _logger.LogWarning("Buổi tiêm {SessionId} không tồn tại hoặc không đang chờ duyệt.", sessionId);
                        return new BaseResponse<bool>
                        {
                            Success = false,
                            Message = "Buổi tiêm không tồn tại hoặc không đang chờ duyệt."
                        };
                    }

                    var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("uid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var managerId))
                    {
                        _logger.LogError("Không thể xác định ID người dùng từ claims.");
                        throw new UnauthorizedAccessException("Không thể xác định ID người dùng.");
                    }

                    session.ApprovedById = managerId;
                    session.ApprovedDate = DateTime.UtcNow;
                    session.Status = "WaitingForParentConsent";
                    session.LastUpdatedBy = managerId.ToString();
                    session.LastUpdatedDate = DateTime.UtcNow;

                    var classIds = session.Classes.Select(c => c.ClassId).ToList();
                    var students = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                        .Where(u => u.StudentClasses.Any(sc => classIds.Contains(sc.ClassId)) &&
                                    u.UserRoles.Any(ur => ur.Role.Name == "STUDENT"))
                        .ToListAsync(cancellationToken);

                    var consents = new List<VaccinationConsent>();
                    foreach (var student in students)
                    {
                        if (student == null)
                        {
                            _logger.LogWarning("Học sinh null trong session ID: {SessionId}", sessionId);
                            continue;
                        }

                        var parent = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                            .FirstOrDefaultAsync(u => u.Id == student.ParentId &&
                                                     u.UserRoles.Any(ur => ur.Role.Name == "PARENT"), cancellationToken);

                        if (parent == null)
                        {
                            _logger.LogWarning("Không tìm thấy phụ huynh cho học sinh ID: {StudentId}", student.Id);
                            continue;
                        }

                        var existingConsent = await _unitOfWork.GetRepositoryByEntity<VaccinationConsent>().GetQueryable()
                            .FirstOrDefaultAsync(c => c.SessionId == sessionId && c.StudentId == student.Id && !c.IsDeleted, cancellationToken);

                        if (existingConsent != null)
                        {
                            _logger.LogWarning("Consent đã tồn tại cho học sinh ID: {StudentId}, session ID: {SessionId}", student.Id, sessionId);
                            continue;
                        }

                        var consent = new VaccinationConsent
                        {
                            Id = Guid.NewGuid(),
                            SessionId = sessionId,
                            StudentId = student.Id,
                            ParentId = parent.Id,
                            Status = "Pending",
                            CreatedDate = DateTime.UtcNow,
                            IsDeleted = false,
                            ConsentFormUrl = $"https://yourdomain.com/consent/{Guid.NewGuid()}"
                        };

                        consents.Add(consent);
                        await _unitOfWork.GetRepositoryByEntity<VaccinationConsent>().AddAsync(consent);

                        try
                        {
                            await _unitOfWork.SaveChangesAsync(cancellationToken);
                            _logger.LogInformation("Lưu consent thành công cho học sinh ID: {StudentId}", student.Id);
                        }
                        catch (Exception saveEx)
                        {
                            _logger.LogError(saveEx, "Lỗi khi lưu consent cho học sinh ID: {StudentId}. InnerException: {InnerException}",
                                student.Id, saveEx.InnerException?.ToString() ?? "Không có InnerException");
                            throw;
                        }

                        var emailBody = GenerateConsentEmail(student, session, parent, consent.Id);
                        await _emailService.SendEmailAsync(parent.Email, "Yêu cầu đồng ý tiêm vaccine", emailBody);
                        _logger.LogInformation("Gửi email cho phụ huynh {ParentEmail} cho học sinh ID: {StudentId}", parent.Email, student.Id);
                    }

                    if (!consents.Any())
                    {
                        _logger.LogWarning("Không tạo được consent nào cho session ID: {SessionId}", sessionId);
                    }

                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    // Xóa cache cụ thể cho session detail và danh sách sessions
                    var sessionCacheKey = _cacheService.GenerateCacheKey("session_detail", sessionId.ToString());
                    await _cacheService.RemoveAsync(sessionCacheKey);
                    _logger.LogDebug("Đã xóa cache cụ thể cho session detail: {CacheKey}", sessionCacheKey);
                    await _cacheService.RemoveByPrefixAsync(SESSION_LIST_PREFIX);
                    _logger.LogDebug("Đã xóa cache danh sách sessions với prefix: {Prefix}", SESSION_LIST_PREFIX);

                    await InvalidateAllCachesAsync();

                    _logger.LogInformation("Duyệt buổi tiêm với ID: {SessionId}, tạo {ConsentCount} consents", sessionId, consents.Count);
                    return new BaseResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Duyệt buổi tiêm và gửi email thành công."
                    };
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi duyệt buổi tiêm: {SessionId}. InnerException: {InnerException}, StackTrace: {StackTrace}",
                    sessionId, ex.InnerException?.ToString() ?? "Không có InnerException", ex.StackTrace);
                return new BaseResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Lỗi duyệt buổi tiêm: {ex.Message}"
                };
            }
        }

        public async Task<BaseResponse<bool>> DeclineSessionAsync(
            Guid sessionId,
            string reason,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var session = await _unitOfWork.GetRepositoryByEntity<VaccinationSession>().GetQueryable()
                        .Include(vs => vs.Classes)
                        .Include(vs => vs.Consents)
                        .FirstOrDefaultAsync(vs => vs.Id == sessionId && !vs.IsDeleted, cancellationToken);

                    if (session == null)
                    {
                        return new BaseResponse<bool>
                        {
                            Success = false,
                            Message = "Buổi tiêm không tồn tại."
                        };
                    }

                    if (session.Status == "Scheduled" || session.Status == "Completed")
                    {
                        return new BaseResponse<bool>
                        {
                            Success = false,
                            Message = "Buổi tiêm đã được lên lịch hoặc hoàn thành, không thể từ chối."
                        };
                    }

                    if (string.IsNullOrWhiteSpace(reason))
                    {
                        return new BaseResponse<bool>
                        {
                            Success = false,
                            Message = "Lý do từ chối là bắt buộc."
                        };
                    }

                    var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("uid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var managerId))
                    {
                        throw new UnauthorizedAccessException("Không thể xác định ID quản lý.");
                    }

                    session.Status = "Declined";
                    session.DeclineReason = reason;
                    session.LastUpdatedBy = managerId.ToString();
                    session.LastUpdatedDate = DateTime.UtcNow;

                    foreach (var consent in session.Consents.Where(c => c.Status == "Pending"))
                    {
                        consent.Status = "Declined";
                        consent.ResponseDate = DateTime.UtcNow;
                        await _unitOfWork.GetRepositoryByEntity<VaccinationConsent>().UpdateAsync(consent);
                    }

                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    // Xóa cache cụ thể cho session detail và danh sách sessions
                    var sessionCacheKey = _cacheService.GenerateCacheKey("session_detail", sessionId.ToString());
                    await _cacheService.RemoveAsync(sessionCacheKey);
                    _logger.LogDebug("Đã xóa cache cụ thể cho session detail: {CacheKey}", sessionCacheKey);
                    await _cacheService.RemoveByPrefixAsync(SESSION_LIST_PREFIX);
                    _logger.LogDebug("Đã xóa cache danh sách sessions với prefix: {Prefix}", SESSION_LIST_PREFIX);
                    // Xóa cache consent status
                    foreach (var consent in session.Consents)
                    {
                        var consentCacheKey = _cacheService.GenerateCacheKey("parent_consent_status", sessionId.ToString(), consent.StudentId.ToString());
                        await _cacheService.RemoveAsync(consentCacheKey);
                        _logger.LogDebug("Đã xóa cache consent status: {CacheKey}", consentCacheKey);
                    }

                    await InvalidateAllCachesAsync();

                    _logger.LogInformation("Manager {ManagerId} đã từ chối buổi tiêm {SessionId} với lý do: {Reason}", managerId, sessionId, reason);
                    return new BaseResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Buổi tiêm đã được từ chối thành công."
                    };
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi từ chối buổi tiêm {SessionId}. InnerException: {InnerException}",
                    sessionId, ex.InnerException?.ToString() ?? "Không có InnerException");
                return new BaseResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Lỗi khi từ chối buổi tiêm: {ex.Message}"
                };
            }
        }

        public async Task<BaseResponse<bool>> FinalizeSessionAsync(Guid sessionId)
        {
            try
            {
                var session = await _unitOfWork.GetRepositoryByEntity<VaccinationSession>().GetQueryable()
                    .Include(vs => vs.Classes)
                    .ThenInclude(vsc => vsc.SchoolClass)
                    .FirstOrDefaultAsync(vs => vs.Id == sessionId && !vs.IsDeleted);

                if (session == null || session.Status != "WaitingForParentConsent")
                {
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "Buổi tiêm không tồn tại hoặc không đang chờ chốt."
                    };
                }

                var consents = await _unitOfWork.GetRepositoryByEntity<VaccinationConsent>().GetQueryable()
                    .Where(c => c.SessionId == sessionId)
                    .ToListAsync();

                var pendingConsents = consents.Where(c => c.Status == "Pending").ToList();
                foreach (var consent in pendingConsents)
                {
                    consent.Status = "Declined";
                    consent.ResponseDate = DateTime.UtcNow;
                    await _unitOfWork.GetRepositoryByEntity<VaccinationConsent>().UpdateAsync(consent);
                }

                if (pendingConsents.Any())
                {
                    await _unitOfWork.SaveChangesAsync();
                }

                session.Status = "Scheduled";
                await _unitOfWork.SaveChangesAsync();

                // Xóa cache cụ thể cho session detail và danh sách sessions
                var sessionCacheKey = _cacheService.GenerateCacheKey("session_detail", sessionId.ToString());
                await _cacheService.RemoveAsync(sessionCacheKey);
                _logger.LogDebug("Đã xóa cache cụ thể cho session detail: {CacheKey}", sessionCacheKey);
                await _cacheService.RemoveByPrefixAsync(SESSION_LIST_PREFIX);
                _logger.LogDebug("Đã xóa cache danh sách sessions với prefix: {Prefix}", SESSION_LIST_PREFIX);
                // Xóa cache consent status
                foreach (var consent in pendingConsents)
                {
                    var consentCacheKey = _cacheService.GenerateCacheKey("parent_consent_status", sessionId.ToString(), consent.StudentId.ToString());
                    await _cacheService.RemoveAsync(consentCacheKey);
                    _logger.LogDebug("Đã xóa cache consent status: {CacheKey}", consentCacheKey);
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
                _logger.LogError(ex, "Error finalizing vaccination session: {SessionId}", sessionId);
                return new BaseResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Lỗi chốt buổi tiêm: {ex.Message}"
                };
            }
        }

        public async Task<BaseResponse<bool>> ParentApproveAsync(
            Guid sessionId,
            Guid studentId,
            ParentApproveRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var validationResult = await _parentApproveValidator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = errors
                    };
                }

                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var consents = await _unitOfWork.GetRepositoryByEntity<VaccinationConsent>().GetQueryable()
                        .Include(c => c.Parent)
                        .Include(c => c.Student)
                        .Include(c => c.Session)
                            .ThenInclude(s => s.VaccineType)
                        .Where(c => c.SessionId == sessionId && !c.IsDeleted && c.ParentId != null)
                        .ToListAsync(cancellationToken);

                    var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("uid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var parentId))
                    {
                        throw new UnauthorizedAccessException("Không thể xác định ID phụ huynh.");
                    }

                    var consentToUpdate = consents.FirstOrDefault(c => c.ParentId == parentId && c.StudentId == studentId && c.Status == "Pending");

                    if (consentToUpdate == null)
                    {
                        var student = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                            .FirstOrDefaultAsync(u => u.Id == studentId && u.ParentId == parentId && u.UserRoles.Any(ur => ur.Role.Name == "STUDENT"), cancellationToken);
                        if (student == null)
                        {
                            return new BaseResponse<bool>
                            {
                                Success = false,
                                Message = "Không tìm thấy học sinh cho phụ huynh này."
                            };
                        }

                        var parent = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                            .FirstOrDefaultAsync(u => u.Id == parentId, cancellationToken);
                        var session = await _unitOfWork.GetRepositoryByEntity<VaccinationSession>().GetQueryable()
                            .Include(s => s.VaccineType)
                            .FirstOrDefaultAsync(s => s.Id == sessionId && !s.IsDeleted, cancellationToken);

                        if (parent == null || session == null)
                        {
                            return new BaseResponse<bool>
                            {
                                Success = false,
                                Message = "Dữ liệu phụ huynh hoặc buổi tiêm không hợp lệ."
                            };
                        }

                        consentToUpdate = new VaccinationConsent
                        {
                            Id = Guid.NewGuid(),
                            SessionId = sessionId,
                            StudentId = studentId,
                            ParentId = parentId,
                            Status = "Pending",
                            CreatedDate = DateTime.UtcNow,
                            IsDeleted = false,
                            ConsentFormUrl = "",
                            Parent = parent,
                            Student = student,
                            Session = session
                        };
                        await _unitOfWork.GetRepositoryByEntity<VaccinationConsent>().AddAsync(consentToUpdate);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        consents.Add(consentToUpdate);
                    }

                    consentToUpdate.Status = request.Status;
                    consentToUpdate.ResponseDate = DateTime.UtcNow;

                    await _unitOfWork.GetRepositoryByEntity<VaccinationConsent>().UpdateAsync(consentToUpdate);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    // Xóa cache cụ thể cho session detail, danh sách sessions và consent status
                    var sessionCacheKey = _cacheService.GenerateCacheKey("session_detail", sessionId.ToString());
                    await _cacheService.RemoveAsync(sessionCacheKey);
                    _logger.LogDebug("Đã xóa cache cụ thể cho session detail: {CacheKey}", sessionCacheKey);
                    await _cacheService.RemoveByPrefixAsync(SESSION_LIST_PREFIX);
                    _logger.LogDebug("Đã xóa cache danh sách sessions với prefix: {Prefix}", SESSION_LIST_PREFIX);
                    var consentCacheKey = _cacheService.GenerateCacheKey("parent_consent_status", sessionId.ToString(), studentId.ToString());
                    await _cacheService.RemoveAsync(consentCacheKey);
                    _logger.LogDebug("Đã xóa cache consent status: {CacheKey}", consentCacheKey);

                    await InvalidateAllCachesAsync();

                    var emailBody = GenerateConfirmationEmail(consentToUpdate);
                    await _emailService.SendEmailAsync(consentToUpdate.Parent.Email,
                        $"Xác nhận {request.Status} yêu cầu tiêm vaccine",
                        emailBody);

                    _logger.LogInformation("Phụ huynh {ParentId} đã {Status} yêu cầu đồng ý {ConsentId} cho học sinh {StudentId}",
                        parentId, request.Status, consentToUpdate.Id, studentId);

                    return new BaseResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = $"Yêu cầu đồng ý đã được {request.Status.ToLower()}."
                    };
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý yêu cầu đồng ý cho buổi tiêm {SessionId} và học sinh {StudentId}. InnerException: {InnerException}",
                    sessionId, studentId, ex.InnerException?.ToString() ?? "Không có InnerException");
                return new BaseResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Lỗi khi xử lý yêu cầu đồng ý: {ex.Message}"
                };
            }
        }

        public async Task<BaseResponse<bool>> AssignNurseToSessionAsync(
            AssignNurseToSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request.Assignments == null || !request.Assignments.Any())
                {
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "Danh sách phân công không được rỗng."
                    };
                }

                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var session = await _unitOfWork.GetRepositoryByEntity<VaccinationSession>().GetQueryable()
                        .FirstOrDefaultAsync(s => s.Id == request.SessionId && !s.IsDeleted, cancellationToken);

                    if (session == null)
                    {
                        return new BaseResponse<bool>
                        {
                            Success = false,
                            Message = "Buổi tiêm không tồn tại."
                        };
                    }

                    foreach (var assignmentRequest in request.Assignments)
                    {
                        var classExists = await _unitOfWork.GetRepositoryByEntity<VaccinationSessionClass>().GetQueryable()
                            .AnyAsync(vsc => vsc.SessionId == request.SessionId && vsc.ClassId == assignmentRequest.ClassId && !vsc.IsDeleted, cancellationToken);

                        if (!classExists)
                        {
                            return new BaseResponse<bool>
                            {
                                Success = false,
                                Message = $"Lớp học {assignmentRequest.ClassId} không thuộc buổi tiêm này."
                            };
                        }

                        var nurse = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                            .FirstOrDefaultAsync(u => u.Id == assignmentRequest.NurseId &&
                                                  u.UserRoles.Any(ur => ur.Role.Name == "SCHOOLNURSE") &&
                                                  !u.IsDeleted, cancellationToken);

                        if (nurse == null)
                        {
                            return new BaseResponse<bool>
                            {
                                Success = false,
                                Message = $"Y tá {assignmentRequest.NurseId} không tồn tại hoặc không có vai SCHOOLNURSE."
                            };
                        }

                        var existingAssignment = await _unitOfWork.GetRepositoryByEntity<VaccinationAssignment>().GetQueryable()
                            .FirstOrDefaultAsync(a => a.SessionId == request.SessionId && a.ClassId == assignmentRequest.ClassId && !a.IsDeleted, cancellationToken);

                        if (existingAssignment != null)
                        {
                            return new BaseResponse<bool>
                            {
                                Success = false,
                                Message = $"Lớp {assignmentRequest.ClassId} đã được phân công y tá."
                            };
                        }

                        var assignment = new VaccinationAssignment
                        {
                            Id = Guid.NewGuid(),
                            SessionId = request.SessionId,
                            ClassId = assignmentRequest.ClassId,
                            NurseId = assignmentRequest.NurseId,
                            AssignedDate = DateTime.UtcNow,
                            CreatedDate = DateTime.UtcNow,
                            IsDeleted = false
                        };

                        await _unitOfWork.GetRepositoryByEntity<VaccinationAssignment>().AddAsync(assignment);
                        _logger.LogInformation("Phân công y tá {NurseId} cho lớp {ClassId} trong buổi tiêm {SessionId}",
                            assignmentRequest.NurseId, assignmentRequest.ClassId, request.SessionId);
                    }

                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    // Xóa cache cụ thể cho session detail và danh sách sessions
                    var sessionCacheKey = _cacheService.GenerateCacheKey("session_detail", request.SessionId.ToString());
                    await _cacheService.RemoveAsync(sessionCacheKey);
                    _logger.LogDebug("Đã xóa cache cụ thể cho session detail: {CacheKey}", sessionCacheKey);
                    await _cacheService.RemoveByPrefixAsync(SESSION_LIST_PREFIX);
                    _logger.LogDebug("Đã xóa cache danh sách sessions với prefix: {Prefix}", SESSION_LIST_PREFIX);

                    await InvalidateAllCachesAsync();

                    return new BaseResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Phân công y tá cho tất cả các lớp thành công."
                    };
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi phân công y tá cho buổi tiêm {SessionId}. InnerException: {InnerException}",
                    request.SessionId, ex.InnerException?.ToString() ?? "Không có InnerException");
                return new BaseResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Lỗi khi phân công y tá: {ex.Message}"
                };
            }
        }

        public async Task<BaseResponse<bool>> ReassignNurseToSessionAsync(
            Guid sessionId,
            ReAssignNurseToSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request.Assignments == null || !request.Assignments.Any())
                {
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "Danh sách phân công không được rỗng."
                    };
                }

                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var session = await _unitOfWork.GetRepositoryByEntity<VaccinationSession>().GetQueryable()
                        .FirstOrDefaultAsync(s => s.Id == sessionId && !s.IsDeleted, cancellationToken);

                    if (session == null)
                    {
                        return new BaseResponse<bool>
                        {
                            Success = false,
                            Message = "Buổi tiêm không tồn tại."
                        };
                    }

                    // Xóa các phân công y tá hiện tại cho buổi tiêm này
                    var existingAssignments = await _unitOfWork.GetRepositoryByEntity<VaccinationAssignment>().GetQueryable()
                        .Where(a => a.SessionId == sessionId && !a.IsDeleted)
                        .ToListAsync(cancellationToken);

                    foreach (var assignment in existingAssignments)
                    {
                        assignment.IsDeleted = true;
                        assignment.LastUpdatedDate = DateTime.UtcNow;
                        await _unitOfWork.GetRepositoryByEntity<VaccinationAssignment>().UpdateAsync(assignment);
                    }

                    // Thêm các phân công mới
                    foreach (var assignmentRequest in request.Assignments)
                    {
                        var classExists = await _unitOfWork.GetRepositoryByEntity<VaccinationSessionClass>().GetQueryable()
                            .AnyAsync(vsc => vsc.SessionId == sessionId && vsc.ClassId == assignmentRequest.ClassId && !vsc.IsDeleted, cancellationToken);

                        if (!classExists)
                        {
                            return new BaseResponse<bool>
                            {
                                Success = false,
                                Message = $"Lớp học {assignmentRequest.ClassId} không thuộc buổi tiêm này."
                            };
                        }

                        var nurse = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                            .FirstOrDefaultAsync(u => u.Id == assignmentRequest.NurseId &&
                                                  u.UserRoles.Any(ur => ur.Role.Name == "SCHOOLNURSE") &&
                                                  !u.IsDeleted, cancellationToken);

                        if (nurse == null)
                        {
                            return new BaseResponse<bool>
                            {
                                Success = false,
                                Message = $"Y tá {assignmentRequest.NurseId} không tồn tại hoặc không có vai SCHOOLNURSE."
                            };
                        }

                        var newAssignment = new VaccinationAssignment
                        {
                            Id = Guid.NewGuid(),
                            SessionId = sessionId,
                            ClassId = assignmentRequest.ClassId,
                            NurseId = assignmentRequest.NurseId,
                            AssignedDate = DateTime.UtcNow,
                            CreatedDate = DateTime.UtcNow,
                            IsDeleted = false
                        };

                        await _unitOfWork.GetRepositoryByEntity<VaccinationAssignment>().AddAsync(newAssignment);
                        _logger.LogInformation("Tái phân công y tá {NurseId} cho lớp {ClassId} trong buổi tiêm {SessionId}",
                            assignmentRequest.NurseId, assignmentRequest.ClassId, sessionId);
                    }

                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    // Xóa cache cụ thể cho session detail, danh sách sessions và nurse assignment statuses
                    var sessionCacheKey = _cacheService.GenerateCacheKey("session_detail", sessionId.ToString());
                    await _cacheService.RemoveAsync(sessionCacheKey);
                    _logger.LogDebug("Đã xóa cache cụ thể cho session detail: {CacheKey}", sessionCacheKey);
                    await _cacheService.RemoveByPrefixAsync(SESSION_LIST_PREFIX);
                    _logger.LogDebug("Đã xóa cache danh sách sessions với prefix: {Prefix}", SESSION_LIST_PREFIX);
                    var nurseAssignmentCacheKey = _cacheService.GenerateCacheKey("nurse_assignment_status", sessionId.ToString());
                    await _cacheService.RemoveAsync(nurseAssignmentCacheKey);
                    _logger.LogDebug("Đã xóa cache nurse assignment statuses: {CacheKey}", nurseAssignmentCacheKey);

                    await InvalidateAllCachesAsync();

                    return new BaseResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Tái phân công y tá cho tất cả các lớp thành công."
                    };
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tái phân công y tá cho buổi tiêm {SessionId}. InnerException: {InnerException}",
                    sessionId, ex.InnerException?.ToString() ?? "Không có InnerException");
                return new BaseResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Lỗi khi tái phân công y tá: {ex.Message}"
                };
            }
        }

        public async Task<BaseResponse<bool>> MarkStudentVaccinatedAsync(
            Guid sessionId, MarkStudentVaccinatedRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Bắt đầu đánh dấu tiêm chủng cho học sinh {StudentId} trong buổi {SessionId}", request.StudentId, sessionId);

                var validationResult = await _markStudentVaccinatedValidator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning("Validation failed: {Errors}", errors);
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = errors
                    };
                }

                var session = await _unitOfWork.GetRepositoryByEntity<VaccinationSession>().GetQueryable()
                    .Include(vs => vs.Classes)
                    .ThenInclude(vsc => vsc.SchoolClass)
                    .Include(vs => vs.Consents)
                    .FirstOrDefaultAsync(vs => vs.Id == sessionId && !vs.IsDeleted, cancellationToken);

                if (session == null)
                {
                    _logger.LogWarning("Buổi tiêm {SessionId} không tồn tại hoặc đã bị xóa.", sessionId);
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "Buổi tiêm không tồn tại hoặc đã bị xóa."
                    };
                }

                if (session.Status != "Scheduled")
                {
                    _logger.LogWarning("Buổi tiêm {SessionId} không ở trạng thái Scheduled.", sessionId);
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "Buổi tiêm không trong trạng thái Scheduled."
                    };
                }

                var consent = session.Consents.FirstOrDefault(c => c.StudentId == request.StudentId && c.Status == "Confirmed" && !c.IsDeleted);
                if (consent == null)
                {
                    _logger.LogWarning("Không tìm thấy yêu cầu đồng ý hợp lệ cho học sinh {StudentId} trong buổi {SessionId}.", request.StudentId, sessionId);
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "Không tìm thấy yêu cầu đồng ý hợp lệ cho học sinh."
                    };
                }

                var student = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                    .FirstOrDefaultAsync(u => u.Id == request.StudentId && u.UserRoles.Any(ur => ur.Role.Name == "STUDENT"), cancellationToken);
                if (student == null)
                {
                    _logger.LogWarning("Học sinh {StudentId} không tồn tại hoặc không có vai trò STUDENT.", request.StudentId);
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "Học sinh không tồn tại."
                    };
                }

                var medicalRecord = await _unitOfWork.GetRepositoryByEntity<MedicalRecord>().GetQueryable()
                    .FirstOrDefaultAsync(mr => mr.UserId == request.StudentId && !mr.IsDeleted, cancellationToken);
                if (medicalRecord == null)
                {
                    _logger.LogWarning("Không tìm thấy hồ sơ y tế cho học sinh {StudentId}.", request.StudentId);
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "Không tìm thấy hồ sơ y tế cho học sinh."
                    };
                }

                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("uid")?.Value;
                Guid? nurseId = Guid.TryParse(userIdClaim, out var parsedId) ? parsedId : (Guid?)null;
                var adminName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "SCHOOLNURSE";

                var vaccinationRecord = await _unitOfWork.GetRepositoryByEntity<VaccinationRecord>().GetQueryable()
                    .FirstOrDefaultAsync(vr => vr.MedicalRecordId == medicalRecord.Id &&
                                            vr.VaccinationTypeId == session.VaccineTypeId &&
                                            !vr.IsDeleted, cancellationToken);

                if (vaccinationRecord == null)
                {
                    _logger.LogInformation("Tạo mới bản ghi tiêm chủng cho học sinh {StudentId}.", request.StudentId);
                    vaccinationRecord = new VaccinationRecord
                    {
                        Id = Guid.NewGuid(),
                        UserId = request.StudentId,
                        MedicalRecordId = medicalRecord.Id,
                        VaccinationTypeId = session.VaccineTypeId,
                        DoseNumber = 1,
                        AdministeredDate = DateTime.UtcNow,
                        AdministeredByUserId = nurseId,
                        AdministeredBy = adminName,
                        BatchNumber = "BATCH-" + DateTime.UtcNow.ToString("yyyyMMdd"),
                        NoteAfterSession = request.NoteAfterSession ?? string.Empty,
                        SessionId = sessionId,
                        VaccinationStatus = "Completed",
                        Symptoms = request.Symptoms ?? string.Empty,
                        Notes = string.Empty,
                        CreatedDate = DateTime.UtcNow,
                        IsDeleted = false
                    };
                    await _unitOfWork.GetRepositoryByEntity<VaccinationRecord>().AddAsync(vaccinationRecord);
                }
                else
                {
                    _logger.LogInformation("Cập nhật bản ghi tiêm chủng cho học sinh {StudentId}.", request.StudentId);
                    vaccinationRecord.DoseNumber += 1;
                    vaccinationRecord.AdministeredDate = DateTime.UtcNow;
                    vaccinationRecord.AdministeredByUserId = nurseId;
                    vaccinationRecord.AdministeredBy = adminName;
                    vaccinationRecord.BatchNumber = "BATCH-" + DateTime.UtcNow.ToString("yyyyMMdd");
                    vaccinationRecord.SessionId = sessionId;
                    vaccinationRecord.VaccinationStatus = "Completed";
                    vaccinationRecord.Symptoms = request.Symptoms ?? string.Empty;
                    vaccinationRecord.NoteAfterSession = request.NoteAfterSession ?? string.Empty;
                    vaccinationRecord.Notes = vaccinationRecord.Notes ?? string.Empty;
                    await _unitOfWork.GetRepositoryByEntity<VaccinationRecord>().UpdateAsync(vaccinationRecord);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Xóa cache cụ thể cho session detail, danh sách sessions, consent status và vaccination result
                var sessionCacheKey = _cacheService.GenerateCacheKey("session_detail", sessionId.ToString());
                await _cacheService.RemoveAsync(sessionCacheKey);
                _logger.LogDebug("Đã xóa cache cụ thể cho session detail: {CacheKey}", sessionCacheKey);
                await _cacheService.RemoveByPrefixAsync(SESSION_LIST_PREFIX);
                _logger.LogDebug("Đã xóa cache danh sách sessions với prefix: {Prefix}", SESSION_LIST_PREFIX);
                var consentCacheKey = _cacheService.GenerateCacheKey("parent_consent_status", sessionId.ToString(), request.StudentId.ToString());
                await _cacheService.RemoveAsync(consentCacheKey);
                _logger.LogDebug("Đã xóa cache consent status: {CacheKey}", consentCacheKey);
                var vaccinationResultCacheKey = _cacheService.GenerateCacheKey("student_vaccination_result", sessionId.ToString(), request.StudentId.ToString());
                await _cacheService.RemoveAsync(vaccinationResultCacheKey);
                _logger.LogDebug("Đã xóa cache vaccination result: {CacheKey}", vaccinationResultCacheKey);

                await InvalidateAllCachesAsync();

                _logger.LogInformation("Đánh dấu học sinh {StudentId} đã được tiêm trong buổi {SessionId} với triệu chứng: {Symptoms}, ghi chú sau khi tiêm: {NoteAfterSession}",
                    request.StudentId, sessionId, request.Symptoms, request.NoteAfterSession);
                return new BaseResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Đánh dấu tiêm chủng thành công."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đánh dấu tiêm chủng cho học sinh {StudentId} trong buổi {SessionId}. Inner exception: {InnerException}",
                    request.StudentId, sessionId, ex.InnerException?.Message);
                return new BaseResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Lỗi đánh dấu tiêm chủng: {ex.Message}. Inner exception: {ex.InnerException?.Message}"
                };
            }
        }

        public async Task<BaseResponse<bool>> MarkStudentNotVaccinatedAsync(
    Guid sessionId,
    MarkStudentNotVaccinatedRequest request,
    CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Bắt đầu đánh dấu không tiêm cho học sinh {StudentId} trong buổi {SessionId}", request.StudentId, sessionId);

                // Validate request
                var validator = new MarkStudentNotVaccinatedRequestValidator();
                var validationResult = await validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning("Validation failed: {Errors}", errors);
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = errors
                    };
                }

                var session = await _unitOfWork.GetRepositoryByEntity<VaccinationSession>().GetQueryable()
                    .Include(vs => vs.Consents)
                    .FirstOrDefaultAsync(vs => vs.Id == sessionId && !vs.IsDeleted, cancellationToken);

                if (session == null)
                {
                    _logger.LogWarning("Buổi tiêm {SessionId} không tồn tại hoặc đã bị xóa.", sessionId);
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "Buổi tiêm không tồn tại hoặc đã bị xóa."
                    };
                }

                if (session.Status != "Scheduled")
                {
                    _logger.LogWarning("Buổi tiêm {SessionId} không ở trạng thái Scheduled.", sessionId);
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "Buổi tiêm không trong trạng thái Scheduled."
                    };
                }

                var consent = session.Consents.FirstOrDefault(c => c.StudentId == request.StudentId && c.Status == "Confirmed" && !c.IsDeleted);
                if (consent == null)
                {
                    _logger.LogWarning("Không tìm thấy yêu cầu đồng ý hợp lệ cho học sinh {StudentId} trong buổi {SessionId}.", request.StudentId, sessionId);
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "Không tìm thấy yêu cầu đồng ý hợp lệ cho học sinh."
                    };
                }

                var student = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                    .FirstOrDefaultAsync(u => u.Id == request.StudentId && u.UserRoles.Any(ur => ur.Role.Name == "STUDENT"), cancellationToken);
                if (student == null)
                {
                    _logger.LogWarning("Học sinh {StudentId} không tồn tại hoặc không có vai trò STUDENT.", request.StudentId);
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "Học sinh không tồn tại."
                    };
                }

                var medicalRecord = await _unitOfWork.GetRepositoryByEntity<MedicalRecord>().GetQueryable()
                    .FirstOrDefaultAsync(mr => mr.UserId == request.StudentId && !mr.IsDeleted, cancellationToken);
                if (medicalRecord == null)
                {
                    _logger.LogWarning("Không tìm thấy hồ sơ y tế cho học sinh {StudentId}.", request.StudentId);
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "Không tìm thấy hồ sơ y tế cho học sinh."
                    };
                }

                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("uid")?.Value;
                Guid? nurseId = Guid.TryParse(userIdClaim, out var parsedId) ? parsedId : (Guid?)null;
                var adminName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "SCHOOLNURSE";

                var vaccinationRecord = await _unitOfWork.GetRepositoryByEntity<VaccinationRecord>().GetQueryable()
                    .FirstOrDefaultAsync(vr => vr.MedicalRecordId == medicalRecord.Id &&
                                            vr.VaccinationTypeId == session.VaccineTypeId &&
                                            !vr.IsDeleted, cancellationToken);

                if (vaccinationRecord == null)
                {
                    _logger.LogInformation("Tạo mới bản ghi không tiêm cho học sinh {StudentId}.", request.StudentId);
                    vaccinationRecord = new VaccinationRecord
                    {
                        Id = Guid.NewGuid(),
                        UserId = request.StudentId,
                        MedicalRecordId = medicalRecord.Id,
                        VaccinationTypeId = session.VaccineTypeId,
                        DoseNumber = 0, // Đánh dấu không tiêm
                        AdministeredDate = DateTime.UtcNow,
                        AdministeredByUserId = nurseId,
                        AdministeredBy = adminName,
                        BatchNumber = "NOT_VACCINATED", // Giá trị mặc định khi không tiêm
                        NoteAfterSession = request.NoteAfterSession ?? string.Empty,
                        SessionId = sessionId,
                        VaccinationStatus = "NotVaccinated",
                        Symptoms = string.Empty,
                        Notes = request.Reason, // Ghi lý do không tiêm
                        CreatedDate = DateTime.UtcNow,
                        IsDeleted = false
                    };
                    await _unitOfWork.GetRepositoryByEntity<VaccinationRecord>().AddAsync(vaccinationRecord);
                }
                else
                {
                    _logger.LogInformation("Cập nhật bản ghi không tiêm cho học sinh {StudentId}.", request.StudentId);
                    vaccinationRecord.DoseNumber = 0;
                    vaccinationRecord.AdministeredDate = DateTime.UtcNow;
                    vaccinationRecord.AdministeredByUserId = nurseId;
                    vaccinationRecord.AdministeredBy = adminName;
                    vaccinationRecord.BatchNumber = "NOT_VACCINATED"; // Cập nhật giá trị mặc định
                    vaccinationRecord.SessionId = sessionId;
                    vaccinationRecord.VaccinationStatus = "NotVaccinated";
                    vaccinationRecord.Symptoms = string.Empty;
                    vaccinationRecord.NoteAfterSession = request.NoteAfterSession ?? string.Empty;
                    vaccinationRecord.Notes = request.Reason; // Cập nhật lý do không tiêm
                    await _unitOfWork.GetRepositoryByEntity<VaccinationRecord>().UpdateAsync(vaccinationRecord);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Gửi email thông báo cho phụ huynh
                var parent = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                    .FirstOrDefaultAsync(u => u.Id == student.ParentId && u.UserRoles.Any(ur => ur.Role.Name == "PARENT"), cancellationToken);
                if (parent != null)
                {
                    var emailBody = $@"
                <h2>Thông báo không tiêm vaccine</h2>
                <p>Kính gửi {parent.FullName},</p>
                <p>Học sinh {student.FullName} không được tiêm vaccine trong buổi tiêm {session.SessionName} vì lý do:</p>
                <p><strong>{request.Reason}</strong></p>
                <p>Ghi chú bổ sung: {request.NoteAfterSession ?? "Không có"}</p>
                <p>Thời gian: {DateTime.UtcNow:dd/MM/yyyy HH:mm}</p>
                <p>Vui lòng liên hệ trường nếu cần thêm thông tin.</p>";
                    await _emailService.SendEmailAsync(parent.Email, "Thông báo không tiêm vaccine", emailBody);
                    _logger.LogInformation("Gửi email thông báo không tiêm cho phụ huynh {ParentEmail} của học sinh {StudentId}", parent.Email, request.StudentId);
                }

                // Xóa cache
                var sessionCacheKey = _cacheService.GenerateCacheKey("session_detail", sessionId.ToString());
                await _cacheService.RemoveAsync(sessionCacheKey);
                _logger.LogDebug("Đã xóa cache cụ thể cho session detail: {CacheKey}", sessionCacheKey);
                await _cacheService.RemoveByPrefixAsync("vaccination_sessions_list");
                _logger.LogDebug("Đã xóa cache danh sách sessions với prefix: {Prefix}", "vaccination_sessions_list");
                var consentCacheKey = _cacheService.GenerateCacheKey("parent_consent_status", sessionId.ToString(), request.StudentId.ToString());
                await _cacheService.RemoveAsync(consentCacheKey);
                _logger.LogDebug("Đã xóa cache consent status: {CacheKey}", consentCacheKey);
                var vaccinationResultCacheKey = _cacheService.GenerateCacheKey("student_vaccination_result", sessionId.ToString(), request.StudentId.ToString());
                await _cacheService.RemoveAsync(vaccinationResultCacheKey);
                _logger.LogDebug("Đã xóa cache vaccination result: {CacheKey}", vaccinationResultCacheKey);

                await InvalidateAllCachesAsync();

                _logger.LogInformation("Đánh dấu không tiêm cho học sinh {StudentId} trong buổi {SessionId} với lý do: {Reason}", request.StudentId, sessionId, request.Reason);
                return new BaseResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Đánh dấu không tiêm thành công."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đánh dấu không tiêm cho học sinh {StudentId} trong buổi {SessionId}. Inner exception: {InnerException}",
                    request.StudentId, sessionId, ex.InnerException?.Message);
                return new BaseResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Lỗi đánh dấu không tiêm: {ex.Message}. Inner exception: {ex.InnerException?.Message}"
                };
            }
        }

        public async Task<BaseResponse<ParentConsentStatusResponse>> GetParentConsentStatusAsync(
            Guid sessionId, Guid studentId, CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = _cacheService.GenerateCacheKey("parent_consent_status", sessionId.ToString(), studentId.ToString());
                var cachedResult = await _cacheService.GetAsync<BaseResponse<ParentConsentStatusResponse>>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogDebug("Parent consent status found in cache for sessionId: {SessionId}, studentId: {StudentId}", sessionId, studentId);
                    return cachedResult;
                }

                var session = await _unitOfWork.GetRepositoryByEntity<VaccinationSession>().GetQueryable()
                    .FirstOrDefaultAsync(vs => vs.Id == sessionId && !vs.IsDeleted, cancellationToken);

                if (session == null)
                {
                    return BaseResponse<ParentConsentStatusResponse>.ErrorResult("Buổi tiêm không tồn tại.");
                }

                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("uid")?.Value;
                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    return BaseResponse<ParentConsentStatusResponse>.ErrorResult("Không thể xác định ID người dùng.");
                }

                var user = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

                if (user == null)
                {
                    return BaseResponse<ParentConsentStatusResponse>.ErrorResult("Người dùng không tồn tại.");
                }

                var isParent = user.UserRoles.Any(ur => ur.Role.Name == "PARENT");
                if (isParent)
                {
                    var student = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                        .FirstOrDefaultAsync(u => u.Id == studentId && u.ParentId == userId && u.UserRoles.Any(ur => ur.Role.Name == "STUDENT") && !u.IsDeleted, cancellationToken);
                    if (student == null)
                    {
                        return BaseResponse<ParentConsentStatusResponse>.ErrorResult("Học sinh không liên quan đến phụ huynh này hoặc không tồn tại.");
                    }
                }
                else if (!user.UserRoles.Any(ur => ur.Role.Name == "SCHOOLNURSE" || ur.Role.Name == "MANAGER"))
                {
                    return BaseResponse<ParentConsentStatusResponse>.ErrorResult("Không có quyền truy cập.");
                }

                var studentCheck = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                    .FirstOrDefaultAsync(u => u.Id == studentId && u.UserRoles.Any(ur => ur.Role.Name == "STUDENT") && !u.IsDeleted, cancellationToken);
                if (studentCheck == null)
                {
                    return BaseResponse<ParentConsentStatusResponse>.ErrorResult("Học sinh không tồn tại.");
                }

                var consent = await _unitOfWork.GetRepositoryByEntity<VaccinationConsent>().GetQueryable()
                    .Include(c => c.Student)
                    .Include(c => c.Parent)
                    .FirstOrDefaultAsync(c => c.SessionId == sessionId && c.StudentId == studentId && !c.IsDeleted, cancellationToken);

                if (consent == null)
                {
                    return BaseResponse<ParentConsentStatusResponse>.ErrorResult("Không tìm thấy yêu cầu đồng ý cho học sinh trong buổi tiêm này.");
                }

                var response = _mapper.Map<ParentConsentStatusResponse>(consent);

                var result = BaseResponse<ParentConsentStatusResponse>.SuccessResult(
                    response, "Lấy trạng thái đồng ý của phụ huynh thành công.");

                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
                await _cacheService.AddToTrackingSetAsync(cacheKey, "parent_consent_status_cache_keys");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving parent consent status for sessionId: {SessionId}, studentId: {StudentId}", sessionId, studentId);
                return BaseResponse<ParentConsentStatusResponse>.ErrorResult($"Lỗi lấy trạng thái đồng ý: {ex.Message}");
            }
        }

        public async Task<BaseResponse<StudentVaccinationResultResponse>> GetStudentVaccinationResultAsync(
            Guid sessionId, Guid studentId, CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = _cacheService.GenerateCacheKey("student_vaccination_result", sessionId.ToString(), studentId.ToString());
                var cachedResult = await _cacheService.GetAsync<BaseResponse<StudentVaccinationResultResponse>>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogDebug("Student vaccination result found in cache for sessionId: {SessionId}, studentId: {StudentId}", sessionId, studentId);
                    return cachedResult;
                }

                var session = await _unitOfWork.GetRepositoryByEntity<VaccinationSession>().GetQueryable()
                    .Include(vs => vs.VaccineType)
                    .FirstOrDefaultAsync(vs => vs.Id == sessionId && !vs.IsDeleted, cancellationToken);

                if (session == null)
                {
                    return BaseResponse<StudentVaccinationResultResponse>.ErrorResult("Buổi tiêm không tồn tại.");
                }

                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("uid")?.Value;
                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    return BaseResponse<StudentVaccinationResultResponse>.ErrorResult("Không thể xác định ID người dùng.");
                }

                var user = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

                if (user == null)
                {
                    return BaseResponse<StudentVaccinationResultResponse>.ErrorResult("Người dùng không tồn tại.");
                }

                var isParent = user.UserRoles.Any(ur => ur.Role.Name == "PARENT");
                if (isParent)
                {
                    var student = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                        .FirstOrDefaultAsync(u => u.Id == studentId && u.ParentId == userId && u.UserRoles.Any(ur => ur.Role.Name == "STUDENT") && !u.IsDeleted, cancellationToken);
                    if (student == null)
                    {
                        return BaseResponse<StudentVaccinationResultResponse>.ErrorResult("Học sinh không liên quan đến phụ huynh này hoặc không tồn tại.");
                    }
                }
                else if (!user.UserRoles.Any(ur => ur.Role.Name == "SCHOOLNURSE" || ur.Role.Name == "MANAGER"))
                {
                    return BaseResponse<StudentVaccinationResultResponse>.ErrorResult("Không có quyền truy cập.");
                }

                var studentCheck = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                    .FirstOrDefaultAsync(u => u.Id == studentId && u.UserRoles.Any(ur => ur.Role.Name == "STUDENT") && !u.IsDeleted, cancellationToken);
                if (studentCheck == null)
                {
                    return BaseResponse<StudentVaccinationResultResponse>.ErrorResult("Học sinh không tồn tại.");
                }

                var vaccinationRecord = await _unitOfWork.GetRepositoryByEntity<VaccinationRecord>().GetQueryable()
                    .Include(vr => vr.Student)
                    .Include(vr => vr.VaccinationType)
                    .Include(vr => vr.AdministeredByUser)
                    .FirstOrDefaultAsync(vr => vr.SessionId == sessionId && vr.UserId == studentId && !vr.IsDeleted, cancellationToken);

                if (vaccinationRecord == null)
                {
                    return BaseResponse<StudentVaccinationResultResponse>.ErrorResult("Không tìm thấy bản ghi tiêm chủng cho học sinh trong buổi tiêm này.");
                }

                var response = _mapper.Map<StudentVaccinationResultResponse>(vaccinationRecord);

                var result = BaseResponse<StudentVaccinationResultResponse>.SuccessResult(
                    response, "Lấy kết quả tiêm chủng của học sinh thành công.");

                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
                await _cacheService.AddToTrackingSetAsync(cacheKey, "student_vaccination_result_cache_keys");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving student vaccination result for sessionId: {SessionId}, studentId: {StudentId}", sessionId, studentId);
                return BaseResponse<StudentVaccinationResultResponse>.ErrorResult($"Lỗi lấy kết quả tiêm chủng: {ex.Message}");
            }
        }

        public async Task<BaseListResponse<NurseAssignmentStatusResponse>> GetNurseAssignmentStatusesAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = _cacheService.GenerateCacheKey(
                    "nurse_assignment_status",
                    sessionId.ToString()
                );

                var cachedResult = await _cacheService.GetAsync<BaseListResponse<NurseAssignmentStatusResponse>>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogDebug("Nurse assignment statuses found in cache with key: {CacheKey}", cacheKey);
                    return cachedResult;
                }

                var session = await _unitOfWork.GetRepositoryByEntity<VaccinationSession>().GetQueryable()
                    .FirstOrDefaultAsync(vs => vs.Id == sessionId && !vs.IsDeleted, cancellationToken);

                if (session == null)
                {
                    _logger.LogWarning("Session not found for sessionId: {SessionId}", sessionId);
                    return BaseListResponse<NurseAssignmentStatusResponse>.ErrorResult("Buổi tiêm không tồn tại.");
                }

                var nurses = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                    .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "SCHOOLNURSE") && !u.IsDeleted)
                    .ToListAsync(cancellationToken);

                var assignments = await _unitOfWork.GetRepositoryByEntity<VaccinationAssignment>().GetQueryable()
                    .Include(a => a.SchoolClass)
                    .Where(a => a.SessionId == sessionId && !a.IsDeleted)
                    .ToListAsync(cancellationToken);

                var responses = nurses.Select(nurse => new NurseAssignmentStatusResponse
                {
                    NurseId = nurse.Id,
                    NurseName = nurse.FullName,
                    IsAssigned = assignments.Any(a => a.NurseId == nurse.Id),
                    AssignedClassId = assignments.FirstOrDefault(a => a.NurseId == nurse.Id)?.ClassId,
                    AssignedClassName = assignments.FirstOrDefault(a => a.NurseId == nurse.Id)?.SchoolClass?.Name ?? "Chưa phân công"
                }).ToList();

                var result = BaseListResponse<NurseAssignmentStatusResponse>.SuccessResult(
                    responses,
                    responses.Count,
                    responses.Count,
                    1,
                    "Lấy danh sách trạng thái phân công y tá thành công.");

                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
                await _cacheService.AddToTrackingSetAsync(cacheKey, "nurse_assignment_status_cache_keys");
                _logger.LogDebug("Cached nurse assignment statuses with key: {CacheKey}", cacheKey);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving nurse assignment statuses for sessionId: {SessionId}", sessionId);
                return BaseListResponse<NurseAssignmentStatusResponse>.ErrorResult("Lỗi lấy trạng thái phân công y tá.");
            }
        }

        public async Task<BaseResponse<bool>> CompleteSessionAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var session = await _unitOfWork.GetRepositoryByEntity<VaccinationSession>().GetQueryable()
                        .Include(vs => vs.Consents)
                        .FirstOrDefaultAsync(vs => vs.Id == sessionId && !vs.IsDeleted, cancellationToken);

                    if (session == null)
                    {
                        return new BaseResponse<bool>
                        {
                            Success = false,
                            Message = "Buổi tiêm không tồn tại."
                        };
                    }

                    if (session.Status != "Scheduled")
                    {
                        return new BaseResponse<bool>
                        {
                            Success = false,
                            Message = "Buổi tiêm phải ở trạng thái Scheduled để hoàn tất."
                        };
                    }

                    var consents = session.Consents.Where(c => c.Status == "Confirmed").ToList();
                    var vaccinatedStudents = await _unitOfWork.GetRepositoryByEntity<VaccinationRecord>().GetQueryable()
                        .Where(vr => vr.SessionId == sessionId && vr.VaccinationStatus == "Completed" && !vr.IsDeleted)
                        .Select(vr => vr.UserId)
                        .ToListAsync(cancellationToken);

                    if (consents.Any() && !consents.All(c => vaccinatedStudents.Contains(c.StudentId)))
                    {
                        return new BaseResponse<bool>
                        {
                            Success = false,
                            Message = "Tất cả học sinh đã xác nhận phải được đánh dấu tiêm trước khi hoàn tất."
                        };
                    }

                    var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("uid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var managerId))
                    {
                        throw new UnauthorizedAccessException("Không thể xác định ID quản lý.");
                    }

                    session.Status = "Completed";
                    session.LastUpdatedBy = managerId.ToString();
                    session.LastUpdatedDate = DateTime.UtcNow;

                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    // Xóa cache cụ thể cho session detail và danh sách sessions
                    var sessionCacheKey = _cacheService.GenerateCacheKey("session_detail", sessionId.ToString());
                    await _cacheService.RemoveAsync(sessionCacheKey);
                    _logger.LogDebug("Đã xóa cache cụ thể cho session detail: {CacheKey}", sessionCacheKey);
                    await _cacheService.RemoveByPrefixAsync(SESSION_LIST_PREFIX);
                    _logger.LogDebug("Đã xóa cache danh sách sessions với prefix: {Prefix}", SESSION_LIST_PREFIX);
                    // Xóa cache consent status và vaccination result
                    foreach (var consent in session.Consents)
                    {
                        var consentCacheKey = _cacheService.GenerateCacheKey("parent_consent_status", sessionId.ToString(), consent.StudentId.ToString());
                        await _cacheService.RemoveAsync(consentCacheKey);
                        _logger.LogDebug("Đã xóa cache consent status: {CacheKey}", consentCacheKey);
                        var vaccinationResultCacheKey = _cacheService.GenerateCacheKey("student_vaccination_result", sessionId.ToString(), consent.StudentId.ToString());
                        await _cacheService.RemoveAsync(vaccinationResultCacheKey);
                        _logger.LogDebug("Đã xóa cache vaccination result: {CacheKey}", vaccinationResultCacheKey);
                    }

                    await InvalidateAllCachesAsync();

                    _logger.LogInformation("Manager {ManagerId} đã hoàn tất buổi tiêm {SessionId}", managerId, sessionId);
                    return new BaseResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Hoàn tất buổi tiêm thành công."
                    };
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hoàn tất buổi tiêm {SessionId}. InnerException: {InnerException}",
                    sessionId, ex.InnerException?.ToString() ?? "Không có InnerException");
                return new BaseResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Lỗi hoàn tất buổi tiêm: {ex.Message}"
                };
            }
        }

        #endregion

        #region Helper Methods

        private VaccinationSessionResponse MapToSessionResponse(VaccinationSession session)
        {
            return new VaccinationSessionResponse
            {
                Id = session.Id,
                VaccineTypeId = session.VaccineTypeId,
                VaccineTypeName = session.VaccineTypeName,
                Location = session.Location,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                Status = session.Status,
                SessionName = session.SessionName,
                Notes = session.Notes
            };
        }

        private IQueryable<VaccinationSession> ApplySessionOrdering(
            IQueryable<VaccinationSession> query,
            string orderBy)
        {
            return orderBy?.ToLower() switch
            {
                "location" => query.OrderBy(vs => vs.Location),
                "location_desc" => query.OrderByDescending(vs => vs.Location),
                "starttime" => query.OrderBy(vs => vs.StartTime),
                "starttime_desc" => query.OrderByDescending(vs => vs.StartTime),
                "sessionname" => query.OrderBy(vs => vs.SessionName),
                "sessionname_desc" => query.OrderByDescending(vs => vs.SessionName),
                _ => query.OrderByDescending(vs => vs.StartTime)
            };
        }

        private string GenerateConsentEmail(
            ApplicationUser student,
            VaccinationSession session,
            ApplicationUser parent,
            Guid consentId)
        {
            if (student == null || session == null || parent == null)
            {
                _logger.LogError("Một hoặc nhiều tham số null trong GenerateConsentEmail: student={StudentId}, session={SessionId}, parent={ParentId}",
                    student?.Id, session?.Id, parent?.Id);
                throw new ArgumentNullException("Thông tin học sinh, buổi tiêm, hoặc phụ huynh không được null.");
            }

            if (session.VaccineType == null)
            {
                _logger.LogError("VaccineType null cho session ID: {SessionId}", session.Id);
                throw new InvalidOperationException("Không tìm thấy thông tin loại vaccine.");
            }

            return $@"
                    <h2>Yêu cầu đồng ý tiêm vaccine</h2>
                    <p>Thông tin học sinh:</p>
                    <ul>
                        <li>Họ và tên: {student.FullName ?? "Không có tên"}</li>
                        <li>Mã học sinh: {student.StudentCode ?? "Không có mã"}</li>
                    </ul>
                    <p>Thông tin tiêm chủng:</p>
                    <ul>
                        <li>Loại vaccine: {session.VaccineType.Name ?? "Không xác định"}</li>
                        <li>Địa điểm: {session.Location ?? "Không xác định"}</li>
                        <li>Thời gian: {session.StartTime} - {session.EndTime}</li>
                    </ul>
                    <p>Thông tin quan trọng: Vui lòng đọc kỹ hướng dẫn trước khi tiêm.</p>
                    <p>Phụ huynh: {parent.FullName ?? "Không có tên"} ({parent.Email ?? "Không có email"})</p>
                    <p>Vui lòng phản hồi tại: <a href='https://yourdomain.com/consent?sessionId={session.Id}&consentId={consentId}'>Link đồng ý</a></p>
                ";
        }

        private string GenerateConfirmationEmail(VaccinationConsent consent)
        {
            if (consent == null)
            {
                throw new ArgumentNullException(nameof(consent), "Consent không được phép null.");
            }

            var action = consent.Status == "Confirmed" ? "đồng ý" : "từ chối";
            var deadline = consent.Session?.StartTime.AddDays(-3) ?? DateTime.UtcNow; // Giá trị mặc định nếu null
            var vaccinationStatus = "Chưa tiêm"; // Giá trị mặc định
            if (consent.Session?.Status == "Scheduled")
            {
                var record = _unitOfWork.GetRepositoryByEntity<VaccinationRecord>().GetQueryable()
                .Where(vr => (consent.Session == null || vr.SessionId == consent.Session.Id) &&
                             (consent.Student == null || vr.UserId == consent.Student.Id) &&
                             vr.VaccinationStatus == "Completed")
                .FirstOrDefault();
                vaccinationStatus = record != null ? "Đã tiêm" : "Chưa tiêm";
            }

            var parentName = consent.Parent?.FullName ?? "Phụ huynh không xác định";
            var studentName = consent.Student?.FullName ?? "Học sinh không xác định";
            var sessionName = consent.Session?.SessionName ?? "Buổi tiêm không xác định";
            var vaccineTypeName = consent.Session?.VaccineTypeName ?? "Không xác định";
            var responseDate = consent.ResponseDate?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa có";

            return $@"<h2>Xác nhận {action} tiêm vaccine</h2>
            <p>Kính gửi {parentName},</p>
            <p>Chúng tôi đã nhận được phản hồi của bạn về yêu cầu tiêm vaccine cho học sinh {studentName}.</p>
            <p>Trạng thái: <strong>{action}</strong></p>
            <p>Buổi tiêm: {sessionName}</p>
            <p>Vaccine: {vaccineTypeName}</p>
            <p>Thời hạn phản hồi: <strong>{deadline:dd/MM/yyyy HH:mm}</strong> (3 ngày trước thời gian tiêm)</p>
            <p>Trạng thái tiêm: <strong>{vaccinationStatus}</strong></p>
            <p>Thời gian phản hồi: {responseDate}</p>
            <p>Cảm ơn bạn đã phản hồi.</p>";
        }

        private async Task InvalidateAllCachesAsync()
        {
            try
            {
                _logger.LogDebug("Starting cache invalidation for vaccination sessions");
                // Xóa toàn bộ tracking set để làm mới tất cả các khóa cache
                await _cacheService.InvalidateTrackingSetAsync(SESSION_CACHE_SET);
                // Xóa thêm các tiền tố cụ thể để đảm bảo
                await Task.WhenAll(
                    _cacheService.RemoveByPrefixAsync(SESSION_CACHE_PREFIX),
                    _cacheService.RemoveByPrefixAsync(SESSION_LIST_PREFIX),
                    _cacheService.RemoveByPrefixAsync("student_vaccination_result"),
                    _cacheService.RemoveByPrefixAsync("parent_consent_status"),
                    _cacheService.RemoveByPrefixAsync("student_sessions")
                );
                _logger.LogDebug("Completed cache invalidation for vaccination sessions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in cache invalidation for vaccination sessions");
            }
        }

        #endregion

    }
}