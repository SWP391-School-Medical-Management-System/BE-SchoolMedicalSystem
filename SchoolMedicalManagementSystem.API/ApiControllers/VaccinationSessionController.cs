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

        #region CRUD Vaccination Session

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

        [HttpGet("{sessionId}/class/{classId}/student-status")]
        [Authorize(Roles = "SCHOOLNURSE, MANAGER, PARENT")]
        public async Task<ActionResult<BaseListResponse<ClassStudentConsentStatusResponse>>> GetClassStudentConsentStatus(
            Guid sessionId,
            Guid classId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _vaccinationSessionService.GetClassStudentConsentStatusAsync(sessionId, classId, cancellationToken);

                if (!response.Success)
                {
                    return NotFound(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseListResponse<ClassStudentConsentStatusResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }   

        [HttpGet("student/{studentId}/sessions")]
        [Authorize(Roles = "PARENT, SCHOOLNURSE, MANAGER")]
        public async Task<ActionResult<BaseListResponse<VaccinationSessionResponse>>> GetSessionsByStudentId(
            Guid studentId,
            CancellationToken cancellationToken = default)  
            {
                try
                {
                    var response = await _vaccinationSessionService.GetSessionsByStudentIdAsync(studentId, cancellationToken);

                    if (!response.Success)
                    {
                        return NotFound(response);
                    }

                    return Ok(response);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, BaseListResponse<VaccinationSessionResponse>.ErrorResult("Lỗi hệ thống."));
                }
            }

        [HttpGet("{sessionId}/detail")]
        [Authorize(Roles = "SCHOOLNURSE, MANAGER, PARENT")]
        public async Task<ActionResult<BaseResponse<VaccinationSessionDetailResponse>>> GetSessionDetail(
            Guid sessionId,
            CancellationToken cancellationToken = default)
            {
                try
                {
                    var response = await _vaccinationSessionService.GetSessionDetailAsync(sessionId, cancellationToken);

                    if (!response.Success)
                    {
                        return NotFound(response);
                    }

                    return Ok(response);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, BaseResponse<VaccinationSessionDetailResponse>.ErrorResult("Lỗi hệ thống."));
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

        [HttpPost("create-whole")]
        [Authorize(Roles = "SCHOOLNURSE")]
        public async Task<ActionResult<BaseResponse<CreateWholeVaccinationSessionResponse>>> CreateWholeVaccinationSession(
            [FromBody] CreateWholeVaccinationSessionRequest model)
            {
                try
                {
                    var result = await _vaccinationSessionService.CreateWholeVaccinationSessionAsync(model);

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

        #endregion

        #region Process Vaccination Session

        [HttpPut("{id}/approve")]
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

        [HttpPut("{sessionId}/decline")]
        [Authorize(Roles = "MANAGER")]
        public async Task<ActionResult<BaseResponse<bool>>> DeclineSession(
            Guid sessionId,
            CancellationToken cancellationToken = default)
            {
                try
                {
                    var result = await _vaccinationSessionService.DeclineSessionAsync(sessionId, cancellationToken);

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

        [HttpPut("session/{sessionId}/approve")]
        [Authorize(Roles = "PARENT")]
        public async Task<ActionResult<BaseResponse<bool>>> ParentApprove(
            Guid sessionId,
            [FromBody] ParentApproveRequest request)
        {
            try
            {
                var result = await _vaccinationSessionService.ParentApproveAsync(sessionId, request);

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

        [HttpPost("{sessionId}/mark-vaccinated/{studentId}")]
        [Authorize(Roles = "SCHOOLNURSE")]
        public async Task<ActionResult<BaseResponse<bool>>> MarkStudentVaccinated(
            Guid sessionId,
            Guid studentId,
            CancellationToken cancellationToken = default)
            {
                try
                {
                    var result = await _vaccinationSessionService.MarkStudentVaccinatedAsync(sessionId, studentId, cancellationToken);

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

        [HttpPost("assign-nurse")]
        [Authorize(Roles = "MANAGER")]
        public async Task<ActionResult<BaseResponse<bool>>> AssignNurseToSession(
        [FromBody] AssignNurseToSessionRequest request)
        {
            try
            {
                var result = await _vaccinationSessionService.AssignNurseToSessionAsync(request);

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

    }
}