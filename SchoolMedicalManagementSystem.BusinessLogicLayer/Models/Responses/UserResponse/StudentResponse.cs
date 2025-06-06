namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;

public class StudentResponse
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
    public string StudentCode { get; set; }

    // Classes information
    public List<StudentClassInfo> Classes { get; set; } = new List<StudentClassInfo>();
    public int ClassCount { get; set; }
    public string CurrentClassName { get; set; }
    public int? CurrentGrade { get; set; }
    public int? CurrentAcademicYear { get; set; }

    // Parent information
    public Guid? ParentId { get; set; }
    public string ParentName { get; set; }
    public string ParentPhone { get; set; }
    public string ParentRelationship { get; set; }

    // Medical information
    public bool HasMedicalRecord { get; set; }
    public string BloodType { get; set; }
    public double? Height { get; set; }
    public double? Weight { get; set; }
    public string EmergencyContact { get; set; }
    public string EmergencyContactPhone { get; set; }

    public DateTime? CreatedDate { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
}