namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.SchoolClassResponse;

public class StudentsBatchResponse
{
    public int TotalStudents { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<StudentBatchResult> Results { get; set; } = new List<StudentBatchResult>();
}

public class StudentBatchResult
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; }
    public string StudentCode { get; set; }
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
}