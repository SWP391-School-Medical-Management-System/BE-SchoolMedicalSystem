namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalItemUsageRequest;

public class CreateMedicalItemUsageRequest
{
    public Guid MedicalItemId { get; set; }
    public Guid HealthEventId { get; set; }
    public double Quantity { get; set; }
    public string? Notes { get; set; }
}