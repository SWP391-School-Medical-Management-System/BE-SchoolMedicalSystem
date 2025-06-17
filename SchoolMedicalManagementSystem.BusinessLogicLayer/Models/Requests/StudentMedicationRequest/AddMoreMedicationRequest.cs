namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;

public class AddMoreMedicationRequest
{
    public Guid StudentMedicationId { get; set; }
    public int AdditionalQuantity { get; set; }
    public string QuantityUnit { get; set; }
    public DateTime NewExpiryDate { get; set; }
    public string? Notes { get; set; }
}