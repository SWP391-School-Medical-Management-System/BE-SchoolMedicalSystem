namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicationScheduleResponse;

public class CleanupOperationResponse
{
    public int RecordsProcessed { get; set; }
    public int RecordsDeleted { get; set; }
    public DateTime CleanupDate { get; set; }
    public string Result => $"Đã xóa {RecordsDeleted}/{RecordsProcessed} bản ghi cũ";
}