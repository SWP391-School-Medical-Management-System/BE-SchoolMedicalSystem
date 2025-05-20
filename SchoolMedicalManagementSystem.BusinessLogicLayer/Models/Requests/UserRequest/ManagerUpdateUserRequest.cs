namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;

public class ManagerUpdateUserRequest
{
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }
    public string Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string Role { get; set; }

    // Fields for Student
    public string StudentCode { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? ParentId { get; set; }

    // Field for Parent
    public string Relationship { get; set; }
}