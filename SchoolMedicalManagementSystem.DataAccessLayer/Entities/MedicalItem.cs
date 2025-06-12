using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Thuốc và vật tư y tế
/// </summary>
public class MedicalItem : BaseEntity
{
    public string Type { get; set; }  // Loại vật phẩm: "Medication" (Thuốc) hoặc "Supply" (Vật tư y tế)
    public string Name { get; set; }  // Tên thuốc/vật tư (ví dụ: "Paracetamol", "Băng gạc")
    public string Description { get; set; }  // Mô tả chi tiết
    public string? Dosage { get; set; }       // Liều lượng (cho thuốc, ví dụ: "500mg")
    public MedicationForm? Form { get; set; } // Dạng thuốc: Tablet, Syrup, Injection, Cream, Drops
    public DateTime? ExpiryDate { get; set; } // Ngày hết hạn
    public int Quantity { get; set; }         // Số lượng
    public string Unit { get; set; }          // Đơn vị: "Tablets", "Bottles", "Boxes"
    public MedicalItemApprovalStatus ApprovalStatus { get; set; } = MedicalItemApprovalStatus.Pending;
    public Guid? RequestedById { get; set; }        // ID School Nurse yêu cầu thêm
    public Guid? ApprovedById { get; set; }         // ID Manager phê duyệt
    public string? Justification { get; set; }      // Lý do cần thêm thuốc/vật tư này
    public string? RejectionReason { get; set; }    // Lý do từ chối (nếu có)
    public DateTime? RequestedAt { get; set; }      // Thời gian yêu cầu
    public DateTime? ApprovedAt { get; set; }       // Thời gian phê duyệt
    public DateTime? RejectedAt { get; set; }       // Thời gian từ chối
    public PriorityLevel Priority { get; set; } = PriorityLevel.Normal;
    public bool IsUrgent { get; set; } = false;    // Yêu cầu khẩn cấp
    
    public virtual ApplicationUser RequestedBy { get; set; }     // School Nurse yêu cầu
    public virtual ApplicationUser ApprovedBy { get; set; }      // Manager phê duyệt
    public virtual ICollection<MedicalItemUsage> Usages { get; set; } // Lịch sử sử dụng
}