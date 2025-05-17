namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Kết quả chi tiết cho từng hạng mục trong kiểm tra sức khỏe
/// </summary>
public class HealthCheckResultItem : BaseEntity
{
    public Guid HealthCheckResultId { get; set; }  // ID kết quả kiểm tra
    public Guid HealthCheckItemId { get; set; }    // ID hạng mục kiểm tra
    public double? Value { get; set; }             // Giá trị đo được
    public bool IsNormal { get; set; }             // Kết quả có bình thường không
    public string Notes { get; set; }              // Ghi chú thêm
    
    public virtual HealthCheckResult HealthCheckResult { get; set; } // Kết quả kiểm tra chứa kết quả chi tiết này
    public virtual HealthCheckItem HealthCheckItem { get; set; }     // Hạng mục kiểm tra
}