using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;

public class CreateManagerRequest : BaseUserRequest
{
    public string StaffCode { get; set; }
}