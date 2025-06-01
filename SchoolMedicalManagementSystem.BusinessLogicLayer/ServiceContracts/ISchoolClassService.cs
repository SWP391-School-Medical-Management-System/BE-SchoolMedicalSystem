using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.SchoolClassRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.SchoolClassResponse;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

public interface ISchoolClassService
{
    Task<BaseListResponse<SchoolClassSummaryResponse>> GetSchoolClassesAsync(
        int pageIndex,
        int pageSize,
        string searchTerm,
        string orderBy,
        int? grade = null,
        int? academicYear = null,
        CancellationToken cancellationToken = default);
    Task<BaseResponse<SchoolClassResponse>> GetSchoolClassByIdAsync(Guid classId);
    Task<BaseResponse<SchoolClassResponse>> CreateSchoolClassAsync(CreateSchoolClassRequest model);
    Task<BaseResponse<SchoolClassResponse>> UpdateSchoolClassAsync(Guid classId, UpdateSchoolClassRequest model);
    Task<BaseResponse<bool>> DeleteSchoolClassAsync(Guid classId);
    Task<BaseResponse<StudentsBatchResponse>> AddStudentsToClassAsync(Guid classId, AddStudentsToClassRequest model);
    Task<BaseResponse<bool>> RemoveStudentFromClassAsync(Guid classId, Guid studentId);
    Task<BaseResponse<SchoolClassStatisticsResponse>> GetSchoolClassStatisticsAsync();
    Task<byte[]> ExportSchoolClassesToExcelAsync(int? grade = null, int? academicYear = null);
    Task<BaseResponse<SchoolClassImportResponse>> ImportSchoolClassesFromExcelAsync(
        ImportSchoolClassExcelRequest request);
    Task<byte[]> DownloadSchoolClassTemplateAsync();
}