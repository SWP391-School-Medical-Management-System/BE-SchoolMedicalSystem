namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.SchoolClassResponse;

public class ExcelImportResult<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int TotalRows { get; set; }
    public int SuccessRows { get; set; }
    public int ErrorRows { get; set; }
    public List<T> ValidData { get; set; } = new List<T>();
    public List<T> InvalidData { get; set; } = new List<T>();
    public List<string> Errors { get; set; } = new List<string>();
}