using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;

public class UpdateParentRequest : BaseUserUpdateRequest
{
    public string Relationship { get; set; }
}