using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthCheckItemRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthCheckItemResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services
{
    public class HealthCheckItemService : IHealthCheckItemService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly ILogger<HealthCheckItemService> _logger;
        private readonly IValidator<CreateHealthCheckItemRequest> _createHealthCheckItemValidator;
        private readonly IValidator<UpdateHealthCheckItemRequest> _updateHealthCheckItemValidator;

        private const string HEALTH_CHECK_ITEM_CACHE_PREFIX = "health_check_item";
        private const string HEALTH_CHECK_ITEM_LIST_PREFIX = "health_check_items_list";
        private const string HEALTH_CHECK_ITEM_CACHE_SET = "health_check_item_cache_keys";

        public HealthCheckItemService(
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ILogger<HealthCheckItemService> logger,
            IValidator<CreateHealthCheckItemRequest> createHealthCheckItemValidator,
            IValidator<UpdateHealthCheckItemRequest> updateHealthCheckItemValidator)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _logger = logger;
            _createHealthCheckItemValidator = createHealthCheckItemValidator;
            _updateHealthCheckItemValidator = updateHealthCheckItemValidator;
        }

        public async Task<BaseListResponse<HealthCheckItemResponse>> GetHealthCheckItemsAsync(
            int pageIndex,
            int pageSize,
            string searchTerm,
            string orderBy,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = _cacheService.GenerateCacheKey(
                    HEALTH_CHECK_ITEM_LIST_PREFIX,
                    pageIndex.ToString(),
                    pageSize.ToString(),
                    searchTerm ?? "",
                    orderBy ?? ""
                );

                var cachedResult = await _cacheService.GetAsync<BaseListResponse<HealthCheckItemResponse>>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogDebug("Danh sách hạng mục kiểm tra sức khỏe được tìm thấy trong cache với key: {CacheKey}", cacheKey);
                    return cachedResult;
                }

                var query = _unitOfWork.GetRepositoryByEntity<HealthCheckItem>().GetQueryable()
                    .Include(hci => hci.HealthCheckItemAssignments)
                    .ThenInclude(hcia => hcia.HealthCheck)
                    .Where(hci => !hci.IsDeleted)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(hci => hci.Name.ToLower().Contains(searchTerm));
                }

                query = ApplyHealthCheckItemOrdering(query, orderBy);

                var totalCount = await query.CountAsync(cancellationToken);
                var healthCheckItems = await query
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                var responses = healthCheckItems.Select(MapToHealthCheckItemResponse).ToList();

                var result = BaseListResponse<HealthCheckItemResponse>.SuccessResult(
                    responses,
                    totalCount,
                    pageSize,
                    pageIndex,
                    "Lấy danh sách hạng mục kiểm tra sức khỏe thành công.");

                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
                await _cacheService.AddToTrackingSetAsync(cacheKey, HEALTH_CHECK_ITEM_CACHE_SET);

                _logger.LogDebug("Đã lưu cache danh sách hạng mục kiểm tra sức khỏe với key: {CacheKey}", cacheKey);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách hạng mục kiểm tra sức khỏe");
                return BaseListResponse<HealthCheckItemResponse>.ErrorResult("Lỗi lấy danh sách hạng mục kiểm tra sức khỏe.");
            }
        }

        public async Task<BaseResponse<HealthCheckItemResponse>> GetHealthCheckItemDetailAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = _cacheService.GenerateCacheKey(
                    HEALTH_CHECK_ITEM_CACHE_PREFIX,
                    id.ToString()
                );

                var cachedResult = await _cacheService.GetAsync<BaseResponse<HealthCheckItemResponse>>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogDebug("Chi tiết hạng mục kiểm tra sức khỏe được tìm thấy trong cache với id: {Id}, cacheKey: {CacheKey}", id, cacheKey);
                    return cachedResult;
                }

                var repo = _unitOfWork.GetRepositoryByEntity<HealthCheckItem>();
                var healthCheckItem = await repo.GetQueryable()
                    .Include(hci => hci.HealthCheckItemAssignments)
                    .ThenInclude(hcia => hcia.HealthCheck)
                    .FirstOrDefaultAsync(hci => hci.Id == id && !hci.IsDeleted, cancellationToken);

                if (healthCheckItem == null)
                {
                    return new BaseResponse<HealthCheckItemResponse>
                    {
                        Success = false,
                        Message = "Không tìm thấy hạng mục kiểm tra sức khỏe."
                    };
                }

                var response = MapToHealthCheckItemResponse(healthCheckItem);

                var result = new BaseResponse<HealthCheckItemResponse>
                {
                    Success = true,
                    Data = response,
                    Message = "Lấy chi tiết hạng mục kiểm tra sức khỏe thành công."
                };

                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
                await _cacheService.AddToTrackingSetAsync(cacheKey, HEALTH_CHECK_ITEM_CACHE_SET);

                _logger.LogDebug("Đã lưu cache chi tiết hạng mục kiểm tra sức khỏe với key: {CacheKey}", cacheKey);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết hạng mục kiểm tra sức khỏe với id: {Id}", id);
                return BaseResponse<HealthCheckItemResponse>.ErrorResult("Lỗi lấy chi tiết hạng mục kiểm tra sức khỏe.");
            }
        }

        public async Task<BaseResponse<HealthCheckItemResponse>> CreateHealthCheckItemAsync(CreateHealthCheckItemRequest model)
        {
            try
            {
                var validationResult = await _createHealthCheckItemValidator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return new BaseResponse<HealthCheckItemResponse>
                    {
                        Success = false,
                        Message = errors
                    };
                }

                var healthCheckItem = _mapper.Map<HealthCheckItem>(model);
                healthCheckItem.Id = Guid.NewGuid();
                healthCheckItem.CreatedBy = "SCHOOLNURSE";
                healthCheckItem.CreatedDate = DateTime.UtcNow;
                healthCheckItem.IsDeleted = false;

                var repo = _unitOfWork.GetRepositoryByEntity<HealthCheckItem>();
                await repo.AddAsync(healthCheckItem);

                // Nếu HealthCheckId được cung cấp, tạo bản ghi trong HealthCheckItemAssignment
                if (model.HealthCheckId != null)
                {
                    var healthCheckRepo = _unitOfWork.GetRepositoryByEntity<HealthCheck>();
                    var healthCheck = await healthCheckRepo.GetQueryable()
                        .FirstOrDefaultAsync(hc => hc.Id == model.HealthCheckId && !hc.IsDeleted);

                    if (healthCheck == null)
                    {
                        return new BaseResponse<HealthCheckItemResponse>
                        {
                            Success = false,
                            Message = "Đợt kiểm tra sức khỏe không tồn tại hoặc đã bị xóa."
                        };
                    }

                    var assignment = new HealthCheckItemAssignment
                    {
                        Id = Guid.NewGuid(),
                        HealthCheckId = (Guid)model.HealthCheckId,
                        HealthCheckItemId = healthCheckItem.Id,
                        CreatedBy = "SCHOOLNURSE",
                        CreatedDate = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    var assignmentRepo = _unitOfWork.GetRepositoryByEntity<HealthCheckItemAssignment>();
                    await assignmentRepo.AddAsync(assignment);
                }

                await _unitOfWork.SaveChangesAsync();

                var cacheKey = _cacheService.GenerateCacheKey(HEALTH_CHECK_ITEM_CACHE_PREFIX, healthCheckItem.Id.ToString());
                await _cacheService.RemoveAsync(cacheKey);
                _logger.LogDebug("Đã xóa cache chi tiết hạng mục kiểm tra sức khỏe: {CacheKey}", cacheKey);
                await _cacheService.RemoveByPrefixAsync(HEALTH_CHECK_ITEM_LIST_PREFIX);
                _logger.LogDebug("Đã xóa cache danh sách hạng mục kiểm tra sức khỏe với prefix: {Prefix}", HEALTH_CHECK_ITEM_LIST_PREFIX);

                await InvalidateAllCachesAsync();

                healthCheckItem = await repo.GetQueryable()
                    .Include(hci => hci.HealthCheckItemAssignments)
                    .ThenInclude(hcia => hcia.HealthCheck)
                    .FirstOrDefaultAsync(hci => hci.Id == healthCheckItem.Id);

                var response = MapToHealthCheckItemResponse(healthCheckItem);

                _logger.LogInformation("Tạo hạng mục kiểm tra sức khỏe thành công với ID: {HealthCheckItemId}", healthCheckItem.Id);
                return new BaseResponse<HealthCheckItemResponse>
                {
                    Success = true,
                    Data = response,
                    Message = "Tạo hạng mục kiểm tra sức khỏe thành công."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo hạng mục kiểm tra sức khỏe");
                return new BaseResponse<HealthCheckItemResponse>
                {
                    Success = false,
                    Message = $"Lỗi tạo hạng mục kiểm tra sức khỏe: {ex.Message}"
                };
            }
        }

        public async Task<BaseResponse<HealthCheckItemResponse>> UpdateHealthCheckItemAsync(Guid id, UpdateHealthCheckItemRequest model)
        {
            try
            {
                var validationResult = await _updateHealthCheckItemValidator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return new BaseResponse<HealthCheckItemResponse>
                    {
                        Success = false,
                        Message = errors
                    };
                }

                var repo = _unitOfWork.GetRepositoryByEntity<HealthCheckItem>();
                var healthCheckItem = await repo.GetQueryable()
                    .Include(hci => hci.HealthCheckItemAssignments)
                    .FirstOrDefaultAsync(hci => hci.Id == id && !hci.IsDeleted);

                if (healthCheckItem == null)
                {
                    return new BaseResponse<HealthCheckItemResponse>
                    {
                        Success = false,
                        Message = "Không tìm thấy hạng mục kiểm tra sức khỏe."
                    };
                }

                _mapper.Map(model, healthCheckItem);
                healthCheckItem.LastUpdatedBy = "SCHOOLNURSE";
                healthCheckItem.LastUpdatedDate = DateTime.UtcNow;

                // Cập nhật HealthCheckItemAssignment nếu HealthCheckId được cung cấp
                if (model.HealthCheckId != null)
                {
                    var healthCheckRepo = _unitOfWork.GetRepositoryByEntity<HealthCheck>();
                    var healthCheck = await healthCheckRepo.GetQueryable()
                        .FirstOrDefaultAsync(hc => hc.Id == model.HealthCheckId && !hc.IsDeleted);

                    if (healthCheck == null)
                    {
                        return new BaseResponse<HealthCheckItemResponse>
                        {
                            Success = false,
                            Message = "Đợt kiểm tra sức khỏe không tồn tại hoặc đã bị xóa."
                        };
                    }

                    var assignmentRepo = _unitOfWork.GetRepositoryByEntity<HealthCheckItemAssignment>();
                    var existingAssignment = healthCheckItem.HealthCheckItemAssignments
                        .FirstOrDefault(hcia => hcia.HealthCheckId == model.HealthCheckId && !hcia.IsDeleted);

                    if (existingAssignment == null)
                    {
                        var newAssignment = new HealthCheckItemAssignment
                        {
                            Id = Guid.NewGuid(),
                            HealthCheckId = (Guid)model.HealthCheckId,
                            HealthCheckItemId = healthCheckItem.Id,
                            CreatedBy = "SCHOOLNURSE",
                            CreatedDate = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        await assignmentRepo.AddAsync(newAssignment);
                    }
                }

                await _unitOfWork.SaveChangesAsync();

                var cacheKey = _cacheService.GenerateCacheKey(HEALTH_CHECK_ITEM_CACHE_PREFIX, id.ToString());
                await _cacheService.RemoveAsync(cacheKey);
                _logger.LogDebug("Đã xóa cache chi tiết hạng mục kiểm tra sức khỏe: {CacheKey}", cacheKey);
                await _cacheService.RemoveByPrefixAsync(HEALTH_CHECK_ITEM_LIST_PREFIX);
                _logger.LogDebug("Đã xóa cache danh sách hạng mục kiểm tra sức khỏe với prefix: {Prefix}", HEALTH_CHECK_ITEM_LIST_PREFIX);

                await InvalidateAllCachesAsync();

                healthCheckItem = await repo.GetQueryable()
                    .Include(hci => hci.HealthCheckItemAssignments)
                    .ThenInclude(hcia => hcia.HealthCheck)
                    .FirstOrDefaultAsync(hci => hci.Id == id);

                var response = MapToHealthCheckItemResponse(healthCheckItem);

                _logger.LogInformation("Cập nhật hạng mục kiểm tra sức khỏe thành công với ID: {HealthCheckItemId}", id);
                return new BaseResponse<HealthCheckItemResponse>
                {
                    Success = true,
                    Data = response,
                    Message = "Cập nhật hạng mục kiểm tra sức khỏe thành công."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật hạng mục kiểm tra sức khỏe: {Id}", id);
                return new BaseResponse<HealthCheckItemResponse>
                {
                    Success = false,
                    Message = $"Lỗi cập nhật hạng mục kiểm tra sức khỏe: {ex.Message}"
                };
            }
        }

        public async Task<BaseResponse<bool>> DeleteHealthCheckItemAsync(Guid id)
        {
            try
            {
                var repo = _unitOfWork.GetRepositoryByEntity<HealthCheckItem>();
                var healthCheckItem = await repo.GetQueryable()
                    .Include(hci => hci.HealthCheckItemAssignments)
                    .FirstOrDefaultAsync(hci => hci.Id == id && !hci.IsDeleted);

                if (healthCheckItem == null)
                {
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "Không tìm thấy hạng mục kiểm tra sức khỏe."
                    };
                }

                // Kiểm tra xem HealthCheckItem có đang được sử dụng trong HealthCheck không
                if (healthCheckItem.HealthCheckItemAssignments.Any(hcia => !hcia.IsDeleted))
                {
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "Không thể xóa hạng mục kiểm tra sức khỏe vì nó đang được sử dụng trong một hoặc nhiều đợt kiểm tra."
                    };
                }

                healthCheckItem.IsDeleted = true;
                healthCheckItem.LastUpdatedBy = "SCHOOLNURSE";
                healthCheckItem.LastUpdatedDate = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();

                var cacheKey = _cacheService.GenerateCacheKey(HEALTH_CHECK_ITEM_CACHE_PREFIX, id.ToString());
                await _cacheService.RemoveAsync(cacheKey);
                _logger.LogDebug("Đã xóa cache chi tiết hạng mục kiểm tra sức khỏe: {CacheKey}", cacheKey);
                await _cacheService.RemoveByPrefixAsync(HEALTH_CHECK_ITEM_LIST_PREFIX);
                _logger.LogDebug("Đã xóa cache danh sách hạng mục kiểm tra sức khỏe với prefix: {Prefix}", HEALTH_CHECK_ITEM_LIST_PREFIX);

                await InvalidateAllCachesAsync();

                _logger.LogInformation("Xóa hạng mục kiểm tra sức khỏe thành công với ID: {HealthCheckItemId}", id);
                return new BaseResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Xóa hạng mục kiểm tra sức khỏe thành công."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa hạng mục kiểm tra sức khỏe: {Id}", id);
                return new BaseResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Lỗi xóa hạng mục kiểm tra sức khỏe: {ex.Message}"
                };
            }
        }

        private HealthCheckItemResponse MapToHealthCheckItemResponse(HealthCheckItem healthCheckItem)
        {
            return new HealthCheckItemResponse
            {
                Id = healthCheckItem.Id,
                Name = healthCheckItem.Name,
                Categories = healthCheckItem.Categories,
                Description = healthCheckItem.Description,
                Unit = healthCheckItem.Unit,
                MinValue = healthCheckItem.MinValue,
                MaxValue = healthCheckItem.MaxValue,
                HealthCheckIds = healthCheckItem.HealthCheckItemAssignments?
                    .Where(hcia => !hcia.IsDeleted)
                    .Select(hcia => hcia.HealthCheckId)
                    .ToList() ?? new List<Guid>()
            };
        }

        private IQueryable<HealthCheckItem> ApplyHealthCheckItemOrdering(IQueryable<HealthCheckItem> query, string orderBy)
        {
            return orderBy?.ToLower() switch
            {
                "name" => query.OrderBy(hci => hci.Name),
                "name_desc" => query.OrderByDescending(hci => hci.Name),
                "category" => query.OrderBy(hci => hci.Categories),
                "category_desc" => query.OrderByDescending(hci => hci.Categories),
                _ => query.OrderBy(hci => hci.Name)
            };
        }

        private async Task InvalidateAllCachesAsync()
        {
            try
            {
                _logger.LogDebug("Bắt đầu xóa toàn bộ cache cho hạng mục kiểm tra sức khỏe và các thực thể liên quan");
                await _cacheService.InvalidateTrackingSetAsync(HEALTH_CHECK_ITEM_CACHE_SET);
                await Task.WhenAll(
                    _cacheService.RemoveByPrefixAsync(HEALTH_CHECK_ITEM_CACHE_PREFIX),
                    _cacheService.RemoveByPrefixAsync(HEALTH_CHECK_ITEM_LIST_PREFIX),
                    _cacheService.RemoveByPrefixAsync("health_check"),
                    _cacheService.RemoveByPrefixAsync("health_check_results"),
                    _cacheService.RemoveByPrefixAsync("health_check_item_assignments")
                );
                _logger.LogDebug("Hoàn thành xóa toàn bộ cache cho hạng mục kiểm tra sức khỏe và các thực thể liên quan");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa toàn bộ cache cho hạng mục kiểm tra sức khỏe");
            }
        }
    }
}