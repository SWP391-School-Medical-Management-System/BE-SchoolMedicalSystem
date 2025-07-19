using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Các hạng mục cần kiểm tra trong đợt kiểm tra sức khỏe
/// </summary>
public class HealthCheckItem : BaseEntity
{
    public Guid HealthCheckId { get; set; }    // ID đợt kiểm tra
    public string Name { get; set; }   // Tên hạng mục kiểm tra
    public HealthCheckItemName Categories { get; set; } // Loại hạng mục: "Height", "Weight", "Vision", "Hearing", "Vital Sign","Others: Skin, Dental.."
    public string Description { get; set; }    // Mô tả chi tiết hạng mục
    public string Unit { get; set; }           // Đơn vị đo: "cm", "kg", "mmHg"
    public double? MinValue { get; set; }      // Giá trị tối thiểu bình thường
    public double? MaxValue { get; set; }      // Giá trị tối đa bình thường
    
    public virtual HealthCheck HealthCheck { get; set; }  // Đợt kiểm tra chứa hạng mục này
    public virtual ICollection<HealthCheckResultItem> ResultItems { get; set; } // Các kết quả liên quan đến hạng mục này
    public virtual HealthCheckAssignment HealthCheckAssignment { get; set; }
}