using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
            IEmailService emailService,
            IHttpContextAccessor httpContextAccessor)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _logger = logger;
            _createSessionValidator = createSessionValidator;
            _updateSessionValidator = updateSessionValidator;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
        }

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

                var responses = sessions.Select(MapToSessionResponse).ToList();

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

                // Lấy ID của School Nurse từ HttpContext
                var nurseId = Guid.TryParse(_httpContextAccessor.HttpContext?.User?.Identity?.Name, out var id) ? id : Guid.Empty;

                var session = _mapper.Map<VaccinationSession>(model);
                session.Id = Guid.NewGuid();
                session.VaccineTypeId = model.VaccineTypeId; // Đảm bảo ánh xạ đúng
                session.Status = "PendingApproval";
                session.CreatedById = nurseId != Guid.Empty ? nurseId : throw new UnauthorizedAccessException("Không thể xác định ID người dùng.");
                session.CreatedDate = DateTime.UtcNow;
                session.IsDeleted = false;
                session.Code = $"VSESSION-{Guid.NewGuid().ToString().Substring(0, 8)}";

                _unitOfWork.GetRepositoryByEntity<VaccinationSession>().AddAsync(session);
                await _unitOfWork.SaveChangesAsync();

                // Liên kết với các lớp
                foreach (var classId in model.ClassIds)
                {
                    _unitOfWork.GetRepositoryByEntity<VaccinationSessionClass>().AddAsync(new VaccinationSessionClass
                    {
                        Id = Guid.NewGuid(),
                        SessionId = session.Id,
                        ClassId = classId,
                        CreatedDate = DateTime.UtcNow // Sử dụng thuộc tính từ BaseEntity
                    });
                }
                await _unitOfWork.SaveChangesAsync();

                await InvalidateAllCachesAsync();

                session = await _unitOfWork.GetRepositoryByEntity<VaccinationSession>().GetQueryable()
                    .Include(vs => vs.VaccineType)
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

        public async Task<BaseResponse<bool>> ApproveSessionAsync(Guid sessionId)
        {
            try
            {
                var session = await _unitOfWork.GetRepositoryByEntity<VaccinationSession>().GetQueryable()
                    .Include(vs => vs.Classes)
                    .ThenInclude(vsc => vsc.SchoolClass)
                    .FirstOrDefaultAsync(vs => vs.Id == sessionId && !vs.IsDeleted);

                if (session == null || session.Status != "PendingApproval")
                {
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "Buổi tiêm không tồn tại hoặc không đang chờ duyệt."
                    };
                }

                session.ApprovedById = Guid.Parse("MANAGER_ID"); // Thay bằng ID từ token
                session.ApprovedDate = DateTime.UtcNow;
                session.Status = "WaitingForParentConsent";
                await _unitOfWork.SaveChangesAsync();

                // Gửi email cho phụ huynh
                var classIds = session.Classes.Select(c => c.ClassId).ToList();
                var students = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                    .Where(u => u.StudentClasses.Any(sc => classIds.Contains(sc.ClassId)) &&
                        u.UserRoles.Any(ur => ur.Role.Name == "STUDENT"))
                    .ToListAsync();

                foreach (var student in students)
                {
                    var parent = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                        .FirstOrDefaultAsync(u => u.Id == student.ParentId &&
                            u.UserRoles.Any(ur => ur.Role.Name == "PARENT"));

                    if (parent != null)
                    {
                        var consent = new VaccinationConsent
                        {
                            Id = Guid.NewGuid(),
                            SessionId = sessionId,
                            StudentId = student.Id,
                            ParentId = parent.Id,
                            Status = "Pending",
                            CreatedDate = DateTime.UtcNow
                        };

                        _unitOfWork.GetRepositoryByEntity<VaccinationConsent>().AddAsync(consent);

                        var emailBody = GenerateConsentEmail(student, session, parent);
                        await _emailService.SendEmailAsync(parent.Email, "Yêu cầu đồng ý tiêm vaccine", emailBody);
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                await InvalidateAllCachesAsync();

                return new BaseResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Duyệt buổi tiêm và gửi email thành công."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving vaccination session: {SessionId}", sessionId);
                return new BaseResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Lỗi duyệt buổi tiêm: {ex.Message}"
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

                if (consents.Any(c => c.Status == "Pending"))
                {
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "Một số phụ huynh chưa phản hồi."
                    };
                }

                session.Status = "Scheduled";
                await _unitOfWork.SaveChangesAsync();

                var classIds = session.Classes.Select(c => c.ClassId).ToList();
                var nurses = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                    .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "SCHOOLNURSE"))
                    .ToListAsync();

                foreach (var classId in classIds)
                {
                    var nurse = nurses.FirstOrDefault(); // Logic phân công đơn giản
                    if (nurse != null)
                    {
                        _unitOfWork.GetRepositoryByEntity<VaccinationAssignment>().AddAsync(new VaccinationAssignment
                        {
                            Id = Guid.NewGuid(),
                            SessionId = sessionId,
                            ClassId = classId,
                            NurseId = nurse.Id,
                            AssignedDate = DateTime.UtcNow,
                            CreatedDate = DateTime.UtcNow
                        });
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                await InvalidateAllCachesAsync();

                return new BaseResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Chốt danh sách và phân công Nurse thành công."
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

        #region Helper Methods

        private VaccinationSessionResponse MapToSessionResponse(VaccinationSession session)
        {
            return new VaccinationSessionResponse
            {
                Id = session.Id,
                VaccineTypeId = session.VaccineTypeId,
                VaccineTypeName = session.VaccineType.Name,
                Location = session.Location,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                Status = session.Status,
                SessionName = session.SessionName,
                Notes = session.Notes
            };
        }

        private IQueryable<VaccinationSession> ApplySessionOrdering(IQueryable<VaccinationSession> query, string orderBy)
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

        private string GenerateConsentEmail(ApplicationUser student, VaccinationSession session, ApplicationUser parent)
        {
            return $@"
                <h2>Yêu cầu đồng ý tiêm vaccine</h2>
                <p>Thông tin học sinh:</p>
                <ul>
                    <li>Họ và tên: {student.FullName}</li>
                    <li>Mã học sinh: {student.StudentCode}</li>
                </ul>
                <p>Thông tin tiêm chủng:</p>
                <ul>
                    <li>Loại vaccine: {session.VaccineType.Name}</li>
                    <li>Địa điểm: {session.Location}</li>
                    <li>Thời gian: {session.StartTime} - {session.EndTime}</li>
                </ul>
                <p>Thông tin quan trọng: Vui lòng đọc kỹ hướng dẫn trước khi tiêm.</p>
                <p>Phụ huynh: {parent.FullName} ({parent.Email})</p>
                <p>Vui lòng phản hồi tại: <a href='https://yourdomain.com/consent?sessionId={session.Id}&consentId={Guid.NewGuid()}'>Link đồng ý</a></p>
            ";
        }

        private async Task InvalidateAllCachesAsync()
        {
            try
            {
                _logger.LogDebug("Starting cache invalidation for vaccination sessions");
                await Task.WhenAll(
                    _cacheService.RemoveByPrefixAsync(SESSION_CACHE_PREFIX),
                    _cacheService.RemoveByPrefixAsync(SESSION_LIST_PREFIX)
                );
                await Task.Delay(100);
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