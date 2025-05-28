using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

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

    [HttpGet("staff")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<BaseListResponse<StaffUserResponse>>> GetStaffUsers(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchTerm = "",
        [FromQuery] string orderBy = null,
        [FromQuery] string role = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(
                    BaseListResponse<StaffUserResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            var response =
                await _userService.GetStaffUsersAsync(pageIndex, pageSize, searchTerm, orderBy, role,
                    cancellationToken);

            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseListResponse<StaffUserResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpGet("staff/{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<BaseResponse<StaffUserResponse>>> GetStaffUserById(Guid id)
    {
        try
        {
            var response = await _userService.GetStaffUserByIdAsync(id);
            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<StaffUserResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPost("managers")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<BaseResponse<ManagerResponse>>> CreateManager([FromBody] CreateManagerRequest model)
    {
        var result = await _userService.CreateManagerAsync(model);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetStaffUserById), new { id = result.Data.Id }, result);
    }

    [HttpPut("managers/{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<BaseResponse<ManagerResponse>>> UpdateManager(Guid id,
        [FromBody] UpdateManagerRequest model)
    {
        var result = await _userService.UpdateManagerAsync(id, model);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("managers/{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DeleteManager(Guid id)
    {
        var result = await _userService.DeleteManagerAsync(id);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("school-nurses")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<BaseResponse<SchoolNurseResponse>>> CreateSchoolNurse(
        [FromBody] CreateSchoolNurseRequest model)
    {
        var result = await _userService.CreateSchoolNurseAsync(model);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetStaffUserById), new { id = result.Data.Id }, result);
    }

    [HttpPut("school-nurses/{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<BaseResponse<SchoolNurseResponse>>> UpdateSchoolNurse(Guid id,
        [FromBody] UpdateSchoolNurseRequest model)
    {
        var result = await _userService.UpdateSchoolNurseAsync(id, model);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("school-nurses/{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DeleteSchoolNurse(Guid id)
    {
        var result = await _userService.DeleteSchoolNurseAsync(id);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("students")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseListResponse<StudentResponse>>> GetStudents(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchTerm = "",
        [FromQuery] string orderBy = null,
        [FromQuery] Guid? classId = null,
        [FromQuery] bool? hasMedicalRecord = null,
        [FromQuery] bool? hasParent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(BaseListResponse<StudentResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            var response = await _userService.GetStudentsAsync(pageIndex, pageSize, searchTerm, orderBy,
                classId, hasMedicalRecord, hasParent, cancellationToken);

            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseListResponse<StudentResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpGet("students/{id}")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseResponse<StudentResponse>>> GetStudentById(Guid id)
    {
        try
        {
            var response = await _userService.GetStudentByIdAsync(id);
            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<StudentResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPost("students")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseResponse<StudentResponse>>> CreateStudent([FromBody] CreateStudentRequest model)
    {
        var result = await _userService.CreateStudentAsync(model);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetStudentById), new { id = result.Data.Id }, result);
    }

    [HttpPut("students/{id}")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseResponse<StudentResponse>>> UpdateStudent(Guid id,
        [FromBody] UpdateStudentRequest model)
    {
        var result = await _userService.UpdateStudentAsync(id, model);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("students/{id}")]
    [Authorize(Roles = "MANAGER")]
    public async Task<IActionResult> DeleteStudent(Guid id)
    {
        try
        {
            var result = await _userService.DeleteStudentAsync(id);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpGet("parents")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseListResponse<ParentResponse>>> GetParents(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchTerm = "",
        [FromQuery] string orderBy = null,
        [FromQuery] bool? hasChildren = null,
        [FromQuery] string relationship = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(BaseListResponse<ParentResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            var response = await _userService.GetParentsAsync(pageIndex, pageSize, searchTerm, orderBy,
                hasChildren, relationship, cancellationToken);

            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseListResponse<ParentResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpGet("parents/{id}")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseResponse<ParentResponse>>> GetParentById(Guid id)
    {
        try
        {
            var response = await _userService.GetParentByIdAsync(id);
            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<ParentResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPost("parents")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseResponse<ParentResponse>>> CreateParent([FromBody] CreateParentRequest model)
    {
        var result = await _userService.CreateParentAsync(model);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetParentById), new { id = result.Data.Id }, result);
    }

    [HttpPut("parents/{id}")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseResponse<ParentResponse>>> UpdateParent(Guid id,
        [FromBody] UpdateParentRequest model)
    {
        var result = await _userService.UpdateParentAsync(id, model);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("parents/{id}")]
    [Authorize(Roles = "MANAGER")]
    public async Task<IActionResult> DeleteParent(Guid id)
    {
        try
        {
            var result = await _userService.DeleteParentAsync(id);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPost("parents/{parentId}/students/{studentId}")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseResponse<bool>>> LinkParentToStudent(Guid parentId, Guid studentId)
    {
        try
        {
            var response = await _userService.LinkParentToStudentAsync(parentId, studentId);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpDelete("students/{studentId}/parent")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseResponse<bool>>> UnlinkParentFromStudent(Guid studentId)
    {
        try
        {
            var response = await _userService.UnlinkParentFromStudentAsync(studentId);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
        }
    }
}