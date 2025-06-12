namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalItemUsageRequest;

public class CorrectMedicalItemUsageRequest
{
    public CreateMedicalItemUsageRequest CorrectedData { get; set; }
    public string CorrectionReason { get; set; }
}