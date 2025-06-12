using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;

public class CreateSchoolNurseRequest : BaseUserRequest
{
    public string StaffCode { get; set; }
    public string LicenseNumber { get; set; }
    public string Specialization { get; set; }
}