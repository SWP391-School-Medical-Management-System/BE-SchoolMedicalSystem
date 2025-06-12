namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthEventRequest;

public class CompleteHealthEventRequest
{
    public string ActionTaken { get; set; }
    public string Outcome { get; set; }
    public string? Notes { get; set; }
}