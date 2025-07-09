using Microsoft.AspNetCore.Http;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.SchoolClassResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Utilities;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.UserResponse;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

public interface IUserService
{
    #region User Management (User)
    Task<BaseResponse<UserResponses>> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest model);

    Task<BaseResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequest model);

    #endregion

    #region Staff Management (Admin)

    Task<BaseListResponse<StaffUserResponse>> GetStaffUsersAsync(
        int pageIndex,
        int pageSize,
        string searchTerm,
        string orderBy,
        string role = null,
        CancellationToken cancellationToken = default);

    Task<BaseResponse<StaffUserResponse>> GetStaffUserByIdAsync(Guid userId);

    Task<BaseResponse<ManagerResponse>> CreateManagerAsync(CreateManagerRequest model);
    Task<BaseResponse<ManagerResponse>> UpdateManagerAsync(Guid managerId, UpdateManagerRequest model);
    Task<BaseResponse<bool>> DeleteManagerAsync(Guid managerId);

    Task<BaseResponse<SchoolNurseResponse>> CreateSchoolNurseAsync(CreateSchoolNurseRequest model);
    Task<BaseResponse<SchoolNurseResponse>> UpdateSchoolNurseAsync(Guid schoolNurseId, UpdateSchoolNurseRequest model);
    Task<BaseResponse<bool>> DeleteSchoolNurseAsync(Guid schoolNurseId);

    #endregion

    #region Student Management (Manager)

    Task<BaseListResponse<StudentResponse>> GetStudentsAsync(
        int pageIndex,
        int pageSize,
        string searchTerm,
        string orderBy,
        Guid? classId = null,
        bool? hasMedicalRecord = null,
        bool? hasParent = null,
        CancellationToken cancellationToken = default);

    Task<BaseResponse<StudentResponse>> GetStudentByIdAsync(Guid studentId);
    Task<BaseListResponse<StudentResponse>> GetStudentsByParentIdAsync(
    Guid parentId,
    int pageIndex = 1,
    int pageSize = 10,
    string searchTerm = null,
    string orderBy = null,
    CancellationToken cancellationToken = default);

    Task<BaseResponse<StudentResponse>> CreateStudentAsync(CreateStudentRequest model, Guid currentUserId);
    Task<BaseResponse<StudentResponse>> UpdateStudentAsync(Guid studentId, UpdateStudentRequest model);
    Task<BaseResponse<bool>> DeleteStudentAsync(Guid studentId);

    #endregion

    #region Parent Management (Manager)

    Task<BaseListResponse<ParentResponse>> GetParentsAsync(
        int pageIndex,
        int pageSize,
        string searchTerm,
        string orderBy,
        bool? hasChildren = null,
        string relationship = null,
        CancellationToken cancellationToken = default);

    Task<BaseResponse<ParentResponse>> GetParentByIdAsync(Guid parentId);
    Task<BaseResponse<ParentResponse>> CreateParentAsync(CreateParentRequest model);
    Task<BaseResponse<ParentResponse>> UpdateParentAsync(Guid parentId, UpdateParentRequest model);
    Task<BaseResponse<bool>> DeleteParentAsync(Guid parentId);

    #endregion

    #region Parent-Student Relationship Management

    Task<BaseResponse<bool>> UnlinkParentFromStudentAsync(Guid studentId, bool forceUnlink = false);

    #endregion

    #region Excel Import/Export Methods

    Task<BaseResponse<ExcelImportResult<ManagerResponse>>> ImportManagersFromExcelAsync(IFormFile file);
    Task<BaseResponse<ExcelImportResult<SchoolNurseResponse>>> ImportSchoolNursesFromExcelAsync(IFormFile file);
    Task<BaseResponse<StudentParentCombinedImportResult>> ImportStudentParentCombinedFromExcelAsync(IFormFile file);
    Task<byte[]> ExportManagersToExcelAsync(string searchTerm = "", string orderBy = null);
    Task<byte[]> ExportSchoolNursesToExcelAsync(string searchTerm = "", string orderBy = null);
    Task<byte[]> ExportParentStudentRelationshipAsync();
    Task<byte[]> DownloadManagerTemplateAsync();
    Task<byte[]> DownloadSchoolNurseTemplateAsync();
    Task<byte[]> DownloadStudentParentCombinedTemplateAsync();

    #endregion

    #region Cache

    Task InvalidateStudentCacheAsync(Guid? studentId = null);

    #endregion
}