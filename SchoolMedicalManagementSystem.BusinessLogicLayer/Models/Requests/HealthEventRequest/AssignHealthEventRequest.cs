namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthEventRequest;

public class AssignHealthEventRequest
{
    public Guid NurseId { get; set; }
    public string? Notes { get; set; }
}