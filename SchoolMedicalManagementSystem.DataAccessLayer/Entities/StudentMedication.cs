using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Thuốc phụ huynh gửi cho trường để nhân viên y tế cho học sinh uống
/// </summary>
public class StudentMedication : BaseEntity
{
    public Guid StudentId { get; set; }              // ID học sinh
    public Guid ParentId { get; set; }               // ID phụ huynh gửi thuốc
    public Guid? ApprovedById { get; set; }          // ID School Nurse phê duyệt
    
    public string MedicationName { get; set; }       // Tên thuốc
    public string Dosage { get; set; }              // Liều lượng
    public string Instructions { get; set; }        // Hướng dẫn sử dụng từ phụ huynh
    public string Frequency { get; set; }           // Tần suất (3 lần/ngày, khi cần, ...)
    public DateTime StartDate { get; set; }         // Ngày bắt đầu cho uống
    public DateTime EndDate { get; set; }           // Ngày kết thúc
    public DateTime ExpiryDate { get; set; }        // Ngày hết hạn thuốc
    
    public string Purpose { get; set; }             // Mục đích sử dụng
    public string SideEffects { get; set; }         // Tác dụng phụ có thể xảy ra
    public string StorageInstructions { get; set; } // Hướng dẫn bảo quản
    
    public StudentMedicationStatus Status { get; set; } = StudentMedicationStatus.PendingApproval;
    public string? RejectionReason { get; set; }    // Lý do từ chối (nếu có)
    public DateTime? ApprovedAt { get; set; }       // Thời gian phê duyệt
    public DateTime? SubmittedAt { get; set; }      // Thời gian phụ huynh gửi
    
    // Navigation properties
    public virtual ApplicationUser Student { get; set; }
    public virtual ApplicationUser Parent { get; set; }
    public virtual ApplicationUser ApprovedBy { get; set; }
    public virtual ICollection<StudentMedicationAdministration> Administrations { get; set; }
    public virtual ICollection<Notification> Notifications { get; set; }
}