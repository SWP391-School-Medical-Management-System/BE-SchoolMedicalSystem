using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicationStockResponse;

public class MedicationStockResponse
{
    public Guid Id { get; set; }
    public Guid StudentMedicationId { get; set; }
    
    // Thông tin lần gửi thuốc từ MedicationStock entity
    public int QuantityAdded { get; set; }           // Số lượng gửi trong lần này
    public string QuantityUnit { get; set; }         // Đơn vị (viên, chai, gói)
    public DateTime ExpiryDate { get; set; }         // HSD của lô thuốc này
    public DateTime DateAdded { get; set; }          // Ngày gửi
    public string? Notes { get; set; }               // Ghi chú khi gửi
    public bool IsInitialStock { get; set; }         // Lần gửi đầu tiên hay bổ sung
    
    // Thông tin thuốc liên quan (từ StudentMedication)
    public string MedicationName { get; set; }       // Tên thuốc
    public string Dosage { get; set; }               // Liều lượng
    public string Purpose { get; set; }              // Mục đích sử dụng
    public StudentMedicationStatus MedicationStatus { get; set; }
    public string MedicationStatusDisplayName { get; set; }
    public MedicationPriority Priority { get; set; }
    public string PriorityDisplayName { get; set; }
    
    // Thông tin học sinh và phụ huynh
    public string StudentName { get; set; }
    public string StudentCode { get; set; }
    public string ParentName { get; set; }
    
    // Thông tin tích lũy và trạng thái hiện tại
    public int TotalQuantitySent { get; set; }       // Tổng số lượng đã gửi
    public int RemainingDoses { get; set; }          // Số liều còn lại hiện tại
    public int MinStockThreshold { get; set; }       // Ngưỡng cảnh báo
    public int TotalDoses { get; set; }              // Tổng số liều
    public int UsedDoses { get; set; }               // Số liều đã sử dụng
    
    // Trạng thái lô thuốc này
    public bool IsExpired { get; set; }              // Đã hết hạn chưa
    public bool IsExpiringSoon { get; set; }         // Sắp hết hạn (7 ngày)
    public int DaysUntilExpiry { get; set; }         // Số ngày còn lại đến hết hạn
    public bool IsLowStock { get; set; }             // Còn ít thuốc
    
    // Audit information
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
    public string? LastUpdatedBy { get; set; }
}