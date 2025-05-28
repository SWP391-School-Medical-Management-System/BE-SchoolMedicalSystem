using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;

public class CreateParentRequest : BaseUserRequest
{
    public string Relationship { get; set; }
}