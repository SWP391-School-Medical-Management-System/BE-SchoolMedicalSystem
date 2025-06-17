using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationAdministrationResponse;

public class AdministerScheduleResponse
{
    public Guid ScheduleId { get; set; }
    public Guid AdministrationId { get; set; }
    public string MedicationName { get; set; }
    public string StudentName { get; set; }
    public string StudentCode { get; set; }
    public DateTime ScheduledTime { get; set; }
    public DateTime AdministeredAt { get; set; }
    public string ActualDosage { get; set; }
    public bool StudentRefused { get; set; }
    public string? RefusalReason { get; set; }
    public string? SideEffectsObserved { get; set; }
    public string? Notes { get; set; }
    public string AdministeredByName { get; set; }
    public MedicationScheduleStatus Status { get; set; }
}