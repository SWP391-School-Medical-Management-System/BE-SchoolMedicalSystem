using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutoMapper;
using FluentValidation;
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
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services.AuthService;

public class AuthService : IAuthService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator;
    private readonly IValidator<ForgotPasswordRequest> _forgotPasswordValidator;

    public AuthService(
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IDistributedCache cache,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IValidator<LoginRequest> loginValidator,
        IValidator<ForgotPasswordRequest> forgotPasswordValidator,
        IValidator<ResetPasswordRequest> resetPasswordValidator
    )
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _emailService = emailService;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
        _loginValidator = loginValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
    }

    public async Task<BaseResponse<LoginResponse>> LoginAsync(LoginRequest model)
    {
        try
        {
            string cacheKey = $"login_{model.Username}";
            var cachedLogin = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedLogin))
            {
                _logger.LogInformation("Using cached login information for user: {Username}", model.Username);
                var cachedResponse = JsonSerializer.Deserialize<BaseResponse<LoginResponse>>(cachedLogin);
                return cachedResponse;
            }

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

            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var userQuery = userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => !u.IsDeleted && u.IsActive);

            var user = await userQuery
                .FirstOrDefaultAsync(u => u.Username == model.Username || u.Email == model.Username);

            if (user == null)
            {
                return new BaseResponse<LoginResponse>
                {
                    Success = false,
                    Message = "Tài khoản không tồn tại."
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

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

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

            var response = new BaseResponse<LoginResponse>
            {
                Success = true,
                Data = loginResponse,
                Message = "Đăng nhập thành công."
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
            _logger.LogError(ex, "Error during login");
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
            string cacheKey = $"refresh_{model.RefreshToken}";
            var cachedRefresh = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedRefresh))
            {
                _logger.LogInformation("Using cached refresh token information");
                var cachedResponse = JsonSerializer.Deserialize<BaseResponse<LoginResponse>>(cachedRefresh);
                return cachedResponse;
            }

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

            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var user = await userRepo.GetQueryable()
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

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

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

            var response = new BaseResponse<LoginResponse>
            {
                Success = true,
                Data = loginResponse,
                Message = "Token đã được làm mới thành công."
            };

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(response),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

            await AddCacheKey(cacheKey);

            return response;
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

    public async Task<BaseResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request)
    {
        try
        {
            var validationResult = await _resetPasswordValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = errors
                };
            }

            var validatedToken = await GetValidatedResetTokenFromRedis(request.Email);
            if (string.IsNullOrEmpty(validatedToken))
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "Phiên làm việc đã hết hạn. Vui lòng yêu cầu đặt lại mật khẩu mới."
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

            if (request.NewPassword != request.ConfirmPassword)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "Mật khẩu mới và xác nhận mật khẩu không khớp."
                };
            }

            await RemoveValidatedResetTokenFromRedis(request.Email);
            await RemoveResetTokenFromRedis(request.Email);

            await RemoveRefreshTokenFromRedis(user.Id.ToString());

            user.PasswordHash = HashPassword(request.NewPassword);
            user.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();

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
                    Success = false,
                    Message = "Không tìm thấy người dùng với email này."
                };
            }

            var resetToken = GenerateResetToken();

            var resetTokenExpiration = TimeSpan.FromMinutes(15);

            await StoreResetTokenInRedis(request.Email, resetToken, resetTokenExpiration);

            var resetUrl =
                $"{_configuration["System:Url"]}/reset-password?email={Uri.EscapeDataString(request.Email)}&token={resetToken}";

            await _emailService.SendPasswordResetLinkAsync(request.Email, user.Username, resetUrl);

            return new BaseResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Đường dẫn đặt lại mật khẩu đã được gửi tới email của bạn."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing forgot password request");
            return new BaseResponse<bool>
            {
                Success = false,
                Message = $"Lỗi xử lý yêu cầu đặt lại mật khẩu: {ex.Message}"
            };
        }
    }

    #region Helper Method

    private async Task<ApplicationUser> GetUserByEmailAsync(string email)
    {
        var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
        return await userRepo.GetQueryable()
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
    }

    private string GenerateResetToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(12))
            .Replace("/", "_")
            .Replace("+", "-")
            .Substring(0, 16);
    }

    private async Task StoreResetTokenInRedis(string email, string resetToken, TimeSpan expiration)
    {
        var key = $"reset_token:{email}";
        await _cache.SetStringAsync(
            key,
            resetToken,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            });
    }

    private async Task<bool> ValidateResetToken(string email, string resetToken)
    {
        var key = $"reset_token:{email}";
        var storedToken = await _cache.GetStringAsync(key);

        if (string.IsNullOrEmpty(storedToken))
            return false;

        return storedToken == resetToken;
    }

    private async Task RemoveResetTokenFromRedis(string email)
    {
        var key = $"reset_token:{email}";
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

    private async Task<string> GetValidatedResetTokenFromRedis(string email)
    {
        var key = $"validated_reset_token:{email}";
        return await _cache.GetStringAsync(key);
    }

    private async Task RemoveValidatedResetTokenFromRedis(string email)
    {
        var key = $"validated_reset_token:{email}";
        await _cache.RemoveAsync(key);
    }

    private string GenerateJwtToken(ApplicationUser user, List<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim("uid", user.Id.ToString())
            // new Claim(ClaimTypes.Name, user.Username),
            // new Claim(ClaimTypes.Email, user.Email)
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