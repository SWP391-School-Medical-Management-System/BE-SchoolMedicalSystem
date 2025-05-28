using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;

namespace SchoolMedicalManagementSystem.API.ApiControllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("admin")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> AdminCreateUser([FromBody] AdminCreateUserRequest model)
    {
        var result = await _userService.AdminCreateUserAsync(model);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("manager")]
    [Authorize(Roles = "MANAGER")]
    public async Task<IActionResult> ManagerCreateUser([FromBody] ManagerCreateUserRequest model)
    {
        var result = await _userService.ManagerCreateUserAsync(model);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPut("{id}/admin")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> AdminUpdateUser(Guid id, [FromBody] AdminUpdateUserRequest model)
    {
        var result = await _userService.AdminUpdateUserAsync(id, model);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPut("{id}/manager")]
    [Authorize(Roles = "MANAGER")]
    public async Task<IActionResult> ManagerUpdateUser(Guid id, [FromBody] ManagerUpdateUserRequest model)
    {
        var result = await _userService.ManagerUpdateUserAsync(id, model);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var result = await _userService.DeleteUserAsync(id);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("paged")]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<ActionResult<BaseListResponse<UserResponse>>> GetUsersPaged(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchTerm = "",
        [FromQuery] string orderBy = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(
                    BaseListResponse<UserResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            var response =
                await _userService.GetUsersAsync(pageIndex, pageSize, searchTerm, orderBy,
                    cancellationToken);
            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseListResponse<UserResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<BaseResponse<UserResponse>>> GetUserById(Guid id)
    {
        try
        {
            var response = await _userService.GetUserByIdAsync(id);
            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<UserResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }
    [HttpPut("{id}/profile")]
 
    [RequestFormLimits(MultipartBodyLengthLimit = 4 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateUserProfile(Guid id, [FromForm] UpdateUserProfileRequest model)
    {
        var result = await _userService.UpdateUserProfileAsync(id, model);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
    [HttpPut("{id}/change-password")]
   
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest model)
    {
        var result = await _userService.ChangePasswordAsync(id, model);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}