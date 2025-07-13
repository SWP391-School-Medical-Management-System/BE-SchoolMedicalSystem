// SchoolMedicalManagementSystem.API.ApiControllers/VaccineTypeController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccineResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.API.ApiControllers
{
    [ApiController]
    [Route("api/vaccine-types")]
    public class VaccineTypeController : ControllerBase
    {
        private readonly IVaccinationService _vaccinationService;

        public VaccineTypeController(IVaccinationService vaccinationService)
        {
            _vaccinationService = vaccinationService;
        }

        [HttpGet]
        public async Task<ActionResult<BaseListResponse<VaccinationTypeResponse>>> GetVaccinationTypes(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string searchTerm = "",
            [FromQuery] string orderBy = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (pageIndex < 1 || pageSize < 1)
                    return BadRequest(BaseListResponse<VaccinationTypeResponse>.ErrorResult("Thông tin phân trang không hợp lệ."));

                var response = await _vaccinationService.GetVaccinationTypesAsync(pageIndex, pageSize, searchTerm, orderBy, cancellationToken);

                if (!response.Success)
                    return NotFound(response);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseListResponse<VaccinationTypeResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "SCHOOLNURSE, PARENT")]
        public async Task<ActionResult<BaseResponse<VaccinationTypeResponse>>> GetVaccineTypeDetail(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _vaccinationService.GetVaccineTypeDetailAsync(id, cancellationToken);

                if (!result.Success)
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<VaccinationTypeResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpPost]
        [Authorize(Roles = "SCHOOLNURSE")]
        public async Task<ActionResult<BaseResponse<VaccinationTypeResponse>>> CreateVaccinationType(
            [FromBody] CreateVaccinationTypeRequest model)
        {
            try
            {
                var result = await _vaccinationService.CreateVaccinationTypeAsync(model);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return CreatedAtAction(nameof(GetVaccinationTypes), new { }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<VaccinationTypeResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "SCHOOLNURSE")]
        public async Task<ActionResult<BaseResponse<VaccinationTypeResponse>>> UpdateVaccinationType(
            Guid id,
            [FromBody] UpdateVaccinationTypeRequest model)
        {
            try
            {
                var result = await _vaccinationService.UpdateVaccinationTypeAsync(id, model);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<VaccinationTypeResponse>.ErrorResult("Lỗi hệ thống."));
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "SCHOOLNURSE")]
        public async Task<ActionResult<BaseResponse<bool>>> DeleteVaccinationType(Guid id)
        {
            try
            {
                var result = await _vaccinationService.DeleteVaccinationTypeAsync(id);

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