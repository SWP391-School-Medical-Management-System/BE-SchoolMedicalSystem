using System.ComponentModel.DataAnnotations;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Lưu trữ phản hồi đồng ý của phụ huynh
/// </summary>
public class VaccinationConsent : BaseEntity
{
    public Guid SessionId { get; set; }        // Foreign Key đến VaccinationSession
    public Guid StudentId { get; set; }        // Foreign Key đến ApplicationUser (học sinh)
    public Guid ParentId { get; set; }         // Foreign Key đến ApplicationUser (phụ huynh)
    public string Status { get; set; }         // Trạng thái: Pending, Confirmed, Declined
    public DateTime? ResponseDate { get; set; } // Thời gian phản hồi, nullable
    public string ConsentFormUrl { get; set; } // Đường dẫn đến form đồng ý, tùy chọn

    public virtual VaccinationSession Session { get; set; } // Quan hệ với buổi tiêm
    public virtual ApplicationUser Student { get; set; }   // Quan hệ với học sinh
    public virtual ApplicationUser Parent { get; set; }    // Quan hệ với phụ huynh
}