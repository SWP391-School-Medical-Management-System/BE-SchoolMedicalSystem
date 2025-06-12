namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;

public class StudentSummaryResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; }
    public string StudentCode { get; set; }
    public string CurrentClassName { get; set; }
    public int? CurrentGrade { get; set; }
    public int ClassCount { get; set; }
    public List<string> ClassNames { get; set; } = new List<string>();
    public bool HasMedicalRecord { get; set; }
}