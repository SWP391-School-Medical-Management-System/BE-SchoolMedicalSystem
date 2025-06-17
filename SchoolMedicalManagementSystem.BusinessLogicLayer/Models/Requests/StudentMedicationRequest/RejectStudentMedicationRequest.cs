namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;

public class RejectStudentMedicationRequest
{
    public string RejectionReason { get; set; }
    public string? Notes { get; set; }
}