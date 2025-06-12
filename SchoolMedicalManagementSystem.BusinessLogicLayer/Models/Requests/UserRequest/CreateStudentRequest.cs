using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;

public class CreateStudentRequest : BaseUserRequest
{
    public string StudentCode { get; set; }
}