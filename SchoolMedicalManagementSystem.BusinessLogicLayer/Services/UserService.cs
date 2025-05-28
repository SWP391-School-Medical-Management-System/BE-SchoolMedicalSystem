using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;
using static System.Net.Mime.MediaTypeNames;
using SixLabors.ImageSharp; 
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services;

public class UserService : IUserService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IDistributedCache _cache;
    private readonly ILogger<UserService> _logger;
    private readonly IValidator<AdminCreateUserRequest> _createUserValidator;
    private readonly IValidator<AdminCreateUserRequest> _adminCreateUserValidator;
    private readonly IValidator<ManagerCreateUserRequest> _managerCreateUserValidator;
    private readonly IValidator<AdminUpdateUserRequest> _adminUpdateUserValidator;
    private readonly IValidator<ManagerUpdateUserRequest> _managerUpdateUserValidator;
    private readonly CloudinaryService _cloudinaryService;

    public UserService(
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IDistributedCache cache,
        ILogger<UserService> logger,
        IValidator<AdminCreateUserRequest> adminCreateUserValidator,
        IValidator<ManagerCreateUserRequest> managerCreateUserValidator,
        IValidator<AdminUpdateUserRequest> adminUpdateUserValidator,
        IValidator<ManagerUpdateUserRequest> managerUpdateUserValidator,
        CloudinaryService cloudinaryService
    )
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _emailService = emailService;
        _cache = cache;
        _logger = logger;
        _adminCreateUserValidator = adminCreateUserValidator;
        _managerCreateUserValidator = managerCreateUserValidator;
        _adminUpdateUserValidator = adminUpdateUserValidator;
        _managerUpdateUserValidator = managerUpdateUserValidator;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<BaseResponse<UserResponse>> GetUserByIdAsync(Guid userId)
    {
        try
        {
            var cacheKey = $"user_{userId}";
            var cachedUser = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedUser))
            {
                var cachedResponse = JsonSerializer.Deserialize<BaseResponse<UserResponse>>(cachedUser);
                return cachedResponse;
            }

            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var user = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.Class)
                .Include(u => u.Parent)
                .Where(u => u.Id == userId && !u.IsDeleted)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return new BaseResponse<UserResponse>
                {
                    Success = false,
                    Message = "Người dùng không tồn tại."
                };
            }

            var userResponse = _mapper.Map<UserResponse>(user);
            userResponse.Role = user.UserRoles.FirstOrDefault()?.Role.Name;

            if (userResponse.Role == "STUDENT")
            {
                if (user.Class != null)
                {
                    userResponse.ClassId = user.Class.Id;
                    userResponse.ClassName = user.Class.Name;
                }

                if (user.Parent != null)
                {
                    userResponse.ParentId = user.Parent.Id;
                    userResponse.ParentName = user.Parent.FullName;
                }
            }
            else if (userResponse.Role == "PARENT")
            {
                var children = await userRepo.GetQueryable()
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .Include(u => u.Class)
                    .Where(u => u.ParentId == userId && !u.IsDeleted)
                    .ToListAsync();

                if (children.Any())
                {
                    userResponse.Children = _mapper.Map<List<UserResponse>>(children);

                    foreach (var child in userResponse.Children)
                    {
                        var childEntity = children.FirstOrDefault(c => c.Id == child.Id);
                        if (childEntity != null)
                        {
                            child.Role = childEntity.UserRoles.FirstOrDefault()?.Role.Name;
                            if (childEntity.Class != null)
                            {
                                child.ClassId = childEntity.Class.Id;
                                child.ClassName = childEntity.Class.Name;
                            }
                        }
                    }
                }
            }

            var response = new BaseResponse<UserResponse>
            {
                Success = true,
                Data = userResponse,
                Message = "Lấy thông tin người dùng thành công."
            };

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(response),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });

            await AddCacheKey(cacheKey);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID");
            return new BaseResponse<UserResponse>
            {
                Success = false,
                Message = $"Lỗi lấy thông tin người dùng: {ex.Message}"
            };
        }
    }

    public async Task<BaseListResponse<UserResponse>> GetUsersAsync(
        int pageIndex,
        int pageSize,
        string searchTerm,
        string orderBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.Class)
                .Include(u => u.Parent)
                .Where(u => !u.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(u =>
                    u.Username.ToLower().Contains(searchTerm) ||
                    u.Email.ToLower().Contains(searchTerm) ||
                    u.FullName.ToLower().Contains(searchTerm) ||
                    u.PhoneNumber.Contains(searchTerm) ||
                    (u.Class != null && u.Class.Name.ToLower().Contains(searchTerm)));
            }

            query = orderBy?.ToLower() switch
            {
                "username" => query.OrderBy(u => u.Username),
                "email" => query.OrderBy(u => u.Email),
                "fullname" => query.OrderBy(u => u.FullName),
                "createdate_desc" => query.OrderByDescending(u => u.CreatedDate),
                "createdate" => query.OrderBy(u => u.CreatedDate),
                "role" => query.OrderBy(u => u.UserRoles.FirstOrDefault().Role.Name),
                _ => query.OrderByDescending(u => u.CreatedDate)
            };

            var totalCount = await query.CountAsync(cancellationToken);
            var users = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = _mapper.Map<List<UserResponse>>(users);

            var cacheKey = $"users_list_{searchTerm}_{orderBy}_{pageIndex}_{pageSize}";
            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedResult = JsonSerializer.Deserialize<BaseListResponse<UserResponse>>(cachedData);
                return cachedResult;
            }

            var result = BaseListResponse<UserResponse>.SuccessResult(
                responses,
                totalCount,
                pageSize,
                pageIndex);

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(result),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                },
                cancellationToken);

            await AddCacheKey(cacheKey);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return BaseListResponse<UserResponse>.ErrorResult("Error retrieving users");
        }
    }

    public async Task<BaseResponse<UserResponse>> AdminCreateUserAsync(AdminCreateUserRequest model)
    {
        try
        {
            var validationResult = await _adminCreateUserValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return new BaseResponse<UserResponse>
                {
                    Success = false,
                    Message = errors
                };
            }

            var adminRoleName = await GetAdminRoleName();

            if (model.Role != "MANAGER" && model.Role != "SCHOOLNURSE")
            {
                return new BaseResponse<UserResponse>
                {
                    Success = false,
                    Message = "Admin chỉ có thể tạo tài khoản người quản lý hoặc y tá trường học."
                };
            }

            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var userQuery = userRepo.GetQueryable();

            if (await userQuery.Where(u => u.Username == model.Username || u.Email == model.Email).AnyAsync())
            {
                return new BaseResponse<UserResponse>
                {
                    Success = false,
                    Message = "Tên đăng nhập hoặc email đã tồn tại."
                };
            }

            string defaultPassword = GenerateDefaultPassword();
            string passwordHash = HashPassword(defaultPassword);

            var user = _mapper.Map<ApplicationUser>(model);
            user.Id = Guid.NewGuid();
            user.PasswordHash = passwordHash;
            user.CreatedBy = adminRoleName;
            user.CreatedDate = DateTime.Now;
            user.IsActive = true;

            await userRepo.AddAsync(user);

            var roleRepo = _unitOfWork.GetRepositoryByEntity<Role>();
            var role = await roleRepo.GetQueryable()
                .Where(r => r.Name == model.Role)
                .FirstOrDefaultAsync();

            if (role == null)
            {
                return new BaseResponse<UserResponse>
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

            await InvalidateUserCache();

            var userResponse = _mapper.Map<UserResponse>(user);
            userResponse.Role = model.Role;

            return new BaseResponse<UserResponse>
            {
                Success = true,
                Data = null,
                Message = "Tài khoản đã được tạo thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user account by admin");
            return new BaseResponse<UserResponse>
            {
                Success = false,
                Message = $"Lỗi tạo tài khoản: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<UserResponse>> AdminUpdateUserAsync(Guid userId, AdminUpdateUserRequest model)
    {
        try
        {
            var validationResult = await _adminUpdateUserValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return new BaseResponse<UserResponse>
                {
                    Success = false,
                    Message = errors
                };
            }

            var adminRoleName = await GetAdminRoleName();

            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var user = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            if (user == null)
            {
                return new BaseResponse<UserResponse>
                {
                    Success = false,
                    Message = "Không tìm thấy người dùng."
                };
            }

            var userRole = user.UserRoles.FirstOrDefault()?.Role.Name;
            if (userRole != "MANAGER" && userRole != "SCHOOLNURSE")
            {
                return new BaseResponse<UserResponse>
                {
                    Success = false,
                    Message = "Admin chỉ có thể cập nhật tài khoản người quản lý hoặc y tá trường học."
                };
            }

            if (user.PhoneNumber != model.PhoneNumber)
            {
                var phoneExists = await userRepo.GetQueryable()
                    .AnyAsync(u => u.PhoneNumber == model.PhoneNumber && u.Id != userId && !u.IsDeleted);
                if (phoneExists)
                {
                    return new BaseResponse<UserResponse>
                    {
                        Success = false,
                        Message = "Số điện thoại đã được sử dụng bởi người dùng khác."
                    };
                }
            }

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;
            user.Gender = model.Gender;
            user.DateOfBirth = model.DateOfBirth;
            user.LastUpdatedBy = adminRoleName;
            user.LastUpdatedDate = DateTime.Now;

            if (userRole == "SCHOOLNURSE")
            {
                user.StaffCode = model.StaffCode;
                user.LicenseNumber = model.LicenseNumber;
                user.Specialization = model.Specialization;
            }

            await _unitOfWork.SaveChangesAsync();

            await InvalidateUserCache();

            var userResponse = _mapper.Map<UserResponse>(user);
            userResponse.Role = userRole;

            return new BaseResponse<UserResponse>
            {
                Success = true,
                Data = null,
                Message = "Tài khoản đã được cập nhật thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user account by admin");
            return new BaseResponse<UserResponse>
            {
                Success = false,
                Message = $"Lỗi cập nhật tài khoản: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<UserResponse>> ManagerCreateUserAsync(ManagerCreateUserRequest model)
    {
        try
        {
            var validationResult = await _managerCreateUserValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return new BaseResponse<UserResponse>
                {
                    Success = false,
                    Message = errors
                };
            }

            var managerRoleName = await GetManagerRoleName();

            if (model.Role != "STUDENT" && model.Role != "PARENT")
            {
                return new BaseResponse<UserResponse>
                {
                    Success = false,
                    Message = "Manager chỉ có thể tạo tài khoản học sinh hoặc phụ huynh."
                };
            }

            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var userQuery = userRepo.GetQueryable();

            if (await userQuery.Where(u => u.Username == model.Username || u.Email == model.Email).AnyAsync())
            {
                return new BaseResponse<UserResponse>
                {
                    Success = false,
                    Message = "Tên đăng nhập hoặc email đã tồn tại."
                };
            }

            if (model.Role == "STUDENT")
            {
                if (model.ClassId.HasValue)
                {
                    var classExists = await _unitOfWork.GetRepositoryByEntity<SchoolClass>().GetQueryable()
                        .AnyAsync(c => c.Id == model.ClassId.Value && !c.IsDeleted);

                    if (!classExists)
                    {
                        return new BaseResponse<UserResponse>
                        {
                            Success = false,
                            Message = "Lớp học không tồn tại."
                        };
                    }
                }

                if (model.ParentId.HasValue)
                {
                    var parent = await userRepo.GetQueryable()
                        .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                        .FirstOrDefaultAsync(u => u.Id == model.ParentId && !u.IsDeleted);

                    if (parent == null || !parent.UserRoles.Any(ur => ur.Role.Name == "PARENT"))
                    {
                        return new BaseResponse<UserResponse>
                        {
                            Success = false,
                            Message = "Phụ huynh không tồn tại hoặc không có vai trò."
                        };
                    }
                }
            }

            string defaultPassword = GenerateDefaultPassword();
            string passwordHash = HashPassword(defaultPassword);

            var user = _mapper.Map<ApplicationUser>(model);
            user.Id = Guid.NewGuid();
            user.PasswordHash = passwordHash;
            user.CreatedBy = managerRoleName;
            user.CreatedDate = DateTime.Now;
            user.IsActive = true;

            await userRepo.AddAsync(user);

            var roleRepo = _unitOfWork.GetRepositoryByEntity<Role>();
            var role = await roleRepo.GetQueryable()
                .Where(r => r.Name == model.Role)
                .FirstOrDefaultAsync();

            if (role == null)
            {
                return new BaseResponse<UserResponse>
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

            await InvalidateUserCache();

            var userResponse = _mapper.Map<UserResponse>(user);
            userResponse.Role = model.Role;

            if (model.Role == "STUDENT" && model.ParentId.HasValue)
            {
                var parent = await userRepo.GetById(model.ParentId.Value);
                if (parent != null)
                {
                    userResponse.ParentId = parent.Id;
                    userResponse.ParentName = parent.FullName;
                }
            }

            if (model.Role == "STUDENT" && model.ClassId.HasValue)
            {
                var schoolClass = await _unitOfWork.GetRepositoryByEntity<SchoolClass>().GetById(model.ClassId.Value);
                if (schoolClass != null)
                {
                    userResponse.ClassId = schoolClass.Id;
                    userResponse.ClassName = schoolClass.Name;
                }
            }

            return new BaseResponse<UserResponse>
            {
                Success = true,
                Data = null,
                Message = "Tài khoản đã được tạo thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user account by manager");
            return new BaseResponse<UserResponse>
            {
                Success = false,
                Message = $"Lỗi tạo tài khoản: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<UserResponse>> ManagerUpdateUserAsync(Guid userId, ManagerUpdateUserRequest model)
    {
        try
        {
            var validationResult = await _managerUpdateUserValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return new BaseResponse<UserResponse>
                {
                    Success = false,
                    Message = errors
                };
            }

            var managerRoleName = await GetManagerRoleName();

            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var user = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            if (user == null)
            {
                return new BaseResponse<UserResponse>
                {
                    Success = false,
                    Message = "Không tìm thấy người dùng."
                };
            }

            var userRole = user.UserRoles.FirstOrDefault()?.Role.Name;
            if (userRole != "STUDENT" && userRole != "PARENT")
            {
                return new BaseResponse<UserResponse>
                {
                    Success = false,
                    Message = "Manager chỉ có thể cập nhật tài khoản học sinh hoặc phụ huynh."
                };
            }

            if (user.PhoneNumber != model.PhoneNumber)
            {
                var phoneExists = await userRepo.GetQueryable()
                    .AnyAsync(u => u.PhoneNumber == model.PhoneNumber && u.Id != userId && !u.IsDeleted);
                if (phoneExists)
                {
                    return new BaseResponse<UserResponse>
                    {
                        Success = false,
                        Message = "Số điện thoại đã được sử dụng bởi người dùng khác."
                    };
                }
            }

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;
            user.Gender = model.Gender;
            user.DateOfBirth = model.DateOfBirth;
            user.LastUpdatedBy = managerRoleName;
            user.LastUpdatedDate = DateTime.Now;

            if (userRole == "STUDENT")
            {
                user.StudentCode = model.StudentCode;

                if (model.ClassId.HasValue && user.ClassId != model.ClassId)
                {
                    var classExists = await _unitOfWork.GetRepositoryByEntity<SchoolClass>().GetQueryable()
                        .AnyAsync(c => c.Id == model.ClassId.Value && !c.IsDeleted);

                    if (!classExists)
                    {
                        return new BaseResponse<UserResponse>
                        {
                            Success = false,
                            Message = "Lớp học không tồn tại."
                        };
                    }

                    user.ClassId = model.ClassId;
                }

                if (model.ParentId.HasValue && user.ParentId != model.ParentId)
                {
                    var parent = await userRepo.GetQueryable()
                        .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                        .FirstOrDefaultAsync(u => u.Id == model.ParentId && !u.IsDeleted);

                    if (parent == null || !parent.UserRoles.Any(ur => ur.Role.Name == "PARENT"))
                    {
                        return new BaseResponse<UserResponse>
                        {
                            Success = false,
                            Message = "Phụ huynh không tồn tại hoặc không có vai trò Parent."
                        };
                    }

                    user.ParentId = model.ParentId;
                }
            }

            if (userRole == "PARENT")
            {
                user.Relationship = model.Relationship;
            }

            await _unitOfWork.SaveChangesAsync();

            await InvalidateUserCache();

            var userResponse = _mapper.Map<UserResponse>(user);
            userResponse.Role = userRole;

            if (userRole == "STUDENT" && user.ParentId.HasValue)
            {
                var parent = await userRepo.GetById(user.ParentId.Value);
                if (parent != null)
                {
                    userResponse.ParentId = parent.Id;
                    userResponse.ParentName = parent.FullName;
                }
            }

            if (userRole == "STUDENT" && user.ClassId.HasValue)
            {
                var schoolClass = await _unitOfWork.GetRepositoryByEntity<SchoolClass>().GetById(user.ClassId.Value);
                if (schoolClass != null)
                {
                    userResponse.ClassId = schoolClass.Id;
                    userResponse.ClassName = schoolClass.Name;
                }
            }

            return new BaseResponse<UserResponse>
            {
                Success = true,
                Data = null,
                Message = "Tài khoản đã được cập nhật thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user account by manager");
            return new BaseResponse<UserResponse>
            {
                Success = false,
                Message = $"Lỗi cập nhật tài khoản: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<bool>> DeleteUserAsync(Guid userId)
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
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "Người dùng không tồn tại hoặc đã bị xóa."
                };
            }

            var userRole = user.UserRoles.FirstOrDefault()?.Role.Name;
            if (userRole == null)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "Không thể xác định vai trò của người dùng cần xóa."
                };
            }

            string updatedBy;

            if (userRole == "MANAGER" || userRole == "SCHOOLNURSE")
            {
                updatedBy = await GetAdminRoleName();
            }
            else if (userRole == "STUDENT" || userRole == "PARENT")
            {
                updatedBy = await GetManagerRoleName();
            }
            else
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "Không thể xóa người dùng với vai trò này."
                };
            }

            user.IsDeleted = true;
            user.LastUpdatedBy = updatedBy;
            user.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();

            await InvalidateUserCache();

            return new BaseResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Người dùng đã được xóa thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user");
            return new BaseResponse<bool>
            {
                Success = false,
                Data = false,
                Message = $"Lỗi xóa người dùng: {ex.Message}"
            };
        }
    }
    public async Task<BaseResponse<UserResponse>> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest model)
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
                return new BaseResponse<UserResponse>
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
                return new BaseResponse<UserResponse>
                {
                    Success = false,
                    Message = "Ngày sinh không hợp lệ."
                };
            }

            // Cập nhật thông tin
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

            await InvalidateUserCache();

            var userResponse = _mapper.Map<UserResponse>(user);
            userResponse.Role = user.UserRoles.FirstOrDefault()?.Role.Name;

            return new BaseResponse<UserResponse>
            {
                Success = true,
                Data = userResponse,
                Message = "Cập nhật thông tin cá nhân thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            return new BaseResponse<UserResponse>
            {
                Success = false,
                Message = $"Lỗi cập nhật thông tin: {ex.Message}"
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
        string fileName = file.FileName;

        return await _cloudinaryService.UploadImageAsync(imageData, fileName);
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
            string newPasswordHash = HashPassword(model.NewPassword);
            user.PasswordHash = newPasswordHash;
            user.LastUpdatedBy = user.Username;
            user.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();

            await InvalidateUserCache();

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


    #region Helper Method

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

    private async Task AddCacheKey(string cacheKey)
    {
        try
        {
            string userCacheKeysKey = "user_cache_keys";

            var cachedKeys = await _cache.GetStringAsync(userCacheKeysKey);
            var keys = string.IsNullOrEmpty(cachedKeys)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(cachedKeys);

            if (keys != null && !keys.Contains(cacheKey))
            {
                keys.Add(cacheKey);

                await _cache.SetStringAsync(
                    userCacheKeysKey,
                    JsonSerializer.Serialize(keys),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                    });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding cache key");
        }
    }

    private async Task InvalidateUserCache()
    {
        try
        {
            string userCacheKeysKey = "user_cache_keys";
            var cachedKeys = await _cache.GetStringAsync(userCacheKeysKey);

            if (!string.IsNullOrEmpty(cachedKeys))
            {
                var keys = JsonSerializer.Deserialize<List<string>>(cachedKeys);
                if (keys != null && keys.Any())
                {
                    foreach (var key in keys)
                    {
                        await _cache.RemoveAsync(key);
                    }

                    await _cache.RemoveAsync(userCacheKeysKey);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating user cache");
        }
    }

    #endregion
}