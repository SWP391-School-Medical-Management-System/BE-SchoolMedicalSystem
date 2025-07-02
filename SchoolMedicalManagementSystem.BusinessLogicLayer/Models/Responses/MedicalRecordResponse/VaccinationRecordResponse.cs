namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalRecordResponse;

public class VaccinationRecordResponse
{
    public Guid Id { get; set; }
    public string VaccinationTypeName { get; set; }
    public int DoseNumber { get; set; }
    public DateTime? AdministeredDate { get; set; }
    public string AdministeredBy { get; set; }
    public string BatchNumber { get; set; }
    public string Notes { get; set; }
    public string NoteAfterSession { get; set; }
    public string VaccinationStatus { get; set; }
    public string Symptoms { get; set; }
}