namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Loại vắc-xin quản lý trong hệ thống
/// </summary>
public class VaccinationType : BaseEntity
{
    public string Name { get; set; }           // Tên loại vắc-xin
    public string Description { get; set; }    // Mô tả chi tiết
    public int RecommendedAge { get; set; }    // Tuổi khuyến nghị (tháng)
    public int DoseCount { get; set; }         // Số liều cần tiêm
    public DateTime ExpiredDate { get; set; }  // Ngày hết hạn của loại vaccine

    public virtual ICollection<VaccinationRecord> Records { get; set; } // Hồ sơ tiêm chủng
    public virtual ICollection<VaccinationSession> Sessions { get; set; } // Buổi tiêm sử dụng loại vaccine
}