using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicationScheduleResponse;

public class MedicationScheduleResponse
{
    public Guid Id { get; set; }
    public Guid StudentMedicationId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public TimeSpan ScheduledTime { get; set; }
    public string ScheduledDosage { get; set; }
    
    public MedicationScheduleStatus Status { get; set; }
    public string StatusDisplayName { get; set; }
    public MedicationPriority Priority { get; set; }
    public string PriorityDisplayName { get; set; }
    
    public Guid? AdministrationId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? MissedAt { get; set; }
    public string? MissedReason { get; set; }
    public bool StudentPresent { get; set; }
    public DateTime? AttendanceCheckedAt { get; set; }
    
    public string? Notes { get; set; }
    public string? SpecialInstructions { get; set; }
    
    public bool ReminderSent { get; set; }
    public DateTime? ReminderSentAt { get; set; }
    public int ReminderCount { get; set; }
    
    public bool RequiresNurseConfirmation { get; set; }
    public Guid? ConfirmedByNurseId { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    
    public string MedicationName { get; set; }
    public string MedicationPurpose { get; set; }
    public MedicationTimeOfDay TimeOfDay { get; set; }
    public string TimeOfDayDisplayName { get; set; }
    public DateTime MedicationStartDate { get; set; }
    public DateTime MedicationEndDate { get; set; }
    public DateTime MedicationExpiryDate { get; set; }
    
    public Guid StudentId { get; set; }
    public string StudentName { get; set; }
    public string StudentCode { get; set; }
    public Guid ParentId { get; set; }
    public string ParentName { get; set; }
    
    public MedicationAdministrationInfo? Administration { get; set; }
    
    public DateTime CreatedDate { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? LastUpdatedBy { get; set; }
}

public class MedicationAdministrationInfo
{
    public Guid Id { get; set; }
    public DateTime AdministeredAt { get; set; }
    public string ActualDosage { get; set; }
    public string? Notes { get; set; }
    public bool StudentRefused { get; set; }
    public string? RefusalReason { get; set; }
    public string? SideEffectsObserved { get; set; }
    public Guid AdministeredById { get; set; }
    public string AdministeredByName { get; set; }
}