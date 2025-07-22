using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthCheckRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthCheckResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.API.ApiControllers
{
    [Route("api/health-checks")]
    [ApiController]
    public class HealthCheckController : ControllerBase
    {
        private readonly IHealthCheckService _healthCheckService;

        public HealthCheckController(IHealthCheckService healthCheckService)
        {
            _healthCheckService = healthCheckService;
        }

        // Lấy danh sách buổi khám (phân trang, tìm kiếm, lọc theo y tá)
        [HttpGet]
        [Authorize(Roles = "SCHOOLNURSE, MANAGER")]
        public async Task<ActionResult<BaseListResponse<HealthCheckResponse>>> GetHealthChecks(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string searchTerm = "",
            [FromQuery] string orderBy = null,
            [FromQuery] Guid? nurseId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (pageIndex < 1 || pageSize < 1)
                    return BadRequest(BaseListResponse<HealthCheckResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

                var response = await _healthCheckService.GetHealthChecksAsync(pageIndex, pageSize, searchTerm, orderBy, nurseId, cancellationToken);
                if (!response.Success)
                    return NotFound(response);

                return Ok(response);
            }
            catch
            {
                return StatusCode(500, BaseListResponse<HealthCheckResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        // Lấy chi tiết một buổi khám
        [HttpGet("{healthCheckId}")]
        [Authorize(Roles = "SCHOOLNURSE, MANAGER, PARENT")]
        public async Task<ActionResult<BaseResponse<HealthCheckDetailResponse>>> GetHealthCheckDetail(
            Guid healthCheckId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _healthCheckService.GetHealthCheckDetailAsync(healthCheckId, cancellationToken);
                if (!response.Success)
                    return NotFound(response);

                return Ok(response);
            }
            catch
            {
                return StatusCode(500, BaseResponse<HealthCheckDetailResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        // Tạo buổi khám mới
        [HttpPost]
        [Authorize(Roles = "SCHOOLNURSE")]
        public async Task<ActionResult<BaseResponse<CreateWholeHealthCheckResponse>>> CreateHealthCheck(
            [FromBody] CreateWholeHealthCheckRequest model)
        {
            try
            {
                var result = await _healthCheckService.CreateWholeHealthCheckAsync(model);
                if (!result.Success)
                    return BadRequest(result);

                return CreatedAtAction(nameof(GetHealthCheckDetail), new { healthCheckId = result.Data.Id }, result);
            }
            catch
            {
                return StatusCode(500, BaseResponse<CreateWholeHealthCheckResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        // Cập nhật buổi khám
        [HttpPut("{healthCheckId}")]
        [Authorize(Roles = "SCHOOLNURSE")]
        public async Task<ActionResult<BaseResponse<HealthCheckResponse>>> UpdateHealthCheck(
            Guid healthCheckId,
            [FromBody] UpdateHealthCheckRequest model)
        {
            try
            {
                var result = await _healthCheckService.UpdateHealthCheckAsync(healthCheckId, model);
                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch
            {
                return StatusCode(500, BaseResponse<HealthCheckResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        // Xóa buổi khám (soft delete)
        [HttpDelete("{healthCheckId}")]
        [Authorize(Roles = "SCHOOLNURSE")]
        public async Task<ActionResult<BaseResponse<bool>>> DeleteHealthCheck(Guid healthCheckId)
        {
            try
            {
                var result = await _healthCheckService.DeleteHealthCheckAsync(healthCheckId);
                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch
            {
                return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
            }
        }

        // Duyệt buổi khám
        [HttpPut("{healthCheckId}/approve")]
        [Authorize(Roles = "MANAGER")]
        public async Task<ActionResult<BaseResponse<bool>>> ApproveHealthCheck(
            Guid healthCheckId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _healthCheckService.ApproveHealthCheckAsync(healthCheckId, cancellationToken);
                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch
            {
                return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
            }
        }

        // Từ chối buổi khám
        [HttpPut("{healthCheckId}/decline")]
        [Authorize(Roles = "MANAGER")]
        public async Task<ActionResult<BaseResponse<bool>>> DeclineHealthCheck(
            Guid healthCheckId,
            [FromQuery] string reason,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _healthCheckService.DeclineHealthCheckAsync(healthCheckId, reason, cancellationToken);
                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch
            {
                return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
            }
        }

        // Chốt danh sách buổi khám
        [HttpPut("{healthCheckId}/finalize")]
        [Authorize(Roles = "MANAGER")]
        public async Task<ActionResult<BaseResponse<bool>>> FinalizeHealthCheck(Guid healthCheckId)
        {
            try
            {
                var result = await _healthCheckService.FinalizeHealthCheckAsync(healthCheckId);
                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch
            {
                return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
            }
        }

        // Phụ huynh đồng ý/từ chối buổi khám
        [HttpPut("{healthCheckId}/parent-approval")]
        [Authorize(Roles = "PARENT")]
        public async Task<ActionResult<BaseResponse<bool>>> ParentApprove(
            Guid healthCheckId,
            Guid studentId,
            [FromBody] ParentApproveHealthCheckRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _healthCheckService.ParentApproveAsync(healthCheckId, studentId, request, cancellationToken);
                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch
            {
                return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
            }
        }

        // Phân công y tá cho buổi khám
        [HttpPost("assign-nurse")]
        [Authorize(Roles = "MANAGER")]
        public async Task<ActionResult<BaseResponse<bool>>> AssignNurseToHealthCheck(
            [FromBody] AssignNurseToHealthCheckRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _healthCheckService.AssignNurseToHealthCheckAsync(request, cancellationToken);
                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch
            {
                return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpPut("{healthCheckId}/reassign-nurse")]
        public async Task<ActionResult<BaseResponse<bool>>> ReassignNurseToHealthCheck(Guid healthCheckId, [FromBody] ReAssignNurseToHealthCheckRequest request)
        {
            var response = await _healthCheckService.ReassignNurseToHealthCheckAsync(healthCheckId, request);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        // Hoàn tất buổi khám
        [HttpPut("{healthCheckId}/complete")]
        [Authorize(Roles = "MANAGER")]
        public async Task<ActionResult<BaseResponse<bool>>> CompleteHealthCheck(
            Guid healthCheckId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _healthCheckService.CompleteHealthCheckAsync(healthCheckId, cancellationToken);
                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch
            {
                return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
            }
        }
    }
}