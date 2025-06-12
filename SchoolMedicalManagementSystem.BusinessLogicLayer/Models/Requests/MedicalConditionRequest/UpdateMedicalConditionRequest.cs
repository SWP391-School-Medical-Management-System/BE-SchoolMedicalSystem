using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalConditionRequest;

public class UpdateMedicalConditionRequest
{
    public MedicalConditionType? Type { get; set; }
    public string? Name { get; set; }
    public SeverityType? Severity { get; set; }
    public string? Reaction { get; set; }
    public string? Treatment { get; set; }
    public string? Medication { get; set; }
    public DateTime? DiagnosisDate { get; set; }
    public string? Hospital { get; set; }
    public string? Doctor { get; set; }
    public string? Notes { get; set; }
}