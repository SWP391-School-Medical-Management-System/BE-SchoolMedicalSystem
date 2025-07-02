namespace SchoolMedicalManagementSystem.BusinessLogicLayer.HangFire;

public interface IHealthEventJob
{
    Task ProcessAllHealthEventsAsync();
    Task EscalatePendingEventsAsync();
    Task SendReminderNotificationsAsync();
    Task CleanupOldCompletedEventsAsync();
}