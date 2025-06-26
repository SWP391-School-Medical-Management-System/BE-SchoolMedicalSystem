namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicationScheduleResponse;

public class DailyMedicationScheduleResponse
{
    public DateTime Date { get; set; }
    public List<MedicationScheduleResponse> Schedules { get; set; } = new();
    public int TotalScheduled { get; set; }
    public int Completed { get; set; }
    public int Pending { get; set; }
    public int Missed { get; set; }
}