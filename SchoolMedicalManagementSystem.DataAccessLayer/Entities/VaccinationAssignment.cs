using System.ComponentModel.DataAnnotations;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Phân công School Nurse cho từng lớp trong buổi tiêm
/// </summary>
public class VaccinationAssignment : BaseEntity
{
    public Guid SessionId { get; set; }    // Foreign Key đến VaccinationSession
    public Guid ClassId { get; set; }      // Foreign Key đến SchoolClass
    public Guid NurseId { get; set; }      // Foreign Key đến ApplicationUser (School Nurse)
    public DateTime AssignedDate { get; set; } // Thời gian phân công

    public virtual VaccinationSession Session { get; set; } // Quan hệ với buổi tiêm
    public virtual SchoolClass SchoolClass { get; set; }   // Quan hệ với lớp học
    public virtual ApplicationUser Nurse { get; set; }     // Quan hệ với School Nurse
}