namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Utilities;

public class ClassEnrollmentBatchResult
{
    public int TotalAttempts { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<ClassEnrollmentResult> Results { get; set; } = new List<ClassEnrollmentResult>();
}