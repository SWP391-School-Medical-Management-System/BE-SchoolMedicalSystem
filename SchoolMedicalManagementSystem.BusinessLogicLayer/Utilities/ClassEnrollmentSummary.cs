namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Utilities;

public class ClassEnrollmentSummary
{
    public string StudentName { get; set; }
    public string StudentCode { get; set; }
    public List<ClassEnrollmentResult> EnrollmentResults { get; set; } = new List<ClassEnrollmentResult>();
}