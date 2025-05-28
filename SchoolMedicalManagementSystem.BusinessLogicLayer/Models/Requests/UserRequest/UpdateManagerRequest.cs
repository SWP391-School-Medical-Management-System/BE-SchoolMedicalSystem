using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;

public class UpdateManagerRequest : BaseUserUpdateRequest
{
    public string StaffCode { get; set; }
}