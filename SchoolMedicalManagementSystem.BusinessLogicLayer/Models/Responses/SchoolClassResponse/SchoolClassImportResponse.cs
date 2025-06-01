namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.SchoolClassResponse;

public class SchoolClassImportResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int TotalRows { get; set; }
    public int SuccessRows { get; set; }
    public int ErrorRows { get; set; }
    public List<SchoolClassImportDetailResponse> ImportDetails { get; set; } = new List<SchoolClassImportDetailResponse>();
    public List<string> Errors { get; set; } = new List<string>();
}