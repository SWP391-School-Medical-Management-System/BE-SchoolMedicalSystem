using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.SchoolClassResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.UserResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts.IAuthService;
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
                return new BaseResponse<UserResponses>
                {
                    Success = false,
                    Message = "Người dùng không tồn tại."
                };
            }

            string profileImageUrl = null;
            if (model.ProfileImage != null)
            {
                profileImageUrl = await ProcessProfileImage(model.ProfileImage);
            }

            if (model.DateOfBirth.HasValue && model.DateOfBirth.Value > DateTime.Now)
            {
                return new BaseResponse<UserResponses>
                {
                    Success = false,
                    Message = "Ngày sinh không hợp lệ."
                };
            }

            user.FullName = model.FullName ?? user.FullName;
            user.PhoneNumber = model.PhoneNumber ?? user.PhoneNumber;
            user.Address = model.Address ?? user.Address;
            user.Gender = model.Gender ?? user.Gender;
            user.DateOfBirth = model.DateOfBirth ?? user.DateOfBirth;
            if (profileImageUrl != null)
            {
                user.ProfileImageUrl = profileImageUrl;
            }
            user.LastUpdatedBy = user.Username;
            user.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();

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
            _logger.LogError(ex, "Error updating user profile");
            return new BaseResponse<UserResponses>
            {
                Success = false,
                Message = $"Lỗi cập nhật thông tin: {ex.Message}"
            };
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

    public async Task<BaseResponse<StudentResponse>> CreateStudentAsync(CreateStudentRequest model)
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
    /// <param name="parentId">ID phụ huynh</param>
    /// <param name="studentId">ID học sinh</param>
    /// <param name="allowReplace">Cho phép thay thế parent hiện tại của student</param>
    /// <returns></returns>
    public async Task<BaseResponse<bool>> LinkParentToStudentAsync(Guid parentId, Guid studentId,
        bool allowReplace = false)
    {
        try
        {
            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();

            // Validate Parent
            var parent = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.Children.Where(c => !c.IsDeleted)) // Để check số lượng children hiện tại
                .FirstOrDefaultAsync(u => u.Id == parentId && !u.IsDeleted &&
                                          u.UserRoles.Any(ur => ur.Role.Name == "PARENT"));

            if (parent == null)
            {
                return BaseResponse<bool>.ErrorResult("Không tìm thấy phụ huynh.");
            }

            // Validate Student
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

            // Kiểm tra Student đã có Parent chưa
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

            // Kiểm tra Student đã link với Parent này rồi chưa
            if (student.ParentId == parentId)
            {
                return BaseResponse<bool>.ErrorResult("Học sinh đã được liên kết với phụ huynh này rồi.");
            }

            // Giới hạn số Student per Parent (optional - có thể config)
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
    /// <param name="studentId">ID học sinh</param>
    /// <param name="forceUnlink">Bỏ qua validation và unlink ngay lập tức (dành cho admin)</param>
    /// <returns></returns>
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

            // VALIDATION: Chỉ check khi không force
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

            // Perform unlink
            student.ParentId = null;
            student.LastUpdatedBy = managerRoleName;
            student.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();

            // Cache invalidation
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

    /// <summary>
    /// Kiểm tra trạng thái liên kết và khả năng link/unlink
    /// </summary>
    /// <param name="parentId">ID phụ huynh</param>
    /// <param name="studentId">ID học sinh</param>
    /// <returns></returns>
    public async Task<BaseResponse<ParentStudentLinkStatusResponse>> GetLinkStatusAsync(Guid parentId, Guid studentId)
    {
        try
        {
            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();

            var parent = await userRepo.GetQueryable()
                .Include(u => u.Children.Where(c => !c.IsDeleted))
                .FirstOrDefaultAsync(u => u.Id == parentId && !u.IsDeleted);

            var student = await userRepo.GetQueryable()
                .Include(u => u.Parent)
                .Include(u => u.MedicalRecord)
                .ThenInclude(mr => mr.MedicalConditions.Where(mc => !mc.IsDeleted))
                .FirstOrDefaultAsync(u => u.Id == studentId && !u.IsDeleted);

            if (parent == null || student == null)
            {
                return BaseResponse<ParentStudentLinkStatusResponse>.ErrorResult("Không tìm thấy parent hoặc student.");
            }

            var status = new ParentStudentLinkStatusResponse
            {
                ParentId = parentId,
                ParentName = parent.FullName,
                StudentId = studentId,
                StudentName = student.FullName,
                IsLinked = student.ParentId == parentId,
                CurrentParentId = student.ParentId,
                CurrentParentName = student.Parent?.FullName,
                TotalStudentsLinkedToParent = parent.Children?.Count ?? 0,
                CanLink = student.ParentId == null || student.ParentId == parentId,
                CanUnlink = student.ParentId == parentId
            };

            if (student.ParentId == parentId)
            {
                var validation = await ValidateUnlinkDependencies(studentId, parentId);
                status.CanUnlink = validation.canUnlink;
                status.UnlinkBlockReason = validation.reason;

                // Check severe medical conditions
                if (student.MedicalRecord?.MedicalConditions != null)
                {
                    status.HasSevereMedicalConditions = student.MedicalRecord.MedicalConditions
                        .Any(mc => !mc.IsDeleted && mc.Severity == SeverityType.Severe);
                }
            }

            return BaseResponse<ParentStudentLinkStatusResponse>.SuccessResult(status,
                "Lấy trạng thái liên kết thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting link status");
            return BaseResponse<ParentStudentLinkStatusResponse>.ErrorResult("Lỗi lấy trạng thái liên kết.");
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

    public async Task<byte[]> DownloadStudentTemplateAsync()
    {
        try
        {
            _logger.LogInformation("Generating school class Excel template");
            return await _excelService.GenerateStudentTemplateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading school class template");
            throw;
        }
    }

    public async Task<byte[]> DownloadParentTemplateAsync()
    {
        try
        {
            _logger.LogInformation("Generating school class Excel template");
            return await _excelService.GenerateParentTemplateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading school class template");
            throw;
        }
    }

    public async Task<BaseResponse<ExcelImportResult<ManagerResponse>>> ImportManagersFromExcelAsync(IFormFile file)
    {
        try
        {
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

    public async Task<BaseResponse<ExcelImportResult<StudentResponse>>> ImportStudentsFromExcelAsync(IFormFile file)
    {
        try
        {
            var excelResult = await _excelService.ReadStudentExcelAsync(file);

            if (!excelResult.Success)
            {
                return new BaseResponse<ExcelImportResult<StudentResponse>>
                {
                    Success = false,
                    Message = excelResult.Message
                };
            }

            var importResult = new ExcelImportResult<StudentResponse>
            {
                TotalRows = excelResult.TotalRows,
                Success = true,
                Message = "Import hoàn tất."
            };

            var successfulStudents = new List<StudentResponse>();
            var failedImports = new List<string>();

            var classRepo = _unitOfWork.GetRepositoryByEntity<SchoolClass>();
            var allClasses = await classRepo.GetQueryable()
                .Where(c => !c.IsDeleted)
                .ToDictionaryAsync(c => c.Name, c => c.Id);

            foreach (var studentData in excelResult.ValidData)
            {
                try
                {
                    var createRequest = _mapper.Map<CreateStudentRequest>(studentData);

                    var createResult = await CreateStudentAsync(createRequest);

                    if (createResult.Success)
                    {
                        successfulStudents.Add(createResult.Data);
                    }
                    else
                    {
                        failedImports.Add($"Student {studentData.Username}: {createResult.Message}");
                    }
                }
                catch (Exception ex)
                {
                    failedImports.Add($"Student {studentData.Username}: {ex.Message}");
                }
            }

            foreach (var invalidData in excelResult.InvalidData)
            {
                failedImports.Add($"Student {invalidData.Username}: {invalidData.ErrorMessage}");
            }

            importResult.ValidData = successfulStudents;
            importResult.SuccessRows = successfulStudents.Count;
            importResult.ErrorRows = failedImports.Count;
            importResult.Errors = failedImports;

            if (failedImports.Any())
            {
                importResult.Message += $" Thành công: {importResult.SuccessRows}, Lỗi: {importResult.ErrorRows}";
            }

            return new BaseResponse<ExcelImportResult<StudentResponse>>
            {
                Success = true,
                Data = importResult,
                Message = "Import Student hoàn tất."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing students from Excel");
            return new BaseResponse<ExcelImportResult<StudentResponse>>
            {
                Success = false,
                Message = $"Lỗi import Student: {ex.Message}"
            };
        }
    }

    public async Task<byte[]> ExportStudentsToExcelAsync(string searchTerm = "", string orderBy = null,
        Guid? classId = null, bool? hasMedicalRecord = null, bool? hasParent = null)
    {
        try
        {
            _logger.LogInformation(
                "Exporting students to Excel with filters - SearchTerm: {SearchTerm}, OrderBy: {OrderBy}, ClassId: {ClassId}, HasMedicalRecord: {HasMedicalRecord}, HasParent: {HasParent}",
                searchTerm, orderBy, classId, hasMedicalRecord, hasParent);

            var studentsResponse = await GetStudentsAsync(1, int.MaxValue, searchTerm, orderBy, classId,
                hasMedicalRecord, hasParent);

            if (!studentsResponse.Success)
            {
                throw new InvalidOperationException($"Failed to get students data: {studentsResponse.Message}");
            }

            return await _excelService.ExportStudentsToExcelAsync(studentsResponse.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting students to Excel");
            throw;
        }
    }

    public async Task<BaseResponse<ExcelImportResult<ParentResponse>>> ImportParentsFromExcelAsync(IFormFile file)
    {
        try
        {
            var excelResult = await _excelService.ReadParentExcelAsync(file);

            if (!excelResult.Success)
            {
                return new BaseResponse<ExcelImportResult<ParentResponse>>
                {
                    Success = false,
                    Message = excelResult.Message
                };
            }

            var importResult = new ExcelImportResult<ParentResponse>
            {
                TotalRows = excelResult.TotalRows,
                Success = true,
                Message = "Import hoàn tất."
            };

            var successfulParents = new List<ParentResponse>();
            var failedImports = new List<string>();

            foreach (var parentData in excelResult.ValidData)
            {
                try
                {
                    var createRequest = _mapper.Map<CreateParentRequest>(parentData);
                    var createResult = await CreateParentAsync(createRequest);

                    if (createResult.Success)
                    {
                        successfulParents.Add(createResult.Data);
                    }
                    else
                    {
                        failedImports.Add($"Parent {parentData.Username}: {createResult.Message}");
                    }
                }
                catch (Exception ex)
                {
                    failedImports.Add($"Parent {parentData.Username}: {ex.Message}");
                }
            }

            foreach (var invalidData in excelResult.InvalidData)
            {
                failedImports.Add($"Parent {invalidData.Username}: {invalidData.ErrorMessage}");
            }

            importResult.ValidData = successfulParents;
            importResult.SuccessRows = successfulParents.Count;
            importResult.ErrorRows = failedImports.Count;
            importResult.Errors = failedImports;

            if (failedImports.Any())
            {
                importResult.Message += $" Thành công: {importResult.SuccessRows}, Lỗi: {importResult.ErrorRows}";
            }

            return new BaseResponse<ExcelImportResult<ParentResponse>>
            {
                Success = true,
                Data = importResult,
                Message = "Import Parent hoàn tất."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing parents from Excel");
            return new BaseResponse<ExcelImportResult<ParentResponse>>
            {
                Success = false,
                Message = $"Lỗi import Parent: {ex.Message}"
            };
        }
    }

    public async Task<byte[]> ExportParentsToExcelAsync(string searchTerm = "", string orderBy = null,
        bool? hasChildren = null, string relationship = null)
    {
        try
        {
            _logger.LogInformation(
                "Exporting parents to Excel with filters - SearchTerm: {SearchTerm}, OrderBy: {OrderBy}, HasChildren: {HasChildren}, Relationship: {Relationship}",
                searchTerm, orderBy, hasChildren, relationship);

            var parentsResponse =
                await GetParentsAsync(1, int.MaxValue, searchTerm, orderBy, hasChildren, relationship);

            if (!parentsResponse.Success)
            {
                throw new InvalidOperationException($"Failed to get parents data: {parentsResponse.Message}");
            }

            return await _excelService.ExportParentsToExcelAsync(parentsResponse.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting parents to Excel");
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
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        var builder = new StringBuilder();
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }

        return builder.ToString();
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

    #endregion

    #region Validation Methods

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

            // Kiểm tra Notifications chưa confirm
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

            // Kiểm tra Appointments đang active
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

            // Kiểm tra Medical Record
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

    #endregion
}