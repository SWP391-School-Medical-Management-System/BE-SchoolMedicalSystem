using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicationStockResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

namespace SchoolMedicalManagementSystem.API.ApiControllers;

[ApiController]
[Route("api/medication-stocks")]
public class MedicationStockController : ControllerBase
{
    private readonly IStudentMedicationService _studentMedicationService;
    private readonly ILogger<MedicationStockController> _logger;

    public MedicationStockController(
        IStudentMedicationService studentMedicationService,
        ILogger<MedicationStockController> logger)
    {
        _studentMedicationService = studentMedicationService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy tất cả lịch sử stock thuốc với filters (School Nurse view)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseListResponse<MedicationStockResponse>>> GetAllMedicationStocks(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? studentId = null,
        [FromQuery] Guid? parentId = null,
        [FromQuery] Guid? medicationId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] bool? isExpired = null,
        [FromQuery] bool? lowStock = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(
                    BaseListResponse<MedicationStockResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            if (pageSize > 100)
                return BadRequest(
                    BaseListResponse<MedicationStockResponse>.ErrorResult("Kích thước trang không được vượt quá 100."));

            if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
                return BadRequest(
                    BaseListResponse<MedicationStockResponse>.ErrorResult(
                        "Ngày bắt đầu không được sau ngày kết thúc."));

            var result = await _studentMedicationService.GetAllMedicationStockHistoryAsync(
                pageIndex, pageSize, studentId, parentId, medicationId,
                fromDate, toDate, isExpired, lowStock, cancellationToken);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all medication stocks");
            return StatusCode(500, BaseListResponse<MedicationStockResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    /// <summary>
    /// Lấy lịch sử stock thuốc của Parent hiện tại (tất cả con em)
    /// </summary>
    [HttpGet("mine")]
    [Authorize(Roles = "PARENT")]
    public async Task<ActionResult<BaseListResponse<MedicationStockResponse>>> GetMyMedicationStocks(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? studentId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(
                    BaseListResponse<MedicationStockResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            if (pageSize > 100)
                return BadRequest(
                    BaseListResponse<MedicationStockResponse>.ErrorResult("Kích thước trang không được vượt quá 100."));

            var result = await _studentMedicationService.GetMyMedicationStockHistoryAsync(
                pageIndex, pageSize, studentId, fromDate, toDate, cancellationToken);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my medication stocks");
            return StatusCode(500, BaseListResponse<MedicationStockResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }

    /// <summary>
    /// Lấy lịch sử stock của một medication cụ thể (School Nurse view - có thể xem tất cả)
    /// </summary>
    [HttpGet("medications/{medicationId}")]
    [Authorize(Roles = "SCHOOLNURSE")]
    public async Task<ActionResult<BaseListResponse<MedicationStockResponse>>> GetMedicationStocksForNurse(
        Guid medicationId,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageIndex < 1 || pageSize < 1)
                return BadRequest(
                    BaseListResponse<MedicationStockResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

            var result = await _studentMedicationService.GetMedicationStockByIdForNurseAsync(
                medicationId, pageIndex, pageSize, cancellationToken);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting medication stocks for nurse: {MedicationId}", medicationId);
            return StatusCode(500, BaseListResponse<MedicationStockResponse>.ErrorResult("Lỗi hệ thống."));
        }
    }
}