using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationResponse;

public class PendingApprovalResponse
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid ParentId { get; set; }
    
    public string MedicationName { get; set; }
    public string Dosage { get; set; }
    public string Instructions { get; set; }
    public string Purpose { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int QuantitySent { get; set; }
    public string QuantityUnit { get; set; }
    
    public string? DoctorName { get; set; }
    public string? Hospital { get; set; }
    public DateTime? PrescriptionDate { get; set; }
    public string? PrescriptionNumber { get; set; }
    
    public MedicationPriority Priority { get; set; }
    public string PriorityDisplayName { get; set; }
    public MedicationTimeOfDay TimeOfDay { get; set; }
    public string TimeOfDayDisplayName { get; set; }
    
    public DateTime? SubmittedAt { get; set; }
    
    public string StudentName { get; set; }
    public string StudentCode { get; set; }
    public string ParentName { get; set; }
    
    public int DaysWaiting { get; set; }
}