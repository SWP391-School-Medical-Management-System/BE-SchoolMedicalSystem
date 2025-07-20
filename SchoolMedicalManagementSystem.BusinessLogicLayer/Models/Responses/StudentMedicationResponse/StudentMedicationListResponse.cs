using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationResponse;

public class StudentMedicationListResponse
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid ParentId { get; set; }
    public Guid? ApprovedById { get; set; }
    
    public string MedicationName { get; set; }
    public string Dosage { get; set; }
    public string Purpose { get; set; }
    public DateTime ExpiryDate { get; set; }
    
    public StudentMedicationStatus Status { get; set; }
    public string StatusDisplayName { get; set; }
    public MedicationPriority Priority { get; set; }
    public string PriorityDisplayName { get; set; }
    
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    
    public string StudentName { get; set; }
    public string StudentCode { get; set; }
    public string ParentName { get; set; }
    public string? ApprovedByName { get; set; }
    
    public int TotalSchedules { get; set; }
    public int TotalAdministrations { get; set; }
    
    // Simple flags
    public bool IsExpiringSoon { get; set; }
    public bool IsLowStock { get; set; }
    public int QuantitySent { get; set; }
    public int QuantityReceive { get; set; }
    public string QuantityUnit { get; set; }
    public int? FrequencyCount { get; set; }
    public List<string> TimesOfDay { get; set; }
    public DateTime? StartDate { get; set; }
    public int TotalDateUsage { get; set; }
    public string Instructions { get; set; }
    public string SpecialNotes { get; set; }
}