using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.SchoolClassRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.SchoolClassResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services;

public class SchoolClassService : ISchoolClassService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<SchoolClassService> _logger;
    private readonly IExcelService _excelService;

    private readonly IValidator<CreateSchoolClassRequest> _createClassValidator;
    private readonly IValidator<UpdateSchoolClassRequest> _updateClassValidator;
    private readonly IValidator<AddStudentsToClassRequest> _addStudentValidator;
    private readonly IValidator<ImportSchoolClassExcelRequest> _importExcelValidator;

    private const string CLASS_CACHE_PREFIX = "class";
    private const string CLASS_LIST_PREFIX = "classes_list";
    private const string CLASS_STATISTICS_PREFIX = "class_statistics";
    private const string CLASS_CACHE_SET = "class_cache_keys";

    public SchoolClassService(
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<SchoolClassService> logger,
        IExcelService excelService,
        IValidator<CreateSchoolClassRequest> createClassValidator,
        IValidator<UpdateSchoolClassRequest> updateClassValidator,
        IValidator<AddStudentsToClassRequest> addStudentValidator,
        IValidator<ImportSchoolClassExcelRequest> importExcelValidator)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
        _logger = logger;
        _excelService = excelService;
        _createClassValidator = createClassValidator;
        _updateClassValidator = updateClassValidator;
        _addStudentValidator = addStudentValidator;
        _importExcelValidator = importExcelValidator;
    }

    #region Class Management

    public async Task<BaseListResponse<SchoolClassSummaryResponse>> GetSchoolClassesAsync(
        int pageIndex,
        int pageSize,
        string searchTerm,
        string orderBy,
        int? grade = null,
        int? academicYear = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(
                CLASS_LIST_PREFIX,
                pageIndex.ToString(),
                pageSize.ToString(),
                searchTerm ?? "",
                orderBy ?? "",
                grade?.ToString() ?? "",
                academicYear?.ToString() ?? ""
            );

            var cachedResult = await _cacheService.GetAsync<BaseListResponse<SchoolClassSummaryResponse>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("School classes list found in cache");
                return cachedResult;
            }

            var query = _unitOfWork.GetRepositoryByEntity<SchoolClass>().GetQueryable()
                .Include(c => c.StudentClasses.Where(sc => !sc.IsDeleted))
                .ThenInclude(sc => sc.Student)
                .Where(c => !c.IsDeleted)
                .AsQueryable();

            query = ApplyClassFilters(query, grade, academicYear, searchTerm);
            query = ApplyClassOrdering(query, orderBy);

            var totalCount = await query.CountAsync(cancellationToken);
            var classes = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = classes.Select(MapToClassSummaryResponse).ToList();

            var result = BaseListResponse<SchoolClassSummaryResponse>.SuccessResult(
                responses,
                totalCount,
                pageSize,
                pageIndex,
                "Lấy danh sách lớp học thành công.");

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            await _cacheService.AddToTrackingSetAsync(cacheKey, CLASS_CACHE_SET);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving school classes");
            return BaseListResponse<SchoolClassSummaryResponse>.ErrorResult("Lỗi lấy danh sách lớp học.");
        }
    }

    public async Task<BaseResponse<SchoolClassResponse>> GetSchoolClassByIdAsync(Guid classId)
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(CLASS_CACHE_PREFIX, classId.ToString());
            var cachedResponse = await _cacheService.GetAsync<BaseResponse<SchoolClassResponse>>(cacheKey);

            if (cachedResponse != null)
            {
                _logger.LogDebug("School class found in cache: {ClassId}", classId);
                return cachedResponse;
            }

            var classRepo = _unitOfWork.GetRepositoryByEntity<SchoolClass>();
            var schoolClass = await classRepo.GetQueryable()
                .Include(c => c.StudentClasses.Where(sc => !sc.IsDeleted))
                .ThenInclude(sc => sc.Student)
                .ThenInclude(s => s.MedicalRecord)
                .Where(c => c.Id == classId && !c.IsDeleted)
                .FirstOrDefaultAsync();

            if (schoolClass == null)
            {
                return new BaseResponse<SchoolClassResponse>
                {
                    Success = false,
                    Message = "Không tìm thấy lớp học."
                };
            }

            var classResponse = MapToClassResponse(schoolClass);

            var response = new BaseResponse<SchoolClassResponse>
            {
                Success = true,
                Data = classResponse,
                Message = "Lấy thông tin lớp học thành công."
            };

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15));
            await _cacheService.AddToTrackingSetAsync(cacheKey, CLASS_CACHE_SET);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting school class by ID: {ClassId}", classId);
            return new BaseResponse<SchoolClassResponse>
            {
                Success = false,
                Message = $"Lỗi lấy thông tin lớp học: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<SchoolClassResponse>> CreateSchoolClassAsync(CreateSchoolClassRequest model)
    {
        try
        {
            var validationResult = await _createClassValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return new BaseResponse<SchoolClassResponse>
                {
                    Success = false,
                    Message = errors
                };
            }

            var classRepo = _unitOfWork.GetRepositoryByEntity<SchoolClass>();
            var duplicateExists = await classRepo.GetQueryable()
                .AnyAsync(c => c.Name == model.Name &&
                               c.AcademicYear == model.AcademicYear &&
                               !c.IsDeleted);

            if (duplicateExists)
            {
                return new BaseResponse<SchoolClassResponse>
                {
                    Success = false,
                    Message = "Tên lớp học đã tồn tại trong năm học này."
                };
            }

            var managerRoleName = await GetManagerRoleName();

            var schoolClass = _mapper.Map<SchoolClass>(model);
            schoolClass.Id = Guid.NewGuid();
            schoolClass.CreatedBy = managerRoleName;
            schoolClass.CreatedDate = DateTime.Now;

            await classRepo.AddAsync(schoolClass);
            await _unitOfWork.SaveChangesAsync();

            await InvalidateClassCacheAsync();

            var classResponse = MapToClassResponse(schoolClass);

            return new BaseResponse<SchoolClassResponse>
            {
                Success = true,
                Data = classResponse,
                Message = "Tạo lớp học thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating school class");
            return new BaseResponse<SchoolClassResponse>
            {
                Success = false,
                Message = $"Lỗi tạo lớp học: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<SchoolClassResponse>> UpdateSchoolClassAsync(Guid classId,
        UpdateSchoolClassRequest model)
    {
        try
        {
            var validationResult = await _updateClassValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return new BaseResponse<SchoolClassResponse>
                {
                    Success = false,
                    Message = errors
                };
            }

            var classRepo = _unitOfWork.GetRepositoryByEntity<SchoolClass>();
            var schoolClass = await classRepo.GetQueryable()
                .Include(c => c.StudentClasses.Where(sc => !sc.IsDeleted))
                .ThenInclude(sc => sc.Student)
                .FirstOrDefaultAsync(c => c.Id == classId && !c.IsDeleted);

            if (schoolClass == null)
            {
                return new BaseResponse<SchoolClassResponse>
                {
                    Success = false,
                    Message = "Không tìm thấy lớp học."
                };
            }

            var duplicateExists = await classRepo.GetQueryable()
                .AnyAsync(c => c.Id != classId &&
                               c.Name == model.Name &&
                               c.AcademicYear == model.AcademicYear &&
                               !c.IsDeleted);

            if (duplicateExists)
            {
                return new BaseResponse<SchoolClassResponse>
                {
                    Success = false,
                    Message = "Tên lớp học đã tồn tại trong năm học này."
                };
            }

            var managerRoleName = await GetManagerRoleName();

            schoolClass.Name = model.Name;
            schoolClass.Grade = model.Grade;
            schoolClass.AcademicYear = model.AcademicYear;
            schoolClass.LastUpdatedBy = managerRoleName;
            schoolClass.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            await InvalidateClassCacheAsync();

            var classResponse = MapToClassResponse(schoolClass);

            return new BaseResponse<SchoolClassResponse>
            {
                Success = true,
                Data = classResponse,
                Message = "Cập nhật lớp học thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating school class");
            return new BaseResponse<SchoolClassResponse>
            {
                Success = false,
                Message = $"Lỗi cập nhật lớp học: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<bool>> DeleteSchoolClassAsync(Guid classId)
    {
        try
        {
            var classRepo = _unitOfWork.GetRepositoryByEntity<SchoolClass>();
            var schoolClass = await classRepo.GetQueryable()
                .Include(c => c.StudentClasses.Where(sc => !sc.IsDeleted))
                .ThenInclude(sc => sc.Student)
                .FirstOrDefaultAsync(c => c.Id == classId && !c.IsDeleted);

            if (schoolClass == null)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "Không tìm thấy lớp học."
                };
            }

            if (schoolClass.StudentClasses != null && schoolClass.StudentClasses.Any(sc => !sc.IsDeleted))
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "Không thể xóa lớp học đang có học sinh. Vui lòng chuyển học sinh sang lớp khác trước."
                };
            }

            var managerRoleName = await GetManagerRoleName();

            schoolClass.IsDeleted = true;
            schoolClass.LastUpdatedBy = managerRoleName;
            schoolClass.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            await InvalidateClassCacheAsync();

            return new BaseResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Xóa lớp học thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting school class: {ClassId}", classId);
            return new BaseResponse<bool>
            {
                Success = false,
                Data = false,
                Message = $"Lỗi xóa lớp học: {ex.Message}"
            };
        }
    }

    #endregion

    #region Student Management in Class

    public async Task<BaseResponse<StudentsBatchResponse>> AddStudentsToClassAsync(Guid classId,
        AddStudentsToClassRequest model)
    {
        try
        {
            var validationResult = await _addStudentValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return new BaseResponse<StudentsBatchResponse>
                {
                    Success = false,
                    Message = errors
                };
            }

            var classRepo = _unitOfWork.GetRepositoryByEntity<SchoolClass>();
            var schoolClass = await classRepo.GetQueryable()
                .FirstOrDefaultAsync(c => c.Id == classId && !c.IsDeleted);

            if (schoolClass == null)
            {
                return new BaseResponse<StudentsBatchResponse>
                {
                    Success = false,
                    Message = "Không tìm thấy lớp học."
                };
            }

            var response = new StudentsBatchResponse
            {
                TotalStudents = model.StudentIds.Count
            };

            var userRepo = _unitOfWork.GetRepositoryByEntity<ApplicationUser>();
            var studentClassRepo = _unitOfWork.GetRepositoryByEntity<StudentClass>();
            var managerRoleName = await GetManagerRoleName();

            var students = await userRepo.GetQueryable()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => model.StudentIds.Contains(u.Id) && !u.IsDeleted)
                .ToListAsync();

            var existingEnrollments = await studentClassRepo.GetQueryable()
                .Where(sc => sc.ClassId == classId &&
                             model.StudentIds.Contains(sc.StudentId) &&
                             !sc.IsDeleted)
                .Select(sc => sc.StudentId)
                .ToListAsync();

            foreach (var studentId in model.StudentIds)
            {
                var result = new StudentBatchResult
                {
                    StudentId = studentId
                };

                try
                {
                    var student = students.FirstOrDefault(s => s.Id == studentId);

                    if (student == null)
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = "Không tìm thấy học sinh hoặc học sinh đã bị xóa.";
                    }
                    else if (!student.UserRoles.Any(ur => ur.Role.Name == "STUDENT"))
                    {
                        result.StudentName = student.FullName;
                        result.StudentCode = student.StudentCode;
                        result.IsSuccess = false;
                        result.ErrorMessage = "Người dùng không phải là học sinh.";
                    }
                    else if (existingEnrollments.Contains(studentId))
                    {
                        result.StudentName = student.FullName;
                        result.StudentCode = student.StudentCode;
                        result.IsSuccess = false;
                        result.ErrorMessage = "Học sinh đã có trong lớp học này.";
                    }
                    else
                    {
                        var studentClass = new StudentClass
                        {
                            Id = Guid.NewGuid(),
                            StudentId = studentId,
                            ClassId = classId,
                            EnrollmentDate = DateTime.Now,
                            CreatedBy = managerRoleName,
                            CreatedDate = DateTime.Now
                        };

                        await studentClassRepo.AddAsync(studentClass);

                        result.StudentName = student.FullName;
                        result.StudentCode = student.StudentCode;
                        result.IsSuccess = true;
                        result.ErrorMessage = "Thành công";

                        response.SuccessCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding student {StudentId} to class {ClassId}", studentId, classId);
                    result.IsSuccess = false;
                    result.ErrorMessage = $"Lỗi: {ex.Message}";
                }

                response.Results.Add(result);
            }

            response.FailureCount = response.TotalStudents - response.SuccessCount;

            if (response.SuccessCount > 0)
            {
                await _unitOfWork.SaveChangesAsync();
                await InvalidateClassCacheAsync();
            }

            return new BaseResponse<StudentsBatchResponse>
            {
                Success = response.SuccessCount > 0,
                Data = response,
                Message = response.SuccessCount > 0
                    ? $"Thêm thành công {response.SuccessCount}/{response.TotalStudents} học sinh vào lớp."
                    : "Không có học sinh nào được thêm vào lớp."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding students to class: {ClassId}", classId);
            return new BaseResponse<StudentsBatchResponse>
            {
                Success = false,
                Message = $"Lỗi thêm học sinh vào lớp: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponse<bool>> RemoveStudentFromClassAsync(Guid classId, Guid studentId)
    {
        try
        {
            var studentClassRepo = _unitOfWork.GetRepositoryByEntity<StudentClass>();
            var studentClass = await studentClassRepo.GetQueryable()
                .Include(sc => sc.Student)
                .ThenInclude(s => s.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(sc => sc.ClassId == classId &&
                                           sc.StudentId == studentId &&
                                           !sc.IsDeleted);

            if (studentClass == null)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "Không tìm thấy học sinh trong lớp học này."
                };
            }

            if (!studentClass.Student.UserRoles.Any(ur => ur.Role.Name == "STUDENT"))
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "Người dùng không phải là học sinh."
                };
            }

            var managerRoleName = await GetManagerRoleName();

            studentClass.IsDeleted = true;
            studentClass.LastUpdatedBy = managerRoleName;
            studentClass.LastUpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            await InvalidateClassCacheAsync();

            return new BaseResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Xóa học sinh khỏi lớp thành công."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing student from class: {ClassId} <- {StudentId}", classId, studentId);
            return new BaseResponse<bool>
            {
                Success = false,
                Data = false,
                Message = $"Lỗi xóa học sinh khỏi lớp: {ex.Message}"
            };
        }
    }

    #endregion

    #region Excel Import/Export

    public async Task<byte[]> DownloadSchoolClassTemplateAsync()
    {
        try
        {
            _logger.LogInformation("Generating school class Excel template");
            return await _excelService.GenerateSchoolClassTemplateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading school class template");
            throw;
        }
    }

    public async Task<byte[]> ExportSchoolClassesToExcelAsync(int? grade = null, int? academicYear = null)
    {
        try
        {
            _logger.LogInformation(
                "Exporting school classes to Excel with filters - Grade: {Grade}, AcademicYear: {AcademicYear}", grade,
                academicYear);

            var classRepo = _unitOfWork.GetRepositoryByEntity<SchoolClass>();
            var query = classRepo.GetQueryable()
                .Include(c => c.StudentClasses.Where(sc => !sc.IsDeleted))
                .ThenInclude(sc => sc.Student)
                .Where(c => !c.IsDeleted);

            if (grade.HasValue)
            {
                query = query.Where(c => c.Grade == grade.Value);
            }

            if (academicYear.HasValue)
            {
                query = query.Where(c => c.AcademicYear == academicYear.Value);
            }

            var classes = await query
                .OrderBy(c => c.AcademicYear)
                .ThenBy(c => c.Grade)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var classResponses = classes.Select(MapToClassResponse).ToList();

            return await _excelService.ExportSchoolClassesToExcelAsync(classResponses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting school classes to Excel");
            throw;
        }
    }

    public async Task<BaseResponse<SchoolClassImportResponse>> ImportSchoolClassesFromExcelAsync(
        ImportSchoolClassExcelRequest request)
    {
        try
        {
            var validationResult = await _importExcelValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                string errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return new BaseResponse<SchoolClassImportResponse>
                {
                    Success = false,
                    Message = errors
                };
            }

            var excelResult = await _excelService.ReadSchoolClassExcelAsync(request.ExcelFile);
            if (!excelResult.Success)
            {
                return new BaseResponse<SchoolClassImportResponse>
                {
                    Success = false,
                    Message = excelResult.Message
                };
            }

            var importResponse = new SchoolClassImportResponse
            {
                TotalRows = excelResult.TotalRows,
                ImportDetails = new List<SchoolClassImportDetailResponse>()
            };

            var classRepo = _unitOfWork.GetRepositoryByEntity<SchoolClass>();
            var managerRoleName = await GetManagerRoleName();
            var successCount = 0;
            var errorCount = 0;

            foreach (var excelClass in excelResult.ValidData)
            {
                var importDetail = new SchoolClassImportDetailResponse
                {
                    RowIndex = excelResult.ValidData.IndexOf(excelClass) + 3,
                    Name = excelClass.Name,
                    Grade = excelClass.Grade,
                    AcademicYear = excelClass.AcademicYear
                };

                try
                {
                    var existingClass = await classRepo.GetQueryable()
                        .FirstOrDefaultAsync(c => c.Name == excelClass.Name &&
                                                  c.AcademicYear == excelClass.AcademicYear &&
                                                  !c.IsDeleted);

                    if (existingClass != null)
                    {
                        if (request.OverwriteExisting)
                        {
                            existingClass.Grade = excelClass.Grade;
                            existingClass.LastUpdatedBy = managerRoleName;
                            existingClass.LastUpdatedDate = DateTime.Now;

                            importDetail.IsSuccess = true;
                            importDetail.ErrorMessage = "Đã cập nhật lớp học hiện có";
                            successCount++;
                        }
                        else
                        {
                            importDetail.IsSuccess = false;
                            importDetail.ErrorMessage = "Lớp học đã tồn tại trong năm học này";
                            errorCount++;
                        }
                    }
                    else
                    {
                        var newClass = new SchoolClass
                        {
                            Id = Guid.NewGuid(),
                            Name = excelClass.Name,
                            Grade = excelClass.Grade,
                            AcademicYear = excelClass.AcademicYear,
                            CreatedBy = managerRoleName,
                            CreatedDate = DateTime.Now
                        };

                        await classRepo.AddAsync(newClass);

                        importDetail.IsSuccess = true;
                        importDetail.ErrorMessage = "Tạo mới thành công";
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importing class {ClassName}", excelClass.Name);
                    importDetail.IsSuccess = false;
                    importDetail.ErrorMessage = $"Lỗi: {ex.Message}";
                    errorCount++;
                }

                importResponse.ImportDetails.Add(importDetail);
            }

            foreach (var invalidClass in excelResult.InvalidData)
            {
                var importDetail = new SchoolClassImportDetailResponse
                {
                    RowIndex = excelResult.InvalidData.IndexOf(invalidClass) + 3,
                    Name = invalidClass.Name,
                    Grade = invalidClass.Grade,
                    AcademicYear = invalidClass.AcademicYear,
                    IsSuccess = false,
                    ErrorMessage = invalidClass.ErrorMessage
                };

                importResponse.ImportDetails.Add(importDetail);
                errorCount++;
            }

            if (successCount > 0)
            {
                await _unitOfWork.SaveChangesAsync();
                await InvalidateClassCacheAsync();
            }

            importResponse.SuccessRows = successCount;
            importResponse.ErrorRows = errorCount;
            importResponse.Success = successCount > 0;
            importResponse.Message = successCount > 0
                ? $"Import thành công {successCount}/{excelResult.TotalRows} lớp học."
                : "Không có lớp học nào được import thành công.";

            if (excelResult.Errors.Any())
            {
                importResponse.Errors = excelResult.Errors;
            }

            return new BaseResponse<SchoolClassImportResponse>
            {
                Success = true,
                Data = importResponse,
                Message = "Xử lý import Excel hoàn tất."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing school classes from Excel");
            return new BaseResponse<SchoolClassImportResponse>
            {
                Success = false,
                Message = $"Lỗi import Excel: {ex.Message}"
            };
        }
    }

    #endregion

    #region Statistics

    public async Task<BaseResponse<SchoolClassStatisticsResponse>> GetSchoolClassStatisticsAsync()
    {
        try
        {
            var cacheKey = _cacheService.GenerateCacheKey(CLASS_STATISTICS_PREFIX, "all");
            var cachedResult = await _cacheService.GetAsync<BaseResponse<SchoolClassStatisticsResponse>>(cacheKey);

            if (cachedResult != null)
            {
                _logger.LogDebug("School class statistics found in cache");
                return cachedResult;
            }

            var classRepo = _unitOfWork.GetRepositoryByEntity<SchoolClass>();
            var classes = await classRepo.GetQueryable()
                .Include(c => c.StudentClasses.Where(sc => !sc.IsDeleted))
                .ThenInclude(sc => sc.Student)
                .Where(c => !c.IsDeleted)
                .ToListAsync();

            var statistics = new SchoolClassStatisticsResponse
            {
                TotalClasses = classes.Count,
                TotalStudents = classes.Sum(c => c.StudentClasses?.Count(sc => !sc.IsDeleted) ?? 0),
                StudentsByGrade = classes
                    .GroupBy(c => c.Grade)
                    .ToDictionary(g => g.Key, g => g.Sum(c => c.StudentClasses?.Count(sc => !sc.IsDeleted) ?? 0)),
                StudentsByAcademicYear = classes
                    .GroupBy(c => c.AcademicYear)
                    .ToDictionary(g => g.Key, g => g.Sum(c => c.StudentClasses?.Count(sc => !sc.IsDeleted) ?? 0))
            };

            statistics.AverageStudentsPerClass = statistics.TotalClasses > 0
                ? Math.Round((double)statistics.TotalStudents / statistics.TotalClasses, 2)
                : 0;

            var response = new BaseResponse<SchoolClassStatisticsResponse>
            {
                Success = true,
                Data = statistics,
                Message = "Lấy thống kê lớp học thành công."
            };

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10));
            await _cacheService.AddToTrackingSetAsync(cacheKey, CLASS_CACHE_SET);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting school class statistics");
            return new BaseResponse<SchoolClassStatisticsResponse>
            {
                Success = false,
                Message = $"Lỗi lấy thống kê lớp học: {ex.Message}"
            };
        }
    }

    #endregion

    #region Helper Methods

    private async Task<string> GetManagerRoleName()
    {
        try
        {
            var managerRole = await _unitOfWork.GetRepositoryByEntity<Role>().GetQueryable()
                .FirstOrDefaultAsync(r => r.Name == "MANAGER");

            return managerRole?.Name ?? "MANAGER";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting manager role name");
            return "MANAGER";
        }
    }

    private SchoolClassResponse MapToClassResponse(SchoolClass schoolClass)
    {
        var response = _mapper.Map<SchoolClassResponse>(schoolClass);

        if (schoolClass.StudentClasses != null)
        {
            var activeStudentClasses = schoolClass.StudentClasses
                .Where(sc => !sc.IsDeleted && !sc.Student.IsDeleted)
                .ToList();

            var activeStudents = activeStudentClasses.Select(sc => sc.Student).ToList();
            response.StudentCount = activeStudents.Count;
            response.MaleStudentCount = activeStudents.Count(s => s.Gender == "Male");
            response.FemaleStudentCount = activeStudents.Count(s => s.Gender == "Female");

            if (response.StudentCount > 0)
            {
                response.MalePercentage =
                    Math.Round((double)response.MaleStudentCount / response.StudentCount * 100, 2);
                response.FemalePercentage =
                    Math.Round((double)response.FemaleStudentCount / response.StudentCount * 100, 2);
            }

            response.Students = _mapper.Map<List<StudentSummaryResponse>>(activeStudentClasses);

            foreach (var studentResponse in response.Students)
            {
                var studentClass = activeStudentClasses.First(sc => sc.Student.Id == studentResponse.Id);
                var student = studentClass.Student;

                if (student.StudentClasses != null)
                {
                    var allActiveClasses = student.StudentClasses.Where(sc => !sc.IsDeleted).ToList();
                    studentResponse.ClassCount = allActiveClasses.Count;
                    studentResponse.ClassNames = allActiveClasses
                        .Select(sc => sc.SchoolClass?.Name)
                        .Where(name => !string.IsNullOrEmpty(name))
                        .ToList();
                }
                else
                {
                    studentResponse.ClassCount = 1;
                    studentResponse.ClassNames = new List<string> { schoolClass.Name };
                }
            }
        }

        return response;
    }

    private SchoolClassSummaryResponse MapToClassSummaryResponse(SchoolClass schoolClass)
    {
        var response = _mapper.Map<SchoolClassSummaryResponse>(schoolClass);

        if (schoolClass.StudentClasses != null)
        {
            var activeStudents = schoolClass.StudentClasses
                .Where(sc => !sc.IsDeleted && !sc.Student.IsDeleted)
                .Select(sc => sc.Student)
                .ToList();

            response.StudentCount = activeStudents.Count;
            response.MaleStudentCount = activeStudents.Count(s => s.Gender == "Male");
            response.FemaleStudentCount = activeStudents.Count(s => s.Gender == "Female");
        }

        return response;
    }

    private IQueryable<SchoolClass> ApplyClassFilters(
        IQueryable<SchoolClass> query,
        int? grade,
        int? academicYear,
        string searchTerm)
    {
        if (grade.HasValue)
        {
            query = query.Where(c => c.Grade == grade.Value);
        }

        if (academicYear.HasValue)
        {
            query = query.Where(c => c.AcademicYear == academicYear.Value);
        }

        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(searchTerm));
        }

        return query;
    }

    private IQueryable<SchoolClass> ApplyClassOrdering(IQueryable<SchoolClass> query, string orderBy)
    {
        return orderBy?.ToLower() switch
        {
            "name" => query.OrderBy(c => c.Name),
            "name_desc" => query.OrderByDescending(c => c.Name),
            "grade" => query.OrderBy(c => c.Grade).ThenBy(c => c.Name),
            "grade_desc" => query.OrderByDescending(c => c.Grade).ThenBy(c => c.Name),
            "academicyear" => query.OrderBy(c => c.AcademicYear).ThenBy(c => c.Grade).ThenBy(c => c.Name),
            "academicyear_desc" => query.OrderByDescending(c => c.AcademicYear).ThenBy(c => c.Grade)
                .ThenBy(c => c.Name),
            "studentcount" => query.OrderBy(c => c.StudentClasses.Count(sc => !sc.IsDeleted)),
            "studentcount_desc" => query.OrderByDescending(c => c.StudentClasses.Count(sc => !sc.IsDeleted)),
            "createdate_desc" => query.OrderByDescending(c => c.CreatedDate),
            "createdate" => query.OrderBy(c => c.CreatedDate),
            _ => query.OrderBy(c => c.Grade).ThenBy(c => c.Name)
        };
    }

    private async Task InvalidateClassCacheAsync()
    {
        try
        {
            await _cacheService.RemoveByPrefixAsync(CLASS_CACHE_PREFIX);
            await _cacheService.RemoveByPrefixAsync(CLASS_LIST_PREFIX);
            await _cacheService.RemoveByPrefixAsync(CLASS_STATISTICS_PREFIX);

            _logger.LogDebug("Invalidated class cache");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating class cache");
        }
    }

    #endregion
}