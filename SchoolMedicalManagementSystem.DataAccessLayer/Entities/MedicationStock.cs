using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Lịch sử gửi thuốc - theo dõi các lần Parent gửi thuốc
/// </summary>
public class MedicationStock : BaseEntity
{
    public Guid StudentMedicationId { get; set; }
    public int QuantityAdded { get; set; }          // Số lượng thêm vào
    public QuantityUnitEnum? QuantityUnit { get; set; }        // Đơn vị
    public DateTime ExpiryDate { get; set; }        // HSD của lô thuốc này
    public DateTime DateAdded { get; set; }         // Ngày gửi
    public string? Notes { get; set; }              // Ghi chú
    public bool IsInitialStock { get; set; }        // Lần gửi đầu tiên hay gửi bổ sung
    
    public virtual StudentMedication StudentMedication { get; set; }
}