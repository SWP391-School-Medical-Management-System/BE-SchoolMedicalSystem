using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Utilities;

public class StudentParentCombinedImportResult
{
    public int TotalRows { get; set; }
    public int SuccessfulStudents { get; set; }
    public int SuccessfulParents { get; set; }
    public int SuccessfulLinks { get; set; }
    public int ErrorRows { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }

    public List<StudentResponse> CreatedStudents { get; set; } = new List<StudentResponse>();
    public List<ParentResponse> CreatedParents { get; set; } = new List<ParentResponse>();

    public List<string> Errors { get; set; } = new List<string>();
    public List<string> Warnings { get; set; } = new List<string>();

    public Dictionary<string, int> ParentChildrenCount { get; set; } = new Dictionary<string, int>();
    public List<string> ExistingParentsUsed { get; set; } = new List<string>();

    public int TotalClassEnrollments { get; set; }
    public int SuccessfulClassEnrollments { get; set; }
    public int FailedClassEnrollments { get; set; }
    public List<ClassEnrollmentSummary> ClassEnrollmentDetails { get; set; } = new List<ClassEnrollmentSummary>();
}