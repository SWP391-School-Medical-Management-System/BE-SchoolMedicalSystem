namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;

public class StudentClassInfo
{
    public Guid StudentClassId { get; set; }
    public Guid ClassId { get; set; }
    public string ClassName { get; set; }
    public int Grade { get; set; }
    public int AcademicYear { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedDate { get; set; }
}