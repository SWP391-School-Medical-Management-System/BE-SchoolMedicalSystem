using AutoMapper;
using FluentValidation;
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
        private readonly IValidator<CreateVaccinationTypeRequest> _createVaccinationTypeValidator;
        private readonly IValidator<UpdateVaccinationTypeRequest> _updateVaccinationTypeValidator;

        private const string VACCINE_TYPE_CACHE_PREFIX = "vaccine_type";
        private const string VACCINE_TYPE_LIST_PREFIX = "vaccine_types_list";
        private const string VACCINE_TYPE_CACHE_SET = "vaccine_type_cache_keys";

        public VaccinationService(
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ILogger<VaccinationService> logger,
            IValidator<CreateVaccinationTypeRequest> createVaccinationTypeValidator,
            IValidator<UpdateVaccinationTypeRequest> updateVaccinationTypeValidator)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _logger = logger;
            _createVaccinationTypeValidator = createVaccinationTypeValidator;
            _updateVaccinationTypeValidator = updateVaccinationTypeValidator;
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
                    _logger.LogDebug("Vaccination types list found in cache for key: {CacheKey}", cacheKey);
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

                _logger.LogDebug("Cached vaccination types list with key: {CacheKey}", cacheKey);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vaccination types");
                return BaseListResponse<VaccinationTypeResponse>.ErrorResult("Lỗi lấy danh sách loại vaccine.");
            }
        }

        public async Task<BaseResponse<VaccinationTypeResponse>> GetVaccineTypeDetailAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = _cacheService.GenerateCacheKey(
                    VACCINE_TYPE_CACHE_PREFIX,
                    id.ToString()
                );

                var cachedResult = await _cacheService.GetAsync<BaseResponse<VaccinationTypeResponse>>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogDebug("Vaccine type detail found in cache for id: {Id}, cacheKey: {CacheKey}", id, cacheKey);
                    return cachedResult;
                }

                var repo = _unitOfWork.GetRepositoryByEntity<VaccinationType>();
                var vaccinationType = await repo.GetQueryable()
                    .FirstOrDefaultAsync(vt => vt.Id == id && !vt.IsDeleted, cancellationToken);

                if (vaccinationType == null)
                {
                    return new BaseResponse<VaccinationTypeResponse>
                    {
                        Success = false,
                        Message = "Không tìm thấy loại vaccine."
                    };
                }

                var response = MapToVaccinationTypeResponse(vaccinationType);

                var result = new BaseResponse<VaccinationTypeResponse>
                {
                    Success = true,
                    Data = response,
                    Message = "Lấy chi tiết loại vaccine thành công."
                };

                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
                await _cacheService.AddToTrackingSetAsync(cacheKey, VACCINE_TYPE_CACHE_SET);

                _logger.LogDebug("Cached vaccine type detail with key: {CacheKey}", cacheKey);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vaccine type detail for id: {Id}", id);
                return BaseResponse<VaccinationTypeResponse>.ErrorResult("Lỗi lấy chi tiết loại vaccine.");
            }
        }

        public async Task<BaseResponse<VaccinationTypeResponse>> CreateVaccinationTypeAsync(CreateVaccinationTypeRequest model)
        {
            try
            {
                var validationResult = await _createVaccinationTypeValidator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return new BaseResponse<VaccinationTypeResponse>
                    {
                        Success = false,
                        Message = errors
                    };
                }

                var vaccinationType = _mapper.Map<VaccinationType>(model);
                vaccinationType.Id = Guid.NewGuid();
                vaccinationType.CreatedBy = "SCHOOLNURSE";
                vaccinationType.CreatedDate = DateTime.UtcNow;
                vaccinationType.IsDeleted = false;

                var repo = _unitOfWork.GetRepositoryByEntity<VaccinationType>();
                await repo.AddAsync(vaccinationType);
                await _unitOfWork.SaveChangesAsync();

                // Xóa cache cụ thể cho danh sách vaccine types và vaccine type detail
                var vaccineTypeCacheKey = _cacheService.GenerateCacheKey(VACCINE_TYPE_CACHE_PREFIX, vaccinationType.Id.ToString());
                await _cacheService.RemoveAsync(vaccineTypeCacheKey);
                _logger.LogDebug("Đã xóa cache cụ thể cho vaccine type detail: {CacheKey}", vaccineTypeCacheKey);
                await _cacheService.RemoveByPrefixAsync(VACCINE_TYPE_LIST_PREFIX);
                _logger.LogDebug("Đã xóa cache danh sách vaccine types với prefix: {Prefix}", VACCINE_TYPE_LIST_PREFIX);

                await InvalidateAllCachesAsync();

                vaccinationType = await repo.GetQueryable()
                    .FirstOrDefaultAsync(vt => vt.Id == vaccinationType.Id);

                var response = MapToVaccinationTypeResponse(vaccinationType);

                _logger.LogInformation("Tạo loại vaccine thành công với ID: {VaccineTypeId}", vaccinationType.Id);
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
                var validationResult = await _updateVaccinationTypeValidator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return new BaseResponse<VaccinationTypeResponse>
                    {
                        Success = false,
                        Message = errors
                    };
                }

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
                vaccinationType.LastUpdatedDate = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();

                // Xóa cache cụ thể cho vaccine type detail và danh sách vaccine types
                var vaccineTypeCacheKey = _cacheService.GenerateCacheKey(VACCINE_TYPE_CACHE_PREFIX, id.ToString());
                await _cacheService.RemoveAsync(vaccineTypeCacheKey);
                _logger.LogDebug("Đã xóa cache cụ thể cho vaccine type detail: {CacheKey}", vaccineTypeCacheKey);
                await _cacheService.RemoveByPrefixAsync(VACCINE_TYPE_LIST_PREFIX);
                _logger.LogDebug("Đã xóa cache danh sách vaccine types với prefix: {Prefix}", VACCINE_TYPE_LIST_PREFIX);

                await InvalidateAllCachesAsync();

                var response = MapToVaccinationTypeResponse(vaccinationType);

                _logger.LogInformation("Cập nhật loại vaccine thành công với ID: {VaccineTypeId}", id);
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
                vaccinationType.LastUpdatedDate = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();

                // Xóa cache cụ thể cho vaccine type detail và danh sách vaccine types
                var vaccineTypeCacheKey = _cacheService.GenerateCacheKey(VACCINE_TYPE_CACHE_PREFIX, id.ToString());
                await _cacheService.RemoveAsync(vaccineTypeCacheKey);
                _logger.LogDebug("Đã xóa cache cụ thể cho vaccine type detail: {CacheKey}", vaccineTypeCacheKey);
                await _cacheService.RemoveByPrefixAsync(VACCINE_TYPE_LIST_PREFIX);
                _logger.LogDebug("Đã xóa cache danh sách vaccine types với prefix: {Prefix}", VACCINE_TYPE_LIST_PREFIX);

                await InvalidateAllCachesAsync();

                _logger.LogInformation("Xóa loại vaccine thành công với ID: {VaccineTypeId}", id);
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
                DoseCount = vaccinationType.DoseCount,
                ExpiredDate = vaccinationType.ExpiredDate,
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
                _logger.LogDebug("Starting comprehensive cache invalidation for vaccine types and related entities");
                // Xóa toàn bộ tracking set của VaccinationType
                await _cacheService.InvalidateTrackingSetAsync(VACCINE_TYPE_CACHE_SET);
                // Xóa các tiền tố cụ thể của VaccinationType và VaccinationSessionService
                await Task.WhenAll(
                    _cacheService.RemoveByPrefixAsync(VACCINE_TYPE_CACHE_PREFIX),
                    _cacheService.RemoveByPrefixAsync(VACCINE_TYPE_LIST_PREFIX),
                    _cacheService.RemoveByPrefixAsync("vaccination_session"),
                    _cacheService.RemoveByPrefixAsync("vaccination_sessions_list"),
                    _cacheService.RemoveByPrefixAsync("student_sessions"),
                    _cacheService.RemoveByPrefixAsync("parent_consent_status"),
                    _cacheService.RemoveByPrefixAsync("student_vaccination_result")
                );
                _logger.LogDebug("Completed comprehensive cache invalidation for vaccine types and related entities");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in comprehensive cache invalidation for vaccine types");
            }
        }

        #endregion
    }
}