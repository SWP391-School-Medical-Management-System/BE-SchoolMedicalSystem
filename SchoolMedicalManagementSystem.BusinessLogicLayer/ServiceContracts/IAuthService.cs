using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.AuthRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.AuthResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts.IAuthService;

public interface IAuthService
{
    Task<BaseResponse<LoginResponse>> LoginAsync(LoginRequest model);
    Task<BaseResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest model);
    Task<BaseResponse<bool>> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<BaseResponse<VerifyOtpResponse>> VerifyForgotPasswordOtpAsync(VerifyOtpRequest request);
    Task<BaseResponse<bool>> ResetPasswordWithOtpAsync(SetForgotPasswordRequest request);
    Task InvalidateUserCacheAsync(string username = null, string email = null, Guid? userId = null);
}