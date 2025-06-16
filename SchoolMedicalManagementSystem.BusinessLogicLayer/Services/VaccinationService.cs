// SchoolMedicalManagementSystem.BusinessLogicLayer.Services/VaccineService.cs
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccineResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services
{
    public class VaccinationService : IVaccinationService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly ILogger<VaccinationService> _logger;

        private const string VACCINE_TYPE_CACHE_PREFIX = "vaccine_type";
        private const string VACCINE_TYPE_LIST_PREFIX = "vaccine_types_list";
        private const string VACCINE_TYPE_CACHE_SET = "vaccine_type_cache_keys";

        public VaccinationService(
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ILogger<VaccinationService> logger)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<BaseListResponse<VaccinationTypeResponse>> GetVaccinationTypesAsync(
            int pageIndex,
            int pageSize,
            string searchTerm,
            string orderBy,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = _cacheService.GenerateCacheKey(
                    VACCINE_TYPE_LIST_PREFIX,
                    pageIndex.ToString(),
                    pageSize.ToString(),
                    searchTerm ?? "",
                    orderBy ?? ""
                );

                var cachedResult = await _cacheService.GetAsync<BaseListResponse<VaccinationTypeResponse>>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogDebug("Vaccination types list found in cache");
                    return cachedResult;
                }

                var query = _unitOfWork.GetRepositoryByEntity<VaccinationType>().GetQueryable()
                    .Where(vt => !vt.IsDeleted)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(vt => vt.Name.ToLower().Contains(searchTerm));
                }

                query = ApplyVaccinationTypeOrdering(query, orderBy);

                var totalCount = await query.CountAsync(cancellationToken);
                var vaccinationTypes = await query
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                var responses = vaccinationTypes.Select(MapToVaccinationTypeResponse).ToList();

                var result = BaseListResponse<VaccinationTypeResponse>.SuccessResult(
                    responses,
                    totalCount,
                    pageSize,
                    pageIndex,
                    "Lấy danh sách loại vaccine thành công.");

                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
                await _cacheService.AddToTrackingSetAsync(cacheKey, VACCINE_TYPE_CACHE_SET);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vaccination types");
                return BaseListResponse<VaccinationTypeResponse>.ErrorResult("Lỗi lấy danh sách loại vaccine.");
            }
        }
        public async Task<BaseResponse<VaccinationTypeResponse>> CreateVaccinationTypeAsync(CreateVaccinationTypeRequest model)
        {
            try
            {
                var vaccinationType = _mapper.Map<VaccinationType>(model);
                vaccinationType.Id = Guid.NewGuid();
                vaccinationType.CreatedBy = "SCHOOLNURSE";
                vaccinationType.CreatedDate = DateTime.Now;

                var repo = _unitOfWork.GetRepositoryByEntity<VaccinationType>();
                await repo.AddAsync(vaccinationType);
                await _unitOfWork.SaveChangesAsync();

                await InvalidateAllCachesAsync();

                vaccinationType = await repo.GetQueryable()
                    .FirstOrDefaultAsync(vt => vt.Id == vaccinationType.Id);

                var response = MapToVaccinationTypeResponse(vaccinationType);

                return new BaseResponse<VaccinationTypeResponse>
                {
                    Success = true,
                    Data = response,
                    Message = "Tạo loại vaccine thành công."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vaccination type");
                return new BaseResponse<VaccinationTypeResponse>
                {
                    Success = false,
                    Message = $"Lỗi tạo loại vaccine: {ex.Message}"
                };
            }
        }


        public async Task<BaseResponse<VaccinationTypeResponse>> UpdateVaccinationTypeAsync(Guid id, UpdateVaccinationTypeRequest model)
        {
            try
            {
                var repo = _unitOfWork.GetRepositoryByEntity<VaccinationType>();
                var vaccinationType = await repo.GetQueryable()
                    .FirstOrDefaultAsync(vt => vt.Id == id && !vt.IsDeleted);

                if (vaccinationType == null)
                {
                    return new BaseResponse<VaccinationTypeResponse>
                    {
                        Success = false,
                        Message = "Không tìm thấy loại vaccine."
                    };
                }

                _mapper.Map(model, vaccinationType);
                vaccinationType.LastUpdatedBy = "SCHOOLNURSE";
                vaccinationType.LastUpdatedDate = DateTime.Now;

                await _unitOfWork.SaveChangesAsync();
                await InvalidateAllCachesAsync();

                var response = MapToVaccinationTypeResponse(vaccinationType);

                return new BaseResponse<VaccinationTypeResponse>
                {
                    Success = true,
                    Data = response,
                    Message = "Cập nhật loại vaccine thành công."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vaccination type: {Id}", id);
                return new BaseResponse<VaccinationTypeResponse>
                {
                    Success = false,
                    Message = $"Lỗi cập nhật loại vaccine: {ex.Message}"
                };
            }
        }

        public async Task<BaseResponse<bool>> DeleteVaccinationTypeAsync(Guid id)
        {
            try
            {
                var repo = _unitOfWork.GetRepositoryByEntity<VaccinationType>();
                var vaccinationType = await repo.GetQueryable()
                    .FirstOrDefaultAsync(vt => vt.Id == id && !vt.IsDeleted);

                if (vaccinationType == null)
                {
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "Không tìm thấy loại vaccine."
                    };
                }

                vaccinationType.IsDeleted = true;
                vaccinationType.LastUpdatedBy = "SCHOOLNURSE";
                vaccinationType.LastUpdatedDate = DateTime.Now;

                await _unitOfWork.SaveChangesAsync();
                await InvalidateAllCachesAsync();

                return new BaseResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Xóa loại vaccine thành công."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vaccination type: {Id}", id);
                return new BaseResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Lỗi xóa loại vaccine: {ex.Message}"
                };
            }
        }

        #region Helper Methods

        private VaccinationTypeResponse MapToVaccinationTypeResponse(VaccinationType vaccinationType)
        {
            return new VaccinationTypeResponse
            {
                Id = vaccinationType.Id,
                Name = vaccinationType.Name,
                Description = vaccinationType.Description,
                RecommendedAge = vaccinationType.RecommendedAge,
                DoseCount = vaccinationType.DoseCount
            };
        }

        private IQueryable<VaccinationType> ApplyVaccinationTypeOrdering(IQueryable<VaccinationType> query, string orderBy)
        {
            return orderBy?.ToLower() switch
            {
                "name" => query.OrderBy(vt => vt.Name),
                "name_desc" => query.OrderByDescending(vt => vt.Name),
                "recommendedage" => query.OrderBy(vt => vt.RecommendedAge),
                "recommendedage_desc" => query.OrderByDescending(vt => vt.RecommendedAge),
                "dosecount" => query.OrderBy(vt => vt.DoseCount),
                "dosecount_desc" => query.OrderByDescending(vt => vt.DoseCount),
                _ => query.OrderBy(vt => vt.Name)
            };
        }

        private async Task InvalidateAllCachesAsync()
        {
            try
            {
                _logger.LogDebug("Starting comprehensive cache invalidation for vaccine types");
                await Task.WhenAll(
                    _cacheService.RemoveByPrefixAsync(VACCINE_TYPE_CACHE_PREFIX),
                    _cacheService.RemoveByPrefixAsync(VACCINE_TYPE_LIST_PREFIX)
                );
                await Task.Delay(100);
                _logger.LogDebug("Completed comprehensive cache invalidation for vaccine types");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in comprehensive cache invalidation for vaccine types");
            }
        }

        #endregion
    }
}