using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;

public class UpdateMedicationStatusRequest
{
    public StudentMedicationStatus Status { get; set; }
    public string? Reason { get; set; }
}