using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineSessionRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccinationSessionResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccineRecordResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.API.ApiControllers
{
    [Route("api/vaccination-sessions")]
    [ApiController]
    public class VaccinationSessionController : ControllerBase
    {
        private readonly IVaccinationSessionService _vaccinationSessionService;

        public VaccinationSessionController(IVaccinationSessionService vaccinationSessionService)
        {
            _vaccinationSessionService = vaccinationSessionService;
        }

        #region CRUD

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
            catch
            {
                return StatusCode(500, BaseListResponse<VaccinationSessionResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpGet("{sessionId}")]
        [Authorize(Roles = "SCHOOLNURSE, MANAGER, PARENT")]
        public async Task<ActionResult<BaseResponse<VaccinationSessionDetailResponse>>> GetSessionDetail(Guid sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _vaccinationSessionService.GetSessionDetailAsync(sessionId, cancellationToken);
                if (!response.Success)
                    return NotFound(response);

                return Ok(response);
            }
            catch
            {
                return StatusCode(500, BaseResponse<VaccinationSessionDetailResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpPost]
        [Authorize(Roles = "SCHOOLNURSE")]
        public async Task<ActionResult<BaseResponse<VaccinationSessionResponse>>> CreateVaccinationSession([FromBody] CreateVaccinationSessionRequest model)
        {
            try
            {
                var result = await _vaccinationSessionService.CreateVaccinationSessionAsync(model);
                if (!result.Success)
                    return BadRequest(result);

                return CreatedAtAction(nameof(GetVaccinationSessions), new { }, result);
            }
            catch
            {
                return StatusCode(500, BaseResponse<VaccinationSessionResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpPost("whole")]
        [Authorize(Roles = "SCHOOLNURSE")]
        public async Task<ActionResult<BaseResponse<CreateWholeVaccinationSessionResponse>>> CreateWholeVaccinationSession([FromBody] CreateWholeVaccinationSessionRequest model)
        {
            try
            {
                var result = await _vaccinationSessionService.CreateWholeVaccinationSessionAsync(model);
                if (!result.Success)
                    return BadRequest(result);

                return CreatedAtAction(nameof(GetVaccinationSessions), new { }, result);
            }
            catch
            {
                return StatusCode(500, BaseResponse<CreateWholeVaccinationSessionResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpPut("{sessionId}")]
        [Authorize(Roles = "SCHOOLNURSE")]
        public async Task<ActionResult<BaseResponse<VaccinationSessionResponse>>> UpdateVaccinationSession(Guid sessionId, [FromBody] UpdateVaccinationSessionRequest model)
        {
            try
            {
                var result = await _vaccinationSessionService.UpdateVaccinationSessionAsync(sessionId, model);
                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch
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
                    return BadRequest(result);

                return Ok(result);
            }
            catch
            {
                return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
            }
        }

        #endregion

        #region Process

        [HttpPut("{sessionId}/approve")]
        [Authorize(Roles = "MANAGER")]
        public async Task<ActionResult<BaseResponse<bool>>> ApproveSession(Guid sessionId)
        {
            try
            {
                var result = await _vaccinationSessionService.ApproveSessionAsync(sessionId);
                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch
            {
                return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpPut("{sessionId}/decline")]
        [Authorize(Roles = "MANAGER")]
        public async Task<ActionResult<BaseResponse<bool>>> DeclineSession(Guid sessionId, [FromQuery] string reason, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _vaccinationSessionService.DeclineSessionAsync(sessionId, reason, cancellationToken);
                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch
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
                    return BadRequest(result);

                return Ok(result);
            }
            catch
            {
                return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpPut("{sessionId}/parent-approval")]
        [Authorize(Roles = "PARENT")]
        public async Task<ActionResult<BaseResponse<bool>>> ParentApprove(
            Guid sessionId,
            Guid studentId,
            [FromBody] ParentApproveRequest request)
        {
            try
            {
                var result = await _vaccinationSessionService.ParentApproveAsync(sessionId, studentId, request);
                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch
            {
                return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpPost("{sessionId}/mark-student-vaccinated")]
        [Authorize(Roles = "SCHOOLNURSE")]
        public async Task<ActionResult<BaseResponse<bool>>> MarkStudentVaccinated(Guid sessionId, [FromBody] MarkStudentVaccinatedRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _vaccinationSessionService.MarkStudentVaccinatedAsync(sessionId, request, cancellationToken);
                if (!response.Success)
                    return BadRequest(response);

                return Ok(response);
            }
            catch
            {
                return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpPost("assign-nurse")]
        [Authorize(Roles = "MANAGER")]
        public async Task<ActionResult<BaseResponse<bool>>> AssignNurseToSession([FromBody] AssignNurseToSessionRequest request)
        {
            try
            {
                var result = await _vaccinationSessionService.AssignNurseToSessionAsync(request);
                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch
            {
                return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpPut("{sessionId}/complete")]
        [Authorize(Roles = "MANAGER")]
        public async Task<ActionResult<BaseResponse<bool>>> CompleteSession(Guid sessionId)
        {
            try
            {
                var result = await _vaccinationSessionService.CompleteSessionAsync(sessionId);
                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch
            {
                return StatusCode(500, BaseResponse<bool>.ErrorResult("Lỗi hệ thống."));
            }
        }

        #endregion

        #region Student and Consent

        [HttpGet("{sessionId}/class/{classId}/student-consent-status")]
        [Authorize(Roles = "SCHOOLNURSE, MANAGER, PARENT")]
        public async Task<ActionResult<BaseListResponse<ClassStudentConsentStatusResponse>>> GetClassStudentConsentStatus(Guid sessionId, Guid classId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _vaccinationSessionService.GetClassStudentConsentStatusAsync(sessionId, classId, cancellationToken);
                if (!response.Success)
                    return NotFound(response);

                return Ok(response);
            }
            catch
            {
                return StatusCode(500, BaseListResponse<ClassStudentConsentStatusResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpGet("{sessionId}/all-class-student-consent-status")]
        [Authorize(Roles = "SCHOOLNURSE, MANAGER, PARENT")]
        public async Task<ActionResult<BaseListResponse<ClassStudentConsentStatusResponse>>> GetAllClassStudentConsentStatus(Guid sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _vaccinationSessionService.GetAllClassStudentConsentStatusAsync(sessionId, cancellationToken);
                if (!response.Success)
                    return NotFound(response);

                return Ok(response);
            }
            catch
            {
                return StatusCode(500, BaseListResponse<ClassStudentConsentStatusResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpGet("{sessionId}/parent-consent-status")]
        [Authorize(Roles = "SCHOOLNURSE, MANAGER, PARENT")]
        public async Task<ActionResult<BaseResponse<ParentConsentStatusResponse>>> GetParentConsentStatus(Guid sessionId, [FromQuery] Guid studentId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _vaccinationSessionService.GetParentConsentStatusAsync(sessionId, studentId, cancellationToken);
                if (!response.Success)
                    return BadRequest(response);

                return Ok(response);
            }
            catch
            {
                return StatusCode(500, BaseResponse<ParentConsentStatusResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpGet("{sessionId}/students/{studentId}/vaccination-result")]
        [Authorize(Roles = "SCHOOLNURSE, MANAGER, PARENT")]
        public async Task<ActionResult<BaseResponse<StudentVaccinationResultResponse>>> GetStudentVaccinationResult(Guid sessionId, Guid studentId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _vaccinationSessionService.GetStudentVaccinationResultAsync(sessionId, studentId, cancellationToken);
                if (!response.Success)
                    return BadRequest(response);

                return Ok(response);
            }
            catch
            {
                return StatusCode(500, BaseResponse<StudentVaccinationResultResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpGet("/api/students/{studentId}/vaccination-sessions")]
        [Authorize(Roles = "PARENT, SCHOOLNURSE, MANAGER")]
        public async Task<ActionResult<BaseListResponse<VaccinationSessionResponse>>> GetSessionsByStudentId(Guid studentId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _vaccinationSessionService.GetSessionsByStudentIdAsync(studentId, cancellationToken);
                if (!response.Success)
                    return NotFound(response);

                return Ok(response);
            }
            catch
            {
                return StatusCode(500, BaseListResponse<VaccinationSessionResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        #endregion
    }
}