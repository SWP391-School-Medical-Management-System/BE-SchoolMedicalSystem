namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;

public abstract class BaseUserRequest
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }
    public string Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
}