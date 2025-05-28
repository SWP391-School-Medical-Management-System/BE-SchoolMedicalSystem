using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

public interface IUserService
{
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
    Task<BaseResponse<StudentResponse>> CreateStudentAsync(CreateStudentRequest model);
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

    Task<BaseResponse<bool>> LinkParentToStudentAsync(Guid parentId, Guid studentId);
    Task<BaseResponse<bool>> UnlinkParentFromStudentAsync(Guid studentId);

    #endregion
}