using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Helpers;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicationStockResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationAdministrationResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.API.Controllers;

[ApiController]
[Route("api/student-medications")]
public class StudentMedicationController : ControllerBase
{
    private readonly IStudentMedicationService _studentMedicationService;
    private readonly ILogger<StudentMedicationController> _logger;

    public StudentMedicationController(
        IStudentMedicationService studentMedicationService,
        ILogger<StudentMedicationController> logger)
    {
        _studentMedicationService = studentMedicationService;
        _logger = logger;
    }

    #region Basic CRUD Operations

    [HttpGet]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseListResponse<StudentMedicationListResponse>>> GetStudentMedications(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? orderBy = null,
        [FromQuery] Guid? studentId = null,
        [FromQuery] Guid? parentId = null,
        [FromQuery] StudentMedicationStatus? status = null,
        [FromQuery] bool? expiringSoon = null,
        [FromQuery] bool? requiresAdministration = null,
        [FromQuery] bool? lowStock = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(
                    BaseListResponse<StudentMedicationListResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            var result = await _studentMedicationService.GetStudentMedicationsAsync(
                pageIndex, pageSize, searchTerm, orderBy,
                studentId, parentId, status, expiringSoon, requiresAdministration,
                cancellationToken);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student medications");
            return StatusCode(500, BaseListResponse<StudentMedicationListResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    /// <summary>
    /// Lấy chi tiết thuốc (tất cả role có thể xem nhưng có permission check)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "SCHOOLNURSE,PARENT,STUDENT")]
    [Authorize]
    public async Task<ActionResult<BaseResponse<StudentMedicationDetailResponse>>> GetStudentMedicationById(Guid id)
    {
        try
        {
            var result = await _studentMedicationService.GetStudentMedicationByIdAsync(id);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student medication by ID: {Id}", id);
            return StatusCode(500, BaseResponse<StudentMedicationDetailResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    /// <summary>
    /// Lấy lịch sử stock thuốc của một medication cụ thể (Parent view)
    /// </summary>
    [HttpGet("{id}/stocks")]
    [Authorize(Roles = "PARENT")]
    public async Task<ActionResult<BaseListResponse<MedicationStockResponse>>> GetMedicationStocks(
        Guid id,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(
                    BaseListResponse<MedicationStockResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            var result = await _studentMedicationService.GetMedicationStockHistoryAsync(
                id, pageIndex, pageSize, cancellationToken);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting medication stocks for: {Id}", id);
            return StatusCode(500, BaseListResponse<MedicationStockResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPost]
    [Authorize(Roles = "PARENT")]
    public async Task<ActionResult<BaseResponse<StudentMedicationResponse>>> CreateStudentMedication(
        [FromBody] CreateStudentMedicationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _studentMedicationService.CreateStudentMedicationAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(
                nameof(GetStudentMedicationById),
                new { id = result.Data.Id },
                result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student medication");
            return StatusCode(500, BaseResponse<StudentMedicationResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPost]
    [Authorize(Roles = "PARENT")]
    [Route("api/student-medications/bulk")]
    public async Task<ActionResult<BaseListResponse<StudentMedicationResponse>>> CreateBulkStudentMedications(
    [FromBody] CreateBulkStudentMedicationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _studentMedicationService.CreateBulkStudentMedicationsAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(
                nameof(GetStudentMedications),
                new { pageIndex = 1, pageSize = 10 },
                result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bulk student medications for student {StudentId}", request.StudentId);
            return StatusCode(500, BaseListResponse<StudentMedicationResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "PARENT")]
    public async Task<ActionResult<BaseResponse<StudentMedicationResponse>>> UpdateStudentMedication(
        Guid id,
        [FromBody] UpdateStudentMedicationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _studentMedicationService.UpdateStudentMedicationAsync(id, request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating student medication: {Id}", id);
            return StatusCode(500, BaseResponse<StudentMedicationResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPost("stocks")]
    [Authorize(Roles = "PARENT")]
    public async Task<ActionResult<BaseResponse<StudentMedicationResponse>>> AddMoreMedication(
        [FromBody] AddMoreMedicationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _studentMedicationService.AddMoreMedicationAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding more medication for: {MedicationId}", request?.StudentMedicationId);
            return StatusCode(500, BaseResponse<StudentMedicationResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPatch("{id}/management")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<StudentMedicationResponse>>> UpdateMedicationManagement(
        Guid id,
        [FromBody] UpdateMedicationManagementRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _studentMedicationService.UpdateMedicationManagementAsync(id, request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating medication management: {Id}", id);
            return StatusCode(500, BaseResponse<StudentMedicationResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "PARENT")]
    public async Task<ActionResult<BaseResponse<bool>>> DeleteStudentMedication(Guid id)
    {
        try
        {
            var result = await _studentMedicationService.DeleteStudentMedicationAsync(id);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting student medication: {Id}", id);
            return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
        }
    }

    #endregion

    #region Approval Workflow Endpoints

    [HttpGet("pending-approvals")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseListResponse<PendingApprovalResponse>>> GetPendingApprovals(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(
                    BaseListResponse<PendingApprovalResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            var result = await _studentMedicationService.GetPendingApprovalsAsync(
                pageIndex, pageSize, cancellationToken);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending approvals");
            return StatusCode(500, BaseListResponse<PendingApprovalResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPut("{id}/approve")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<StudentMedicationResponse>>> ApproveStudentMedication(
        Guid id,
        [FromBody] ApproveStudentMedicationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _studentMedicationService.ApproveStudentMedicationAsync(id, request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving student medication: {Id}", id);
            return StatusCode(500, BaseResponse<StudentMedicationResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpPut("{id}/reject")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<StudentMedicationResponse>>> RejectStudentMedication(
        Guid id,
        [FromBody] RejectStudentMedicationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(request.RejectionReason))
            {
                return BadRequest(
                    BaseResponse<StudentMedicationResponse>.ErrorResult("Lý do từ chối không được để trống."));
            }

            var result = await _studentMedicationService.RejectStudentMedicationAsync(id, request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting student medication: {Id}", id);
            return StatusCode(500, BaseResponse<StudentMedicationResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    #endregion

    #region Status Management Endpoints

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseResponse<StudentMedicationResponse>>> UpdateMedicationStatus(
        Guid id,
        [FromBody] UpdateMedicationStatusRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _studentMedicationService.UpdateMedicationStatusAsync(id, request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating medication status: {Id}", id);
            return StatusCode(500, BaseResponse<StudentMedicationResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    #endregion

    #region Parent Specific Endpoints

    [HttpGet("my-children")]
    [Authorize(Roles = "PARENT")]
    public async Task<ActionResult<BaseListResponse<ParentMedicationResponse>>> GetMyChildrenMedications(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] StudentMedicationStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(
                    BaseListResponse<ParentMedicationResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            var result = await _studentMedicationService.GetMyChildrenMedicationsAsync(
                pageIndex, pageSize, status, cancellationToken);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my children medications");
            return StatusCode(500, BaseListResponse<ParentMedicationResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    [HttpGet("my-requests")]
    [Authorize(Roles = "PARENT")]
    public async Task<ActionResult<BaseListResponse<StudentMedicationResponse>>> GetMyRequests(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? orderBy = null,
        [FromQuery] StudentMedicationStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(
                    BaseListResponse<StudentMedicationResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            var currentUserId = Guid.Parse(UserHelper.GetCurrentUserId(HttpContext));

            var result = await _studentMedicationService.GetStudentMedicationsAsync(
                pageIndex, pageSize, searchTerm, orderBy,
                studentId: null,
                parentId: currentUserId,
                status: status,
                expiringSoon: null,
                requiresAdministration: null,
                cancellationToken);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my requests");
            return StatusCode(500, BaseListResponse<StudentMedicationResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    #endregion

    #region Student Specific Endpoints

    [HttpGet("my-medications")]
    [Authorize(Roles = "STUDENT")]
    public async Task<ActionResult<BaseListResponse<StudentMedicationResponse>>> GetMyMedications(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] StudentMedicationStatus? status = null,
        [FromQuery] bool? expiringSoon = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(
                    BaseListResponse<StudentMedicationResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            var currentUserId = Guid.Parse(UserHelper.GetCurrentUserId(HttpContext));

            var result = await _studentMedicationService.GetStudentMedicationsAsync(
                pageIndex, pageSize, null, null,
                studentId: currentUserId,
                parentId: null,
                status: status,
                expiringSoon: expiringSoon,
                requiresAdministration: null,
                cancellationToken);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my medications");
            return StatusCode(500, BaseListResponse<StudentMedicationResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    #endregion

    #region Medication Administration

    /// <summary>
    /// Lịch sử cho uống thuốc (tất cả có thể xem với permission check)
    /// </summary>
    [HttpGet("{id}/administration-history")]
    [Authorize]
    public async Task<ActionResult<BaseListResponse<MedicationAdministrationResponse>>> GetAdministrationHistory(
        Guid id,
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
                    BaseListResponse<MedicationAdministrationResponse>.ErrorResult(
                        "Thông tin phân trang không hợp lệ."));

            var result = await _studentMedicationService.GetAdministrationHistoryAsync(
                id, pageIndex, pageSize, fromDate, toDate, cancellationToken);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting administration history: {Id}", id);
            return StatusCode(500, BaseListResponse<MedicationAdministrationResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    #endregion
}