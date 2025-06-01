namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;

public class StudentExcelModel
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }
    public string Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string StudentCode { get; set; }
    public string ClassNames { get; set; }
    public List<string> ClassList { get; set; } = new List<string>();
    public bool IsValid { get; set; } = true;
    public string ErrorMessage { get; set; } = string.Empty;
}