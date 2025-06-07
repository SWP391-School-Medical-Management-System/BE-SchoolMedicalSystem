using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.SchoolClassRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.SchoolClassResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

namespace SchoolMedicalManagementSystem.API.ApiControllers;

[ApiController]
[Route("api/school-classes")]
public class SchoolClassController : ControllerBase
{
    private readonly ISchoolClassService _schoolClassService;

    public SchoolClassController(ISchoolClassService schoolClassService)
    {
        _schoolClassService = schoolClassService;
    }

    [HttpGet]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseListResponse<SchoolClassSummaryResponse>>> GetSchoolClasses(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchTerm = "",
        [FromQuery] string orderBy = null,
        [FromQuery] int? grade = null,
        [FromQuery] int? academicYear = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(
                    BaseListResponse<SchoolClassSummaryResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            var response = await _schoolClassService.GetSchoolClassesAsync(
                pageIndex, pageSize, searchTerm, orderBy, grade, academicYear, cancellationToken);

            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseListResponse<SchoolClassSummaryResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseResponse<SchoolClassResponse>>> GetSchoolClassById(Guid id)
    {
        try
        {
            var response = await _schoolClassService.GetSchoolClassByIdAsync(id);
            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<SchoolClassResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPost]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseResponse<SchoolClassResponse>>> CreateSchoolClass(
        [FromBody] CreateSchoolClassRequest model)
    {
        try
        {
            var result = await _schoolClassService.CreateSchoolClassAsync(model);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetSchoolClassById), new { id = result.Data.Id }, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<SchoolClassResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseResponse<SchoolClassResponse>>> UpdateSchoolClass(
        Guid id, [FromBody] UpdateSchoolClassRequest model)
    {
        try
        {
            var result = await _schoolClassService.UpdateSchoolClassAsync(id, model);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<SchoolClassResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseResponse<bool>>> DeleteSchoolClass(Guid id)
    {
        try
        {
            var result = await _schoolClassService.DeleteSchoolClassAsync(id);

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

    [HttpPost("{id}/students/batch")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseResponse<StudentsBatchResponse>>> AddStudentsToClass(
        Guid id, [FromBody] AddStudentsToClassRequest model)
    {
        try
        {
            var response = await _schoolClassService.AddStudentsToClassAsync(id, model);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<StudentsBatchResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpDelete("{classId}/students/{studentId}")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseResponse<bool>>> RemoveStudentFromClass(Guid classId, Guid studentId)
    {
        try
        {
            var response = await _schoolClassService.RemoveStudentFromClassAsync(classId, studentId);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpGet("template")]
    [Authorize(Roles = "MANAGER")]
    public async Task<IActionResult> DownloadTemplate()
    {
        try
        {
            var fileBytes = await _schoolClassService.DownloadSchoolClassTemplateAsync();
            var fileName = $"SchoolClass_Template_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error generating template: " + ex.Message });
        }
    }

    [HttpPost("import")]
    [Authorize(Roles = "MANAGER")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<BaseResponse<SchoolClassImportResponse>>> ImportFromExcel(
        [FromForm] ImportSchoolClassExcelRequest request)
    {
        if (request.ExcelFile == null || request.ExcelFile.Length == 0)
        {
            return BadRequest(new BaseResponse<SchoolClassImportResponse>
            {
                Success = false,
                Message = "Excel file is required."
            });
        }

        var result = await _schoolClassService.ImportSchoolClassesFromExcelAsync(request);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// PREVIEW API:
    /// - Mục đích: Xem trước kết quả import TRƯỚC KHI lưu vào database
    /// - User có thể check lỗi và sửa file Excel trước khi import thật
    /// - Tránh import sai dữ liệu và phải rollback
    /// </summary>
    /// <param name="excelFile"></param>
    /// <returns></returns>
    [HttpPost("import/preview")]
    [Authorize(Roles = "MANAGER")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<BaseResponse<List<SchoolClassImportDetailResponse>>>> PreviewImport(
        IFormFile excelFile)
    {
        if (excelFile == null || excelFile.Length == 0)
        {
            return BadRequest(new BaseResponse<List<SchoolClassImportDetailResponse>>
            {
                Success = false,
                Message = "Excel file is required for preview."
            });
        }

        try
        {
            var previewRequest = new ImportSchoolClassExcelRequest
            {
                ExcelFile = excelFile,
                OverwriteExisting = false
            };

            var result = await _schoolClassService.ImportSchoolClassesFromExcelAsync(previewRequest);

            if (!result.Success)
            {
                return BadRequest(new BaseResponse<List<SchoolClassImportDetailResponse>>
                {
                    Success = false,
                    Message = result.Message
                });
            }

            return Ok(new BaseResponse<List<SchoolClassImportDetailResponse>>
            {
                Success = true,
                Data = result.Data.ImportDetails,
                Message = $"Preview completed: {result.Data.SuccessRows} valid, {result.Data.ErrorRows} errors"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new BaseResponse<List<SchoolClassImportDetailResponse>>
            {
                Success = false,
                Message = "Error during preview: " + ex.Message
            });
        }
    }

    [HttpGet("export")]
    [Authorize(Roles = "MANAGER")]
    public async Task<IActionResult> ExportToExcel(
        [FromQuery] int? grade = null,
        [FromQuery] int? academicYear = null)
    {
        try
        {
            var fileBytes = await _schoolClassService.ExportSchoolClassesToExcelAsync(grade, academicYear);

            var fileName = "SchoolClasses";
            if (grade.HasValue) fileName += $"_Grade{grade}";
            if (academicYear.HasValue) fileName += $"_Year{academicYear}";
            fileName += $"_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error exporting to Excel: " + ex.Message });
        }
    }
}