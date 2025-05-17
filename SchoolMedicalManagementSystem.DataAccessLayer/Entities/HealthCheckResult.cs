namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Kết quả kiểm tra sức khỏe của một học sinh
/// </summary>
public class HealthCheckResult : BaseEntity
{
    public Guid UserId { get; set; }           // ID học sinh
    public Guid HealthCheckId { get; set; }    // ID đợt kiểm tra
    public string OverallAssessment { get; set; } // Đánh giá tổng thể
    public string Recommendations { get; set; }   // Khuyến nghị sau kiểm tra
    public DateTime? AppointmentDate { get; set; } // Ngày hẹn tái khám (nếu cần)
    public bool HasAbnormality { get; set; }      // Có bất thường không
    
    public virtual ApplicationUser Student { get; set; }     // Học sinh được kiểm tra
    public virtual HealthCheck HealthCheck { get; set; }     // Đợt kiểm tra
    public virtual ICollection<HealthCheckResultItem> ResultItems { get; set; } // Kết quả chi tiết từng hạng mục
}