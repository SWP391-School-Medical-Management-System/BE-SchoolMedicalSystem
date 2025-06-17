using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationAdministrationResponse;

public class MedicationAdministrationResponse
{
    public Guid Id { get; set; }
    public Guid StudentMedicationId { get; set; }
    public Guid AdministeredById { get; set; }
    public DateTime AdministeredAt { get; set; }
    
    public string ActualDosage { get; set; }
    public string? Notes { get; set; }
    public bool StudentRefused { get; set; }
    public string? RefusalReason { get; set; }
    public string? SideEffectsObserved { get; set; }
    
    public string AdministeredByName { get; set; }
    public string MedicationName { get; set; }
    public string StudentName { get; set; }
    public string StudentCode { get; set; }
    public string ParentName { get; set; }
    
    public DateTime CreatedDate { get; set; }
    
    public Guid? ScheduleId { get; set; }
    public DateTime? OriginalScheduledTime { get; set; }
    public string? ScheduledDosage { get; set; }
    public MedicationScheduleStatus? ScheduleStatus { get; set; }
    
    public MedicationPriority MedicationPriority { get; set; }
    public string MedicationPurpose { get; set; }
    public int RemainingDoses { get; set; }
    public bool IsLowStock { get; set; }
}