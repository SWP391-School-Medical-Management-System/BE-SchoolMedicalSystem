using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalConditionRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalCondition;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.API.ApiControllers;

[ApiController]
[Route("api/medical-conditions")]
public class MedicalConditionController : ControllerBase
{
    private readonly IMedicalConditionService _medicalConditionService;

    public MedicalConditionController(IMedicalConditionService medicalConditionService)
    {
        _medicalConditionService = medicalConditionService;
    }

    [HttpGet]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseListResponse<MedicalConditionResponse>>> GetMedicalConditions(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchTerm = "",
        [FromQuery] string orderBy = null,
        [FromQuery] Guid? medicalRecordId = null,
        [FromQuery] MedicalConditionType? type = null,
        [FromQuery] string severity = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(
                    BaseListResponse<MedicalConditionResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            var response = await _medicalConditionService.GetMedicalConditionsAsync(
                pageIndex, pageSize, searchTerm, orderBy, medicalRecordId, type, severity, cancellationToken);

            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseListResponse<MedicalConditionResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<MedicalConditionResponse>>> GetMedicalConditionById(Guid id)
    {
        try
        {
            var response = await _medicalConditionService.GetMedicalConditionByIdAsync(id);
            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<MedicalConditionResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpGet("medical-record/{medicalRecordId}")]
    [Authorize(Roles = "SCHOOLNURSE,PARENT")]
    public async Task<ActionResult<BaseListResponse<MedicalConditionResponse>>> GetMedicalConditionsByRecordId(
        Guid medicalRecordId,
        [FromQuery] MedicalConditionType? type = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _medicalConditionService.GetMedicalConditionsByRecordIdAsync(
                medicalRecordId, type, cancellationToken);

            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseListResponse<MedicalConditionResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPost]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<MedicalConditionResponse>>> CreateMedicalCondition(
        [FromBody] CreateMedicalConditionRequest model)
    {
        try
        {
            var result = await _medicalConditionService.CreateMedicalConditionAsync(model);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetMedicalConditionById), new { id = result.Data.Id }, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<MedicalConditionResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<MedicalConditionResponse>>> UpdateMedicalCondition(
        Guid id, [FromBody] UpdateMedicalConditionRequest model)
    {
        try
        {
            var result = await _medicalConditionService.UpdateMedicalConditionAsync(id, model);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<MedicalConditionResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<bool>>> DeleteMedicalCondition(Guid id)
    {
        try
        {
            var result = await _medicalConditionService.DeleteMedicalConditionAsync(id);

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
}