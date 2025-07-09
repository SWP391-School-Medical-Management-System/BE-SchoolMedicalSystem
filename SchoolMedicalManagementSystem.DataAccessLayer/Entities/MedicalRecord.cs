namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Hồ sơ y tế của học sinh, lưu trữ thông tin sức khỏe cơ bản
/// </summary>
public class MedicalRecord : BaseEntity
{
    public Guid UserId { get; set; }        // ID của học sinh
    public string BloodType { get; set; }   // Nhóm máu: "A", "B", "AB", "O"
    public string EmergencyContact { get; set; }      // Tên người liên hệ khẩn cấp
    public string EmergencyContactPhone { get; set; } // SĐT liên hệ khẩn cấp
    
    public virtual ApplicationUser Student { get; set; }                   // Học sinh sở hữu hồ sơ y tế
    public virtual ICollection<MedicalCondition> MedicalConditions { get; set; } // Các tình trạng y tế (dị ứng, bệnh mãn tính, lịch sử y tế)
    public virtual ICollection<VaccinationRecord> VaccinationRecords { get; set; } // Hồ sơ tiêm chủng
    public virtual ICollection<VisionRecord> VisionRecords { get; set; }
    public virtual ICollection<HearingRecord> HearingRecords { get; set; }
    public virtual ICollection<PhysicalRecord> PhysicalRecords { get; set; }
}