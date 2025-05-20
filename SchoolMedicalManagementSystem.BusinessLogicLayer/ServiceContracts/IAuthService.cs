using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.AuthRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.AuthResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts.IAuthService;

public interface IAuthService
{
    Task<BaseResponse<LoginResponse>> LoginAsync(LoginRequest model);
    Task<BaseResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest model);
    Task<BaseResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request);
    Task<BaseResponse<bool>> ForgotPasswordAsync(ForgotPasswordRequest request);
}