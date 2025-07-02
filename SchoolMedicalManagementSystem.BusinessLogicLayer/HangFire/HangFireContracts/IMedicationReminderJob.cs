namespace SchoolMedicalManagementSystem.BusinessLogicLayer.HangFire;

public interface IMedicationReminderJob
{
    Task ProcessAllRemindersAsync();
    Task SendUpcomingRemindersAsync();
    Task SendImmediateRemindersAsync();
    Task SendOverdueAlertsAsync();
    Task CheckLowStockAlertsAsync();
}