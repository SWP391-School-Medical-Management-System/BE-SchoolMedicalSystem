namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;

public class AdminCreateUserRequest
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }
    public string Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string Role { get; set; }
    
    // Fields for SchoolNurse
    public string StaffCode { get; set; }
    public string LicenseNumber { get; set; }
    public string Specialization { get; set; }
}