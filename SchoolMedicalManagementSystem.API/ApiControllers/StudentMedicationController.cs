using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;
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
    [Authorize(Roles = "SCHOOLNURSE,ADMIN,MANAGER")]
    public async Task<IActionResult> GetStudentMedications(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? orderBy = null,
        [FromQuery] Guid? studentId = null,
        [FromQuery] Guid? parentId = null,
        [FromQuery] StudentMedicationStatus? status = null,
        [FromQuery] bool? expiringSoon = null,
        [FromQuery] bool? requiresAdministration = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _studentMedicationService.GetStudentMedicationsAsync(
            pageIndex, pageSize, searchTerm, orderBy,
            studentId, parentId, status, expiringSoon, requiresAdministration,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetStudentMedicationById(Guid id)
    {
        var result = await _studentMedicationService.GetStudentMedicationByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "PARENT")]
    public async Task<IActionResult> CreateStudentMedication([FromBody] CreateStudentMedicationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _studentMedicationService.CreateStudentMedicationAsync(request);

        if (result.Success)
        {
            return CreatedAtAction(
                nameof(GetStudentMedicationById),
                new { medicationId = result.Data.Id },
                result);
        }

        return BadRequest(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "PARENT")]
    public async Task<IActionResult> UpdateStudentMedication(
        Guid id,
        [FromBody] UpdateStudentMedicationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _studentMedicationService.UpdateStudentMedicationAsync(id, request);

        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "PARENT")]
    public async Task<IActionResult> DeleteStudentMedication(Guid medicationId)
    {
        var result = await _studentMedicationService.DeleteStudentMedicationAsync(medicationId);
        return Ok(result);
    }

    #endregion

    #region Approval Workflow

    [HttpPost("{id}/approve")]
    [Authorize(Roles = "SCHOOLNURSE,ADMIN,MANAGER")]
    public async Task<IActionResult> ApproveStudentMedication(
        Guid medicationId,
        [FromBody] ApproveStudentMedicationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _studentMedicationService.ApproveStudentMedicationAsync(medicationId, request);
        return Ok(result);
    }

    [HttpGet("pending-approvals")]
    [Authorize(Roles = "SCHOOLNURSE,ADMIN,MANAGER")]
    public async Task<IActionResult> GetPendingApprovals(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _studentMedicationService.GetPendingApprovalsAsync(pageIndex, pageSize, cancellationToken);
        return Ok(result);
    }

    #endregion

    #region Status Management

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "SCHOOLNURSE,ADMIN,MANAGER")]
    public async Task<IActionResult> UpdateMedicationStatus(
        Guid id,
        [FromBody] UpdateMedicationStatusRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _studentMedicationService.UpdateMedicationStatusAsync(id, request);
        return Ok(result);
    }

    [HttpGet("expired")]
    [Authorize(Roles = "SCHOOLNURSE,ADMIN,MANAGER")]
    public async Task<IActionResult> GetExpiredMedications()
    {
        var result = await _studentMedicationService.GetExpiredMedicationsAsync();
        return Ok(result);
    }

    [HttpGet("expiring-soon")]
    [Authorize(Roles = "SCHOOLNURSE,ADMIN,MANAGER")]
    public async Task<IActionResult> GetExpiringSoonMedications([FromQuery] int days = 7)
    {
        var result = await _studentMedicationService.GetExpiringSoonMedicationsAsync(days);
        return Ok(result);
    }

    #endregion

    #region Parent Specific Endpoints

    [HttpGet("my-children")]
    [Authorize(Roles = "PARENT")]
    public async Task<IActionResult> GetMyChildrenMedications(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] StudentMedicationStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _studentMedicationService.GetMyChildrenMedicationsAsync(
            pageIndex, pageSize, status, cancellationToken);
        return Ok(result);
    }

    #endregion
}