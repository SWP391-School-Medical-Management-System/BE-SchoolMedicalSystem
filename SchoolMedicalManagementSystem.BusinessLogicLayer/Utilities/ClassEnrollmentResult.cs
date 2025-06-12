namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Utilities;

public class ClassEnrollmentResult
{
    public string ClassName { get; set; }
    public int Grade { get; set; }
    public int AcademicYear { get; set; }
    public Guid? ClassId { get; set; }
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
}