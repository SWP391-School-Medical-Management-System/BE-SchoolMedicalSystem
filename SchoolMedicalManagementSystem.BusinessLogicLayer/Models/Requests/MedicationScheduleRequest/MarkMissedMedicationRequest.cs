namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicationScheduleRequest;

/// <summary>
/// Yêu cầu đánh dấu bỏ lỡ thuốc
/// </summary>
public class MarkMissedMedicationRequest
{
    public Guid ScheduleId { get; set; }
    public string MissedReason { get; set; }
    public string? Notes { get; set; }
}