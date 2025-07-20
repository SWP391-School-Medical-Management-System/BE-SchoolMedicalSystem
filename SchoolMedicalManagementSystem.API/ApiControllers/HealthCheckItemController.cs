using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthCheckItemRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthCheckItemResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.API.ApiControllers
{
    [ApiController]
    [Route("api/health-check-items")]
    public class HealthCheckItemController : ControllerBase
    {
        private readonly IHealthCheckItemService _healthCheckItemService;

        public HealthCheckItemController(IHealthCheckItemService healthCheckItemService)
        {
            _healthCheckItemService = healthCheckItemService;
        }

        [HttpGet]
        public async Task<ActionResult<BaseListResponse<HealthCheckItemResponse>>> GetHealthCheckItems(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string searchTerm = "",
            [FromQuery] string orderBy = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (pageIndex < 1 || pageSize < 1)
                    return BadRequest(BaseListResponse<HealthCheckItemResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

                var response = await _healthCheckItemService.GetHealthCheckItemsAsync(pageIndex, pageSize, searchTerm, orderBy, cancellationToken);

                if (!response.Success)
                    return NotFound(response);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseListResponse<HealthCheckItemResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "SCHOOLNURSE, PARENT")]
        public async Task<ActionResult<BaseResponse<HealthCheckItemResponse>>> GetHealthCheckItemDetail(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _healthCheckItemService.GetHealthCheckItemDetailAsync(id, cancellationToken);

                if (!result.Success)
                    return NotFound(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<HealthCheckItemResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpPost]
        [Authorize(Roles = "SCHOOLNURSE")]
        public async Task<ActionResult<BaseResponse<HealthCheckItemResponse>>> CreateHealthCheckItem(
            [FromBody] CreateHealthCheckItemRequest model)
        {
            try
            {
                var result = await _healthCheckItemService.CreateHealthCheckItemAsync(model);

                if (!result.Success)
                    return BadRequest(result);

                return CreatedAtAction(nameof(GetHealthCheckItems), new { }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<HealthCheckItemResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "SCHOOLNURSE")]
        public async Task<ActionResult<BaseResponse<HealthCheckItemResponse>>> UpdateHealthCheckItem(
            Guid id,
            [FromBody] UpdateHealthCheckItemRequest model)
        {
            try
            {
                var result = await _healthCheckItemService.UpdateHealthCheckItemAsync(id, model);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<HealthCheckItemResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "SCHOOLNURSE")]
        public async Task<ActionResult<BaseResponse<bool>>> DeleteHealthCheckItem(Guid id)
        {
            try
            {
                var result = await _healthCheckItemService.DeleteHealthCheckItemAsync(id);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
            }
        }
    }
}