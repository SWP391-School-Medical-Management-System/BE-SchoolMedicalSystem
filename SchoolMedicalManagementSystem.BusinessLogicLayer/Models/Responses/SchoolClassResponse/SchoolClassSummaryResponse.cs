namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.SchoolClassResponse;

public class SchoolClassSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int Grade { get; set; }
    public int AcademicYear { get; set; }
    public int StudentCount { get; set; }
    public int MaleStudentCount { get; set; }
    public int FemaleStudentCount { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
}