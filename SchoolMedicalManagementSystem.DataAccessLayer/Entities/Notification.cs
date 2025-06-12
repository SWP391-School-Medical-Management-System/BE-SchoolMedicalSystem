using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Hệ thống thông báo
/// </summary>
public class Notification : BaseEntity
{
    public string Title { get; set; }         // Tiêu đề thông báo
    public string Content { get; set; }       // Nội dung thông báo
    public NotificationType NotificationType { get; set; } // Loại thông báo: HealthCheck, HealthEvent, Vaccination, General
    public Guid? SenderId { get; set; }        // ID người gửi
    public Guid RecipientId { get; set; }     // ID người nhận
    public bool RequiresConfirmation { get; set; } // Yêu cầu xác nhận
    public bool IsRead { get; set; }              // Đã đọc chưa
    public DateTime? ReadAt { get; set; }         // Thời gian đọc
    public bool IsConfirmed { get; set; }         // Đã xác nhận
    public DateTime? ConfirmedAt { get; set; }    // Thời gian xác nhận
    public string? ConfirmationNotes { get; set; } = "";  // Ghi chú từ phụ huynh khi xác nhận
    public bool IsDismissed { get; set; } = false; // THÊM MỚI: Đã dismiss popup chưa
    public DateTime? DismissedAt { get; set; }      // THÊM MỚI: Thời gian dismiss
    
    public Guid? HealthCheckId { get; set; }       // ID đợt kiểm tra sức khỏe (nếu có)
    public Guid? HealthEventId { get; set; }       // ID sự kiện y tế (nếu có)
    public Guid? VaccinationRecordId { get; set; } // ID hồ sơ tiêm chủng (nếu có)
    public Guid? AppointmentId { get; set; } // ID lịch hẹn tư vấn (nếu có)
    
    public virtual ApplicationUser? Sender { get; set; }     // Người gửi thông báo
    public virtual ApplicationUser Recipient { get; set; }  // Người nhận thông báo
    public virtual HealthCheck HealthCheck { get; set; }    // Đợt kiểm tra sức khỏe liên quan
    public virtual HealthEvent HealthEvent { get; set; }    // Sự kiện y tế liên quan
    public virtual VaccinationRecord VaccinationRecord { get; set; } // Hồ sơ tiêm chủng liên quan
    public virtual Appointment Appointment { get; set; }
}
