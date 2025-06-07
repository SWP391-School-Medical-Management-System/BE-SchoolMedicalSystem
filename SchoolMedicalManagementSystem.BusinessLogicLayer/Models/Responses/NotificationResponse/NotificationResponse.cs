using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.NotificationResponse;

public class NotificationResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public NotificationType NotificationType { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; }
    public Guid RecipientId { get; set; }
    public bool RequiresConfirmation { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsConfirmed { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string ConfirmationNotes { get; set; }
    public DateTime? CreatedDate { get; set; }

    // Additional fields for alerts
    public string StudentName { get; set; }
    public string StudentCode { get; set; }
    public string MedicalConditionName { get; set; }
    public string Severity { get; set; }
}