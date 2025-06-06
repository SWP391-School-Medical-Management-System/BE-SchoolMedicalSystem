namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalRecordResponse;

public class VaccinationRecordResponse
{
    public Guid Id { get; set; }
    public string VaccinationTypeName { get; set; }
    public int DoseNumber { get; set; }
    public DateTime AdministeredDate { get; set; }
    public string? Notes { get; set; }
}