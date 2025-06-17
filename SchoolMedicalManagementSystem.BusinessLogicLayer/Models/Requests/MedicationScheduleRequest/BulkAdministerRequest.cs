namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;

public class BulkAdministerRequest
{
    public List<BulkAdministerItem> Schedules { get; set; } = new();
}

public class BulkAdministerItem
{
    public Guid ScheduleId { get; set; }
    public string ActualDosage { get; set; }
    public string? Notes { get; set; }
    public bool StudentRefused { get; set; }
    public string? RefusalReason { get; set; }
    public string? SideEffectsObserved { get; set; }
}