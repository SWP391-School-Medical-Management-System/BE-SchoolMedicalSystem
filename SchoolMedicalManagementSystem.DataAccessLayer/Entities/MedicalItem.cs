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
    public string Dosage { get; set; }       // Liều lượng (cho thuốc, ví dụ: "500mg")
    public MedicationForm? Form { get; set; } // Dạng thuốc: Tablet, Syrup, Injection, Cream, Drops
    public DateTime? ExpiryDate { get; set; } // Ngày hết hạn
    public int Quantity { get; set; }         // Số lượng
    public string Unit { get; set; }          // Đơn vị: "Tablets", "Bottles", "Boxes"
    
    public virtual ICollection<MedicalItemUsage> Usages { get; set; } // Lịch sử sử dụng
}