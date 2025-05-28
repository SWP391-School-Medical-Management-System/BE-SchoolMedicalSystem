namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;

// For listing children under parent
public class StudentSummaryResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; }
    public string StudentCode { get; set; }
    public string ClassName { get; set; }
    public int? Grade { get; set; }
    public bool HasMedicalRecord { get; set; }
}