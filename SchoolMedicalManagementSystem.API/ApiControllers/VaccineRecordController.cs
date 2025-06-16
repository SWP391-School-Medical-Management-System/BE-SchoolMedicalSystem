using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineRecordRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalRecordResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

namespace SchoolMedicalManagementSystem.API.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VaccineRecordController : ControllerBase
    {
        private readonly IVaccinationRecordService _vaccinationRecordService;

        public VaccineRecordController(IVaccinationRecordService vaccinationRecordService)
        {
            _vaccinationRecordService = vaccinationRecordService;
        }

        [HttpGet("{medicalRecordId}")]
        //[Authorize(Roles = "SCHOOLNURSE")]
        public async Task<ActionResult<BaseListResponse<VaccinationRecordResponse>>> GetVaccinationRecords(
            Guid medicalRecordId,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string searchTerm = "",
            [FromQuery] string orderBy = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (pageIndex < 1 || pageSize < 1)
                    return BadRequest(BaseListResponse<VaccinationRecordResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

                var response = await _vaccinationRecordService.GetVaccinationRecordsAsync(medicalRecordId, pageIndex, pageSize, searchTerm, orderBy, cancellationToken);

                if (!response.Success)
                    return NotFound(response);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseListResponse<VaccinationRecordResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpPost("{medicalRecordId}")]
        //[Authorize(Roles = "SCHOOLNURSE")]
        public async Task<ActionResult<BaseResponse<VaccinationRecordResponse>>> CreateVaccinationRecord(
            Guid medicalRecordId,
            [FromBody] CreateVaccinationRecordRequest model)
        {
            try
            {
                var result = await _vaccinationRecordService.CreateVaccinationRecordAsync(medicalRecordId, model);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return CreatedAtAction(nameof(GetVaccinationRecords), new { medicalRecordId = medicalRecordId }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<VaccinationRecordResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpPut("{recordId}")]
        //[Authorize(Roles = "SCHOOLNURSE")]
        public async Task<ActionResult<BaseResponse<VaccinationRecordResponse>>> UpdateVaccinationRecord(
            Guid recordId,
            [FromBody] UpdateVaccinationRecordRequest model)
        {
            try
            {
                var result = await _vaccinationRecordService.UpdateVaccinationRecordAsync(recordId, model);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<VaccinationRecordResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpDelete("{recordId}")]
        [Authorize(Roles = "SCHOOLNURSE")]
        public async Task<ActionResult<BaseResponse<bool>>> DeleteVaccinationRecord(Guid recordId)
        {
            try
            {
                var result = await _vaccinationRecordService.DeleteVaccinationRecordAsync(recordId);

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
