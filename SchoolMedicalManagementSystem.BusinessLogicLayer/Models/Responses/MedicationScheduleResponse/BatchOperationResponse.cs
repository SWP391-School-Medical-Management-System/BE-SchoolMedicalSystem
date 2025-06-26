namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicationScheduleResponse;

public class BatchOperationResponse
{
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<Guid> SuccessfulIds { get; set; } = new();
    public List<Guid> FailedIds { get; set; } = new();
    public string Summary => $"Thành công: {SuccessCount}/{TotalRequested}. Thất bại: {FailureCount}";
}