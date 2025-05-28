using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;

public class UpdateStudentRequest : BaseUserUpdateRequest
{
    public string StudentCode { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? ParentId { get; set; }
}