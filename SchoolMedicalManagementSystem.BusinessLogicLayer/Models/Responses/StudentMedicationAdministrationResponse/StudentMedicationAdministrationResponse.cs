namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationAdministrationResponse;

public class StudentMedicationAdministrationResponse
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

    public string MedicationName { get; set; }
    public string StudentName { get; set; }
    public string AdministeredByName { get; set; }

    public DateTime? CreatedDate { get; set; }
}