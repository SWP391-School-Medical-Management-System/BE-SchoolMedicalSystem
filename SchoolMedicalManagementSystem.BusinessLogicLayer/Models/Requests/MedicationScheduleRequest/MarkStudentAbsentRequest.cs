namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicationScheduleRequest;

public class MarkStudentAbsentRequest
{
    public Guid ScheduleId { get; set; }
    public string? Notes { get; set; } = "Học sinh vắng mặt";
}