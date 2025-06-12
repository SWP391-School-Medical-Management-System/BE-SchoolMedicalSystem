using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthEventRequest;

public class UpdateHealthEventRequest
{
    public HealthEventType EventType { get; set; }
    public string Description { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Location { get; set; }
    public string? ActionTaken { get; set; }
    public string? Outcome { get; set; }
    public bool IsEmergency { get; set; }
    public Guid? RelatedMedicalConditionId { get; set; }
}