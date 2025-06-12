using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.AuthRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.AuthResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts.IAuthService;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Utilities;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services.AuthService;

public class AuthService : IAuthService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ICacheService _cacheService;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<ForgotPasswordRequest> _forgotPasswordValidator;
    private readonly IValidator<VerifyOtpRequest> _verifyOtpValidator;
    private readonly IValidator<SetForgotPasswordRequest> _setForgotPasswordValidator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private const string USER_CACHE_PREFIX = "user_info";
    private const string USER_CACHE_SET = "user_cache_keys";

    public AuthService(
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ICacheService cacheService,
        IDistributedCache cache,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IValidator<LoginRequest> loginValidator,
        IValidator<ForgotPasswordRequest> forgotPasswordValidator,
        IValidator<VerifyOtpRequest> verifyOtpValidator,
        IValidator<SetForgotPasswordRequest> setForgotPasswordValidator,
        IHttpContextAccessor httpContextAccessor
    )
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _emailService = emailService;
        _cacheService = cacheService;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
        _loginValidator = loginValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _verifyOtpValidator = verifyOtpValidator;
        _setForgotPasswordValidator = setForgotPasswordValidator;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<BaseResponse<LoginResponse>> LoginAsync(LoginRequest model)
    {
        try
        {
            var validationResult = await _loginValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return new BaseResponse<LoginResponse>
                {
                    Success = false,
                    Message = errors
                };
            }

            string userCacheKey = _cacheService.GenerateCacheKey(USER_CACHE_PREFIX, model.Username.ToLower());
            ApplicationUser user = null;
            List<string> roles = null;

            var cachedUserInfo = await _cacheService.GetAsync<CachedUserInfo>(userCacheKey);
            if (cachedUserInfo != null)
            {
                _logger.LogDebug("User info found in cache for: {Username}", model.Username);
                
                if (!VerifyPassword(model.Password, cachedUserInfo.PasswordHash))
                {
                    return new BaseResponse<LoginResponse>
                    {
                        Success = false,
                        Message = "Mật khẩu không chính xác."
                    };
                }

                if (cachedUserInfo.IsDeleted || !cachedUserInfo.IsActive)
                {
                    await _cacheService.RemoveAsync(userCacheKey);
                    user = await GetUserFromDatabase(model.Username);
                    
                    if (user == null || user.IsDeleted || !user.IsActive)
                    {
                        return new BaseResponse<LoginResponse>
                        {
                            Success = false,
                            Message = "Tài khoản không tồn tại hoặc đã bị vô hiệu hóa."
                        };
                    }
                    roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
                }
                else
                {
                    user = new ApplicationUser 
                    { 
                        Id = cachedUserInfo.Id,
                        Username = cachedUserInfo.Username,
                        Email = cachedUserInfo.Email,
                        FullName = cachedUserInfo.FullName,
                        PasswordHash = cachedUserInfo.PasswordHash,
                        IsActive = cachedUserInfo.IsActive,
                        IsDeleted = cachedUserInfo.IsDeleted
                    };
                    roles = cachedUserInfo.Roles;
                }
            }
            else
            {
                _logger.LogDebug("User info not in cache, querying database for: {Username}", model.Username);
                
                user = await GetUserFromDatabase(model.Username);

                if (user == null)
                {
                    return new BaseResponse<LoginResponse>
                    {
                        Success = false,
                        Message = "Tài khoản không tồn tại."
                    };
                }

                if (user.IsDeleted || !user.IsActive)
                {
                    return new BaseResponse<LoginResponse>
                    {
                        Success = false,
                        Message = "Tài khoản đã bị vô hiệu hóa."
                    };
                }

                if (!VerifyPassword(model.Password, user.PasswordHash))
                {
                    return new BaseResponse<LoginResponse>
                    {
                        Success = false,
                        Message = "Mật khẩu không chính xác."
                    };
                }

                roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

                var cacheData = new CachedUserInfo
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    PasswordHash = user.PasswordHash,
                    IsActive = user.IsActive,
                    IsDeleted = user.IsDeleted,
                    Roles = roles
                };

                await _cacheService.SetAsync(userCacheKey, cacheData, TimeSpan.FromMinutes(5));
                await _cacheService.AddToTrackingSetAsync(userCacheKey, USER_CACHE_SET);
            }

            var token = GenerateJwtToken(user, roles);
            var refreshToken = GenerateRefreshToken();

            await StoreRefreshTokenInRedis(user.Id.ToString(), refreshToken);

            var loginResponse = new LoginResponse
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = roles.FirstOrDefault(),
                Token = token,
                RefreshToken = refreshToken
            };

            return new BaseResponse<LoginResponse>
            {
                Success = true,
                Data = loginResponse,
                Message = "Đăng nhập thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user: {Username}", model.Username);
            return new BaseResponse<LoginResponse>
            {
                Success = false,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest model)
    {
        try
        {
            var principal = GetPrincipalFromExpiredToken(model.AccessToken);
            if (principal == null)
            {
                return new BaseResponse<LoginResponse>
                {
                    Success = false,
                    Message = "Token không hợp lệ hoặc đã hết hạn."
                };
            }

            var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == "uid");
            if (userIdClaim == null)
            {
                return new BaseResponse<LoginResponse>
                {
                    Success = false,
                    Message = "Token không hợp lệ."
                };
            }

            var userId = userIdClaim.Value;

            var storedRefreshToken = await GetRefreshTokenFromRedis(userId);
            if (storedRefreshToken == null || storedRefreshToken != model.RefreshToken)
            {
                return new BaseResponse<LoginResponse>
                {
                    Success = false,
                    Message = "Refresh token không hợp lệ hoặc đã hết hạn."
                };
            }

            var userCacheKey = _cacheService.GenerateCacheKey(USER_CACHE_PREFIX, "id", userId);
            var cachedUserInfo = await _cacheService.GetAsync<CachedUserInfo>(userCacheKey);
            
            ApplicationUser user = null;
            List<string> roles = null;

            if (cachedUserInfo != null && !cachedUserInfo.IsDeleted && cachedUserInfo.IsActive)
            {
                user = new ApplicationUser 
                { 
                    Id = cachedUserInfo.Id,
                    Username = cachedUserInfo.Username,
                    Email = cachedUserInfo.Email,
                    FullName = cachedUserInfo.FullName
                };
                roles = cachedUserInfo.Roles;
            }
            else
            {
                var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
                user = await userRepo.GetQueryable()
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id.ToString() == userId && !u.IsDeleted && u.IsActive);

                if (user == null)
                {
                    return new BaseResponse<LoginResponse>
                    {
                        Success = false,
                        Message = "Người dùng không tồn tại hoặc đã bị vô hiệu hóa."
                    };
                }

                roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            }

            var newToken = GenerateJwtToken(user, roles);
            var newRefreshToken = GenerateRefreshToken();

            await StoreRefreshTokenInRedis(userId, newRefreshToken);

            var loginResponse = new LoginResponse
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = roles.FirstOrDefault(),
                Token = newToken,
                RefreshToken = newRefreshToken
            };

            return new BaseResponse<LoginResponse>
            {
                Success = true,
                Data = loginResponse,
                Message = "Token đã được làm mới thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return new BaseResponse<LoginResponse>
            {
                Success = false,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<bool>> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        try
        {
            var validationResult = await _forgotPasswordValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = errors
                };
            }

            var user = await GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                return new BaseResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Nếu email tồn tại trong hệ thống, mã OTP đã được gửi tới email của bạn."
                };
            }

            var otp = GenerateOtp();
            var otpExpiration = TimeSpan.FromMinutes(
                double.Parse(_configuration["SMTP:expiryInMinutes"] ?? "15")
            );

            await StoreOtpInRedis(request.Email, otp, otpExpiration);

            await _emailService.SendForgotPasswordOtpAsync(
                request.Email,
                otp,
                int.Parse(_configuration["SMTP:expiryInMinutes"] ?? "15")
            );

            return new BaseResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Mã OTP đã được gửi tới email của bạn. Vui lòng kiểm tra email để xác nhận."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing forgot password request for email: {Email}", request.Email);
            return new BaseResponse<bool>
            {
                Success = false,
                Data = false,
                Message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau."
            };
        }
    }

    public async Task<BaseResponse<VerifyOtpResponse>> VerifyForgotPasswordOtpAsync(VerifyOtpRequest request)
    {
        try
        {
            var validationResult = await _verifyOtpValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return new BaseResponse<VerifyOtpResponse>
                {
                    Success = false,
                    Message = errors
                };
            }

            var isValidOtp = await ValidateOtp(request.Email, request.Otp);
            if (!isValidOtp)
            {
                return new BaseResponse<VerifyOtpResponse>
                {
                    Success = false,
                    Message = "Mã OTP không hợp lệ hoặc đã hết hạn."
                };
            }

            await StoreVerifiedOtpInRedis(request.Email, request.Otp);

            return new BaseResponse<VerifyOtpResponse>
            {
                Success = true,
                Data = new VerifyOtpResponse
                {
                    Success = true,
                    Message = "Mã OTP hợp lệ. Bạn có thể thiết lập mật khẩu mới."
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying OTP for email: {Email}", request.Email);
            return new BaseResponse<VerifyOtpResponse>
            {
                Success = false,
                Message = "Đã xảy ra lỗi khi xác minh OTP. Vui lòng thử lại sau."
            };
        }
    }

    public async Task<BaseResponse<bool>> ResetPasswordWithOtpAsync(SetForgotPasswordRequest request)
    {
        try
        {
            var validationResult = await _setForgotPasswordValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = errors
                };
            }

            var verifiedOtp = await GetVerifiedOtpFromRedis(request.Email);
            if (string.IsNullOrEmpty(verifiedOtp) || verifiedOtp != request.OTP)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "Phiên làm việc đã hết hạn. Vui lòng yêu cầu OTP mới."
                };
            }

            var user = await GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "Không tìm thấy người dùng với email này."
                };
            }

            var clientIpAddress = GetClientIpAddress();

            await RemoveOtpFromRedis(request.Email);
            await RemoveVerifiedOtpFromRedis(request.Email);
            await RemoveRefreshTokenFromRedis(user.Id.ToString());

            user.PasswordHash = HashPassword(request.NewPassword);
            user.LastUpdatedDate = DateTime.Now;
            user.LastUpdatedBy = "System - Password Reset";

            await _unitOfWork.SaveChangesAsync();

            await InvalidateUserCacheAsync(user.Username, user.Email, user.Id);

            await _emailService.SendPasswordResetConfirmationAsync(request.Email, user.FullName, clientIpAddress);

            return new BaseResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Mật khẩu đã được đặt lại thành công. Bạn có thể đăng nhập với mật khẩu mới."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for email: {Email}", request.Email);
            return new BaseResponse<bool>
            {
                Success = false,
                Message = "Đã xảy ra lỗi khi đặt lại mật khẩu. Vui lòng thử lại sau."
            };
        }
    }

    #region Public Methods for Cache Invalidation

    public async Task InvalidateUserCacheAsync(string username = null, string email = null, Guid? userId = null)
    {
        try
        {
            var keysToRemove = new List<string>();

            if (!string.IsNullOrEmpty(username))
            {
                keysToRemove.Add(_cacheService.GenerateCacheKey(USER_CACHE_PREFIX, username.ToLower()));
            }

            if (!string.IsNullOrEmpty(email))
            {
                keysToRemove.Add(_cacheService.GenerateCacheKey(USER_CACHE_PREFIX, email.ToLower()));
            }

            if (userId.HasValue)
            {
                keysToRemove.Add(_cacheService.GenerateCacheKey(USER_CACHE_PREFIX, "id", userId.Value.ToString()));
            }

            foreach (var key in keysToRemove)
            {
                await _cacheService.RemoveAsync(key);
            }

            _logger.LogDebug("Invalidated user cache for username: {Username}, email: {Email}, userId: {UserId}", 
                username, email, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating user cache");
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task<ApplicationUser> GetUserFromDatabase(string usernameOrEmail)
    {
        var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
        return await userRepo.GetQueryable()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Where(u => !u.IsDeleted && u.IsActive)
            .FirstOrDefaultAsync(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail);
    }

    private async Task<ApplicationUser> GetUserByEmailAsync(string email)
    {
        var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
        return await userRepo.GetQueryable()
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
    }

    private string GenerateOtp()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    private async Task StoreOtpInRedis(string email, string otp, TimeSpan expiration)
    {
        var key = $"otp:{email}";
        await _cache.SetStringAsync(
            key,
            otp,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            });
    }

    private async Task<bool> ValidateOtp(string email, string otp)
    {
        var key = $"otp:{email}";
        var storedOtp = await _cache.GetStringAsync(key);

        if (string.IsNullOrEmpty(storedOtp))
            return false;

        return storedOtp == otp;
    }

    private string GetClientIpAddress()
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return "Unknown";

            string ipAddress = null;

            if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    ipAddress = ipAddress.Split(',')[0].Trim();
                }
            }

            if (string.IsNullOrEmpty(ipAddress) && context.Request.Headers.ContainsKey("X-Real-IP"))
            {
                ipAddress = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            }

            if (string.IsNullOrEmpty(ipAddress) && context.Request.Headers.ContainsKey("CF-Connecting-IP"))
            {
                ipAddress = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
            }

            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = context.Connection.RemoteIpAddress?.ToString();
            }

            if (ipAddress == "::1")
            {
                ipAddress = "127.0.0.1 (localhost)";
            }

            return !string.IsNullOrEmpty(ipAddress) ? ipAddress : "Unknown";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get client IP address");
            return "Unknown";
        }
    }

    private async Task StoreVerifiedOtpInRedis(string email, string otp)
    {
        var key = $"verified_otp:{email}";
        await _cache.SetStringAsync(
            key,
            otp,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });
    }

    private async Task<string> GetVerifiedOtpFromRedis(string email)
    {
        var key = $"verified_otp:{email}";
        return await _cache.GetStringAsync(key);
    }

    private async Task RemoveOtpFromRedis(string email)
    {
        var key = $"otp:{email}";
        await _cache.RemoveAsync(key);
    }

    private async Task RemoveVerifiedOtpFromRedis(string email)
    {
        var key = $"verified_otp:{email}";
        await _cache.RemoveAsync(key);
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

    private string GenerateJwtToken(ApplicationUser user, List<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim("uid", user.Id.ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim("r", role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        double expiryMinutes = double.Parse(_configuration["JWT:ExpiryMinutes"]);

        var token = new JwtSecurityToken(
            issuer: _configuration["JWT:ValidIssuer"],
            audience: _configuration["JWT:ValidAudience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private async Task StoreRefreshTokenInRedis(string userId, string refreshToken)
    {
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
        };

        var key = $"refresh_token:{userId}";
        await _cache.SetStringAsync(key, refreshToken, cacheOptions);
    }

    private async Task<string> GetRefreshTokenFromRedis(string userId)
    {
        var key = $"refresh_token:{userId}";
        return await _cache.GetStringAsync(key);
    }

    private async Task RemoveRefreshTokenFromRedis(string userId)
    {
        var key = $"refresh_token:{userId}";
        await _cache.RemoveAsync(key);
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"])),
            ValidateIssuer = true,
            ValidIssuer = _configuration["JWT:ValidIssuer"],
            ValidateAudience = true,
            ValidAudience = _configuration["JWT:ValidAudience"],
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var principal =
                tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    private bool VerifyPassword(string password, string passwordHash)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        var builder = new StringBuilder();
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }

        return builder.ToString() == passwordHash;
    }

    #endregion
}