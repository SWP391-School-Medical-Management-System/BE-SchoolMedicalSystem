namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;

public class ApproveStudentMedicationRequest
{
    public bool IsApproved { get; set; }
    public string? RejectionReason { get; set; }
    public string? Notes { get; set; }
}