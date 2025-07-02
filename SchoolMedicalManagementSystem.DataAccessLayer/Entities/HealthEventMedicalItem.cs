using System.ComponentModel.DataAnnotations;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Entity lưu trữ thông tin tĩnh (snapshot) về sự kiện y tế và sử dụng thuốc/vật tư
/// </summary>
public class HealthEventMedicalItem : BaseEntity
{
    public Guid HealthEventId { get; set; }       // ID của sự kiện y tế
    public Guid MedicalItemUsageId { get; set; }  // ID của lần sử dụng thuốc/vật tư
    public string StudentName { get; set; }       // Tên học sinh
    public string StudentClass { get; set; }      // Lớp học của học sinh
    public string? NurseName { get; set; }        // Tên y tá xử lý (có thể null nếu chưa phân công)
    public string? MedicationName { get; set; }   // Tên thuốc (có thể null nếu không sử dụng thuốc)
    public double? MedicationQuantity { get; set; } // Số lượng thuốc
    public string? MedicationDosage { get; set; } // Liều lượng thuốc (ví dụ: "500mg mỗi 8 giờ")
    public double? SupplyQuantity { get; set; }   // Số lượng vật tư y tế (có thể null nếu không sử dụng vật tư)

    // Navigation properties
    public virtual HealthEvent HealthEvent { get; set; }       // Sự kiện y tế liên quan
    public virtual MedicalItemUsage MedicalItemUsage { get; set; } // Lần sử dụng thuốc/vật tư
}