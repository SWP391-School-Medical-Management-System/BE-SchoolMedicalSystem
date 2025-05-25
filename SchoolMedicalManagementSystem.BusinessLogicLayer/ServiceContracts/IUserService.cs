using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

public interface IUserService
{
    Task<BaseResponse<UserResponse>> GetUserByIdAsync(Guid userId);
    Task<BaseListResponse<UserResponse>> GetUsersAsync(
        int pageIndex,
        int pageSize,
        string searchTerm,
        string orderBy,
        CancellationToken cancellationToken = default);

    Task<BaseResponse<UserResponse>> AdminCreateUserAsync(AdminCreateUserRequest model);
    Task<BaseResponse<UserResponse>> AdminUpdateUserAsync(Guid userId, AdminUpdateUserRequest model);
    Task<BaseResponse<UserResponse>> ManagerCreateUserAsync(ManagerCreateUserRequest model);
    Task<BaseResponse<UserResponse>> ManagerUpdateUserAsync(Guid userId, ManagerUpdateUserRequest model);
    Task<BaseResponse<bool>> DeleteUserAsync(Guid userId);
}