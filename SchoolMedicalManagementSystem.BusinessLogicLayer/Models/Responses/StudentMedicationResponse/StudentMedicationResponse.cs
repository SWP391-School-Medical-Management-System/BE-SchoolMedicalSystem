using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationResponse;

public class StudentMedicationResponse
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid ParentId { get; set; }
    public Guid? ApprovedById { get; set; }
    
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
    
    public StudentMedicationStatus Status { get; set; }
    public string StatusDisplayName { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    
    public DateTime? CreatedDate { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
    
    public string StudentName { get; set; }
    public string StudentCode { get; set; }
    public string ParentName { get; set; }
    public string? ApprovedByName { get; set; }
    
    public bool CanApprove { get; set; }
    public bool CanAdminister { get; set; }
    public bool IsExpiringSoon { get; set; }
    public bool IsExpired { get; set; }
    public bool IsActive { get; set; }
    public int DaysUntilExpiry { get; set; }
    public int AdministrationCount { get; set; }
}