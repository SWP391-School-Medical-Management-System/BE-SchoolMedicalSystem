using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationResponse;

public class StudentMedicationDetailResponse
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid ParentId { get; set; }
    public Guid? ApprovedById { get; set; }
    
    // Thông tin thuốc đầy đủ từ entity
    public string MedicationName { get; set; }
    public string Dosage { get; set; }
    public string Instructions { get; set; }
    public string Frequency { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string Purpose { get; set; }
    public string? SideEffects { get; set; }
    public string? StorageInstructions { get; set; }
    
    // Thông tin từ parent
    public string? DoctorName { get; set; }
    public string? Hospital { get; set; }
    public DateTime? PrescriptionDate { get; set; }
    public string? PrescriptionNumber { get; set; }
    public int QuantitySent { get; set; }
    public string QuantityUnit { get; set; }
    public string? SpecialNotes { get; set; }
    public string? EmergencyContactInstructions { get; set; }
    
    // Cài đặt quản lý từ entity
    public int TotalDoses { get; set; }
    public int RemainingDoses { get; set; }
    public int MinStockThreshold { get; set; }
    public bool AutoGenerateSchedule { get; set; }
    public bool RequireNurseConfirmation { get; set; }
    public bool SkipOnAbsence { get; set; }
    public bool LowStockAlertSent { get; set; }
    public string? ManagementNotes { get; set; }
    public bool SkipWeekends { get; set; }
    public string? SpecificTimes { get; set; }
    public string? SkipDates { get; set; }
    
    // Trạng thái
    public StudentMedicationStatus Status { get; set; }
    public string StatusDisplayName { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    
    public MedicationPriority Priority { get; set; }
    public string PriorityDisplayName { get; set; }
    public string TimesOfDay { get; set; }
    public string TimesOfDayDisplayName { get; set; }
    
    // Audit từ entity
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
    public string? LastUpdatedBy { get; set; }
    
    // Navigation properties
    public string StudentName { get; set; }
    public string StudentCode { get; set; }
    public string ParentName { get; set; }
    public string? ApprovedByName { get; set; }
    
    // Basic statistics
    public int TotalSchedules { get; set; }
    public int TotalAdministrations { get; set; }
    public int TotalStockReceived { get; set; }
    
    // Simple computed properties
    public bool IsExpiringSoon { get; set; }
    public bool IsLowStock { get; set; }
    public int? DaysUntilExpiry { get; set; }
    
    // Thông tin thêm về stock
    public int TotalQuantitySent { get; set; }
    public int UsedDoses { get; set; }
    public double UsagePercentage { get; set; }
    public bool IsStockAvailable { get; set; }
}