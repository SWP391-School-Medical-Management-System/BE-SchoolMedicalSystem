namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Thông tin về đợt kiểm tra sức khỏe định kỳ cho học sinh
/// </summary>
public class HealthCheck : BaseEntity
{
    public string Title { get; set; }          // Tiêu đề kiểm tra sức khỏe (ví dụ: "Kiểm tra sức khỏe học kỳ 1")
    public string Description { get; set; }    // Mô tả chi tiết đợt kiểm tra
    public DateTime ScheduledDate { get; set; } // Ngày lên lịch kiểm tra
    public Guid? ConductedById { get; set; }   // ID của y tá thực hiện
    public bool IsCompleted { get; set; }      // Trạng thái hoàn thành
    
    public virtual ApplicationUser ConductedBy { get; set; }            // Y tá thực hiện kiểm tra
    public virtual ICollection<HealthCheckItem> CheckItems { get; set; } // Các mục kiểm tra
    public virtual ICollection<HealthCheckResult> Results { get; set; }  // Kết quả kiểm tra
    public virtual ICollection<Notification> Notifications { get; set; } // Thông báo kiểm tra
}