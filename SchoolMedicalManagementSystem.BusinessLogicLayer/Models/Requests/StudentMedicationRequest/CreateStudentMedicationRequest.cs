namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;

public class CreateStudentMedicationRequest
{
    public Guid StudentId { get; set; }
    public string MedicationName { get; set; }
    public string Dosage { get; set; }
    public string Instructions { get; set; }
    public string Frequency { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string Purpose { get; set; }
    public string? SideEffects { get; set; }
    public string? StorageInstructions { get; set; }
}