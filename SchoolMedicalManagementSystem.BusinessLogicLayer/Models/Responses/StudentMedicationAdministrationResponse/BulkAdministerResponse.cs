namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationAdministrationResponse;

public class BulkAdministerResponse
{
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<AdministerScheduleResponse> SuccessfulAdministrations { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}