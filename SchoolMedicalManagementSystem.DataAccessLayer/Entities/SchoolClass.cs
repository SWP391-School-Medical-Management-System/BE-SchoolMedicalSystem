namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Thông tin về các lớp học trong trường
/// </summary>
public class SchoolClass : BaseEntity
{
    public string Name { get; set; } // Tên lớp học, ví dụ: "1A", "2B"
    public int Grade { get; set; } // Khối lớp, ví dụ: 1, 2, 3, 4, 5
    public int AcademicYear { get; set; } // Năm học, ví dụ: 2024

    public virtual ICollection<StudentClass> StudentClasses { get; set; }
    public virtual ICollection<VaccinationSessionClass> VaccinationSessionClasses { get; set; }
    public virtual ICollection<VaccinationAssignment> VaccinationAssignments { get; set; }
}