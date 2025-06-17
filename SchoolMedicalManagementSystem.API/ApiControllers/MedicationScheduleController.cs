using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicationScheduleRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicationScheduleResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationAdministrationResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.API.Controllers;

[ApiController]
[Route("api/medication-schedules")]
public class MedicationScheduleController : ControllerBase
{
    private readonly IMedicationScheduleService _medicationScheduleService;
    private readonly ILogger<MedicationScheduleController> _logger;

    public MedicationScheduleController(
        IMedicationScheduleService medicationScheduleService,
        ILogger<MedicationScheduleController> logger)
    {
        _medicationScheduleService = medicationScheduleService;
        _logger = logger;
    }

    #region Daily Views

    [HttpGet("daily")]
    [Authorize(Roles = "SCHOOLNURSE,MANAGER")]
    public async Task<ActionResult<BaseResponse<DailyMedicationScheduleResponse>>> GetDailySchedule(
        [FromQuery] DateTime date,
        [FromQuery] Guid? studentId = null,
        [FromQuery] MedicationScheduleStatus? status = null,
        [FromQuery] bool includeCompleted = true)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _medicationScheduleService.GetDailyScheduleAsync(
                date, studentId, status, includeCompleted);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily schedule for date: {Date}", date);
            return StatusCode(500, BaseResponse<DailyMedicationScheduleResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpGet("my-schedules")]
    [Authorize(Roles = "STUDENT")]
    public async Task<ActionResult<BaseResponse<List<MedicationScheduleResponse>>>> GetMySchedules(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] MedicationScheduleStatus? status = null)
    {
        try
        {
            var result = await _medicationScheduleService.GetMySchedulesAsync(
                startDate, endDate, status);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my schedules");
            return StatusCode(500, BaseResponse<List<MedicationScheduleResponse>>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpGet("children-schedules")]
    [Authorize(Roles = "PARENT")]
    public async Task<ActionResult<BaseResponse<List<DailyMedicationScheduleResponse>>>> GetChildrenSchedules(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] int days = 7)
    {
        try
        {
            var result = await _medicationScheduleService.GetChildrenSchedulesAsync(
                startDate, days);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting children schedules");
            return StatusCode(500, BaseResponse<List<DailyMedicationScheduleResponse>>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "STUDENT,PARENT,SCHOOLNURSE,MANAGER")]
    public async Task<ActionResult<BaseResponse<MedicationScheduleResponse>>> GetScheduleDetail(Guid id)
    {
        try
        {
            var result = await _medicationScheduleService.GetScheduleDetailAsync(id);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting schedule detail: {ScheduleId}", id);
            return StatusCode(500, BaseResponse<MedicationScheduleResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    #endregion

    #region Schedule Actions

    [HttpPost("{id}/administer")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<AdministerScheduleResponse>>> AdministerSchedule(
        Guid id,
        [FromBody] AdministerScheduleRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _medicationScheduleService.AdministerScheduleAsync(id, request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error administering schedule: {ScheduleId}", id);
            return StatusCode(500, BaseResponse<AdministerScheduleResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPost("bulk-administer")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<BulkAdministerResponse>>> BulkAdministerSchedules(
        [FromBody] BulkAdministerRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!request.Schedules?.Any() == true)
            {
                return BadRequest(BaseResponse<BulkAdministerResponse>.ErrorResult(
                    "Danh sách lịch trình không được để trống."));
            }

            var result = await _medicationScheduleService.BulkAdministerSchedulesAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk administering schedules");
            return StatusCode(500, BaseResponse<BulkAdministerResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPut("{id}/quick-complete")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<MedicationScheduleResponse>>> QuickCompleteSchedule(
        Guid id,
        [FromBody] QuickCompleteRequest request)
    {
        try
        {
            var result = await _medicationScheduleService.QuickCompleteScheduleAsync(
                id, request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error quick completing schedule: {ScheduleId}", id);
            return StatusCode(500, BaseResponse<MedicationScheduleResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPost("missed")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<MedicationScheduleResponse>>> MarkMissed(
        [FromBody] MarkMissedMedicationRequest request)
    {
        try
        {
            var result = await _medicationScheduleService.MarkMissedAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking medication schedule as missed: {ScheduleId}", request.ScheduleId);
            return StatusCode(500, BaseResponse<MedicationScheduleResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPost("student-absent")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<MedicationScheduleResponse>>> MarkStudentAbsent(
        [FromBody] MarkStudentAbsentRequest request)
    {
        try
        {
            var result = await _medicationScheduleService.MarkStudentAbsentAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking medication schedule as absent: {ScheduleId}", request.ScheduleId);
            return StatusCode(500, BaseResponse<MedicationScheduleResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    #endregion
}