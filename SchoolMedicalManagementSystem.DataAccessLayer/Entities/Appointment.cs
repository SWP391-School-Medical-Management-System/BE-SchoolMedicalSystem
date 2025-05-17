namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Lịch hẹn tư vấn y tế giữa nhà trường và phụ huynh
/// </summary>
public class Appointment : BaseEntity
{
    public Guid StudentId { get; set; }            // ID học sinh cần được tư vấn
    public Guid ParentId { get; set; }             // ID phụ huynh tham gia buổi tư vấn
    public Guid? CounselorId { get; set; }         // ID y tá/quản lý y tế thực hiện tư vấn
    
    public DateTime AppointmentDate { get; set; }  // Ngày hẹn
    public DateTime AppointmentTime { get; set; }  // Giờ hẹn
    public int Duration { get; set; }              // Thời lượng (phút)
    public string Location { get; set; }           // Địa điểm (Phòng y tế, Văn phòng...)
    
    public string Reason { get; set; }             // Lý do cần tư vấn
    public string Notes { get; set; }              // Ghi chú thêm
    
    public string Status { get; set; }             // Trạng thái: "Scheduled", "Completed", "Cancelled", "Rescheduled"
    
    // Các trường tham chiếu đến nguồn gốc tạo lịch hẹn
    public Guid? HealthCheckResultId { get; set; } // ID kết quả kiểm tra sức khỏe (nếu lịch hẹn xuất phát từ kiểm tra)
    public Guid? HealthEventId { get; set; }       // ID sự kiện y tế (nếu lịch hẹn xuất phát từ sự kiện)
    
    // Thông tin sau khi hoàn thành tư vấn
    public string Recommendations { get; set; }    // Khuyến nghị sau tư vấn
    public string FollowUpPlan { get; set; }       // Kế hoạch theo dõi tiếp theo
    public DateTime? FollowUpDate { get; set; }    // Ngày hẹn theo dõi tiếp theo (nếu cần)
    
    public virtual ApplicationUser Student { get; set; }        // Học sinh
    public virtual ApplicationUser Parent { get; set; }         // Phụ huynh
    public virtual ApplicationUser Counselor { get; set; }      // Người tư vấn
    public virtual HealthCheckResult HealthCheckResult { get; set; } // Kết quả kiểm tra sức khỏe liên quan
    public virtual HealthEvent HealthEvent { get; set; }        // Sự kiện y tế liên quan
    public virtual ICollection<Notification> Notifications { get; set; } // Thông báo liên quan đến lịch hẹn
}