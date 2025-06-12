namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

public class StudentClass : BaseEntity
{
    public Guid StudentId { get; set; }
    public Guid ClassId { get; set; }
    public DateTime EnrollmentDate { get; set; } = DateTime.Now;

    public virtual ApplicationUser Student { get; set; }
    public virtual SchoolClass SchoolClass { get; set; }
}