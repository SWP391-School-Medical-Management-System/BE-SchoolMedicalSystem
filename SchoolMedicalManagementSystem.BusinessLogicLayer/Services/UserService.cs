using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Helpers;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.SchoolClassResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.UserResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts.IAuthService;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Utilities;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services;

public class UserService : IUserService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ICacheService _cacheService;
    private readonly IExcelService _excelService;
    private readonly IAuthService _authService;
    private readonly ILogger<UserService> _logger;

    private readonly IValidator<CreateManagerRequest> _createManagerValidator;
    private readonly IValidator<UpdateManagerRequest> _updateManagerValidator;
    private readonly IValidator<CreateSchoolNurseRequest> _createSchoolNurseValidator;
    private readonly IValidator<UpdateSchoolNurseRequest> _updateSchoolNurseValidator;
    private readonly IValidator<CreateStudentRequest> _createStudentValidator;
    private readonly IValidator<UpdateStudentRequest> _updateStudentValidator;
    private readonly IValidator<CreateParentRequest> _createParentValidator;
    private readonly IValidator<UpdateParentRequest> _updateParentValidator;

    private readonly CloudinaryService _cloudinaryService;

    private const string STAFF_CACHE_PREFIX = "staff_user";
    private const string STAFF_LIST_PREFIX = "staff_users_list";
    private const string STUDENT_CACHE_PREFIX = "student";
    private const string PARENT_CACHE_PREFIX = "parent";
    private const string STUDENT_LIST_PREFIX = "students_list";
    private const string PARENT_LIST_PREFIX = "parents_list";
    private const string STATISTICS_PREFIX = "statistics";
    private const string STAFF_CACHE_SET = "staff_cache_keys";
    private const string STUDENT_CACHE_SET = "student_cache_keys";
    private const string PARENT_CACHE_SET = "parent_cache_keys";
    private const string CLASS_ENROLLMENT_PREFIX = "class_enrollment";
    private const string CLASS_ENROLLMENT_CACHE_SET = "class_enrollment_cache_keys";
    private const string USER_PROFILE_PREFIX = "user_profile";
    private const string USER_CACHE_PREFIX = "user_cache";
    private const string MEDICATION_LIST_PREFIX = "medication_cache";

    public UserService(
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ICacheService cacheService,
        IExcelService excelService,
        ILogger<UserService> logger,
        IAuthService authService,
        IValidator<CreateManagerRequest> createManagerValidator,
        IValidator<UpdateManagerRequest> updateManagerValidator,
        IValidator<CreateSchoolNurseRequest> createSchoolNurseValidator,
        IValidator<UpdateSchoolNurseRequest> updateSchoolNurseValidator,
        IValidator<CreateStudentRequest> createStudentValidator,
        IValidator<UpdateStudentRequest> updateStudentValidator,
        IValidator<CreateParentRequest> createParentValidator,
        IValidator<UpdateParentRequest> updateParentValidator,
        CloudinaryService cloudinaryService
    )
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _emailService = emailService;
        _excelService = excelService;
        _cacheService = cacheService;
        _logger = logger;
        _authService = authService;
        _createManagerValidator = createManagerValidator;
        _updateManagerValidator = updateManagerValidator;
        _createSchoolNurseValidator = createSchoolNurseValidator;
        _updateSchoolNurseValidator = updateSchoolNurseValidator;
        _createStudentValidator = createStudentValidator;
        _updateStudentValidator = updateStudentValidator;
        _createParentValidator = createParentValidator;
        _updateParentValidator = updateParentValidator;
        _cloudinaryService = cloudinaryService;
    }

    #region User Management (User)

    public async Task<BaseResponse<UserResponses>> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest model)
    {
        try
        {
            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var user = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            if (user == null)
            {
                return new BaseResponse<UserResponses> { Success = false, Message = "Người dùng không tồn tại." };
            }

            string profileImageUrl = null;
            if (model.ProfileImage != null)
            {
                profileImageUrl = await ProcessProfileImage(model.ProfileImage);
                _logger.LogDebug("Processed ProfileImageUrl: {ProfileImageUrl}", profileImageUrl);
                if (profileImageUrl == null)
                {
                    return new BaseResponse<UserResponses> { Success = false, Message = "Lỗi khi xử lý ảnh đại diện." };
                }
            }

            if (model.DateOfBirth.HasValue && model.DateOfBirth.Value > DateTime.Now)
            {
                return new BaseResponse<UserResponses> { Success = false, Message = "Ngày sinh không hợp lệ." };
            }

            // Cập nhật thông tin người dùng
            user.FullName = model.FullName ?? user.FullName;
            user.PhoneNumber = model.PhoneNumber ?? user.PhoneNumber;
            user.Address = model.Address ?? user.Address;
            user.Gender = model.Gender ?? user.Gender;
            user.DateOfBirth = model.DateOfBirth ?? user.DateOfBirth;
            if (profileImageUrl != null)
            {
                user.ProfileImageUrl = profileImageUrl;
                _logger.LogDebug("Updated user ProfileImageUrl: {ProfileImageUrl}", user.ProfileImageUrl);
            }

            user.LastUpdatedBy = user.Username;
            user.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            _logger.LogDebug("Saved user to DB: {UserId}, ProfileImageUrl: {ProfileImageUrl}", user.Id, user.ProfileImageUrl);

            // Làm mới tất cả cache liên quan đến người dùng
            await InvalidateUserRelatedCachesAsync(user);

            var userResponse = _mapper.Map<UserResponses>(user);
            userResponse.Role = user.UserRoles.FirstOrDefault()?.Role.Name;

            return new BaseResponse<UserResponses>
            {
                Success = true,
                Data = userResponse,
                Message = "Cập nhật thông tin cá nhân thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile for userId: {UserId}", userId);
            return new BaseResponse<UserResponses> { Success = false, Message = $"Lỗi cập nhật thông tin: {ex.Message}" };
        }
    }


    public async Task<BaseResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequest model)
    {
        try
        {
            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var user = await userRepo.GetQueryable()
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            if (user == null)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "Người dùng không tồn tại."
                };
            }

            string oldPasswordHash = HashPassword(model.OldPassword);
            if (user.PasswordHash != oldPasswordHash)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "Mật khẩu cũ không đúng."
                };
            }

            if (string.IsNullOrWhiteSpace(model.NewPassword) || model.NewPassword.Length < 6)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "Mật khẩu mới phải có ít nhất 6 ký tự."
                };
            }

            // Cập nhật mật khẩu mới
            string newPasswordHash = HashPassword(model.NewPassword);
            user.PasswordHash = newPasswordHash;
            user.LastUpdatedBy = user.Username;
            user.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();

            await InvalidateUserLoginCacheAsync(user);

            return new BaseResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Đổi mật khẩu thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return new BaseResponse<bool>
            {
                Success = false,
                Data = false,
                Message = $"Lỗi đổi mật khẩu: {ex.Message}"
            };
        }
    }

    private async Task<string> ProcessProfileImage(IFormFile file)
    {
        if (file.Length > 2 * 1024 * 1024)
        {
            throw new ArgumentException("Kích thước ảnh vượt quá 2MB.");
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var fileExtension = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(fileExtension))
        {
            throw new ArgumentException("Chỉ chấp nhận file ảnh có định dạng JPG, JPEG hoặc PNG.");
        }

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var imageData = ms.ToArray();
        string fileName = $"{Guid.NewGuid()}{fileExtension}";

        return await _cloudinaryService.UploadImageAsync(imageData, fileName);
    }

    #endregion

    #region Staff Management (Admin)

    public async Task<BaseListResponse<StaffUserResponse>> GetStaffUsersAsync(
        int pageIndex,
        int pageSize,
        string searchTerm,
        string orderBy,
        string role = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(
                STAFF_LIST_PREFIX,
                pageIndex.ToString(),
                pageSize.ToString(),
                searchTerm ?? "",
                orderBy ?? "",
                role ?? ""
            );

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<StaffUserResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Staff users list found in cache");
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => !u.IsDeleted &&
                            u.UserRoles.Any(ur => ur.Role.Name == "MANAGER" || ur.Role.Name == "SCHOOLNURSE"))
                .AsQueryable();

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == role));
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(u =>
                    u.Username.ToLower().Contains(searchTerm) ||
                    u.Email.ToLower().Contains(searchTerm) ||
                    u.FullName.ToLower().Contains(searchTerm) ||
                    u.PhoneNumber.Contains(searchTerm) ||
                    (u.StaffCode != null && u.StaffCode.ToLower().Contains(searchTerm)));
            }

            // Apply ordering
            query = ApplyStaffOrdering(query, orderBy);

            var totalCount = await query.CountAsync(cancellationToken);
            var users = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = users.Select(user =>
            {
                var response = _mapper.Map<StaffUserResponse>(user);
                response.Role = user.UserRoles.FirstOrDefault()?.Role.Name;
                return response;
            }).ToList();

            var result = BaseListResponse<StaffUserResponse>.SuccessResult(
                responses,
                totalCount,
                pageSize,
                pageIndex,
                "Lấy danh sách nhân viên thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            await _cacheService.AddToTrackingSetAsync(cacheKey, STAFF_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving staff users");
            return BaseListResponse<StaffUserResponse>.ErrorResult("Lỗi lấy danh sách nhân viên.");
        }
    }

    public async Task<BaseResponse<StaffUserResponse>> GetStaffUserByIdAsync(Guid userId)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(STAFF_CACHE_PREFIX, userId.ToString());
            var cachedResponse = await _cacheService.GetAsync<BaseResponse<StaffUserResponse>>(cacheKey);

            if (cachedResponse != null)
            {
                _logger.LogDebug("Staff user found in cache: {UserId}", userId);
                return cachedResponse;
            }

            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var user = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.Id == userId && !u.IsDeleted &&
                            u.UserRoles.Any(ur => ur.Role.Name == "MANAGER" || ur.Role.Name == "SCHOOLNURSE"))
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return new BaseResponse<StaffUserResponse>
                {
                    Success = false,
                    Message = "Không tìm thấy nhân viên."
                };
            }

            var userResponse = _mapper.Map<StaffUserResponse>(user);
            userResponse.Role = user.UserRoles.FirstOrDefault()?.Role.Name;

            var response = new BaseResponse<StaffUserResponse>
            {
                Success = true,
                Data = userResponse,
                Message = "Lấy thông tin nhân viên thành công."
            };

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15));
            await _cacheService.AddToTrackingSetAsync(cacheKey, STAFF_CACHE_SET);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting staff user by ID: {UserId}", userId);
            return new BaseResponse<StaffUserResponse>
            {
                Success = false,
                Message = $"Lỗi lấy thông tin nhân viên: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<ManagerResponse>> CreateManagerAsync(CreateManagerRequest model)
    {
        try
        {
            var validationResult = await _createManagerValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return new BaseResponse<ManagerResponse>
                {
                    Success = false,
                    Message = errors
                };
            }

            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();

            if (await userRepo.GetQueryable().AnyAsync(u =>
                    (u.Username == model.Username || u.Email == model.Email || u.StaffCode == model.StaffCode)
                    && !u.IsDeleted))
            {
                return new BaseResponse<ManagerResponse>
                {
                    Success = false,
                    Message = "Tên đăng nhập, email hoặc mã nhân viên đã tồn tại."
                };
            }

            if (!string.IsNullOrEmpty(model.PhoneNumber))
            {
                var phoneNumberExists = await userRepo.GetQueryable()
                    .AnyAsync(u => u.PhoneNumber == model.PhoneNumber && !u.IsDeleted);

                if (phoneNumberExists)
                {
                    return new BaseResponse<ManagerResponse>
                    {
                        Success = false,
                        Message = "Số điện thoại đã được sử dụng."
                    };
                }
            }

            string defaultPassword = GenerateDefaultPassword();
            string passwordHash = HashPassword(defaultPassword);
            var adminRoleName = await GetAdminRoleName();

            var user = _mapper.Map<ApplicationUser>(model);
            user.Id = Guid.NewGuid();
            user.PasswordHash = passwordHash;
            user.CreatedBy = adminRoleName;
            user.CreatedDate = DateTime.Now;
            user.IsActive = true;

            await userRepo.AddAsync(user);

            var roleRepo = _unitOfWork.GetRepositoryByEntity<Role>();
            var role = await roleRepo.GetQueryable()
                .FirstOrDefaultAsync(r => r.Name == "MANAGER");

            if (role == null)
            {
                return new BaseResponse<ManagerResponse>
                {
                    Success = false,
                    Message = "Vai trò không hợp lệ."
                };
            }

            var userRole = new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = role.Id,
                CreatedBy = adminRoleName,
                CreatedDate = DateTime.Now
            };

            await _unitOfWork.GetRepositoryByEntity<UserRole>().AddAsync(userRole);
            await _unitOfWork.SaveChangesAsync();

            await _emailService.SendAccountCreationEmailAsync(user.Email, user.Username, defaultPassword);
            await InvalidateStaffCacheAsync();

            var managerResponse = _mapper.Map<ManagerResponse>(user);

            return new BaseResponse<ManagerResponse>
            {
                Success = true,
                Data = managerResponse,
                Message = "Tài khoản Manager đã được tạo thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating manager account");
            return new BaseResponse<ManagerResponse>
            {
                Success = false,
                Message = $"Lỗi tạo tài khoản Manager: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<ManagerResponse>> UpdateManagerAsync(Guid managerId, UpdateManagerRequest model)
    {
        try
        {
            var validationResult = await _updateManagerValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return new BaseResponse<ManagerResponse>
                {
                    Success = false,
                    Message = errors
                };
            }

            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var user = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == managerId && !u.IsDeleted &&
                                          u.UserRoles.Any(ur => ur.Role.Name == "MANAGER"));

            if (user == null)
            {
                return new BaseResponse<ManagerResponse>
                {
                    Success = false,
                    Message = "Không tìm thấy Manager."
                };
            }

            if (user.PhoneNumber != model.PhoneNumber || user.StaffCode != model.StaffCode)
            {
                var duplicateExists = await userRepo.GetQueryable()
                    .AnyAsync(u => u.Id != managerId && !u.IsDeleted &&
                                   (u.PhoneNumber == model.PhoneNumber || u.StaffCode == model.StaffCode));

                if (duplicateExists)
                {
                    return new BaseResponse<ManagerResponse>
                    {
                        Success = false,
                        Message = "Số điện thoại hoặc mã nhân viên đã được sử dụng."
                    };
                }
            }

            var adminRoleName = await GetAdminRoleName();

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;
            user.Gender = model.Gender;
            user.DateOfBirth = model.DateOfBirth;
            user.StaffCode = model.StaffCode;
            user.LastUpdatedBy = adminRoleName;
            user.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();

            await InvalidateUserLoginCacheAsync(user);
            await InvalidateStaffCacheAsync();

            var managerResponse = _mapper.Map<ManagerResponse>(user);

            return new BaseResponse<ManagerResponse>
            {
                Success = true,
                Data = managerResponse,
                Message = "Tài khoản Manager đã được cập nhật thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating manager account");
            return new BaseResponse<ManagerResponse>
            {
                Success = false,
                Message = $"Lỗi cập nhật tài khoản Manager: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<bool>> DeleteManagerAsync(Guid managerId)
    {
        try
        {
            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var user = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == managerId && !u.IsDeleted &&
                                          u.UserRoles.Any(ur => ur.Role.Name == "MANAGER"));

            if (user == null)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "Không tìm thấy Manager."
                };
            }

            var canDeleteResult = await ValidateManagerDeletion(managerId);
            if (!canDeleteResult.CanDelete)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = canDeleteResult.Reason
                };
            }

            var adminRoleName = await GetAdminRoleName();

            user.IsDeleted = true;
            user.LastUpdatedBy = adminRoleName;
            user.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            await InvalidateStaffCacheAsync();

            return new BaseResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Manager đã được xóa thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting manager: {ManagerId}", managerId);
            return new BaseResponse<bool>
            {
                Success = false,
                Data = false,
                Message = $"Lỗi xóa Manager: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<SchoolNurseResponse>> CreateSchoolNurseAsync(CreateSchoolNurseRequest model)
    {
        try
        {
            var validationResult = await _createSchoolNurseValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return new BaseResponse<SchoolNurseResponse>
                {
                    Success = false,
                    Message = errors
                };
            }

            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();

            if (await userRepo.GetQueryable().AnyAsync(u =>
                    (u.Username == model.Username || u.Email == model.Email || u.StaffCode == model.StaffCode)
                    && !u.IsDeleted))
            {
                return new BaseResponse<SchoolNurseResponse>
                {
                    Success = false,
                    Message = "Tên đăng nhập, email hoặc mã nhân viên đã tồn tại."
                };
            }

            if (!string.IsNullOrEmpty(model.PhoneNumber))
            {
                var phoneNumberExists = await userRepo.GetQueryable()
                    .AnyAsync(u => u.PhoneNumber == model.PhoneNumber && !u.IsDeleted);

                if (phoneNumberExists)
                {
                    return new BaseResponse<SchoolNurseResponse>
                    {
                        Success = false,
                        Message = "Số điện thoại đã được sử dụng."
                    };
                }
            }

            string defaultPassword = GenerateDefaultPassword();
            string passwordHash = HashPassword(defaultPassword);
            var adminRoleName = await GetAdminRoleName();

            var user = _mapper.Map<ApplicationUser>(model);
            user.Id = Guid.NewGuid();
            user.PasswordHash = passwordHash;
            user.CreatedBy = adminRoleName;
            user.CreatedDate = DateTime.Now;
            user.IsActive = true;

            await userRepo.AddAsync(user);

            var roleRepo = _unitOfWork.GetRepositoryByEntity<Role>();
            var role = await roleRepo.GetQueryable()
                .FirstOrDefaultAsync(r => r.Name == "SCHOOLNURSE");

            if (role == null)
            {
                return new BaseResponse<SchoolNurseResponse>
                {
                    Success = false,
                    Message = "Vai trò không hợp lệ."
                };
            }

            var userRole = new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = role.Id,
                CreatedBy = adminRoleName,
                CreatedDate = DateTime.Now
            };

            await _unitOfWork.GetRepositoryByEntity<UserRole>().AddAsync(userRole);
            await _unitOfWork.SaveChangesAsync();

            await _emailService.SendAccountCreationEmailAsync(user.Email, user.Username, defaultPassword);
            await InvalidateStaffCacheAsync();

            var schoolNurseResponse = _mapper.Map<SchoolNurseResponse>(user);

            return new BaseResponse<SchoolNurseResponse>
            {
                Success = true,
                Data = schoolNurseResponse,
                Message = "Tài khoản School Nurse đã được tạo thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating school nurse account");
            return new BaseResponse<SchoolNurseResponse>
            {
                Success = false,
                Message = $"Lỗi tạo tài khoản School Nurse: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<SchoolNurseResponse>> UpdateSchoolNurseAsync(Guid schoolNurseId,
        UpdateSchoolNurseRequest model)
    {
        try
        {
            var validationResult = await _updateSchoolNurseValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return new BaseResponse<SchoolNurseResponse>
                {
                    Success = false,
                    Message = errors
                };
            }

            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var user = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == schoolNurseId && !u.IsDeleted &&
                                          u.UserRoles.Any(ur => ur.Role.Name == "SCHOOLNURSE"));

            if (user == null)
            {
                return new BaseResponse<SchoolNurseResponse>
                {
                    Success = false,
                    Message = "Không tìm thấy School Nurse."
                };
            }

            if (user.PhoneNumber != model.PhoneNumber || user.StaffCode != model.StaffCode)
            {
                var duplicateExists = await userRepo.GetQueryable()
                    .AnyAsync(u => u.Id != schoolNurseId && !u.IsDeleted &&
                                   (u.PhoneNumber == model.PhoneNumber || u.StaffCode == model.StaffCode));

                if (duplicateExists)
                {
                    return new BaseResponse<SchoolNurseResponse>
                    {
                        Success = false,
                        Message = "Số điện thoại hoặc mã nhân viên đã được sử dụng."
                    };
                }
            }

            var adminRoleName = await GetAdminRoleName();

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;
            user.Gender = model.Gender;
            user.DateOfBirth = model.DateOfBirth;
            user.StaffCode = model.StaffCode;
            user.LicenseNumber = model.LicenseNumber;
            user.Specialization = model.Specialization;
            user.LastUpdatedBy = adminRoleName;
            user.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            await InvalidateStaffCacheAsync();

            var schoolNurseResponse = _mapper.Map<SchoolNurseResponse>(user);

            return new BaseResponse<SchoolNurseResponse>
            {
                Success = true,
                Data = schoolNurseResponse,
                Message = "Tài khoản School Nurse đã được cập nhật thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating school nurse account");
            return new BaseResponse<SchoolNurseResponse>
            {
                Success = false,
                Message = $"Lỗi cập nhật tài khoản School Nurse: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<bool>> DeleteSchoolNurseAsync(Guid schoolNurseId)
    {
        try
        {
            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var user = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == schoolNurseId && !u.IsDeleted &&
                                          u.UserRoles.Any(ur => ur.Role.Name == "SCHOOLNURSE"));

            if (user == null)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "Không tìm thấy School Nurse."
                };
            }

            var canDeleteResult = await ValidateSchoolNurseDeletion(schoolNurseId);
            if (!canDeleteResult.CanDelete)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = canDeleteResult.Reason
                };
            }

            var adminRoleName = await GetAdminRoleName();

            user.IsDeleted = true;
            user.LastUpdatedBy = adminRoleName;
            user.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            await InvalidateStaffCacheAsync();

            return new BaseResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "School Nurse đã được xóa thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting school nurse: {SchoolNurseId}", schoolNurseId);
            return new BaseResponse<bool>
            {
                Success = false,
                Data = false,
                Message = $"Lỗi xóa School Nurse: {ex.Message}"
            };
        }
    }

    #endregion

    #region Student Management (Manager)

    public async Task<BaseListResponse<StudentResponse>> GetStudentsAsync(
        int pageIndex,
        int pageSize,
        string searchTerm,
        string orderBy,
        Guid? classId = null,
        bool? hasMedicalRecord = null,
        bool? hasParent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(
                STUDENT_LIST_PREFIX,
                pageIndex.ToString(),
                pageSize.ToString(),
                searchTerm ?? "",
                orderBy ?? "",
                classId?.ToString() ?? "",
                hasMedicalRecord?.ToString() ?? "",
                hasParent?.ToString() ?? ""
            );

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<StudentResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Students list found in cache");
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.StudentClasses.Where(sc => !sc.IsDeleted))
                .ThenInclude(sc => sc.SchoolClass)
                .Include(u => u.Parent)
                .Include(u => u.MedicalRecord)
                .Where(u => !u.IsDeleted && u.UserRoles.Any(ur => ur.Role.Name == "STUDENT"))
                .AsQueryable();

            query = ApplyStudentFilters(query, classId, hasMedicalRecord, hasParent, searchTerm);
            query = ApplyStudentOrdering(query, orderBy);

            var totalCount = await query.CountAsync(cancellationToken);
            var students = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = students.Select(MapToStudentResponse).ToList();

            var result = BaseListResponse<StudentResponse>.SuccessResult(
                responses,
                totalCount,
                pageSize,
                pageIndex,
                "Lấy danh sách học sinh thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            await _cacheService.AddToTrackingSetAsync(cacheKey, STUDENT_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving students");
            return BaseListResponse<StudentResponse>.ErrorResult("Lỗi lấy danh sách học sinh.");
        }
    }

    public async Task<BaseResponse<StudentResponse>> GetStudentByIdAsync(Guid studentId)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(STUDENT_CACHE_PREFIX, studentId.ToString());
            var cachedResponse = await _cacheService.GetAsync<BaseResponse<StudentResponse>>(cacheKey);

            if (cachedResponse != null)
            {
                _logger.LogDebug("Student found in cache: {StudentId}", studentId);
                return cachedResponse;
            }

            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var student = await userRepo.GetQueryable()
                .AsSplitQuery()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.StudentClasses.Where(sc => !sc.IsDeleted))
                .ThenInclude(sc => sc.SchoolClass)
                .Include(u => u.Parent)
                .Include(u => u.MedicalRecord)
                .Where(u => u.Id == studentId && !u.IsDeleted &&
                            u.UserRoles.Any(ur => ur.Role.Name == "STUDENT"))
                .FirstOrDefaultAsync();

            if (student == null)
            {
                return new BaseResponse<StudentResponse>
                {
                    Success = false,
                    Message = "Không tìm thấy học sinh."
                };
            }

            var studentResponse = MapToStudentResponse(student);

            var response = new BaseResponse<StudentResponse>
            {
                Success = true,
                Data = studentResponse,
                Message = "Lấy thông tin học sinh thành công."
            };

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15));
            await _cacheService.AddToTrackingSetAsync(cacheKey, STUDENT_CACHE_SET);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student by ID: {StudentId}", studentId);
            return new BaseResponse<StudentResponse>
            {
                Success = false,
                Message = $"Lỗi lấy thông tin học sinh: {ex.Message}"
            };
        }
    }

    public async Task<BaseListResponse<StudentResponse>> GetStudentsByParentIdAsync(
    Guid parentId,
    int pageIndex = 1,
    int pageSize = 10,
    string searchTerm = null,
    string orderBy = null,
    CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(
                "parent_students",
                parentId.ToString(),
                pageIndex.ToString(),
                pageSize.ToString(),
                searchTerm ?? "",
                orderBy ?? ""
            );

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<StudentResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Students list for parent found in cache: {ParentId}", parentId);
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.StudentClasses.Where(sc => !sc.IsDeleted))
                .ThenInclude(sc => sc.SchoolClass)
                .Include(u => u.Parent)
                .Include(u => u.MedicalRecord)
                .Where(u => !u.IsDeleted && u.UserRoles.Any(ur => ur.Role.Name == "STUDENT") && u.ParentId == parentId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(u =>
                    u.FullName.ToLower().Contains(searchTerm) ||
                    u.StudentCode.ToLower().Contains(searchTerm));
            }

            query = ApplyStudentOrdering(query, orderBy);

            var totalCount = await query.CountAsync(cancellationToken);
            var students = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = students.Select(MapToStudentResponse).ToList();

            var result = BaseListResponse<StudentResponse>.SuccessResult(
                responses,
                totalCount,
                pageSize,
                pageIndex,
                "Lấy danh sách học sinh của phụ huynh thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            await _cacheService.AddToTrackingSetAsync(cacheKey, "parent_students_cache_keys");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving students for parent: {ParentId}", parentId);
            return BaseListResponse<StudentResponse>.ErrorResult("Lỗi lấy danh sách học sinh của phụ huynh.");
        }
    }
    public async Task<BaseResponse<StudentResponse>> CreateStudentAsync(CreateStudentRequest model, Guid currentUserId)
    {
        try
        {
            var validationResult = await _createStudentValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return new BaseResponse<StudentResponse>
                {
                    Success = false,
                    Message = errors
                };
            }

            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var studentClassRepo = _unitOfWork.GetRepositoryByEntity<StudentClass>();
            var schoolClassRepo = _unitOfWork.GetRepositoryByEntity<SchoolClass>();
            var medicalRecordRepo = _unitOfWork.GetRepositoryByEntity<MedicalRecord>();
            var visionRecordRepo = _unitOfWork.GetRepositoryByEntity<VisionRecord>();
            var hearingRecordRepo = _unitOfWork.GetRepositoryByEntity<HearingRecord>();
            var physicalRecordRepo = _unitOfWork.GetRepositoryByEntity<PhysicalRecord>();

            var duplicateCheck = await userRepo.GetQueryable().AnyAsync(u =>
                (u.Username == model.Username || u.Email == model.Email || u.StudentCode == model.StudentCode)
                && !u.IsDeleted);

            if (duplicateCheck)
            {
                return new BaseResponse<StudentResponse>
                {
                    Success = false,
                    Message = "Tên đăng nhập, email hoặc mã học sinh đã tồn tại."
                };
            }

            if (!string.IsNullOrEmpty(model.PhoneNumber))
            {
                var phoneNumberExists = await userRepo.GetQueryable()
                    .AnyAsync(u => u.PhoneNumber == model.PhoneNumber && !u.IsDeleted);

                if (phoneNumberExists)
                {
                    return new BaseResponse<StudentResponse>
                    {
                        Success = false,
                        Message = "Số điện thoại đã được sử dụng."
                    };
                }
            }

            if (model.ParentId.HasValue)
            {
                var parent = await userRepo.GetQueryable()
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == model.ParentId.Value && !u.IsDeleted);

                if (parent == null)
                {
                    return new BaseResponse<StudentResponse>
                    {
                        Success = false,
                        Message = "Phụ huynh không tồn tại."
                    };
                }

                if (!parent.UserRoles.Any(ur => ur.Role.Name == "PARENT"))
                {
                    return new BaseResponse<StudentResponse>
                    {
                        Success = false,
                        Message = "Người dùng được chỉ định không phải là phụ huynh."
                    };
                }
            }

            if (model.ClassId.HasValue)
            {
                var schoolClass = await schoolClassRepo.GetQueryable()
                    .FirstOrDefaultAsync(sc => sc.Id == model.ClassId.Value && !sc.IsDeleted);

                if (schoolClass == null)
                {
                    return new BaseResponse<StudentResponse>
                    {
                        Success = false,
                        Message = "Lớp học không tồn tại."
                    };
                }
            }

            string defaultPassword = GenerateDefaultPassword();
            string passwordHash = HashPassword(defaultPassword);
            var managerRoleName = await GetManagerRoleName();

            var user = _mapper.Map<ApplicationUser>(model);
            user.Id = Guid.NewGuid();
            user.PasswordHash = passwordHash;
            user.CreatedBy = managerRoleName;
            user.CreatedDate = DateTime.Now;
            user.IsActive = true;

            await userRepo.AddAsync(user);

            var roleRepo = _unitOfWork.GetRepositoryByEntity<Role>();
            var role = await roleRepo.GetQueryable()
                .FirstOrDefaultAsync(r => r.Name == "STUDENT");

            if (role == null)
            {
                return new BaseResponse<StudentResponse>
                {
                    Success = false,
                    Message = "Vai trò không hợp lệ."
                };
            }

            var userRole = new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = role.Id,
                CreatedBy = managerRoleName,
                CreatedDate = DateTime.Now
            };

            await _unitOfWork.GetRepositoryByEntity<UserRole>().AddAsync(userRole);

            if (model.ClassId.HasValue)
            {
                var studentClass = new StudentClass
                {
                    Id = Guid.NewGuid(),
                    StudentId = user.Id,
                    ClassId = model.ClassId.Value,
                    EnrollmentDate = DateTime.Now,
                    CreatedBy = managerRoleName,
                    CreatedDate = DateTime.Now
                };

                await studentClassRepo.AddAsync(studentClass);
            }

            // Tạo MedicalRecord và các bảng liên quan
            var medicalRecord = new MedicalRecord
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CreatedBy = managerRoleName,
                CreatedDate = DateTime.Now,
                BloodType = "Unknown",
                EmergencyContact = "Unknown",
                EmergencyContactPhone = "Unknown",
                MedicalConditions = new List<MedicalCondition>(),
                VaccinationRecords = new List<VaccinationRecord>(),
                VisionRecords = new List<VisionRecord>(),
                HearingRecords = new List<HearingRecord>(),
                PhysicalRecords = new List<PhysicalRecord>()
            };

            await medicalRecordRepo.AddAsync(medicalRecord);

            // Tạo VisionRecord với giá trị mặc định
            var visionRecord = new VisionRecord
            {
                Id = Guid.NewGuid(),
                MedicalRecordId = medicalRecord.Id,
                LeftEye = 0,
                RightEye = 0,
                CheckDate = DateTime.MinValue,
                Comments = "Not recorded",
                RecordedBy = currentUserId
            };
            await visionRecordRepo.AddAsync(visionRecord);

            // Tạo HearingRecord với giá trị mặc định
            var hearingRecord = new HearingRecord
            {
                Id = Guid.NewGuid(),
                MedicalRecordId = medicalRecord.Id,
                LeftEar = "Not recorded",
                RightEar = "Not recorded",
                CheckDate = DateTime.MinValue,
                Comments = null,
                RecordedBy = currentUserId
            };
            await hearingRecordRepo.AddAsync(hearingRecord);

            // Tạo PhysicalRecord với giá trị mặc định
            var physicalRecord = new PhysicalRecord
            {
                Id = Guid.NewGuid(),
                MedicalRecordId = medicalRecord.Id,
                Height = 0,
                Weight = 0,
                BMI = 0,
                CheckDate = DateTime.MinValue,
                Comments = "Not recorded",
                RecordedBy = currentUserId
            };
            await physicalRecordRepo.AddAsync(physicalRecord);

            await _unitOfWork.SaveChangesAsync();

            await _emailService.SendAccountCreationEmailAsync(user.Email, user.Username, defaultPassword);
            await InvalidateStudentCacheAsync();

            var studentWithRelations = await userRepo.GetQueryable()
                .Include(u => u.StudentClasses.Where(sc => !sc.IsDeleted))
                .ThenInclude(sc => sc.SchoolClass)
                .Include(u => u.Parent)
                .Include(u => u.MedicalRecord)
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            var studentResponse = MapToStudentResponse(studentWithRelations);

            return new BaseResponse<StudentResponse>
            {
                Success = true,
                Data = studentResponse,
                Message = "Tài khoản học sinh đã được tạo thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student account");
            return new BaseResponse<StudentResponse>
            {
                Success = false,
                Message = $"Lỗi tạo tài khoản học sinh: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<StudentResponse>> UpdateStudentAsync(Guid studentId, UpdateStudentRequest model)
    {
        try
        {
            var validationResult = await _updateStudentValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return new BaseResponse<StudentResponse>
                {
                    Success = false,
                    Message = errors
                };
            }

            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var user = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == studentId && !u.IsDeleted &&
                                          u.UserRoles.Any(ur => ur.Role.Name == "STUDENT"));

            if (user == null)
            {
                return new BaseResponse<StudentResponse>
                {
                    Success = false,
                    Message = "Không tìm thấy học sinh."
                };
            }

            if (user.PhoneNumber != model.PhoneNumber || user.StudentCode != model.StudentCode)
            {
                var duplicateExists = await userRepo.GetQueryable()
                    .AnyAsync(u => u.Id != studentId && !u.IsDeleted &&
                                   (u.PhoneNumber == model.PhoneNumber || u.StudentCode == model.StudentCode));

                if (duplicateExists)
                {
                    return new BaseResponse<StudentResponse>
                    {
                        Success = false,
                        Message = "Số điện thoại hoặc mã học sinh đã được sử dụng."
                    };
                }
            }

            var managerRoleName = await GetManagerRoleName();

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;
            user.Gender = model.Gender;
            user.DateOfBirth = model.DateOfBirth;
            user.StudentCode = model.StudentCode;
            user.LastUpdatedBy = managerRoleName;
            user.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();

            await InvalidateUserLoginCacheAsync(user);
            await InvalidateStudentCacheAsync();

            var studentWithRelations = await userRepo.GetQueryable()
                .Include(u => u.StudentClasses.Where(sc => !sc.IsDeleted))
                .ThenInclude(sc => sc.SchoolClass)
                .Include(u => u.Parent)
                .Include(u => u.MedicalRecord)
                .FirstOrDefaultAsync(u => u.Id == studentId);

            var studentResponse = MapToStudentResponse(studentWithRelations);

            return new BaseResponse<StudentResponse>
            {
                Success = true,
                Data = studentResponse,
                Message = "Tài khoản học sinh đã được cập nhật thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating student account");
            return new BaseResponse<StudentResponse>
            {
                Success = false,
                Message = $"Lỗi cập nhật tài khoản học sinh: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<bool>> DeleteStudentAsync(Guid studentId)
    {
        try
        {
            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var student = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.MedicalRecord)
                .FirstOrDefaultAsync(u => u.Id == studentId && !u.IsDeleted &&
                                          u.UserRoles.Any(ur => ur.Role.Name == "STUDENT"));

            if (student == null)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "Không tìm thấy học sinh."
                };
            }

            var canDeleteResult = await ValidateStudentDeletion(studentId);
            if (!canDeleteResult.CanDelete)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = canDeleteResult.Reason
                };
            }

            var managerRoleName = await GetManagerRoleName();

            student.IsDeleted = true;
            student.LastUpdatedBy = managerRoleName;
            student.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            await InvalidateStudentCacheAsync(studentId);

            return new BaseResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Học sinh đã được xóa thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting student: {StudentId}", studentId);
            return new BaseResponse<bool>
            {
                Success = false,
                Data = false,
                Message = $"Lỗi xóa học sinh: {ex.Message}"
            };
        }
    }

    #endregion

    #region Parent Management (Manager)

    public async Task<BaseListResponse<ParentResponse>> GetParentsAsync(
        int pageIndex,
        int pageSize,
        string searchTerm,
        string orderBy,
        bool? hasChildren = null,
        string relationship = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(
                PARENT_LIST_PREFIX,
                pageIndex.ToString(),
                pageSize.ToString(),
                searchTerm ?? "",
                orderBy ?? "",
                hasChildren?.ToString() ?? "",
                relationship ?? ""
            );

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<ParentResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Parents list found in cache");
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.Children.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.StudentClasses.Where(sc => !sc.IsDeleted))
                .ThenInclude(sc => sc.SchoolClass)
                .Include(u => u.Children.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.MedicalRecord)
                .Where(u => !u.IsDeleted && u.UserRoles.Any(ur => ur.Role.Name == "PARENT"))
                .AsQueryable();

            query = ApplyParentFilters(query, hasChildren, relationship, searchTerm);
            query = ApplyParentOrdering(query, orderBy);

            var totalCount = await query.CountAsync(cancellationToken);
            var parents = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = parents.Select(MapToParentResponse).ToList();

            var result = BaseListResponse<ParentResponse>.SuccessResult(
                responses,
                totalCount,
                pageSize,
                pageIndex,
                "Lấy danh sách phụ huynh thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            await _cacheService.AddToTrackingSetAsync(cacheKey, PARENT_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving parents");
            return BaseListResponse<ParentResponse>.ErrorResult("Lỗi lấy danh sách phụ huynh.");
        }
    }

    public async Task<BaseResponse<ParentResponse>> GetParentByIdAsync(Guid parentId)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(PARENT_CACHE_PREFIX, parentId.ToString());
            var cachedResponse = await _cacheService.GetAsync<BaseResponse<ParentResponse>>(cacheKey);

            if (cachedResponse != null)
            {
                _logger.LogDebug("Parent found in cache: {ParentId}", parentId);
                return cachedResponse;
            }

            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var parent = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.Children.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.StudentClasses.Where(sc => !sc.IsDeleted))
                .ThenInclude(sc => sc.SchoolClass)
                .Include(u => u.Children.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.MedicalRecord)
                .Where(u => u.Id == parentId && !u.IsDeleted &&
                            u.UserRoles.Any(ur => ur.Role.Name == "PARENT"))
                .FirstOrDefaultAsync();

            if (parent == null)
            {
                return new BaseResponse<ParentResponse>
                {
                    Success = false,
                    Message = "Không tìm thấy phụ huynh."
                };
            }

            var parentResponse = MapToParentResponse(parent);

            var response = new BaseResponse<ParentResponse>
            {
                Success = true,
                Data = parentResponse,
                Message = "Lấy thông tin phụ huynh thành công."
            };

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15));
            await _cacheService.AddToTrackingSetAsync(cacheKey, PARENT_CACHE_SET);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parent by ID: {ParentId}", parentId);
            return new BaseResponse<ParentResponse>
            {
                Success = false,
                Message = $"Lỗi lấy thông tin phụ huynh: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<ParentResponse>> CreateParentAsync(CreateParentRequest model)
    {
        try
        {
            var validationResult = await _createParentValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return new BaseResponse<ParentResponse>
                {
                    Success = false,
                    Message = errors
                };
            }

            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();

            var duplicateCheck = await userRepo.GetQueryable().AnyAsync(u =>
                (u.Username == model.Username || u.Email == model.Email) && !u.IsDeleted);

            if (duplicateCheck)
            {
                return new BaseResponse<ParentResponse>
                {
                    Success = false,
                    Message = "Tên đăng nhập hoặc email đã tồn tại."
                };
            }

            if (!string.IsNullOrEmpty(model.PhoneNumber))
            {
                var phoneNumberExists = await userRepo.GetQueryable()
                    .AnyAsync(u => u.PhoneNumber == model.PhoneNumber && !u.IsDeleted);

                if (phoneNumberExists)
                {
                    return new BaseResponse<ParentResponse>
                    {
                        Success = false,
                        Message = "Số điện thoại đã được sử dụng."
                    };
                }
            }

            string defaultPassword = GenerateDefaultPassword();
            string passwordHash = HashPassword(defaultPassword);
            var managerRoleName = await GetManagerRoleName();

            var user = _mapper.Map<ApplicationUser>(model);
            user.Id = Guid.NewGuid();
            user.PasswordHash = passwordHash;
            user.CreatedBy = managerRoleName;
            user.CreatedDate = DateTime.Now;
            user.IsActive = true;

            await userRepo.AddAsync(user);

            var roleRepo = _unitOfWork.GetRepositoryByEntity<Role>();
            var role = await roleRepo.GetQueryable()
                .FirstOrDefaultAsync(r => r.Name == "PARENT");

            if (role == null)
            {
                return new BaseResponse<ParentResponse>
                {
                    Success = false,
                    Message = "Vai trò không hợp lệ."
                };
            }

            var userRole = new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = role.Id,
                CreatedBy = managerRoleName,
                CreatedDate = DateTime.Now
            };

            await _unitOfWork.GetRepositoryByEntity<UserRole>().AddAsync(userRole);
            await _unitOfWork.SaveChangesAsync();

            await _emailService.SendAccountCreationEmailAsync(user.Email, user.Username, defaultPassword);
            await InvalidateParentCacheAsync();

            var parentWithRelations = await userRepo.GetQueryable()
                .Include(u => u.Children.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.StudentClasses.Where(sc => !sc.IsDeleted))
                .ThenInclude(sc => sc.SchoolClass)
                .Include(u => u.Children.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.MedicalRecord)
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            var parentResponse = MapToParentResponse(parentWithRelations);

            return new BaseResponse<ParentResponse>
            {
                Success = true,
                Data = parentResponse,
                Message = "Tài khoản phụ huynh đã được tạo thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating parent account");
            return new BaseResponse<ParentResponse>
            {
                Success = false,
                Message = $"Lỗi tạo tài khoản phụ huynh: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<ParentResponse>> UpdateParentAsync(Guid parentId, UpdateParentRequest model)
    {
        try
        {
            var validationResult = await _updateParentValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return new BaseResponse<ParentResponse>
                {
                    Success = false,
                    Message = errors
                };
            }

            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var user = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == parentId && !u.IsDeleted &&
                                          u.UserRoles.Any(ur => ur.Role.Name == "PARENT"));

            if (user == null)
            {
                return new BaseResponse<ParentResponse>
                {
                    Success = false,
                    Message = "Không tìm thấy phụ huynh."
                };
            }

            if (user.PhoneNumber != model.PhoneNumber)
            {
                var duplicateExists = await userRepo.GetQueryable()
                    .AnyAsync(u => u.Id != parentId && !u.IsDeleted && u.PhoneNumber == model.PhoneNumber);

                if (duplicateExists)
                {
                    return new BaseResponse<ParentResponse>
                    {
                        Success = false,
                        Message = "Số điện thoại đã được sử dụng."
                    };
                }
            }

            var managerRoleName = await GetManagerRoleName();

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;
            user.Gender = model.Gender;
            user.DateOfBirth = model.DateOfBirth;
            user.Relationship = model.Relationship;
            user.LastUpdatedBy = managerRoleName;
            user.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();

            await InvalidateUserLoginCacheAsync(user);
            await InvalidateParentCacheAsync();

            var parentWithRelations = await userRepo.GetQueryable()
                .Include(u => u.Children.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.StudentClasses.Where(sc => !sc.IsDeleted))
                .ThenInclude(sc => sc.SchoolClass)
                .Include(u => u.Children.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.MedicalRecord)
                .FirstOrDefaultAsync(u => u.Id == parentId);

            var parentResponse = MapToParentResponse(parentWithRelations);

            return new BaseResponse<ParentResponse>
            {
                Success = true,
                Data = parentResponse,
                Message = "Tài khoản phụ huynh đã được cập nhật thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating parent account");
            return new BaseResponse<ParentResponse>
            {
                Success = false,
                Message = $"Lỗi cập nhật tài khoản phụ huynh: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<bool>> DeleteParentAsync(Guid parentId)
    {
        try
        {
            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var parent = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.Children.Where(c => !c.IsDeleted))
                .FirstOrDefaultAsync(u => u.Id == parentId && !u.IsDeleted &&
                                          u.UserRoles.Any(ur => ur.Role.Name == "PARENT"));

            if (parent == null)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "Không tìm thấy phụ huynh."
                };
            }

            var canDeleteResult = await ValidateParentDeletion(parentId, parent);
            if (!canDeleteResult.CanDelete)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = canDeleteResult.Reason
                };
            }

            var managerRoleName = await GetManagerRoleName();

            parent.IsDeleted = true;
            parent.LastUpdatedBy = managerRoleName;
            parent.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            await InvalidateParentCacheAsync(parentId);

            return new BaseResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Phụ huynh đã được xóa thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting parent: {ParentId}", parentId);
            return new BaseResponse<bool>
            {
                Success = false,
                Data = false,
                Message = $"Lỗi xóa phụ huynh: {ex.Message}"
            };
        }
    }

    #endregion

    #region Parent-Student Relationship Management

    /// <summary>
    /// Link một parent với một student
    /// </summary>
    public async Task<BaseResponse<bool>> LinkParentToStudentAsync(Guid parentId, Guid studentId,
        bool allowReplace = false)
    {
        try
        {
            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();

            var parent = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.Children.Where(c => !c.IsDeleted))
                .FirstOrDefaultAsync(u => u.Id == parentId && !u.IsDeleted &&
                                          u.UserRoles.Any(ur => ur.Role.Name == "PARENT"));

            if (parent == null)
            {
                return BaseResponse<bool>.ErrorResult("Không tìm thấy phụ huynh.");
            }

            var student = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.Parent) // Để check parent hiện tại
                .FirstOrDefaultAsync(u => u.Id == studentId && !u.IsDeleted &&
                                          u.UserRoles.Any(ur => ur.Role.Name == "STUDENT"));

            if (student == null)
            {
                return BaseResponse<bool>.ErrorResult("Không tìm thấy học sinh.");
            }

            if (student.ParentId.HasValue && student.ParentId != parentId)
            {
                if (!allowReplace)
                {
                    return BaseResponse<bool>.ErrorResult(
                        $"Học sinh {student.FullName} đã được liên kết với phụ huynh '{student.Parent?.FullName}'. " +
                        "Vui lòng hủy liên kết hiện tại trước khi liên kết mới.");
                }
                else
                {
                    _logger.LogInformation("Replacing parent {OldParentId} with {NewParentId} for student {StudentId}",
                        student.ParentId, parentId, studentId);
                }
            }

            if (student.ParentId == parentId)
            {
                return BaseResponse<bool>.ErrorResult("Học sinh đã được liên kết với phụ huynh này rồi.");
            }

            var maxStudentsPerParent = 10;
            var currentStudentCount = parent.Children?.Count ?? 0;

            if (currentStudentCount >= maxStudentsPerParent)
            {
                return BaseResponse<bool>.ErrorResult(
                    $"Phụ huynh {parent.FullName} đã liên kết với {currentStudentCount} học sinh. " +
                    $"Không thể liên kết thêm (tối đa {maxStudentsPerParent} học sinh).");
            }

            var managerRoleName = await GetManagerRoleName();

            student.ParentId = parentId;
            student.LastUpdatedBy = managerRoleName;
            student.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();

            await InvalidateUserLoginCacheAsync(parent);
            await InvalidateUserLoginCacheAsync(student);
            await InvalidateStudentCacheAsync();
            await InvalidateParentCacheAsync();

            _logger.LogInformation(
                "Successfully linked parent {ParentId} ({ParentName}) to student {StudentId} ({StudentName})",
                parentId, parent.FullName, studentId, student.FullName);

            return BaseResponse<bool>.SuccessResult(true,
                $"Liên kết phụ huynh '{parent.FullName}' với học sinh '{student.FullName}' thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking parent {ParentId} to student {StudentId}", parentId, studentId);
            return BaseResponse<bool>.ErrorResult($"Lỗi liên kết phụ huynh với học sinh: {ex.Message}");
        }
    }

    /// <summary>
    /// Hủy liên kết parent khỏi student
    /// </summary>
    public async Task<BaseResponse<bool>> UnlinkParentFromStudentAsync(Guid studentId, bool forceUnlink = false)
    {
        try
        {
            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();

            var student = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.Parent)
                .Include(u => u.MedicalRecord)
                .FirstOrDefaultAsync(u => u.Id == studentId && !u.IsDeleted &&
                                          u.UserRoles.Any(ur => ur.Role.Name == "STUDENT"));

            if (student == null)
            {
                return BaseResponse<bool>.ErrorResult("Không tìm thấy học sinh.");
            }

            if (!student.ParentId.HasValue)
            {
                return BaseResponse<bool>.ErrorResult("Học sinh chưa được liên kết với phụ huynh nào.");
            }

            if (!forceUnlink)
            {
                var validationResult = await ValidateUnlinkDependencies(studentId, student.ParentId.Value);
                if (!validationResult.canUnlink)
                {
                    return BaseResponse<bool>.ErrorResult(
                        $"{validationResult.reason} " +
                        "Liên hệ quản trị viên nếu cần huỷ liên kết khẩn cấp.");
                }
            }
            else
            {
                _logger.LogWarning("Force unlinking parent {ParentId} from student {StudentId} - bypassing validations",
                    student.ParentId, studentId);
            }

            var parent = student.Parent;
            var managerRoleName = await GetManagerRoleName();

            student.ParentId = null;
            student.LastUpdatedBy = managerRoleName;
            student.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();

            await InvalidateUserLoginCacheAsync(student);
            if (parent != null)
            {
                await InvalidateUserLoginCacheAsync(parent);
            }

            await InvalidateStudentCacheAsync();
            await InvalidateParentCacheAsync();

            var message = forceUnlink
                ? "Hủy liên kết phụ huynh với học sinh thành công (buộc phải hủy liên kết)."
                : "Hủy liên kết phụ huynh với học sinh thành công.";

            _logger.LogInformation(
                "Successfully unlinked parent {ParentId} ({ParentName}) from student {StudentId} ({StudentName})",
                parent?.Id, parent?.FullName, studentId, student.FullName);

            return BaseResponse<bool>.SuccessResult(true, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlinking parent from student: {StudentId}", studentId);
            return BaseResponse<bool>.ErrorResult($"Lỗi hủy liên kết phụ huynh với học sinh: {ex.Message}");
        }
    }

    #endregion

    #region Excel Import/Export Methods

    public async Task<byte[]> DownloadManagerTemplateAsync()
    {
        try
        {
            _logger.LogInformation("Generating school class Excel template");
            return await _excelService.GenerateManagerTemplateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading school class template");
            throw;
        }
    }

    public async Task<byte[]> DownloadSchoolNurseTemplateAsync()
    {
        try
        {
            _logger.LogInformation("Generating school class Excel template");
            return await _excelService.GenerateSchoolNurseTemplateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading school class template");
            throw;
        }
    }

    public async Task<byte[]> DownloadStudentParentCombinedTemplateAsync()
    {
        try
        {
            _logger.LogInformation("Generating student-parent combined Excel template");
            return await _excelService.GenerateStudentParentCombinedTemplateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading student-parent combined template");
            throw;
        }
    }

    public async Task<BaseResponse<ExcelImportResult<ManagerResponse>>> ImportManagersFromExcelAsync(IFormFile file)
    {
        try
        {
            var templateValidation = await ValidateManagerTemplate(file);
            if (templateValidation != null && !templateValidation.Success)
            {
                return templateValidation;
            }

            var excelResult = await _excelService.ReadManagerExcelAsync(file);

            if (!excelResult.Success)
            {
                return new BaseResponse<ExcelImportResult<ManagerResponse>>
                {
                    Success = false,
                    Message = excelResult.Message
                };
            }

            var importResult = new ExcelImportResult<ManagerResponse>
            {
                TotalRows = excelResult.TotalRows,
                Success = true,
                Message = "Import hoàn tất."
            };

            var successfulManagers = new List<ManagerResponse>();
            var failedImports = new List<string>();

            foreach (var managerData in excelResult.ValidData)
            {
                try
                {
                    var createRequest = _mapper.Map<CreateManagerRequest>(managerData);
                    var createResult = await CreateManagerAsync(createRequest);

                    if (createResult.Success)
                    {
                        successfulManagers.Add(createResult.Data);
                    }
                    else
                    {
                        failedImports.Add($"Manager {managerData.Username}: {createResult.Message}");
                    }
                }
                catch (Exception ex)
                {
                    failedImports.Add($"Manager {managerData.Username}: {ex.Message}");
                }
            }

            foreach (var invalidData in excelResult.InvalidData)
            {
                failedImports.Add($"Manager {invalidData.Username}: {invalidData.ErrorMessage}");
            }

            importResult.ValidData = successfulManagers;
            importResult.SuccessRows = successfulManagers.Count;
            importResult.ErrorRows = failedImports.Count;
            importResult.Errors = failedImports;

            if (failedImports.Any())
            {
                importResult.Message += $" Thành công: {importResult.SuccessRows}, Lỗi: {importResult.ErrorRows}";
            }

            return new BaseResponse<ExcelImportResult<ManagerResponse>>
            {
                Success = true,
                Data = importResult,
                Message = "Import Manager hoàn tất."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing managers from Excel");
            return new BaseResponse<ExcelImportResult<ManagerResponse>>
            {
                Success = false,
                Message = $"Lỗi import Manager: {ex.Message}"
            };
        }
    }

    public async Task<byte[]> ExportManagersToExcelAsync(string searchTerm = "", string orderBy = null)
    {
        try
        {
            _logger.LogInformation(
                "Exporting managers to Excel with filters - SearchTerm: {SearchTerm}, OrderBy: {OrderBy}", searchTerm,
                orderBy);

            var managersResponse = await GetStaffUsersAsync(1, int.MaxValue, searchTerm, orderBy, "MANAGER");

            if (!managersResponse.Success)
            {
                throw new InvalidOperationException($"Failed to get managers data: {managersResponse.Message}");
            }

            return await _excelService.ExportManagersToExcelAsync(managersResponse.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting managers to Excel");
            throw;
        }
    }

    public async Task<BaseResponse<ExcelImportResult<SchoolNurseResponse>>> ImportSchoolNursesFromExcelAsync(
        IFormFile file)
    {
        try
        {
            var templateValidation = await ValidateSchoolNurseTemplate(file);
            if (templateValidation != null && !templateValidation.Success)
            {
                return templateValidation;
            }

            var excelResult = await _excelService.ReadSchoolNurseExcelAsync(file);

            if (!excelResult.Success)
            {
                return new BaseResponse<ExcelImportResult<SchoolNurseResponse>>
                {
                    Success = false,
                    Message = excelResult.Message
                };
            }

            var importResult = new ExcelImportResult<SchoolNurseResponse>
            {
                TotalRows = excelResult.TotalRows,
                Success = true,
                Message = "Import hoàn tất."
            };

            var successfulNurses = new List<SchoolNurseResponse>();
            var failedImports = new List<string>();

            foreach (var nurseData in excelResult.ValidData)
            {
                try
                {
                    var createRequest = _mapper.Map<CreateSchoolNurseRequest>(nurseData);
                    var createResult = await CreateSchoolNurseAsync(createRequest);

                    if (createResult.Success)
                    {
                        successfulNurses.Add(createResult.Data);
                    }
                    else
                    {
                        failedImports.Add($"School Nurse {nurseData.Username}: {createResult.Message}");
                    }
                }
                catch (Exception ex)
                {
                    failedImports.Add($"School Nurse {nurseData.Username}: {ex.Message}");
                }
            }

            foreach (var invalidData in excelResult.InvalidData)
            {
                failedImports.Add($"School Nurse {invalidData.Username}: {invalidData.ErrorMessage}");
            }

            importResult.ValidData = successfulNurses;
            importResult.SuccessRows = successfulNurses.Count;
            importResult.ErrorRows = failedImports.Count;
            importResult.Errors = failedImports;

            if (failedImports.Any())
            {
                importResult.Message += $" Thành công: {importResult.SuccessRows}, Lỗi: {importResult.ErrorRows}";
            }

            return new BaseResponse<ExcelImportResult<SchoolNurseResponse>>
            {
                Success = true,
                Data = importResult,
                Message = "Import School Nurse hoàn tất."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing school nurses from Excel");
            return new BaseResponse<ExcelImportResult<SchoolNurseResponse>>
            {
                Success = false,
                Message = $"Lỗi import School Nurse: {ex.Message}"
            };
        }
    }

    public async Task<byte[]> ExportSchoolNursesToExcelAsync(string searchTerm = "", string orderBy = null)
    {
        try
        {
            _logger.LogInformation(
                "Exporting school nurses to Excel with filters - SearchTerm: {SearchTerm}, OrderBy: {OrderBy}",
                searchTerm, orderBy);

            var nursesResponse = await GetStaffUsersAsync(1, int.MaxValue, searchTerm, orderBy, "SCHOOLNURSE");

            if (!nursesResponse.Success)
            {
                throw new InvalidOperationException($"Failed to get school nurses data: {nursesResponse.Message}");
            }

            return await _excelService.ExportSchoolNursesToExcelAsync(nursesResponse.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting school nurses to Excel");
            throw;
        }
    }

    public async Task<BaseResponse<StudentParentCombinedImportResult>> ImportStudentParentCombinedFromExcelAsync(
    IFormFile file)
    {
        var executionStrategy = _unitOfWork.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(async () =>
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            var failedImports = new List<string>();
            try
            {
                var excelResult = await _excelService.ReadStudentParentCombinedExcelAsync(file);

                if (!excelResult.Success)
                {
                    return new BaseResponse<StudentParentCombinedImportResult>
                    {
                        Success = false,
                        Message = excelResult.Message
                    };
                }

                var importResult = new StudentParentCombinedImportResult
                {
                    TotalRows = excelResult.TotalRows,
                    Success = true,
                    Message = "Import hoàn tất."
                };

                var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
                var roleRepo = _unitOfWork.GetRepositoryByEntity<Role>();
                var classRepo = _unitOfWork.GetRepositoryByEntity<SchoolClass>();
                var medicalRecordRepo = _unitOfWork.GetRepositoryByEntity<MedicalRecord>();

                var studentRole = await roleRepo.GetQueryable().FirstOrDefaultAsync(r => r.Name == "STUDENT");
                var parentRole = await roleRepo.GetQueryable().FirstOrDefaultAsync(r => r.Name == "PARENT");

                if (studentRole == null || parentRole == null)
                {
                    return new BaseResponse<StudentParentCombinedImportResult>
                    {
                        Success = false,
                        Message = "Không tìm thấy vai trò STUDENT hoặc PARENT trong hệ thống."
                    };
                }

                var parentPhoneNumbers = excelResult.ValidData.Select(d => d.ParentPhoneNumber).Distinct().ToList();
                var existingParents = await userRepo.GetQueryable()
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .Where(u => !u.IsDeleted &&
                                parentPhoneNumbers.Contains(u.PhoneNumber) &&
                                u.UserRoles.Any(ur => ur.Role.Name == "PARENT"))
                    .ToDictionaryAsync(u => u.PhoneNumber, u => u);

                var allClasses = await classRepo.GetQueryable()
                    .Where(c => !c.IsDeleted)
                    .ToListAsync();
                var classLookup = allClasses.ToDictionary(c => $"{c.Name}|{c.Grade}|{c.AcademicYear}", c => c);

                var processedParents = new Dictionary<string, Guid>();
                var createdParents = new List<ParentResponse>();
                var createdStudents = new List<StudentResponse>();
                var warnings = new List<string>();

                var managerRoleName = await GetManagerRoleName();

                foreach (var data in excelResult.ValidData)
                {
                    try
                    {
                        var duplicateCheck = await ValidateUniqueConstraintsForStudent(data);
                        if (!duplicateCheck.IsValid)
                        {
                            failedImports.Add($"Học sinh {data.StudentUsername}: {duplicateCheck.Message}");
                            throw new InvalidOperationException($"Lỗi: {duplicateCheck.Message}");
                        }

                        var classValidationResult = await ValidateAndResolveClasses(data, classLookup);
                        if (!classValidationResult.IsValid)
                        {
                            failedImports.Add($"Học sinh {data.StudentUsername}: {classValidationResult.Message}");
                            throw new InvalidOperationException($"Lỗi: {classValidationResult.Message}");
                        }

                        var hasParentInfo = !string.IsNullOrWhiteSpace(data.ParentFullName) &&
                                            !string.IsNullOrWhiteSpace(data.ParentPhoneNumber) &&
                                            !string.IsNullOrWhiteSpace(data.ParentEmail);

                        Guid? parentId = null;
                        bool isNewParent = false;

                        if (hasParentInfo)
                        {
                            if (existingParents.ContainsKey(data.ParentPhoneNumber))
                            {
                                parentId = existingParents[data.ParentPhoneNumber].Id;
                                data.IsParentExisting = true;
                                data.ExistingParentId = parentId;

                                warnings.Add(
                                    $"Học sinh {data.StudentFullName}: Sử dụng phụ huynh có sẵn - {existingParents[data.ParentPhoneNumber].FullName}");
                            }
                            else if (processedParents.ContainsKey(data.ParentPhoneNumber))
                            {
                                parentId = processedParents[data.ParentPhoneNumber];
                                data.IsParentExisting = true;
                                data.ExistingParentId = parentId;

                                warnings.Add($"Học sinh {data.StudentFullName}: Sử dụng phụ huynh đã tạo trong batch này");
                            }
                            else
                            {
                                var createParentResult =
                                    await CreateParentFromCombinedData(data, parentRole, managerRoleName);
                                if (!createParentResult.Success)
                                {
                                    failedImports.Add($"Phụ huynh {data.ParentFullName}: {createParentResult.Message}");
                                    throw new InvalidOperationException($"Lỗi: {createParentResult.Message}");
                                }

                                parentId = createParentResult.Data.Id;
                                processedParents[data.ParentPhoneNumber] = parentId.Value;
                                createdParents.Add(createParentResult.Data);
                                isNewParent = true;
                            }
                        }
                        else
                        {
                            warnings.Add($"Học sinh {data.StudentFullName}: Được tạo mà không có phụ huynh");
                        }

                        var createStudentResult = await CreateStudentFromCombinedData(data, studentRole, managerRoleName);
                        if (!createStudentResult.Success)
                        {
                            failedImports.Add($"Học sinh {data.StudentUsername}: {createStudentResult.Message}");
                            throw new InvalidOperationException($"Lỗi: {createStudentResult.Message}");
                        }

                        var student = createStudentResult.Data;

                        // Tạo MedicalRecord với các danh sách rỗng
                        var medicalRecord = new MedicalRecord
                        {
                            Id = Guid.NewGuid(),
                            UserId = student.Id,
                            CreatedBy = managerRoleName,
                            CreatedDate = DateTime.Now,
                            BloodType = "Unknown",
                            EmergencyContact = "Unknown",
                            EmergencyContactPhone = "Unknown",
                            MedicalConditions = new List<MedicalCondition>(),
                            VaccinationRecords = new List<VaccinationRecord>(),
                            VisionRecords = new List<VisionRecord>(),
                            HearingRecords = new List<HearingRecord>(),
                            PhysicalRecords = new List<PhysicalRecord>()
                        };
                        await medicalRecordRepo.AddAsync(medicalRecord);

                        if (parentId.HasValue)
                        {
                            var linkResult = await LinkParentToStudentAsync(parentId.Value, student.Id, true);
                            if (!linkResult.Success)
                            {
                                failedImports.Add($"Liên kết {data.StudentFullName} với phụ huynh: {linkResult.Message}");
                                throw new InvalidOperationException($"Lỗi: {linkResult.Message}");
                            }

                            importResult.SuccessfulLinks++;
                        }

                        var studentWithParent = await userRepo.GetQueryable()
                            .Include(u => u.Parent)
                            .Include(u => u.StudentClasses.Where(sc => !sc.IsDeleted))
                            .ThenInclude(sc => sc.SchoolClass)
                            .Include(u => u.MedicalRecord)
                            .FirstOrDefaultAsync(u => u.Id == student.Id);

                        if (studentWithParent != null)
                        {
                            student = MapToStudentResponse(studentWithParent);
                        }

                        createdStudents.Add(student);

                        var classEnrollmentResult =
                            await AddStudentToMultipleClasses(student.Id, data.ClassInfoList, managerRoleName);
                        data.ClassEnrollmentResults = classEnrollmentResult.Results;

                        importResult.TotalClassEnrollments += classEnrollmentResult.TotalAttempts;
                        importResult.SuccessfulClassEnrollments += classEnrollmentResult.SuccessCount;
                        importResult.FailedClassEnrollments += classEnrollmentResult.FailureCount;

                        importResult.ClassEnrollmentDetails.Add(new ClassEnrollmentSummary
                        {
                            StudentName = student.FullName,
                            StudentCode = student.StudentCode,
                            EnrollmentResults = classEnrollmentResult.Results
                        });

                        if (hasParentInfo)
                        {
                            if (!isNewParent)
                            {
                                var existingParent = existingParents.ContainsKey(data.ParentPhoneNumber)
                                    ? existingParents[data.ParentPhoneNumber]
                                    : await userRepo.GetQueryable().FirstOrDefaultAsync(u => u.Id == parentId);

                                if (existingParent != null)
                                {
                                    var currentChildrenCount =
                                        importResult.ParentChildrenCount.GetValueOrDefault(data.ParentPhoneNumber, 0) + 1;

                                    await _emailService.SendChildAddedNotificationAsync(
                                        parentEmail: existingParent.Email,
                                        childName: data.StudentFullName,
                                        parentName: existingParent.FullName,
                                        studentCode: data.StudentCode,
                                        className: string.Join(", ", data.ClassList),
                                        relationship: data.ParentRelationship ?? existingParent.Relationship ?? "Guardian",
                                        totalChildren: currentChildrenCount,
                                        parentPhone: existingParent.PhoneNumber
                                    );
                                }
                            }

                            if (!importResult.ParentChildrenCount.ContainsKey(data.ParentPhoneNumber))
                            {
                                importResult.ParentChildrenCount[data.ParentPhoneNumber] = 0;
                            }

                            importResult.ParentChildrenCount[data.ParentPhoneNumber]++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing row for student {StudentUsername}", data.StudentUsername);
                        failedImports.Add($"Học sinh {data.StudentUsername}: Lỗi xử lý - {ex.Message}");
                        throw;
                    }
                }

                await _unitOfWork.SaveChangesAsync();

                await _cacheService.RemoveByPrefixAsync(STUDENT_LIST_PREFIX);
                _logger.LogDebug("Đã xóa cache danh sách học sinh với prefix: {Prefix}", STUDENT_LIST_PREFIX);
                await _cacheService.RemoveByPrefixAsync(PARENT_LIST_PREFIX);
                _logger.LogDebug("Đã xóa cache danh sách phụ huynh với prefix: {Prefix}", PARENT_LIST_PREFIX);
                await _cacheService.RemoveByPrefixAsync(CLASS_ENROLLMENT_PREFIX);
                _logger.LogDebug("Đã xóa cache liên kết lớp học với prefix: {Prefix}", CLASS_ENROLLMENT_PREFIX);

                await InvalidateAllCachesAsync();

                importResult.SuccessfulStudents = createdStudents.Count;
                importResult.SuccessfulParents = createdParents.Count;
                importResult.ErrorRows = failedImports.Count;
                importResult.Errors = failedImports;
                importResult.Warnings = warnings;

                importResult.CreatedStudents = createdStudents;
                importResult.CreatedParents = createdParents;

                var studentsWithoutParents = createdStudents.Count(s => !s.HasParent);

                if (failedImports.Any())
                {
                    throw new InvalidOperationException($"Tồn tại lỗi trong dữ liệu import. Chi tiết: {string.Join(", ", failedImports)}");
                }
                else
                {
                    await transaction.CommitAsync();
                    importResult.Message = $"Import thành công! " +
                                           $"Đã tạo {importResult.SuccessfulStudents} học sinh " +
                                           $"({importResult.SuccessfulLinks} có phụ huynh, {studentsWithoutParents} chưa có), " +
                                           $"{importResult.SuccessfulParents} phụ huynh mới, " +
                                           $"sử dụng {importResult.ExistingParentsUsed.Count} phụ huynh có sẵn, " +
                                           $"add vào {importResult.SuccessfulClassEnrollments} lớp học.";
                }

                return new BaseResponse<StudentParentCombinedImportResult>
                {
                    Success = true,
                    Data = importResult,
                    Message = importResult.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing student-parent combined from Excel");
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                    _logger.LogWarning("Giao dịch đã bị rollback do lỗi: {Error}", ex.Message);
                }
                return new BaseResponse<StudentParentCombinedImportResult>
                {
                    Success = false,
                    Message = $"Lỗi import Student-Parent: {ex.Message}. Vui lòng kiểm tra dữ liệu và import lại.",
                    Data = new StudentParentCombinedImportResult
                    {
                        Errors = failedImports
                    }
                };
            }
        });
    }

    public async Task<byte[]> ExportParentStudentRelationshipAsync()
    {
        try
        {
            _logger.LogInformation("Exporting parent-student relationship report with class information");

            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();

            var studentsWithParents = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.Parent)
                .Include(u => u.StudentClasses.Where(sc => !sc.IsDeleted))
                .ThenInclude(sc => sc.SchoolClass)
                .Where(u => !u.IsDeleted && u.UserRoles.Any(ur => ur.Role.Name == "STUDENT"))
                .OrderBy(u => u.FullName)
                .ToListAsync();

            using var package = new ExcelPackage();

            var studentsSheet = package.Workbook.Worksheets.Add("Chi_Tiet_HS_PH_Lop");
            await CreateDetailedStudentParentClassSheet(studentsSheet, studentsWithParents);

            var parentsSheet = package.Workbook.Worksheets.Add("Danh_Sach_Phu_Huynh");
            await CreateParentChildrenSummaryWithClassesSheet(parentsSheet);

            var orphanStudentsSheet = package.Workbook.Worksheets.Add("Hoc_Sinh_Chua_Co_PH");
            await CreateStudentsWithoutParentsWithClassesSheet(orphanStudentsSheet, studentsWithParents);

            var classStatsSheet = package.Workbook.Worksheets.Add("Thong_Ke_Lop_Hoc");
            await CreateClassEnrollmentStatisticsSheet(classStatsSheet, studentsWithParents);

            return package.GetAsByteArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting parent-student relationship");
            throw;
        }
    }

    #endregion

    #region Helper Methods

    private async Task<string> GetAdminRoleName()
    {
        try
        {
            var adminRole = await _unitOfWork.GetRepositoryByEntity<Role>().GetQueryable()
                .FirstOrDefaultAsync(r => r.Name == "ADMIN");

            return adminRole?.Name ?? "ADMIN";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin role name");
            return "ADMIN";
        }
    }

    private async Task<string> GetManagerRoleName()
    {
        try
        {
            var managerRole = await _unitOfWork.GetRepositoryByEntity<Role>().GetQueryable()
                .FirstOrDefaultAsync(r => r.Name == "MANAGER");

            return managerRole?.Name ?? "MANAGER";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting manager role name");
            return "MANAGER";
        }
    }

    private string GenerateDefaultPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private string HashPassword(string password)
    {
        return PasswordHelper.HashPassword(password);
    }

    private StudentResponse MapToStudentResponse(ApplicationUser student)
    {
        var response = _mapper.Map<StudentResponse>(student);

        if (student.StudentClasses != null && student.StudentClasses.Any(sc => !sc.IsDeleted))
        {
            var activeStudentClasses = student.StudentClasses
                .Where(sc => !sc.IsDeleted)
                .ToList();

            response.Classes = _mapper.Map<List<StudentClassInfo>>(activeStudentClasses);
            response.ClassCount = response.Classes.Count;

            if (activeStudentClasses.Any())
            {
                var currentClass = activeStudentClasses
                    .OrderByDescending(sc => sc.SchoolClass?.Grade ?? 0) // Highest grade first
                    .ThenByDescending(sc => sc.EnrollmentDate) // If same grade, take most recent
                    .First();

                response.CurrentClassName = currentClass.SchoolClass?.Name;
                response.CurrentGrade = currentClass.SchoolClass?.Grade;
            }
        }

        return response;
    }

    private ParentResponse MapToParentResponse(ApplicationUser parent)
    {
        var response = _mapper.Map<ParentResponse>(parent);

        if (parent.Children != null && parent.Children.Any())
        {
            var activeChildren = parent.Children.Where(c => !c.IsDeleted).ToList();

            response.Children = new List<StudentSummaryResponse>();

            foreach (var child in activeChildren)
            {
                var studentSummary = new StudentSummaryResponse
                {
                    Id = child.Id,
                    FullName = child.FullName,
                    StudentCode = child.StudentCode,
                    HasMedicalRecord = child.MedicalRecord != null
                };

                if (child.StudentClasses != null)
                {
                    var activeClasses = child.StudentClasses.Where(sc => !sc.IsDeleted).ToList();
                    studentSummary.ClassCount = activeClasses.Count;

                    if (activeClasses.Any())
                    {
                        var currentClass = activeClasses
                            .OrderByDescending(sc => sc.SchoolClass?.Grade ?? 0)
                            .ThenByDescending(sc => sc.EnrollmentDate)
                            .First();

                        studentSummary.CurrentClassName = currentClass.SchoolClass?.Name;
                        studentSummary.CurrentGrade = currentClass.SchoolClass?.Grade;

                        studentSummary.ClassNames = activeClasses
                            .Select(sc => sc.SchoolClass?.Name)
                            .Where(name => !string.IsNullOrEmpty(name))
                            .OrderBy(name => name)
                            .ToList();
                    }
                }

                response.Children.Add(studentSummary);
            }

            response.ChildrenCount = response.Children.Count;
        }

        return response;
    }

    private IQueryable<ApplicationUser> ApplyStaffOrdering(IQueryable<ApplicationUser> query, string orderBy)
    {
        return orderBy?.ToLower() switch
        {
            "username" => query.OrderBy(u => u.Username),
            "username_desc" => query.OrderByDescending(u => u.Username),
            "email" => query.OrderBy(u => u.Email),
            "email_desc" => query.OrderByDescending(u => u.Email),
            "fullname" => query.OrderBy(u => u.FullName),
            "fullname_desc" => query.OrderByDescending(u => u.FullName),
            "staffcode" => query.OrderBy(u => u.StaffCode),
            "staffcode_desc" => query.OrderByDescending(u => u.StaffCode),
            "role" => query.OrderBy(u => u.UserRoles.FirstOrDefault().Role.Name),
            "role_desc" => query.OrderByDescending(u => u.UserRoles.FirstOrDefault().Role.Name),
            "createdate_desc" => query.OrderByDescending(u => u.CreatedDate),
            "createdate" => query.OrderBy(u => u.CreatedDate),
            _ => query.OrderByDescending(u => u.CreatedDate)
        };
    }

    private IQueryable<ApplicationUser> ApplyStudentFilters(
        IQueryable<ApplicationUser> query,
        Guid? classId,
        bool? hasMedicalRecord,
        bool? hasParent,
        string searchTerm)
    {
        if (classId.HasValue)
        {
            query = query.Where(u => u.StudentClasses.Any(sc => sc.ClassId == classId.Value && !sc.IsDeleted));
        }

        if (hasMedicalRecord.HasValue)
        {
            if (hasMedicalRecord.Value)
            {
                query = query.Where(u => u.MedicalRecord != null);
            }
            else
            {
                query = query.Where(u => u.MedicalRecord == null);
            }
        }

        if (hasParent.HasValue)
        {
            if (hasParent.Value)
            {
                query = query.Where(u => u.ParentId != null);
            }
            else
            {
                query = query.Where(u => u.ParentId == null);
            }
        }

        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(u =>
                u.Username.ToLower().Contains(searchTerm) ||
                u.Email.ToLower().Contains(searchTerm) ||
                u.FullName.ToLower().Contains(searchTerm) ||
                u.PhoneNumber.Contains(searchTerm) ||
                u.StudentCode.ToLower().Contains(searchTerm) ||
                u.StudentClasses.Any(sc => !sc.IsDeleted && sc.SchoolClass.Name.ToLower().Contains(searchTerm)) ||
                (u.Parent != null && u.Parent.FullName.ToLower().Contains(searchTerm)));
        }

        return query;
    }

    private IQueryable<ApplicationUser> ApplyStudentOrdering(IQueryable<ApplicationUser> query, string orderBy)
    {
        return orderBy?.ToLower() switch
        {
            "studentcode" => query.OrderBy(u => u.StudentCode),
            "studentcode_desc" => query.OrderByDescending(u => u.StudentCode),
            "fullname" => query.OrderBy(u => u.FullName),
            "fullname_desc" => query.OrderByDescending(u => u.FullName),
            "class" => query.OrderBy(u =>
                u.StudentClasses.Where(sc => !sc.IsDeleted).FirstOrDefault().SchoolClass.Name),
            "class_desc" => query.OrderByDescending(u =>
                u.StudentClasses.Where(sc => !sc.IsDeleted).FirstOrDefault().SchoolClass.Name),
            "parent" => query.OrderBy(u => u.Parent.FullName),
            "parent_desc" => query.OrderByDescending(u => u.Parent.FullName),
            "createdate_desc" => query.OrderByDescending(u => u.CreatedDate),
            "createdate" => query.OrderBy(u => u.CreatedDate),
            _ => query.OrderBy(u => u.FullName)
        };
    }

    private IQueryable<ApplicationUser> ApplyParentFilters(
        IQueryable<ApplicationUser> query,
        bool? hasChildren,
        string relationship,
        string searchTerm)
    {
        if (hasChildren.HasValue)
        {
            if (hasChildren.Value)
            {
                query = query.Where(u => u.Children.Any(c => !c.IsDeleted));
            }
            else
            {
                query = query.Where(u => !u.Children.Any(c => !c.IsDeleted));
            }
        }

        if (!string.IsNullOrEmpty(relationship))
        {
            query = query.Where(u => u.Relationship == relationship);
        }

        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(u =>
                u.Username.ToLower().Contains(searchTerm) ||
                u.Email.ToLower().Contains(searchTerm) ||
                u.FullName.ToLower().Contains(searchTerm) ||
                u.PhoneNumber.Contains(searchTerm) ||
                u.Children.Any(c => c.FullName.ToLower().Contains(searchTerm) ||
                                    c.StudentCode.ToLower().Contains(searchTerm)));
        }

        return query;
    }

    private IQueryable<ApplicationUser> ApplyParentOrdering(IQueryable<ApplicationUser> query, string orderBy)
    {
        return orderBy?.ToLower() switch
        {
            "fullname" => query.OrderBy(u => u.FullName),
            "fullname_desc" => query.OrderByDescending(u => u.FullName),
            "relationship" => query.OrderBy(u => u.Relationship),
            "relationship_desc" => query.OrderByDescending(u => u.Relationship),
            "childrencount" => query.OrderBy(u => u.Children.Count(c => !c.IsDeleted)),
            "childrencount_desc" => query.OrderByDescending(u => u.Children.Count(c => !c.IsDeleted)),
            "createdate_desc" => query.OrderByDescending(u => u.CreatedDate),
            "createdate" => query.OrderBy(u => u.CreatedDate),
            _ => query.OrderBy(u => u.FullName)
        };
    }

    private async Task<(bool IsValid, string Message)> ValidateUniqueConstraints(StudentParentCombinedExcelModel data)
    {
        var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();

        var studentExists = await userRepo.GetQueryable().AnyAsync(u =>
            (u.Username == data.StudentUsername ||
             u.Email == data.StudentEmail ||
             u.StudentCode == data.StudentCode) && !u.IsDeleted);

        if (studentExists)
        {
            return (false, "Tên đăng nhập, email hoặc mã học sinh đã tồn tại");
        }

        if (!string.IsNullOrEmpty(data.StudentPhoneNumber) &&
            data.StudentPhoneNumber != data.ParentPhoneNumber)
        {
            var studentPhoneExists = await userRepo.GetQueryable().AnyAsync(u =>
                u.PhoneNumber == data.StudentPhoneNumber && !u.IsDeleted);

            if (studentPhoneExists)
            {
                return (false, "Số điện thoại học sinh đã tồn tại trong hệ thống (và khác với SĐT phụ huynh)");
            }
        }

        if (!string.IsNullOrEmpty(data.ParentEmail))
        {
            var existingParentByPhone = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.PhoneNumber == data.ParentPhoneNumber &&
                                          !u.IsDeleted &&
                                          u.UserRoles.Any(ur => ur.Role.Name == "PARENT"));

            if (existingParentByPhone != null)
            {
                if (existingParentByPhone.Email != data.ParentEmail)
                {
                    return (false,
                        $"Số điện thoại phụ huynh {data.ParentPhoneNumber} đã tồn tại với email khác ({existingParentByPhone.Email}). " +
                        "Bạn không thể sử dụng email khác cho phụ huynh đã có sẵn.");
                }
            }
            else
            {
                var parentEmailExists = await userRepo.GetQueryable().AnyAsync(u =>
                    u.Email == data.ParentEmail && !u.IsDeleted);

                if (parentEmailExists)
                {
                    return (false, "Email phụ huynh đã tồn tại trong hệ thống");
                }
            }
        }

        return (true, "");
    }

    private async Task<BaseResponse<ParentResponse>> CreateParentFromCombinedData(
        StudentParentCombinedExcelModel data, Role parentRole, string managerRoleName)
    {
        try
        {
            var username = $"parent_{data.ParentPhoneNumber}";
            data.GeneratedParentUsername = username;

            if (string.IsNullOrEmpty(data.ParentEmail))
            {
                return new BaseResponse<ParentResponse>
                {
                    Success = false,
                    Message = "Email phụ huynh bắt buộc khi tạo mới"
                };
            }

            var defaultPassword = GenerateDefaultPassword();
            var passwordHash = HashPassword(defaultPassword);

            var parent = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = data.ParentEmail,
                PasswordHash = passwordHash,
                FullName = data.ParentFullName,
                PhoneNumber = data.ParentPhoneNumber,
                Address = data.ParentAddress,
                Gender = data.ParentGender,
                DateOfBirth = data.ParentDateOfBirth,
                Relationship = data.ParentRelationship,
                CreatedBy = managerRoleName,
                CreatedDate = DateTime.Now,
                IsActive = true
            };

            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            await userRepo.AddAsync(parent);

            var userRole = new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = parent.Id,
                RoleId = parentRole.Id,
                CreatedBy = managerRoleName,
                CreatedDate = DateTime.Now
            };

            await _unitOfWork.GetRepositoryByEntity<UserRole>().AddAsync(userRole);
            await _unitOfWork.SaveChangesAsync();

            await _emailService.SendAccountCreationEmailAsync(parent.Email, parent.Username, defaultPassword);

            var parentResponse = _mapper.Map<ParentResponse>(parent);

            return new BaseResponse<ParentResponse>
            {
                Success = true,
                Data = parentResponse,
                Message = "Tạo phụ huynh thành công"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating parent from combined data");
            return new BaseResponse<ParentResponse>
            {
                Success = false,
                Message = $"Lỗi tạo phụ huynh: {ex.Message}"
            };
        }
    }

    private async Task<BaseResponse<StudentResponse>> CreateStudentFromCombinedData(
        StudentParentCombinedExcelModel data, Role studentRole, string managerRoleName)
    {
        try
        {
            var defaultPassword = GenerateDefaultPassword();
            var passwordHash = HashPassword(defaultPassword);

            var student = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Username = data.StudentUsername,
                Email = data.StudentEmail,
                PasswordHash = passwordHash,
                FullName = data.StudentFullName,
                PhoneNumber = data.StudentPhoneNumber,
                Address = data.StudentAddress,
                Gender = data.StudentGender,
                DateOfBirth = data.StudentDateOfBirth,
                StudentCode = data.StudentCode,
                CreatedBy = managerRoleName,
                CreatedDate = DateTime.Now,
                IsActive = true
            };

            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            await userRepo.AddAsync(student);

            var userRole = new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = student.Id,
                RoleId = studentRole.Id,
                CreatedBy = managerRoleName,
                CreatedDate = DateTime.Now
            };

            await _unitOfWork.GetRepositoryByEntity<UserRole>().AddAsync(userRole);
            await _unitOfWork.SaveChangesAsync();

            await _emailService.SendAccountCreationEmailAsync(student.Email, student.Username, defaultPassword);

            var studentWithRelations = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.StudentClasses.Where(sc => !sc.IsDeleted))
                .ThenInclude(sc => sc.SchoolClass)
                .Include(u => u.Parent)
                .Include(u => u.MedicalRecord)
                .FirstOrDefaultAsync(u => u.Id == student.Id);

            var studentResponse = MapToStudentResponse(studentWithRelations ?? student);

            return new BaseResponse<StudentResponse>
            {
                Success = true,
                Data = studentResponse,
                Message = "Tạo học sinh thành công"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student from combined data");
            return new BaseResponse<StudentResponse>
            {
                Success = false,
                Message = $"Lỗi tạo học sinh: {ex.Message}"
            };
        }
    }

    private async Task CreateDetailedStudentParentClassSheet(ExcelWorksheet worksheet, List<ApplicationUser> students)
    {
        var headers = new[]
        {
            "STT", "Mã Học Sinh", "Họ Tên Học Sinh", "SĐT Học Sinh", "Có Phụ Huynh", "Tên Phụ Huynh",
            "SĐT Phụ Huynh", "Email Phụ Huynh", "Mối Quan Hệ", "Kiểu Liên Kết", "Số Lớp Đang Học",
            "Danh Sách Lớp", "Lớp Chính", "Khối Chính", "Năm Học", "Ngày Tạo HS", "Ngày Liên Kết PH"
        };

        CreateHeaderRow(worksheet, headers);

        for (int i = 0; i < students.Count; i++)
        {
            var student = students[i];
            var row = i + 2;

            var activeClasses = student.StudentClasses?
                .Where(sc => !sc.IsDeleted && sc.SchoolClass != null)
                .OrderByDescending(sc => sc.SchoolClass.Grade)
                .ThenByDescending(sc => sc.EnrollmentDate)
                .ToList() ?? new List<StudentClass>();

            var primaryClass = activeClasses.FirstOrDefault()?.SchoolClass;
            var classNames = string.Join("; ", activeClasses.Select(sc =>
                $"{sc.SchoolClass.Name} (K{sc.SchoolClass.Grade}-{sc.SchoolClass.AcademicYear})"));

            var linkageType = "";
            if (student.ParentId.HasValue)
            {
                if (student.PhoneNumber == student.Parent?.PhoneNumber)
                {
                    linkageType = "Cùng SĐT";
                }
                else
                {
                    linkageType = "Liên kết hàng ngang";
                }
            }
            else
            {
                linkageType = "Chưa có phụ huynh";
            }

            worksheet.Cells[row, 1].Value = i + 1;
            worksheet.Cells[row, 2].Value = student.StudentCode;
            worksheet.Cells[row, 3].Value = student.FullName;
            worksheet.Cells[row, 4].Value = student.PhoneNumber;
            worksheet.Cells[row, 5].Value = student.ParentId.HasValue ? "Có" : "Không";
            worksheet.Cells[row, 6].Value = student.Parent?.FullName ?? "";
            worksheet.Cells[row, 7].Value = student.Parent?.PhoneNumber ?? "";
            worksheet.Cells[row, 8].Value = student.Parent?.Email ?? "";
            worksheet.Cells[row, 9].Value = student.Parent?.Relationship ?? "";
            worksheet.Cells[row, 10].Value = linkageType;
            worksheet.Cells[row, 11].Value = activeClasses.Count;
            worksheet.Cells[row, 12].Value = classNames;
            worksheet.Cells[row, 13].Value = primaryClass?.Name ?? "Chưa có lớp";
            worksheet.Cells[row, 14].Value = primaryClass?.Grade;
            worksheet.Cells[row, 15].Value = primaryClass?.AcademicYear;
            worksheet.Cells[row, 16].Value = student.CreatedDate?.ToString("dd/MM/yyyy");
            worksheet.Cells[row, 17].Value = student.Parent?.CreatedDate?.ToString("dd/MM/yyyy");

            if (!student.ParentId.HasValue)
            {
                using (var range = worksheet.Cells[row, 1, row, headers.Length])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightPink);
                }
            }
            else if (linkageType == "Cùng SĐT")
            {
                using (var range = worksheet.Cells[row, 4, row, 4])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
                }

                using (var range = worksheet.Cells[row, 7, row, 7])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
                }
            }
            else if (activeClasses.Count == 0)
            {
                using (var range = worksheet.Cells[row, 1, row, headers.Length])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
                }
            }
        }

        worksheet.Cells.AutoFitColumns();
        await Task.CompletedTask;
    }

    private async Task CreateParentChildrenSummaryWithClassesSheet(ExcelWorksheet worksheet)
    {
        var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();

        var parents = await userRepo.GetQueryable()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Include(u => u.Children.Where(c => !c.IsDeleted))
            .ThenInclude(c => c.StudentClasses.Where(sc => !sc.IsDeleted))
            .ThenInclude(sc => sc.SchoolClass)
            .Where(u => !u.IsDeleted && u.UserRoles.Any(ur => ur.Role.Name == "PARENT"))
            .OrderByDescending(u => u.Children.Count(c => !c.IsDeleted))
            .ThenBy(u => u.FullName)
            .ToListAsync();

        var headers = new[]
        {
            "STT", "Họ Tên Phụ Huynh", "SĐT", "Email", "Mối Quan Hệ",
            "Số Con", "Con Cùng SĐT", "Con SĐT Riêng", "Danh Sách Con", "Tổng Số Lớp Con Đang Học",
            "Chi Tiết Lớp Học", "Ngày Tạo TK", "Ngày Cập Nhật"
        };

        CreateHeaderRow(worksheet, headers);

        for (int i = 0; i < parents.Count; i++)
        {
            var parent = parents[i];
            var row = i + 2;

            var children = parent.Children.Where(c => !c.IsDeleted).ToList();
            var childrenNames = string.Join("; ", children.Select(c => c.FullName));

            var childrenWithSamePhone = children.Count(c => c.PhoneNumber == parent.PhoneNumber);
            var childrenWithDifferentPhone = children.Count - childrenWithSamePhone;

            var totalClasses = children.Sum(c => c.StudentClasses?.Count(sc => !sc.IsDeleted) ?? 0);
            var classDetails = string.Join("; ", children.SelectMany(c =>
                (c.StudentClasses?.Where(sc => !sc.IsDeleted) ?? new List<StudentClass>())
                .Select(sc => $"{c.FullName}→{sc.SchoolClass?.Name}(K{sc.SchoolClass?.Grade})")
            ));

            worksheet.Cells[row, 1].Value = i + 1;
            worksheet.Cells[row, 2].Value = parent.FullName;
            worksheet.Cells[row, 3].Value = parent.PhoneNumber;
            worksheet.Cells[row, 4].Value = parent.Email;
            worksheet.Cells[row, 5].Value = parent.Relationship;
            worksheet.Cells[row, 6].Value = children.Count;
            worksheet.Cells[row, 7].Value = childrenWithSamePhone;
            worksheet.Cells[row, 8].Value = childrenWithDifferentPhone;
            worksheet.Cells[row, 9].Value = childrenNames;
            worksheet.Cells[row, 10].Value = totalClasses;
            worksheet.Cells[row, 11].Value = classDetails;
            worksheet.Cells[row, 12].Value = parent.CreatedDate?.ToString("dd/MM/yyyy");
            worksheet.Cells[row, 13].Value = parent.LastUpdatedDate?.ToString("dd/MM/yyyy");

            if (children.Count == 0)
            {
                using (var range = worksheet.Cells[row, 1, row, headers.Length])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                }
            }
            else if (children.Count >= 3)
            {
                using (var range = worksheet.Cells[row, 1, row, headers.Length])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                }
            }

            if (childrenWithSamePhone > 0)
            {
                using (var range = worksheet.Cells[row, 7, row, 7])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
                }
            }
        }

        worksheet.Cells.AutoFitColumns();
    }

    private async Task CreateStudentsWithoutParentsWithClassesSheet(ExcelWorksheet worksheet,
        List<ApplicationUser> allStudents)
    {
        var studentsWithoutParents = allStudents.Where(s => !s.ParentId.HasValue).ToList();

        var headers = new[]
        {
            "STT", "Mã Học Sinh", "Họ Tên", "Email", "SĐT", "Ngày Sinh",
            "Số Lớp Đang Học", "Danh Sách Lớp", "Lớp Chính", "Khối", "Năm Học",
            "Linkage Type", "Gợi Ý Liên Kết", "Ngày Tạo TK"
        };

        CreateHeaderRow(worksheet, headers);

        for (int i = 0; i < studentsWithoutParents.Count; i++)
        {
            var student = studentsWithoutParents[i];
            var row = i + 2;

            var activeClasses = student.StudentClasses?
                .Where(sc => !sc.IsDeleted && sc.SchoolClass != null)
                .OrderByDescending(sc => sc.SchoolClass.Grade)
                .ThenByDescending(sc => sc.EnrollmentDate)
                .ToList() ?? new List<StudentClass>();

            var primaryClass = activeClasses.FirstOrDefault()?.SchoolClass;
            var classNames = string.Join("; ", activeClasses.Select(sc =>
                $"{sc.SchoolClass.Name}(K{sc.SchoolClass.Grade}-{sc.SchoolClass.AcademicYear})"));

            var suggestionText = "Cần tạo phụ huynh mới";

            worksheet.Cells[row, 1].Value = i + 1;
            worksheet.Cells[row, 2].Value = student.StudentCode;
            worksheet.Cells[row, 3].Value = student.FullName;
            worksheet.Cells[row, 4].Value = student.Email;
            worksheet.Cells[row, 5].Value = student.PhoneNumber;
            worksheet.Cells[row, 6].Value = student.DateOfBirth?.ToString("dd/MM/yyyy");
            worksheet.Cells[row, 7].Value = activeClasses.Count;
            worksheet.Cells[row, 8].Value = classNames;
            worksheet.Cells[row, 9].Value = primaryClass?.Name ?? "Chưa có lớp";
            worksheet.Cells[row, 10].Value = primaryClass?.Grade;
            worksheet.Cells[row, 11].Value = primaryClass?.AcademicYear;
            worksheet.Cells[row, 12].Value = "Chưa có phụ huynh";
            worksheet.Cells[row, 13].Value = suggestionText;
            worksheet.Cells[row, 14].Value = student.CreatedDate?.ToString("dd/MM/yyyy");

            using (var range = worksheet.Cells[row, 1, row, headers.Length])
            {
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightPink);
            }
        }

        worksheet.Cells.AutoFitColumns();
        await Task.CompletedTask;
    }

    private async Task CreateClassEnrollmentStatisticsSheet(ExcelWorksheet worksheet, List<ApplicationUser> students)
    {
        var classStats = students
            .SelectMany(s =>
                s.StudentClasses?.Where(sc => !sc.IsDeleted && sc.SchoolClass != null) ?? new List<StudentClass>())
            .GroupBy(sc => new
            {
                ClassId = sc.ClassId,
                ClassName = sc.SchoolClass.Name,
                Grade = sc.SchoolClass.Grade,
                AcademicYear = sc.SchoolClass.AcademicYear
            })
            .Select(g => new
            {
                ClassInfo = g.Key,
                TotalStudents = g.Count(),
                StudentsWithParents = g.Count(sc => sc.Student.ParentId.HasValue),
                StudentsWithoutParents = g.Count(sc => !sc.Student.ParentId.HasValue),
                StudentsWithSamePhone = g.Count(sc => sc.Student.ParentId.HasValue &&
                                                      sc.Student.PhoneNumber == sc.Student.Parent.PhoneNumber),
                StudentsWithExplicitLink = g.Count(sc => sc.Student.ParentId.HasValue &&
                                                         sc.Student.PhoneNumber != sc.Student.Parent.PhoneNumber),
                MaleStudents = g.Count(sc => sc.Student.Gender == "Male"),
                FemaleStudents = g.Count(sc => sc.Student.Gender == "Female"),
                StudentDetails = g.Select(sc => new
                {
                    sc.Student.FullName,
                    sc.Student.StudentCode,
                    sc.Student.PhoneNumber,
                    sc.Student.ParentId,
                    ParentName = sc.Student.Parent?.FullName,
                    ParentPhone = sc.Student.Parent?.PhoneNumber,
                    EnrollmentDate = sc.EnrollmentDate
                }).ToList(),
            })
            .OrderBy(x => x.ClassInfo.AcademicYear)
            .ThenBy(x => x.ClassInfo.Grade)
            .ThenBy(x => x.ClassInfo.ClassName)
            .ToList();

        var headers = new[]
        {
            "STT", "Tên Lớp", "Khối", "Năm Học", "Tổng HS", "HS Có PH", "HS Chưa Có PH",
            "% Có PH", "HS Cùng SĐT", "HS SĐT Riêng", "Nam", "Nữ",
            "Danh Sách HS", "HS Chưa Có PH", "Ngày Tạo Lớp Gần Nhất"
        };

        CreateHeaderRow(worksheet, headers);

        for (int i = 0; i < classStats.Count; i++)
        {
            var stat = classStats[i];
            var row = i + 2;

            var parentPercentage = stat.TotalStudents > 0
                ? Math.Round((double)stat.StudentsWithParents / stat.TotalStudents * 100, 1)
                : 0;

            var allStudentsInClass = string.Join("; ", stat.StudentDetails.Select(s =>
                $"{s.FullName}({s.StudentCode}){(s.ParentId.HasValue ? "✓" : "✗")}"));

            var studentsWithoutParents = string.Join("; ", stat.StudentDetails
                .Where(s => !s.ParentId.HasValue)
                .Select(s => $"{s.FullName}({s.StudentCode})"));

            var latestEnrollmentDate = stat.StudentDetails.Any()
                ? stat.StudentDetails.Max(s => s.EnrollmentDate)
                : (DateTime?)null;

            worksheet.Cells[row, 1].Value = i + 1;
            worksheet.Cells[row, 2].Value = stat.ClassInfo.ClassName;
            worksheet.Cells[row, 3].Value = stat.ClassInfo.Grade;
            worksheet.Cells[row, 4].Value = stat.ClassInfo.AcademicYear;
            worksheet.Cells[row, 5].Value = stat.TotalStudents;
            worksheet.Cells[row, 6].Value = stat.StudentsWithParents;
            worksheet.Cells[row, 7].Value = stat.StudentsWithoutParents;
            worksheet.Cells[row, 8].Value = $"{parentPercentage}%";
            worksheet.Cells[row, 9].Value = stat.StudentsWithSamePhone;
            worksheet.Cells[row, 10].Value = stat.StudentsWithExplicitLink;
            worksheet.Cells[row, 11].Value = stat.MaleStudents;
            worksheet.Cells[row, 12].Value = stat.FemaleStudents;
            worksheet.Cells[row, 13].Value = allStudentsInClass;
            worksheet.Cells[row, 14].Value = studentsWithoutParents;
            worksheet.Cells[row, 15].Value = latestEnrollmentDate?.ToString("dd/MM/yyyy") ?? "";

            if (stat.StudentsWithoutParents > 0)
            {
                using (var range = worksheet.Cells[row, 7, row, 7])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightCoral);
                }
            }

            if (parentPercentage < 50)
            {
                using (var range = worksheet.Cells[row, 8, row, 8])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightPink);
                }
            }
            else if (parentPercentage == 100)
            {
                using (var range = worksheet.Cells[row, 8, row, 8])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                }
            }

            if (stat.StudentsWithSamePhone > 0)
            {
                using (var range = worksheet.Cells[row, 9, row, 9])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
                }
            }
        }

        worksheet.Cells.AutoFitColumns();
        await Task.CompletedTask;
    }

    private void CreateHeaderRow(ExcelWorksheet worksheet, string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
            worksheet.Cells[1, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
        }
    }

    #endregion

    #region Validation Methods

    private async Task<BaseResponse<ExcelImportResult<ManagerResponse>>> ValidateManagerTemplate(IFormFile file)
    {
        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                return new BaseResponse<ExcelImportResult<ManagerResponse>>
                {
                    Success = false,
                    Message = "Không tìm thấy worksheet trong file Excel."
                };
            }

            var warnings = new List<string>();
            var rowCount = worksheet.Dimension?.Rows ?? 0;

            if (rowCount >= 2)
            {
                var firstRowData = worksheet.Cells[2, 1].Text?.Trim();
                if (firstRowData == "manager001" || firstRowData == "manager002")
                {
                    warnings.Add(
                        "⚠️ CẢNH BÁO: Phát hiện dữ liệu mẫu trong file! Vui lòng xóa tất cả dữ liệu mẫu trước khi import.");
                }
            }

            if (warnings.Any())
            {
                return new BaseResponse<ExcelImportResult<ManagerResponse>>
                {
                    Success = false,
                    Message = "File chứa dữ liệu mẫu. " + string.Join(" ", warnings)
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating manager template");
            return new BaseResponse<ExcelImportResult<ManagerResponse>>
            {
                Success = false,
                Message = $"Lỗi kiểm tra template: {ex.Message}"
            };
        }
    }

    private async Task<BaseResponse<ExcelImportResult<SchoolNurseResponse>>> ValidateSchoolNurseTemplate(IFormFile file)
    {
        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                return new BaseResponse<ExcelImportResult<SchoolNurseResponse>>
                {
                    Success = false,
                    Message = "Không tìm thấy worksheet trong file Excel."
                };
            }

            var warnings = new List<string>();
            var rowCount = worksheet.Dimension?.Rows ?? 0;

            if (rowCount >= 2)
            {
                var firstRowData = worksheet.Cells[2, 1].Text?.Trim();
                if (firstRowData == "nurse001" || firstRowData == "nurse002")
                {
                    warnings.Add(
                        "⚠️ CẢNH BÁO: Phát hiện dữ liệu mẫu trong file! Vui lòng xóa tất cả dữ liệu mẫu trước khi import.");
                }
            }

            if (warnings.Any())
            {
                return new BaseResponse<ExcelImportResult<SchoolNurseResponse>>
                {
                    Success = false,
                    Message = "File chứa dữ liệu mẫu. " + string.Join(" ", warnings)
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating school nurse template");
            return new BaseResponse<ExcelImportResult<SchoolNurseResponse>>
            {
                Success = false,
                Message = $"Lỗi kiểm tra template: {ex.Message}"
            };
        }
    }

    private async Task<(bool CanDelete, string Reason)> ValidateStudentDeletion(Guid studentId)
    {
        try
        {
            var hasMedicalRecord = await _unitOfWork.GetRepositoryByEntity<MedicalRecord>().GetQueryable()
                .AnyAsync(mr => mr.UserId == studentId && !mr.IsDeleted);

            if (hasMedicalRecord)
            {
                return (false, "Không thể xóa học sinh đã có hồ sơ y tế.");
            }

            var hasHealthEvents = await _unitOfWork.GetRepositoryByEntity<HealthEvent>().GetQueryable()
                .AnyAsync(he => he.UserId == studentId && !he.IsDeleted);

            if (hasHealthEvents)
            {
                return (false, "Không thể xóa học sinh đã có sự kiện y tế.");
            }

            var hasHealthCheckResults = await _unitOfWork.GetRepositoryByEntity<HealthCheckResult>().GetQueryable()
                .AnyAsync(hcr => hcr.UserId == studentId && !hcr.IsDeleted);

            if (hasHealthCheckResults)
            {
                return (false, "Không thể xóa học sinh đã có kết quả kiểm tra sức khỏe.");
            }

            var hasAppointments = await _unitOfWork.GetRepositoryByEntity<Appointment>().GetQueryable()
                .AnyAsync(a => a.StudentId == studentId && !a.IsDeleted);

            if (hasAppointments)
            {
                return (false, "Không thể xóa học sinh đã có lịch hẹn tư vấn.");
            }

            var hasVaccinationRecords = await _unitOfWork.GetRepositoryByEntity<VaccinationRecord>().GetQueryable()
                .AnyAsync(vr => vr.UserId == studentId && !vr.IsDeleted);

            if (hasVaccinationRecords)
            {
                return (false, "Không thể xóa học sinh đã có hồ sơ tiêm chủng.");
            }

            return (true, "Có thể xóa học sinh.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating student deletion: {StudentId}", studentId);
            return (false, "Lỗi kiểm tra khả năng xóa học sinh.");
        }
    }

    private async Task<(bool CanDelete, string Reason)> ValidateParentDeletion(Guid parentId,
        ApplicationUser parent = null)
    {
        try
        {
            if (parent?.Children != null && parent.Children.Any(c => !c.IsDeleted))
            {
                return (false, "Không thể xóa phụ huynh đang có học sinh liên kết.");
            }

            if (parent == null)
            {
                var hasLinkedStudents = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                    .AnyAsync(u => u.ParentId == parentId && !u.IsDeleted);

                if (hasLinkedStudents)
                {
                    return (false, "Không thể xóa phụ huynh đang có học sinh liên kết.");
                }
            }

            var hasAppointments = await _unitOfWork.GetRepositoryByEntity<Appointment>().GetQueryable()
                .AnyAsync(a => a.ParentId == parentId && !a.IsDeleted);

            if (hasAppointments)
            {
                return (false, "Không thể xóa phụ huynh đã có lịch hẹn tư vấn.");
            }

            var hasImportantNotifications = await _unitOfWork.GetRepositoryByEntity<Notification>().GetQueryable()
                .AnyAsync(n => (n.SenderId == parentId || n.RecipientId == parentId) &&
                               n.RequiresConfirmation && !n.IsConfirmed && !n.IsDeleted);

            if (hasImportantNotifications)
            {
                return (false, "Không thể xóa phụ huynh có thông báo quan trọng chưa xác nhận.");
            }

            return (true, "Có thể xóa phụ huynh.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating parent deletion: {ParentId}", parentId);
            return (false, "Lỗi kiểm tra khả năng xóa phụ huynh.");
        }
    }

    private async Task<(bool CanDelete, string Reason)> ValidateManagerDeletion(Guid managerId)
    {
        try
        {
            var hasCreatedUsers = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                .AnyAsync(u => u.CreatedBy == "MANAGER" && !u.IsDeleted);

            if (hasCreatedUsers)
            {
                return (false, "Không thể xóa Manager đã tạo tài khoản người dùng.");
            }

            var hasBlogPosts = await _unitOfWork.GetRepositoryByEntity<BlogPost>().GetQueryable()
                .AnyAsync(bp => bp.AuthorId == managerId && !bp.IsDeleted);

            if (hasBlogPosts)
            {
                return (false, "Không thể xóa Manager đã tạo bài viết blog.");
            }

            var hasGeneratedReports = await _unitOfWork.GetRepositoryByEntity<Report>().GetQueryable()
                .AnyAsync(r => r.GeneratedById == managerId && !r.IsDeleted);

            if (hasGeneratedReports)
            {
                return (false, "Không thể xóa Manager đã tạo báo cáo.");
            }

            var hasSentNotifications = await _unitOfWork.GetRepositoryByEntity<Notification>().GetQueryable()
                .AnyAsync(n => n.SenderId == managerId && !n.IsDeleted);

            if (hasSentNotifications)
            {
                return (false, "Không thể xóa Manager đã gửi thông báo.");
            }

            return (true, "Có thể xóa Manager.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating manager deletion: {ManagerId}", managerId);
            return (false, "Lỗi kiểm tra khả năng xóa Manager.");
        }
    }

    private async Task<(bool CanDelete, string Reason)> ValidateSchoolNurseDeletion(Guid schoolNurseId)
    {
        try
        {
            var hasAppointments = await _unitOfWork.GetRepositoryByEntity<Appointment>().GetQueryable()
                .AnyAsync(a => a.CounselorId == schoolNurseId && !a.IsDeleted);

            if (hasAppointments)
            {
                return (false, "Không thể xóa School Nurse đã có lịch hẹn tư vấn.");
            }

            var hasHandledHealthEvents = await _unitOfWork.GetRepositoryByEntity<HealthEvent>().GetQueryable()
                .AnyAsync(he => he.HandledById == schoolNurseId && !he.IsDeleted);

            if (hasHandledHealthEvents)
            {
                return (false, "Không thể xóa School Nurse đã xử lý sự kiện y tế.");
            }

            var hasConductedHealthChecks = await _unitOfWork.GetRepositoryByEntity<HealthCheck>().GetQueryable()
                .AnyAsync(hc => hc.ConductedById == schoolNurseId && !hc.IsDeleted);

            if (hasConductedHealthChecks)
            {
                return (false, "Không thể xóa School Nurse đã thực hiện kiểm tra sức khỏe.");
            }

            var hasUsedMedicalItems = await _unitOfWork.GetRepositoryByEntity<MedicalItemUsage>().GetQueryable()
                .AnyAsync(miu => miu.UsedById == schoolNurseId && !miu.IsDeleted);

            if (hasUsedMedicalItems)
            {
                return (false, "Không thể xóa School Nurse đã sử dụng thuốc/vật tư y tế.");
            }

            var hasBlogPosts = await _unitOfWork.GetRepositoryByEntity<BlogPost>().GetQueryable()
                .AnyAsync(bp => bp.AuthorId == schoolNurseId && !bp.IsDeleted);

            if (hasBlogPosts)
            {
                return (false, "Không thể xóa School Nurse đã tạo bài viết blog.");
            }

            var hasGeneratedReports = await _unitOfWork.GetRepositoryByEntity<Report>().GetQueryable()
                .AnyAsync(r => r.GeneratedById == schoolNurseId && !r.IsDeleted);

            if (hasGeneratedReports)
            {
                return (false, "Không thể xóa School Nurse đã tạo báo cáo.");
            }

            var hasSentNotifications = await _unitOfWork.GetRepositoryByEntity<Notification>().GetQueryable()
                .AnyAsync(n => n.SenderId == schoolNurseId && !n.IsDeleted);

            if (hasSentNotifications)
            {
                return (false, "Không thể xóa School Nurse đã gửi thông báo.");
            }

            return (true, "Có thể xóa School Nurse.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating school nurse deletion: {SchoolNurseId}", schoolNurseId);
            return (false, "Lỗi kiểm tra khả năng xóa School Nurse.");
        }
    }

    private async Task<(bool canUnlink, string reason)> ValidateUnlinkDependencies(Guid studentId, Guid parentId)
    {
        try
        {
            var notificationRepo = _unitOfWork.GetRepositoryByEntity<Notification>();
            var appointmentRepo = _unitOfWork.GetRepositoryByEntity<Appointment>();

            var pendingNotifications = await notificationRepo.GetQueryable()
                .Where(n => n.RecipientId == parentId &&
                            !n.IsDeleted &&
                            n.RequiresConfirmation &&
                            !n.IsConfirmed)
                .CountAsync();

            if (pendingNotifications > 0)
            {
                return (false,
                    $"Không thể hủy liên kết. Phụ huynh còn {pendingNotifications} thông báo chưa xác nhận.");
            }

            var activeAppointments = await appointmentRepo.GetQueryable()
                .Where(a => a.StudentId == studentId &&
                            a.ParentId == parentId &&
                            !a.IsDeleted &&
                            a.Status == AppointmentStatus.Scheduled &&
                            a.AppointmentDate > DateTime.Now)
                .CountAsync();

            if (activeAppointments > 0)
            {
                return (false,
                    $"Không thể hủy liên kết. Còn {activeAppointments} lịch hẹn đang hoạt động.");
            }

            var medicalRecordRepo = _unitOfWork.GetRepositoryByEntity<MedicalRecord>();
            var medicalRecord = await medicalRecordRepo.GetQueryable()
                .Include(mr => mr.MedicalConditions)
                .FirstOrDefaultAsync(mr => mr.UserId == studentId && !mr.IsDeleted);

            if (medicalRecord != null)
            {
                var severeConditions = medicalRecord.MedicalConditions
                    .Where(mc => !mc.IsDeleted && mc.Severity == SeverityType.Severe)
                    .Count();

                if (severeConditions > 0)
                {
                    return (false,
                        $"Không thể hủy liên kết. Học sinh có {severeConditions} tình trạng y tế nghiêm trọng cần theo dõi của phụ huynh.");
                }
            }

            return (true, "");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating unlink dependencies");
            return (false, "Lỗi kiểm tra dependencies.");
        }
    }

    private async Task<(bool IsValid, string Message)> ValidateUniqueConstraintsForStudent(
        StudentParentCombinedExcelModel data)
    {
        var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();

        var studentExists = await userRepo.GetQueryable().AnyAsync(u =>
            (u.Username == data.StudentUsername ||
             u.Email == data.StudentEmail ||
             u.StudentCode == data.StudentCode) && !u.IsDeleted);

        if (studentExists)
        {
            return (false, "Tên đăng nhập, email hoặc mã học sinh đã tồn tại");
        }

        if (!string.IsNullOrEmpty(data.StudentPhoneNumber) &&
            data.StudentPhoneNumber != data.ParentPhoneNumber)
        {
            var studentPhoneExists = await userRepo.GetQueryable().AnyAsync(u =>
                u.PhoneNumber == data.StudentPhoneNumber && !u.IsDeleted);

            if (studentPhoneExists)
            {
                return (false, "Số điện thoại học sinh đã tồn tại trong hệ thống");
            }
        }

        if (!string.IsNullOrEmpty(data.ParentEmail))
        {
            var existingParentByPhone = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.PhoneNumber == data.ParentPhoneNumber &&
                                          !u.IsDeleted &&
                                          u.UserRoles.Any(ur => ur.Role.Name == "PARENT"));

            if (existingParentByPhone != null)
            {
                if (existingParentByPhone.Email != data.ParentEmail)
                {
                    return (false,
                        $"Số điện thoại phụ huynh {data.ParentPhoneNumber} đã tồn tại với email khác ({existingParentByPhone.Email}). " +
                        "Bạn phải sử dụng cùng email với phụ huynh đã có sẵn.");
                }
            }
            else
            {
                var parentEmailExists = await userRepo.GetQueryable().AnyAsync(u =>
                    u.Email == data.ParentEmail && !u.IsDeleted);

                if (parentEmailExists)
                {
                    return (false, "Email phụ huynh đã tồn tại trong hệ thống");
                }
            }
        }

        return (true, "");
    }

    private async Task<(bool IsValid, string Message)> ValidateAndResolveClasses(
        StudentParentCombinedExcelModel data,
        Dictionary<string, SchoolClass> classLookup)
    {
        if (!data.ClassInfoList.Any())
        {
            return (true, "");
        }

        var errors = new List<string>();

        foreach (var classInfo in data.ClassInfoList)
        {
            if (!classInfo.IsValid)
            {
                errors.Add($"Lớp {classInfo.Name}: {classInfo.ErrorMessage}");
                continue;
            }

            var classKey = $"{classInfo.Name}|{classInfo.Grade}|{classInfo.AcademicYear}";
            if (!classLookup.ContainsKey(classKey))
            {
                errors.Add(
                    $"Không tìm thấy lớp {classInfo.Name} (Khối {classInfo.Grade}, Năm {classInfo.AcademicYear})");
                continue;
            }

            classInfo.ClassId = classLookup[classKey].Id;
        }

        return (!errors.Any(), string.Join("; ", errors));
    }

    private async Task<ClassEnrollmentBatchResult> AddStudentToMultipleClasses(
        Guid studentId,
        List<ClassInfo> classInfoList,
        string managerRoleName)
    {
        var result = new ClassEnrollmentBatchResult
        {
            TotalAttempts = classInfoList.Count(c => c.IsValid && c.ClassId.HasValue)
        };

        if (!classInfoList.Any() || result.TotalAttempts == 0)
        {
            return result;
        }

        var studentClassRepo = _unitOfWork.GetRepositoryByEntity<StudentClass>();

        var existingEnrollments = await studentClassRepo.GetQueryable()
            .Where(sc => sc.StudentId == studentId && !sc.IsDeleted)
            .Select(sc => sc.ClassId)
            .ToListAsync();

        foreach (var classInfo in classInfoList.Where(c => c.IsValid && c.ClassId.HasValue))
        {
            var enrollmentResult = new ClassEnrollmentResult
            {
                ClassName = classInfo.Name,
                Grade = classInfo.Grade,
                AcademicYear = classInfo.AcademicYear,
                ClassId = classInfo.ClassId
            };

            try
            {
                if (existingEnrollments.Contains(classInfo.ClassId.Value))
                {
                    enrollmentResult.IsSuccess = false;
                    enrollmentResult.Message = $"Học sinh đã trong lớp {classInfo.Name}";
                    result.FailureCount++;
                }
                else
                {
                    var deletedRecord = await studentClassRepo.GetQueryable()
                        .FirstOrDefaultAsync(sc => sc.StudentId == studentId &&
                                                   sc.ClassId == classInfo.ClassId.Value &&
                                                   sc.IsDeleted);

                    if (deletedRecord != null)
                    {
                        deletedRecord.IsDeleted = false;
                        deletedRecord.EnrollmentDate = DateTime.Now;
                        deletedRecord.LastUpdatedBy = managerRoleName;
                        deletedRecord.LastUpdatedDate = DateTime.Now;

                        enrollmentResult.IsSuccess = true;
                        enrollmentResult.Message = "Đã kích hoạt lại trong lớp";
                    }
                    else
                    {
                        var studentClass = new StudentClass
                        {
                            Id = Guid.NewGuid(),
                            StudentId = studentId,
                            ClassId = classInfo.ClassId.Value,
                            EnrollmentDate = DateTime.Now,
                            CreatedBy = managerRoleName,
                            CreatedDate = DateTime.Now
                        };

                        await studentClassRepo.AddAsync(studentClass);
                        enrollmentResult.IsSuccess = true;
                        enrollmentResult.Message = "Đã thêm vào lớp thành công";
                    }

                    result.SuccessCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling student {StudentId} in class {ClassId}",
                    studentId, classInfo.ClassId);

                enrollmentResult.IsSuccess = false;
                enrollmentResult.Message = $"Lỗi: {ex.Message}";
                result.FailureCount++;
            }

            result.Results.Add(enrollmentResult);
        }

        if (result.SuccessCount > 0)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        return result;
    }

    #endregion

    #region Cache Methods

    private async Task InvalidateStaffCacheAsync()
    {
        try
        {
            await _cacheService.RemoveByPrefixAsync(STAFF_CACHE_PREFIX);
            await _cacheService.RemoveByPrefixAsync(STAFF_LIST_PREFIX);
            await _cacheService.RemoveByPrefixAsync(STATISTICS_PREFIX);

            _logger.LogDebug("Invalidated staff cache");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating staff cache");
        }
    }

    private async Task InvalidateUserRelatedCachesAsync(ApplicationUser user)
    {
        try
        {
            // Xóa cache hồ sơ người dùng
            var userCacheKey = _cacheService.GenerateCacheKey(USER_PROFILE_PREFIX, user.Id.ToString());
            await _cacheService.RemoveAsync(userCacheKey);
            _logger.LogDebug("Đã xóa cache hồ sơ người dùng: {CacheKey}", userCacheKey);

            // Xóa cache đăng nhập
            var loginCacheKey = _cacheService.GenerateCacheKey(USER_CACHE_PREFIX, user.Username.ToLower());
            await _cacheService.RemoveAsync(loginCacheKey);
            _logger.LogDebug("Đã xóa cache đăng nhập: {CacheKey}", loginCacheKey);

            // Xóa cache danh sách liên quan
            var cacheTasks = new List<Task>
        {
            _cacheService.RemoveByPrefixAsync(STUDENT_LIST_PREFIX),
            _cacheService.RemoveByPrefixAsync(PARENT_LIST_PREFIX),
            _cacheService.RemoveByPrefixAsync(CLASS_ENROLLMENT_PREFIX)
        };

            // Nếu người dùng là y tá, xóa cache liên quan đến y tá
            if (user.UserRoles.Any(ur => ur.Role.Name.ToUpper() == "SCHOOLNURSE"))
            {
                cacheTasks.Add(_cacheService.RemoveByPrefixAsync("nurse_list"));
            }

            // Nếu người dùng là học sinh hoặc phụ huynh, xóa cache liên quan
            if (user.UserRoles.Any(ur => ur.Role.Name.ToUpper() == "STUDENT" || ur.Role.Name.ToUpper() == "PARENT"))
            {
                cacheTasks.Add(_cacheService.RemoveByPrefixAsync(MEDICATION_LIST_PREFIX));
            }

            // Thực hiện tất cả các lệnh xóa cache đồng thời
            await Task.WhenAll(cacheTasks);

            // Gọi InvalidateUserCacheAsync từ AuthService để đảm bảo làm mới các cache liên quan
            await _authService.InvalidateUserCacheAsync(user.Username, user.Email, user.Id);

            _logger.LogInformation("Đã làm mới tất cả cache liên quan đến người dùng: {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi làm mới cache cho người dùng: {UserId}", user.Id);
        }
    }

    public async Task InvalidateStudentCacheAsync(Guid? studentId = null)
    {
        try
        {
            if (studentId.HasValue)
            {
                var studentCacheKey = _cacheService.GenerateCacheKey(STUDENT_CACHE_PREFIX, studentId.ToString());
                await _cacheService.RemoveAsync(studentCacheKey);
            }

            await _cacheService.RemoveByPrefixAsync(STUDENT_LIST_PREFIX);
            await _cacheService.RemoveByPrefixAsync(STATISTICS_PREFIX);

            _logger.LogDebug("Invalidated student cache");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating student cache");
        }
    }

    private async Task InvalidateParentCacheAsync(Guid? parentId = null)
    {
        try
        {
            if (parentId.HasValue)
            {
                var parentCacheKey = _cacheService.GenerateCacheKey(PARENT_CACHE_PREFIX, parentId.ToString());
                await _cacheService.RemoveAsync(parentCacheKey);
            }

            await _cacheService.RemoveByPrefixAsync(PARENT_LIST_PREFIX);
            await _cacheService.RemoveByPrefixAsync(STATISTICS_PREFIX);

            _logger.LogDebug("Invalidated parent cache");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating parent cache");
        }
    }

    private async Task InvalidateUserLoginCacheAsync(ApplicationUser user)
    {
        try
        {
            if (user != null && _authService != null)
            {
                await _authService.InvalidateUserCacheAsync(user.Username, user.Email, user.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating user login cache for user: {UserId}", user?.Id);
        }
    }

    private async Task InvalidateAllCachesAsync()
    {
        try
        {
            _logger.LogDebug("Starting complete cache invalidation");

            await Task.WhenAll(
                _cacheService.RemoveByPrefixAsync("class"),
                _cacheService.RemoveByPrefixAsync("classes_list"),
                _cacheService.RemoveByPrefixAsync(STUDENT_CACHE_PREFIX),
                _cacheService.RemoveByPrefixAsync(STUDENT_LIST_PREFIX),
                _cacheService.RemoveByPrefixAsync(PARENT_CACHE_PREFIX),
                _cacheService.RemoveByPrefixAsync(USER_PROFILE_PREFIX),
                _cacheService.RemoveByPrefixAsync(PARENT_LIST_PREFIX),
                _cacheService.RemoveByPrefixAsync(USER_CACHE_PREFIX) // Thêm dòng này
            );

            await Task.Delay(100);

            _logger.LogDebug("Completed complete cache invalidation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in complete cache invalidation");
        }
    }

    #endregion
}