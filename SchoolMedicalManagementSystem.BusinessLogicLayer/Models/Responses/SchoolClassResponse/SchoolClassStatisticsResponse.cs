namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.SchoolClassResponse;

public class SchoolClassStatisticsResponse
{
    public int TotalClasses { get; set; }
    public int TotalStudents { get; set; }
    public Dictionary<int, int> StudentsByGrade { get; set; } = new Dictionary<int, int>();
    public Dictionary<int, int> ClassesByGrade { get; set; } = new Dictionary<int, int>();
    public Dictionary<int, int> StudentsByAcademicYear { get; set; } = new Dictionary<int, int>();
    public double AverageStudentsPerClass { get; set; }
}