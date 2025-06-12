using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Helpers;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalItemRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalItemUsageRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalItemResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalItemUsageResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.API.ApiControllers;

[ApiController]
[Route("api/medical-items")]
public class MedicalItemController : ControllerBase
{
    private readonly IMedicalItemService _medicalItemService;

    public MedicalItemController(IMedicalItemService medicalItemService)
    {
        _medicalItemService = medicalItemService;
    }

    #region Medical Item CRUD Operations

    [HttpGet]
    [Authorize(Roles = "SCHOOLNURSE,MANAGER")]
    public async Task<ActionResult<BaseListResponse<MedicalItemResponse>>> GetMedicalItems(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchTerm = "",
        [FromQuery] string orderBy = null,
        [FromQuery] string? type = null,
        [FromQuery] bool? lowStock = null,
        [FromQuery] bool? expiringSoon = null,
        [FromQuery] bool? expired = null,
        [FromQuery] string? alertsOnly = null,
        [FromQuery] int? expiryDays = 30,
        [FromQuery] MedicalItemApprovalStatus? approvalStatus = null,
        [FromQuery] PriorityLevel? priority = null,
        [FromQuery] bool? urgentOnly = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(
                    BaseListResponse<MedicalItemResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            if (!string.IsNullOrEmpty(alertsOnly))
            {
                var validAlertTypes = new[]
                    { "low_stock", "expired", "expiring_soon", "all_alerts", "critical", "out_of_stock" };
                if (!validAlertTypes.Contains(alertsOnly.ToLower()))
                {
                    return BadRequest(BaseListResponse<MedicalItemResponse>.ErrorResult(
                        "Loại cảnh báo không hợp lệ. Sử dụng: low_stock, expired, expiring_soon, all_alerts, critical, out_of_stock"));
                }
            }

            if (expiryDays.HasValue && (expiryDays < 1 || expiryDays > 365))
            {
                return BadRequest(BaseListResponse<MedicalItemResponse>.ErrorResult("Số ngày không hợp lệ (1-365)."));
            }

            var response = await _medicalItemService.GetMedicalItemsAsync(
                pageIndex, pageSize, searchTerm, orderBy, type, lowStock, expiringSoon, expired,
                alertsOnly, expiryDays, approvalStatus, priority, urgentOnly, cancellationToken);

            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseListResponse<MedicalItemResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "SCHOOLNURSE,MANAGER")]
    public async Task<ActionResult<BaseResponse<MedicalItemResponse>>> GetMedicalItemById(Guid id)
    {
        try
        {
            var response = await _medicalItemService.GetMedicalItemByIdAsync(id);
            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<MedicalItemResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPost]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<MedicalItemResponse>>> CreateMedicalItem(
        [FromBody] CreateMedicalItemRequest model)
    {
        try
        {
            var result = await _medicalItemService.CreateMedicalItemAsync(model);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetMedicalItemById), new { id = result.Data.Id }, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<MedicalItemResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    // Business logic in service handles:
    // - School Nurse: Can update own Pending/Rejected items
    // - Manager: Can update Approved items (basic info)
    [HttpPut("{id}")]
    [Authorize(Roles = "SCHOOLNURSE,MANAGER")]
    public async Task<ActionResult<BaseResponse<MedicalItemResponse>>> UpdateMedicalItem(
        Guid id, [FromBody] UpdateMedicalItemRequest model)
    {
        try
        {
            var result = await _medicalItemService.UpdateMedicalItemAsync(id, model);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<MedicalItemResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseResponse<bool>>> DeleteMedicalItem(Guid id)
    {
        try
        {
            var result = await _medicalItemService.DeleteMedicalItemAsync(id);

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

    #region Approval Workflow

    [HttpGet("pending-approvals")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseListResponse<MedicalItemResponse>>> GetPendingApprovals(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchTerm = "",
        [FromQuery] string orderBy = null,
        [FromQuery] PriorityLevel? priority = null,
        [FromQuery] bool? urgentOnly = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(
                    BaseListResponse<MedicalItemResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            var response = await _medicalItemService.GetPendingApprovalsAsync(
                pageIndex, pageSize, searchTerm, orderBy, priority, urgentOnly, cancellationToken);

            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseListResponse<MedicalItemResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPut("{id}/approve")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseResponse<MedicalItemResponse>>> ApproveMedicalItem(
        Guid id,
        [FromBody] ApproveMedicalItemRequest model)
    {
        try
        {
            var result = await _medicalItemService.ApproveMedicalItemAsync(id, model);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<MedicalItemResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPut("{id}/reject")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseResponse<MedicalItemResponse>>> RejectMedicalItem(
        Guid id,
        [FromBody] RejectMedicalItemRequest model)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(model.RejectionReason))
            {
                return BadRequest(
                    BaseResponse<MedicalItemResponse>.ErrorResult("Lý do từ chối không được để trống."));
            }

            var result = await _medicalItemService.RejectMedicalItemAsync(id, model);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<MedicalItemResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpGet("my-requests")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseListResponse<MedicalItemResponse>>> GetMyRequests(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchTerm = "",
        [FromQuery] string orderBy = null,
        [FromQuery] MedicalItemApprovalStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(
                    BaseListResponse<MedicalItemResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            var currentUserId = Guid.Parse(UserHelper.GetCurrentUserId(HttpContext));

            var response = await _medicalItemService.GetMedicalItemsAsync(
                pageIndex, pageSize, searchTerm, orderBy,
                type: null,
                lowStock: null,
                expiringSoon: null,
                expired: null,
                alertsOnly: null,
                expiryDays: 30,
                approvalStatus: status,
                priority: null,
                urgentOnly: null,
                cancellationToken);

            if (!response.Success)
                return NotFound(response);

            var filteredData = response.Data
                .Where(x => x.RequestedByStaffCode != null)
                .ToList();

            var filteredResponse = BaseListResponse<MedicalItemResponse>.SuccessResult(
                filteredData,
                filteredData.Count,
                pageSize,
                pageIndex,
                "Lấy danh sách yêu cầu của tôi thành công.");

            return Ok(filteredResponse);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseListResponse<MedicalItemResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    #endregion

    #region Stock Management

    [HttpGet("stock-summary")]
    [Authorize(Roles = "SCHOOLNURSE,MANAGER")]
    public async Task<ActionResult<BaseResponse<MedicalItemStockSummaryResponse>>> GetStockSummary()
    {
        try
        {
            var response = await _medicalItemService.GetStockSummaryAsync();
            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<MedicalItemStockSummaryResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPut("{id}/stock")]
    [Authorize(Roles = "MANAGER")]
    public async Task<ActionResult<BaseResponse<bool>>> UpdateStockQuantity(
        Guid id,
        [FromBody] UpdateStockQuantityRequest request)
    {
        try
        {
            if (request.NewQuantity < 0)
                return BadRequest(BaseResponse<bool>.ErrorResult("Số lượng không được âm."));

            var result = await _medicalItemService.UpdateStockQuantityAsync(id, request.NewQuantity, request.Reason);

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

    #region Medical Item Usage Management

    [HttpPost("usage")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<MedicalItemUsageResponse>>> RecordMedicalItemUsage(
        [FromBody] CreateMedicalItemUsageRequest model)
    {
        try
        {
            var result = await _medicalItemService.RecordMedicalItemUsageAsync(model);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<MedicalItemUsageResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPost("usage/{originalUsageId}/correct")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<MedicalItemUsageResponse>>> CorrectMedicalItemUsage(
        Guid originalUsageId,
        [FromBody] CorrectMedicalItemUsageRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CorrectionReason))
            {
                return BadRequest(
                    BaseResponse<MedicalItemUsageResponse>.ErrorResult("Lý do điều chỉnh không được để trống."));
            }

            var result = await _medicalItemService.CorrectMedicalItemUsageAsync(
                originalUsageId, request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<MedicalItemUsageResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPost("usage/{originalUsageId}/return")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<MedicalItemUsageResponse>>> ReturnMedicalItem(
        Guid originalUsageId,
        [FromBody] ReturnMedicalItemRequest request)
    {
        try
        {
            if (request.ReturnQuantity <= 0)
            {
                return BadRequest(
                    BaseResponse<MedicalItemUsageResponse>.ErrorResult("Số lượng hoàn trả phải lớn hơn 0."));
            }

            if (string.IsNullOrWhiteSpace(request.ReturnReason))
            {
                return BadRequest(
                    BaseResponse<MedicalItemUsageResponse>.ErrorResult("Lý do hoàn trả không được để trống."));
            }

            var result = await _medicalItemService.ReturnMedicalItemAsync(
                originalUsageId, request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseResponse<MedicalItemUsageResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpGet("usage-history")]
    [Authorize(Roles = "SCHOOLNURSE,MANAGER")]
    public async Task<ActionResult<BaseListResponse<MedicalItemUsageResponse>>> GetUsageHistory(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchTerm = "",
        [FromQuery] string orderBy = null,
        [FromQuery] Guid? medicalItemId = null,
        [FromQuery] Guid? studentId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(
                    BaseListResponse<MedicalItemUsageResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            var response = await _medicalItemService.GetUsageHistoryAsync(
                pageIndex, pageSize, searchTerm, orderBy, medicalItemId, studentId, fromDate, toDate,
                cancellationToken);

            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseListResponse<MedicalItemUsageResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpGet("usage-history/student/{studentId}")]
    [Authorize(Roles = "SCHOOLNURSE,MANAGER")]
    public async Task<ActionResult<BaseListResponse<MedicalItemUsageResponse>>> GetUsageHistoryByStudent(
        Guid studentId,
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
                    BaseListResponse<MedicalItemUsageResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            var response = await _medicalItemService.GetUsageHistoryByStudentAsync(
                studentId, pageIndex, pageSize, fromDate, toDate, cancellationToken);

            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, BaseListResponse<MedicalItemUsageResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    #endregion
}