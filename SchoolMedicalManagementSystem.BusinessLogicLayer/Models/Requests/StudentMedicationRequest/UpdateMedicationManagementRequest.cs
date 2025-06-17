namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;

public class UpdateMedicationManagementRequest
{
    public int? TotalDoses { get; set; }
    public int? RemainingDoses { get; set; }
    public int? MinStockThreshold { get; set; }
    
    public bool? SkipOnAbsence { get; set; }
    public bool? RequireNurseConfirmation { get; set; }
    public bool? AutoGenerateSchedule { get; set; }
    
    public bool? SkipWeekends { get; set; }
    public string? SpecificTimes { get; set; }          // JSON: ["08:00", "12:00", "18:00"]
    public string? SkipDates { get; set; }              // JSON: ["2024-12-25", "2024-01-01"]
    
    public string? ManagementNotes { get; set; }
}