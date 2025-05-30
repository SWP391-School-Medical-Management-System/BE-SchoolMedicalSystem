namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;

public class SchoolNurseExcelModel
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }
    public string Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string StaffCode { get; set; }
    public string LicenseNumber { get; set; }
    public string Specialization { get; set; }
    public bool IsValid { get; set; } = true;
    public string ErrorMessage { get; set; } = string.Empty;
}