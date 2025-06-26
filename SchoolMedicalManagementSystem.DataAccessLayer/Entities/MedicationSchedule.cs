using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Lịch trình uống thuốc hàng ngày - được tạo tự động từ StudentMedication
/// </summary>
public class MedicationSchedule : BaseEntity
{
    public Guid StudentMedicationId { get; set; }    // ID thuốc học sinh
    public DateTime ScheduledDate { get; set; }      // Ngày cần uống thuốc
    public TimeSpan ScheduledTime { get; set; }      // Giờ cần uống thuốc
    public string ScheduledDosage { get; set; }      // Liều lượng theo lịch
    
    // Thông tin thực hiện
    public MedicationScheduleStatus Status { get; set; } = MedicationScheduleStatus.Pending;
    public Guid? AdministrationId { get; set; }      // ID lần cho uống thuốc (nếu đã thực hiện)
    public DateTime? CompletedAt { get; set; }        // Thời gian hoàn thành
    public DateTime? MissedAt { get; set; }           // Thời gian đánh dấu bỏ lỡ
    public string? MissedReason { get; set; }         // Lý do bỏ lỡ
    public MedicationPriority Priority { get; set; } = MedicationPriority.Normal;
    public bool StudentPresent { get; set; } = true;   // Học sinh có mặt không
    public DateTime? AttendanceCheckedAt { get; set; } // Thời gian kiểm tra điểm danh
    
    // Thông tin nhắc nhở
    public bool ReminderSent { get; set; } = false;  // Đã gửi nhắc nhở chưa
    public DateTime? ReminderSentAt { get; set; }     // Thời gian gửi nhắc nhở
    public int ReminderCount { get; set; } = 0;       // Số lần đã nhắc nhở
    
    // Ghi chú
    public string? Notes { get; set; }                // Ghi chú từ y tá
    public string? SpecialInstructions { get; set; }  // Hướng dẫn đặc biệt cho ngày này
    
    // Nurse Confirmation (cho thuốc quan trọng)
    public bool RequiresNurseConfirmation { get; set; } = false;
    public Guid? ConfirmedByNurseId { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    
    // Navigation Properties
    public virtual StudentMedication StudentMedication { get; set; }
    public virtual MedicationAdministration Administration { get; set; }
}