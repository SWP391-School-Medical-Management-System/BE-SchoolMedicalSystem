namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.SchoolClassResponse;

public class SchoolClassImportDetailResponse
{
    public int RowIndex { get; set; }
    public string Name { get; set; }
    public int Grade { get; set; }
    public int AcademicYear { get; set; }
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
}