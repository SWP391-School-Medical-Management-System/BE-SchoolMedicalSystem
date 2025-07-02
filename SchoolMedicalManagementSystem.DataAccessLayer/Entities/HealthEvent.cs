using System.ComponentModel.DataAnnotations;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Sự kiện y tế xảy ra tại trường - ốm đau, tai nạn, dị ứng
/// </summary>
public class HealthEvent : BaseEntity
{
    public Guid UserId { get; set; }              // ID học sinh gặp sự cố
    public Guid? HandledById { get; set; }        // ID y tá xử lý sự cố
    public HealthEventType EventType { get; set; } // Loại sự kiện: Injury, Illness, AllergicReaction, Fall, ChronicIllnessEpisode
    public string Description { get; set; }      // Mô tả chi tiết sự kiện
    public DateTime OccurredAt { get; set; }     // Thời gian xảy ra
    public string Location { get; set; }         // Địa điểm xảy ra (lớp học, sân chơi, phòng y tế)
    public string? ActionTaken { get; set; }      // Hành động đã thực hiện để xử lý
    public string? Outcome { get; set; }          // Kết quả xử lý
    public bool IsEmergency { get; set; }        // Có phải trường hợp khẩn cấp không
    public Guid? RelatedMedicalConditionId { get; set; } // ID tình trạng y tế liên quan (dị ứng, bệnh mãn tính)
    public HealthEventStatus Status { get; set; } = HealthEventStatus.Pending;           // Trạng thái
    public AssignmentMethod AssignmentMethod { get; set; } = AssignmentMethod.Unassigned; // Cách phân công
    public DateTime? AssignedAt { get; set; }     // Thời gian được phân công
    public DateTime? CompletedAt { get; set; }    // Thời gian hoàn thành
    public string CurrentHealthStatus { get; set; }   // Tình trạng sức khoẻ hiện tại (Nhịp tim, thân nhiệt, huyết áp, nhịp thở)
    public string? ParentNotice { get; set; }   // Lưu ý về nhà cho phụ huynh


    // Navigation properties
    public virtual ApplicationUser Student { get; set; }       // Học sinh gặp sự cố
    public virtual ApplicationUser HandledBy { get; set; }     // Y tá xử lý sự cố
    public virtual ICollection<MedicalItemUsage> MedicalItemsUsed { get; set; } // Thuốc và vật tư y tế đã sử dụng
    public virtual ICollection<HealthEventMedicalItem> HealthEventMedicalItems { get; set; }
    public virtual ICollection<Notification> Notifications { get; set; } // Thông báo về sự kiện
    public virtual MedicalCondition RelatedMedicalCondition { get; set; } // Tình trạng y tế liên quan
    public virtual ICollection<Appointment> Appointments { get; set; }
}
