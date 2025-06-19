using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineSessionRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccinationSessionResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.API.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VaccinationSessionController : ControllerBase
    {
        private readonly IVaccinationSessionService _vaccinationSessionService;

        public VaccinationSessionController(IVaccinationSessionService vaccinationSessionService)
        {
            _vaccinationSessionService = vaccinationSessionService;
        }

        [HttpGet]
        [Authorize(Roles = "SCHOOLNURSE, MANAGER")]
        public async Task<ActionResult<BaseListResponse<VaccinationSessionResponse>>> GetVaccinationSessions(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string searchTerm = "",
            [FromQuery] string orderBy = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (pageIndex < 1 || pageSize < 1)
                    return BadRequest(BaseListResponse<VaccinationSessionResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

                var response = await _vaccinationSessionService.GetVaccinationSessionsAsync(pageIndex, pageSize, searchTerm, orderBy, cancellationToken);

                if (!response.Success)
                    return NotFound(response);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseListResponse<VaccinationSessionResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpPost]
        [Authorize(Roles = "SCHOOLNURSE")]
        public async Task<ActionResult<BaseResponse<VaccinationSessionResponse>>> CreateVaccinationSession(
            [FromBody] CreateVaccinationSessionRequest model)
        {
            try
            {
                var result = await _vaccinationSessionService.CreateVaccinationSessionAsync(model);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return CreatedAtAction(nameof(GetVaccinationSessions), new { }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<VaccinationSessionResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpPut("{sessionId}")]
        [Authorize(Roles = "SCHOOLNURSE")]
        public async Task<ActionResult<BaseResponse<VaccinationSessionResponse>>> UpdateVaccinationSession(
            Guid sessionId,
            [FromBody] UpdateVaccinationSessionRequest model)
        {
            try
            {
                var result = await _vaccinationSessionService.UpdateVaccinationSessionAsync(sessionId, model);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<VaccinationSessionResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpDelete("{sessionId}")]
        [Authorize(Roles = "SCHOOLNURSE")]
        public async Task<ActionResult<BaseResponse<bool>>> DeleteVaccinationSession(Guid sessionId)
        {
            try
            {
                var result = await _vaccinationSessionService.DeleteVaccinationSessionAsync(sessionId);

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

        [HttpPut("{sessionId}/approve")]
        [Authorize(Roles = "MANAGER")]
        public async Task<ActionResult<BaseResponse<bool>>> ApproveSession(Guid sessionId)
        {
            try
            {
                var result = await _vaccinationSessionService.ApproveSessionAsync(sessionId);

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

        [HttpPut("{sessionId}/finalize")]
        [Authorize(Roles = "MANAGER")]
        public async Task<ActionResult<BaseResponse<bool>>> FinalizeSession(Guid sessionId)
        {
            try
            {
                var result = await _vaccinationSessionService.FinalizeSessionAsync(sessionId);

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
}