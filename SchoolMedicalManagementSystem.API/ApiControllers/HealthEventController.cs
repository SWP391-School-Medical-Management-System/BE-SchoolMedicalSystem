using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthEventRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthEventResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.API.ApiControllers;

[ApiController]
[Route("api/health-events")]
public class HealthEventController : ControllerBase
{
    private readonly IHealthEventService _healthEventService;

    public HealthEventController(IHealthEventService healthEventService)
    {
        _healthEventService = healthEventService;
    }

    #region Basic CRUD Operations

    [HttpGet]
    [Authorize(Roles = "SCHOOLNURSE,MANAGER")]
    public async Task<ActionResult<BaseListResponse<HealthEventResponse>>> GetHealthEvents(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchTerm = "",
        [FromQuery] string orderBy = null,
        [FromQuery] Guid? studentId = null,
        [FromQuery] HealthEventType? eventType = null,
        [FromQuery] bool? isEmergency = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? location = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(
                    BaseListResponse<HealthEventResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            var response = await _healthEventService.GetHealthEventsAsync(
                pageIndex, pageSize, searchTerm, orderBy, studentId, eventType, isEmergency,
                fromDate, toDate, location, cancellationToken);

            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseListResponse<HealthEventResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<HealthEventResponse>>> GetHealthEventById(Guid id)
    {
        try
        {
            var response = await _healthEventService.GetHealthEventByIdAsync(id);
            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<HealthEventResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPost]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<HealthEventResponse>>> CreateHealthEvent(
        [FromBody] CreateHealthEventRequest model)
    {
        try
        {
            var result = await _healthEventService.CreateHealthEventAsync(model);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetHealthEventById), new { id = result.Data.Id }, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<HealthEventResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<HealthEventResponse>>> UpdateHealthEvent(
        Guid id, [FromBody] UpdateHealthEventRequest model)
    {
        try
        {
            var result = await _healthEventService.UpdateHealthEventAsync(id, model);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<HealthEventResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<bool>>> DeleteHealthEvent(Guid id)
    {
        try
        {
            var result = await _healthEventService.DeleteHealthEventAsync(id);

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

    #endregion

    #region Health Event Management

    /// <summary>
    /// Lấy danh sách sự kiện y tế theo học sinh
    /// </summary>
    [HttpGet("student/{studentId}")]
    [Authorize(Roles = "SCHOOLNURSE,PARENT,MANAGER")]
    public async Task<ActionResult<BaseListResponse<HealthEventResponse>>> GetHealthEventsByStudent(
        Guid studentId,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] HealthEventType? eventType = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (studentId == Guid.Empty)
                return BadRequest(BaseListResponse<HealthEventResponse>.ErrorResult("ID học sinh không hợp lệ."));

            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(
                    BaseListResponse<HealthEventResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            if (pageSize > 100)
                return BadRequest(
                    BaseListResponse<HealthEventResponse>.ErrorResult("Kích thước trang không được vượt quá 100."));

            var response = await _healthEventService.GetHealthEventsByStudentAsync(
                studentId, pageIndex, pageSize, eventType, fromDate, toDate, cancellationToken);

            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseListResponse<HealthEventResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    /// <summary>
    /// Lấy danh sách sự kiện y tế khẩn cấp
    /// </summary>
    [HttpGet("emergency")]
    [Authorize(Roles = "SCHOOLNURSE,MANAGER")]
    public async Task<ActionResult<BaseListResponse<HealthEventResponse>>> GetEmergencyEvents(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(
                    BaseListResponse<HealthEventResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            if (pageSize > 100)
                return BadRequest(
                    BaseListResponse<HealthEventResponse>.ErrorResult("Kích thước trang không được vượt quá 100."));

            var response = await _healthEventService.GetEmergencyEventsAsync(
                pageIndex, pageSize, fromDate, toDate, cancellationToken);

            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseListResponse<HealthEventResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    /// <summary>
    /// School Nurse tự nhận sự kiện y tế
    /// </summary>
    [HttpPost("{id}/take-ownership")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<HealthEventResponse>>> TakeOwnership(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(BaseResponse<HealthEventResponse>.ErrorResult("ID sự kiện không hợp lệ."));

            var result = await _healthEventService.TakeOwnershipAsync(id);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<HealthEventResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    /// <summary>
    /// Manager phân công sự kiện y tế cho School Nurse
    /// </summary>
    [HttpPost("{id}/assign")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseResponse<HealthEventResponse>>> AssignToNurse(
        Guid id, [FromBody] AssignHealthEventRequest request)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(BaseResponse<HealthEventResponse>.ErrorResult("ID sự kiện không hợp lệ."));

            if (request.NurseId == Guid.Empty)
                return BadRequest(BaseResponse<HealthEventResponse>.ErrorResult("ID y tá không hợp lệ."));

            var result = await _healthEventService.AssignToNurseAsync(id, request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<HealthEventResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    /// <summary>
    /// Lấy danh sách sự kiện chưa được phân công
    /// </summary>
    [HttpGet("unassigned")]
    [Authorize(Roles = "SCHOOLNURSE,MANAGER")]
    public async Task<ActionResult<BaseListResponse<HealthEventResponse>>> GetUnassignedEvents(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(
                    BaseListResponse<HealthEventResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            if (pageSize > 100)
                return BadRequest(
                    BaseListResponse<HealthEventResponse>.ErrorResult("Kích thước trang không được vượt quá 100."));

            var response = await _healthEventService.GetUnassignedEventsAsync(pageIndex, pageSize, cancellationToken);

            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseListResponse<HealthEventResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    /// <summary>
    /// Hoàn thành xử lý sự kiện y tế
    /// </summary>
    [HttpPost("{id}/complete")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<HealthEventResponse>>> CompleteEvent(
        Guid id, [FromBody] CompleteHealthEventRequest request)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(BaseResponse<HealthEventResponse>.ErrorResult("ID sự kiện không hợp lệ."));

            var result = await _healthEventService.CompleteEventAsync(id, request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<HealthEventResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    /// <summary>
    /// Lấy danh sách sự kiện được phân công cho người dùng hiện tại
    /// </summary>
    [HttpGet("my-assignments")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseListResponse<HealthEventResponse>>> GetMyAssignedEvents(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] HealthEventStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(
                    BaseListResponse<HealthEventResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            if (pageSize > 100)
                return BadRequest(
                    BaseListResponse<HealthEventResponse>.ErrorResult("Kích thước trang không được vượt quá 100."));

            var response =
                await _healthEventService.GetMyAssignedEventsAsync(pageIndex, pageSize, status, cancellationToken);

            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseListResponse<HealthEventResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    #endregion
}