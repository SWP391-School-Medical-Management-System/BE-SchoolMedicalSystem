using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicationScheduleRequest;

public class CreateMedicationScheduleRequest
{
    public Guid StudentMedicationId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public MedicationFrequencyType FrequencyType { get; set; } = MedicationFrequencyType.Daily;
    public List<DayOfWeek>? SpecificDays { get; set; } = new();
    public List<MedicationTimeOfDay> TimesOfDay { get; set; } = new();
    public List<TimeSpan> ScheduledTimes { get; set; } = new();
    public MedicationPriority Priority { get; set; } = MedicationPriority.Normal;
    public bool RequireNurseConfirmation { get; set; } = false;
    public bool SkipWeekends { get; set; } = true;
    public string? SpecialInstructions { get; set; }
}