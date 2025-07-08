using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.SchoolClassResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Utilities;

namespace SchoolMedicalManagementSystem.API.ApiControllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
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

    [HttpGet("managers/template")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DownloadManagerTemplate()
    {
        try
        {
            var fileBytes = await _userService.DownloadManagerTemplateAsync();
            var fileName = $"Template_Manager_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<string>.ErrorResult("Lỗi tạo template Manager."));
        }
    }

    [HttpPost("managers/import")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<BaseResponse<ExcelImportResult<ManagerResponse>>>> ImportManagers(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(BaseResponse<string>.ErrorResult("File không được để trống."));

            if (file.Length > 10 * 1024 * 1024) // 10MB
                return BadRequest(BaseResponse<string>.ErrorResult("Kích thước file không được vượt quá 10MB."));

            var allowedExtensions = new[] { ".xlsx", ".xls" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest(BaseResponse<string>.ErrorResult("Chỉ hỗ trợ file Excel (.xlsx, .xls)."));

            var result = await _userService.ImportManagersFromExcelAsync(file);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<string>.ErrorResult("Lỗi import Manager từ Excel."));
        }
    }

    [HttpGet("managers/export")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ExportManagers(
        [FromQuery] string searchTerm = "",
        [FromQuery] string orderBy = null)
    {
        try
        {
            var fileBytes = await _userService.ExportManagersToExcelAsync(searchTerm, orderBy);
            var fileName = $"Danh_Sach_Manager_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<string>.ErrorResult("Lỗi export Manager ra Excel."));
        }
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

    [HttpGet("school-nurses/template")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DownloadSchoolNurseTemplate()
    {
        try
        {
            var fileBytes = await _userService.DownloadSchoolNurseTemplateAsync();
            var fileName = $"Template_SchoolNurse_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<string>.ErrorResult("Lỗi tạo template School Nurse."));
        }
    }

    [HttpPost("school-nurses/import")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<BaseResponse<ExcelImportResult<SchoolNurseResponse>>>> ImportSchoolNurses(
        IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(BaseResponse<string>.ErrorResult("File không được để trống."));

            if (file.Length > 10 * 1024 * 1024) // 10MB
                return BadRequest(BaseResponse<string>.ErrorResult("Kích thước file không được vượt quá 10MB."));

            var allowedExtensions = new[] { ".xlsx", ".xls" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest(BaseResponse<string>.ErrorResult("Chỉ hỗ trợ file Excel (.xlsx, .xls)."));

            var result = await _userService.ImportSchoolNursesFromExcelAsync(file);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<string>.ErrorResult("Lỗi import School Nurse từ Excel."));
        }
    }

    [HttpGet("school-nurses/export")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ExportSchoolNurses(
        [FromQuery] string searchTerm = "",
        [FromQuery] string orderBy = null)
    {
        try
        {
            var fileBytes = await _userService.ExportSchoolNursesToExcelAsync(searchTerm, orderBy);
            var fileName = $"Danh_Sach_SchoolNurse_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<string>.ErrorResult("Lỗi export School Nurse ra Excel."));
        }
    }

    [HttpGet("download-student-parent-template")]
    [Authorize(Roles = "MANAGER")]
    public async Task<IActionResult> DownloadStudentParentCombinedTemplate()
    {
        try
        {
            var fileBytes = await _userService.DownloadStudentParentCombinedTemplateAsync();
            var fileName = $"Template_HocSinh_PhuHuynh_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new BaseResponse<object>
            {
                Success = false,
                Message = "Lỗi tải template Excel kết hợp học sinh-phụ huynh."
            });
        }
    }

    [HttpPost("import-parent-student-relationship")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseResponse<StudentParentCombinedImportResult>>> ImportStudentParentCombined(
        IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new BaseResponse<StudentParentCombinedImportResult>
                {
                    Success = false,
                    Message = "Vui lòng chọn file Excel để import."
                });
            }

            if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
            {
                return BadRequest(new BaseResponse<StudentParentCombinedImportResult>
                {
                    Success = false,
                    Message = "Chỉ chấp nhận file Excel (.xlsx, .xls)."
                });
            }

            if (file.Length > 10 * 1024 * 1024) // 10MB limit
            {
                return BadRequest(new BaseResponse<StudentParentCombinedImportResult>
                {
                    Success = false,
                    Message = "Kích thước file không được vượt quá 10MB."
                });
            }

            _logger.LogInformation("Starting student-parent combined import. File: {FileName}, Size: {FileSize}KB",
                file.FileName, file.Length / 1024);

            var result = await _userService.ImportStudentParentCombinedFromExcelAsync(file);

            if (result.Success && result.Data != null)
            {
                _logger.LogInformation("Student-parent combined import completed. " +
                                       "Students: {Students}, Parents: {Parents}, Links: {Links}, Errors: {Errors}",
                    result.Data.SuccessfulStudents, result.Data.SuccessfulParents,
                    result.Data.SuccessfulLinks, result.Data.ErrorRows);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in student-parent combined import");
            return StatusCode(500, new BaseResponse<StudentParentCombinedImportResult>
            {
                Success = false,
                Message = "Lỗi xử lý import học sinh-phụ huynh kết hợp."
            });
        }
    }

    [HttpGet("export-parent-student-relationship")]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<IActionResult> ExportParentStudentRelationship()
    {
        try
        {
            _logger.LogInformation("Starting parent-student relationship export");

            var fileBytes = await _userService.ExportParentStudentRelationshipAsync();
            var fileName = $"BaoCao_QuanHe_PhuHuynh_HocSinh_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            _logger.LogInformation("Parent-student relationship export completed successfully. File size: {FileSize}KB",
                fileBytes.Length / 1024);

            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting parent-student relationship report");
            return StatusCode(500, new BaseResponse<object>
            {
                Success = false,
                Message = "Lỗi xuất báo cáo quan hệ phụ huynh-học sinh."
            });
        }
    }

    [HttpGet("students")]
    [Authorize(Roles = "MANAGER, SCHOOLNURSE")]
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
    [Authorize(Roles = "MANAGER, PARENT, SCHOOLNURSE")]
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

    [HttpGet("parents/{parentId}/students")]
    [Authorize(Roles = "MANAGER, PARENT, SCHOOLNURSE")]
    public async Task<ActionResult<BaseListResponse<StudentResponse>>> GetStudentsByParentId(
    Guid parentId,
    [FromQuery] int pageIndex = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string searchTerm = "",
    [FromQuery] string orderBy = null,
    CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(BaseListResponse<StudentResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            var response = await _userService.GetStudentsByParentIdAsync(parentId, pageIndex, pageSize, searchTerm, orderBy, cancellationToken);

            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving students by parent ID: {ParentId}", parentId);
            return StatusCode(500, BaseListResponse<StudentResponse>.ErrorResult("Lỗi hệ thống."));
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

    /// <summary>
    /// Hủy liên kết phụ huynh khỏi học sinh
    /// </summary>
    [HttpDelete("students/{studentId}/parent")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseResponse<bool>>> UnlinkParentFromStudent(
        Guid studentId,
        [FromQuery] bool forceUnlink = false)
    {
        try
        {
            var response = await _userService.UnlinkParentFromStudentAsync(studentId, forceUnlink);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
        }
    }
    
    [HttpPut("{id}/profile")]
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