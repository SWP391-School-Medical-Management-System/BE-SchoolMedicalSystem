namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;

public class UserResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }
    public string Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string ProfileImageUrl { get; set; }
    public string Role { get; set; }

    // Fields for Student
    public string StudentCode { get; set; }
    public Guid? ClassId { get; set; }
    public string ClassName { get; set; }
    public Guid? ParentId { get; set; }
    public string ParentName { get; set; }

    // Fields for SchoolNurse
    public string StaffId { get; set; }
    public string LicenseNumber { get; set; }
    public string Specialization { get; set; }

    // Field for Parent 
    public string Relationship { get; set; }
    public List<UserResponse> Children { get; set; }
}