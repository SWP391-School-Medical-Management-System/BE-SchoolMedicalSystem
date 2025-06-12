namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.SchoolClassResponse;

public class SchoolClassExcelModel
{
    public string Name { get; set; }
    public int Grade { get; set; }
    public int AcademicYear { get; set; }
    public string ErrorMessage { get; set; }
    public bool IsValid { get; set; } = true;
}