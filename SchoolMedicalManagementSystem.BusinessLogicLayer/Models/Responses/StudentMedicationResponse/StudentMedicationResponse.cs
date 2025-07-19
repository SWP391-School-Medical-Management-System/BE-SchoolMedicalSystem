using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationResponse;

public class StudentMedicationResponse
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public Guid StudentId { get; set; }
    public Guid ParentId { get; set; }
    public Guid? ApprovedById { get; set; }
    
    public string MedicationName { get; set; }
    public string Dosage { get; set; }
    public string Purpose { get; set; }
    public int? FrequencyCount { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int QuantitySent { get; set; }
    public string QuantityUnit { get; set; }
    
    public StudentMedicationStatus Status { get; set; }
    public string StatusDisplayName { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    
    public MedicationPriority Priority { get; set; }
    public string PriorityDisplayName { get; set; }
    
    public string StudentName { get; set; }
    public string StudentCode { get; set; }
    public string ParentName { get; set; }
    public string? ApprovedByName { get; set; }
    
    public DateTime CreatedDate { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
    public string Code { get; set; }
}
