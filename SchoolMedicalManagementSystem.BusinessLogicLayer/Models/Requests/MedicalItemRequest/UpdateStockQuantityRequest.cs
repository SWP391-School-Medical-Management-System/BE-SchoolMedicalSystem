namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalItemRequest;

public class UpdateStockQuantityRequest
{
    public int NewQuantity { get; set; }
    public string? Reason { get; set; }
}