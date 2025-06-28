using System.ComponentModel.DataAnnotations;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Liên kết buổi tiêm với các lớp học
/// </summary>
public class VaccinationSessionClass : BaseEntity
{
    [Key]
    public Guid Id { get; set; }                   // Primary Key (thêm vì yêu cầu có Id trong BaseEntity không áp dụng)
    public Guid SessionId { get; set; }            // Foreign Key đến VaccinationSession
    public Guid ClassId { get; set; }              // Foreign Key đến SchoolClass

    public virtual VaccinationSession Session { get; set; } // Quan hệ với buổi tiêm
    public virtual SchoolClass SchoolClass { get; set; }   // Quan hệ với lớp học
}