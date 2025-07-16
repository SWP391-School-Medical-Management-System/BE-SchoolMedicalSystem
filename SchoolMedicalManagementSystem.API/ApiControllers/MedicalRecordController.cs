using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalRecordRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalRecordResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

namespace SchoolMedicalManagementSystem.API.ApiControllers;

[ApiController]
[Route("api/medical-records")]
public class MedicalRecordController : ControllerBase
{
    private readonly IMedicalRecordService _medicalRecordService;

    public MedicalRecordController(IMedicalRecordService medicalRecordService)
    {
        _medicalRecordService = medicalRecordService;
    }

    [HttpGet]
    public async Task<ActionResult<BaseListResponse<MedicalRecordResponse>>> GetMedicalRecords(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchTerm = "",
        [FromQuery] string orderBy = null,
        [FromQuery] string bloodType = null,
        [FromQuery] bool? hasAllergies = null,
        [FromQuery] bool? hasChronicDisease = null,
        [FromQuery] bool? needsUpdate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(
                    BaseListResponse<MedicalRecordResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            var response = await _medicalRecordService.GetMedicalRecordsAsync(
                pageIndex, pageSize, searchTerm, orderBy, bloodType, hasAllergies, hasChronicDisease, needsUpdate,
                cancellationToken);

            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseListResponse<MedicalRecordResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BaseResponse<MedicalRecordDetailResponse>>> GetMedicalRecordById(Guid id)
    {
        try
        {
            var response = await _medicalRecordService.GetMedicalRecordByIdAsync(id);
            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<MedicalRecordDetailResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<BaseResponse<MedicalRecordDetailResponse>>> GetMedicalRecordByStudentId(
        Guid studentId)
    {
        try
        {
            var response = await _medicalRecordService.GetMedicalRecordByStudentIdAsync(studentId);
            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<MedicalRecordDetailResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPost]
    public async Task<ActionResult<BaseResponse<MedicalRecordDetailResponse>>> CreateMedicalRecord(
        [FromBody] CreateMedicalRecordRequest model)
    {
        try
        {
            var result = await _medicalRecordService.CreateMedicalRecordAsync(model);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetMedicalRecordById), new { id = result.Data.Id }, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<MedicalRecordDetailResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BaseResponse<MedicalRecordDetailResponse>>> UpdateMedicalRecord(
        Guid id, [FromBody] UpdateMedicalRecordRequest model)
    {
        try
        {
            var result = await _medicalRecordService.UpdateMedicalRecordAsync(id, model);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<MedicalRecordDetailResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPost("{studentId}/update-by-parent")]
    public async Task<IActionResult> UpdateMedicalRecordByParent(Guid studentId, [FromBody] UpdateMedicalRecordByParentRequest model)
    {
        var parentId = User.FindFirst("uid")?.Value; // Lấy uid từ token
        if (string.IsNullOrEmpty(parentId) || !Guid.TryParse(parentId, out Guid currentParentId))
        {
            return BadRequest(new BaseResponse<MedicalRecordDetailResponse>
            {
                Success = false,
                Message = "Không thể xác định phụ huynh hiện tại."
            });
        }

        var response = await _medicalRecordService.UpdateMedicalRecordByParentAsync(studentId, model, currentParentId);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<BaseResponse<bool>>> DeleteMedicalRecord(Guid id)
    {
        try
        {
            var result = await _medicalRecordService.DeleteMedicalRecordAsync(id);

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

    [HttpPost("{studentId}/vision-record")]
    public async Task<IActionResult> CreateVisionRecordByParent(Guid studentId, [FromBody] CreateVisionRecordRequest model)
    {
        var parentId = User.FindFirst("uid")?.Value;
        if (string.IsNullOrEmpty(parentId) || !Guid.TryParse(parentId, out Guid currentParentId))
        {
            return BadRequest(new BaseResponse<MedicalRecordDetailResponse>
            {
                Success = false,
                Message = "Không thể xác định phụ huynh hiện tại."
            });
        }

        var response = await _medicalRecordService.CreateVisionRecordByParentAsync(studentId, model, currentParentId);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("{studentId}/hearing-record")]
    public async Task<IActionResult> CreateHearingRecordByParent(Guid studentId, [FromBody] CreateHearingRecordRequest model)
    {
        var parentId = User.FindFirst("uid")?.Value;
        if (string.IsNullOrEmpty(parentId) || !Guid.TryParse(parentId, out Guid currentParentId))
        {
            return BadRequest(new BaseResponse<MedicalRecordDetailResponse>
            {
                Success = false,
                Message = "Không thể xác định phụ huynh hiện tại."
            });
        }

        var response = await _medicalRecordService.CreateHearingRecordByParentAsync(studentId, model, currentParentId);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("{studentId}/physical-record")]
    public async Task<IActionResult> CreatePhysicalRecordByParent(Guid studentId, [FromBody] CreatePhysicalRecordRequest model)
    {
        var parentId = User.FindFirst("uid")?.Value;
        if (string.IsNullOrEmpty(parentId) || !Guid.TryParse(parentId, out Guid currentParentId))
        {
            return BadRequest(new BaseResponse<MedicalRecordDetailResponse>
            {
                Success = false,
                Message = "Không thể xác định phụ huynh hiện tại."
            });
        }

        var response = await _medicalRecordService.CreatePhysicalRecordByParentAsync(studentId, model, currentParentId);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }
}