namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.NotificationResponse;

public class MedicalConditionAlertResponse
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; }
    public string StudentCode { get; set; }
    public string ConditionName { get; set; }
    public string ConditionType { get; set; }
    public string Severity { get; set; }
    public string Treatment { get; set; }
    public string Medication { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string AlertMessage { get; set; }
}