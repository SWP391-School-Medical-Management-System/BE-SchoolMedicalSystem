using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalItemRequest;

public class CreateMedicalItemRequest
{
    public string Type { get; set; } // "Medication" hoặc "Supply"
    public string Name { get; set; }
    public string Description { get; set; }
    public string? Dosage { get; set; } // Cho Medication
    public MedicationForm? Form { get; set; } // Cho Medication
    public DateTime? ExpiryDate { get; set; }
    public int Quantity { get; set; }
    public string Unit { get; set; }
    public string Justification { get; set; }
    public PriorityLevel Priority { get; set; } = PriorityLevel.Normal;
    public bool IsUrgent { get; set; } = false;
}