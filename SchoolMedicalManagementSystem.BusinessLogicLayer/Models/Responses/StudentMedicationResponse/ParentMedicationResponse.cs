using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationResponse;

public class ParentMedicationResponse
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    
    public string MedicationName { get; set; }
    public string Dosage { get; set; }
    public string Purpose { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int QuantitySent { get; set; }
    public string QuantityUnit { get; set; }
    
    public StudentMedicationStatus Status { get; set; }
    public string StatusDisplayName { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    
    public string StudentName { get; set; }
    public string StudentCode { get; set; }
    public string? ApprovedByName { get; set; }
    
    public int RemainingDoses { get; set; }
    public bool IsLowStock { get; set; }
    public bool IsExpiringSoon { get; set; }
    public int TotalAdministrations { get; set; }
    public DateTime? LastAdministeredAt { get; set; }
}
