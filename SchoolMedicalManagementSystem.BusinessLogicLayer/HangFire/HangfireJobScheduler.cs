using Hangfire;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.HangFire;

public static class HangfireJobScheduler
{
    public static void ScheduleAllRecurringJobs()
    {
        // =================== MEDICATION SCHEDULE JOBS ===================
        
        // Process today medications - every minute
        RecurringJob.AddOrUpdate<IMedicationScheduleJob>(
            "process-today-medications",
            job => job.ProcessTodayMedicationsAsync(),
            Cron.Minutely,
            TimeZoneInfo.Local,
            "default");

        // Process approved to active transition - every minute
        RecurringJob.AddOrUpdate<IMedicationScheduleJob>(
            "process-approved-to-active",
            job => job.ProcessApprovedToActiveTransitionAsync(),
            Cron.Minutely,
            TimeZoneInfo.Local,
            "default");

        // Process newly approved medications - every minute
        RecurringJob.AddOrUpdate<IMedicationScheduleJob>(
            "process-newly-approved",
            job => job.ProcessNewlyApprovedMedicationsAsync(),
            Cron.Minutely,
            TimeZoneInfo.Local,
            "default");

        // Process tomorrow medications - every hour after 6 PM
        RecurringJob.AddOrUpdate<IMedicationScheduleJob>(
            "process-tomorrow-medications",
            job => job.ProcessTomorrowMedicationsAsync(),
            "0 18-23 * * *",
            TimeZoneInfo.Local,
            "default");

        // =================== MEDICATION REMINDER JOBS ===================
        
        // All reminders - every minute
        RecurringJob.AddOrUpdate<IMedicationReminderJob>(
            "medication-reminders-all",
            job => job.ProcessAllRemindersAsync(),
            Cron.Minutely,
            TimeZoneInfo.Local,
            "high");

        // Upcoming reminders - every minute
        RecurringJob.AddOrUpdate<IMedicationReminderJob>(
            "medication-reminders-upcoming",
            job => job.SendUpcomingRemindersAsync(),
            Cron.Minutely,
            TimeZoneInfo.Local,
            "high");

        // Overdue alerts - every 5 minutes
        RecurringJob.AddOrUpdate<IMedicationReminderJob>(
            "medication-overdue-alerts",
            job => job.SendOverdueAlertsAsync(),
            "*/5 * * * *",
            TimeZoneInfo.Local,
            "high");

        // Low stock alerts - every 4 hours
        RecurringJob.AddOrUpdate<IMedicationReminderJob>(
            "medication-low-stock-alerts",
            job => job.CheckLowStockAlertsAsync(),
            "0 */4 * * *",
            TimeZoneInfo.Local,
            "default");

        // =================== MEDICATION CLEANUP JOBS ===================
        
        // Full cleanup - every 6 hours at specific times
        RecurringJob.AddOrUpdate<IMedicationCleanupJob>(
            "medication-cleanup-full",
            job => job.CleanupExpiredDataAsync(),
            "0 2,8,14,20 * * *",
            TimeZoneInfo.Local,
            "low");

        // Expired medications cleanup - daily at 1:30 AM
        RecurringJob.AddOrUpdate<IMedicationCleanupJob>(
            "medication-cleanup-expired",
            job => job.CleanupExpiredMedicationsAsync(),
            "30 1 * * *",
            TimeZoneInfo.Local,
            "low");

        // Old notifications cleanup - daily at 2:00 AM
        RecurringJob.AddOrUpdate<IMedicationCleanupJob>(
            "medication-cleanup-notifications",
            job => job.CleanupOldNotificationsAsync(),
            "0 2 * * *",
            TimeZoneInfo.Local,
            "low");

        // Old administration records cleanup - daily at 2:30 AM
        RecurringJob.AddOrUpdate<IMedicationCleanupJob>(
            "medication-cleanup-administration",
            job => job.CleanupOldAdministrationRecordsAsync(),
            "30 2 * * *",
            TimeZoneInfo.Local,
            "low");

        // =================== HEALTH EVENT JOBS ===================
        
        // Process all health events - every minute
        RecurringJob.AddOrUpdate<IHealthEventJob>(
            "health-events-all",
            job => job.ProcessAllHealthEventsAsync(),
            Cron.Minutely,
            TimeZoneInfo.Local,
            "high");

        // Health event reminders - every 5 minutes
        RecurringJob.AddOrUpdate<IHealthEventJob>(
            "health-event-reminders",
            job => job.SendReminderNotificationsAsync(),
            "*/5 * * * *",
            TimeZoneInfo.Local,
            "high");

        // Cleanup old completed health events - daily at 1:00 AM
        RecurringJob.AddOrUpdate<IHealthEventJob>(
            "health-event-cleanup",
            job => job.CleanupOldCompletedEventsAsync(),
            "0 1 * * *",
            TimeZoneInfo.Local,
            "low");
    }

    public static void RemoveAllRecurringJobs()
    {
        // Medication jobs
        RecurringJob.RemoveIfExists("process-today-medications");
        RecurringJob.RemoveIfExists("process-approved-to-active");
        RecurringJob.RemoveIfExists("process-newly-approved");
        RecurringJob.RemoveIfExists("process-tomorrow-medications");
        RecurringJob.RemoveIfExists("medication-reminders-all");
        RecurringJob.RemoveIfExists("medication-reminders-upcoming");
        RecurringJob.RemoveIfExists("medication-overdue-alerts");
        RecurringJob.RemoveIfExists("medication-low-stock-alerts");
        RecurringJob.RemoveIfExists("medication-cleanup-full");
        RecurringJob.RemoveIfExists("medication-cleanup-expired");
        RecurringJob.RemoveIfExists("medication-cleanup-notifications");
        RecurringJob.RemoveIfExists("medication-cleanup-administration");
        
        // Health event jobs
        RecurringJob.RemoveIfExists("health-events-all");
        RecurringJob.RemoveIfExists("health-event-reminders");
        RecurringJob.RemoveIfExists("health-event-cleanup");
    }

    public static void ScheduleOneTimeJobs()
    {
        // One-time jobs for immediate execution
        BackgroundJob.Enqueue<IMedicationScheduleJob>(
            job => job.ProcessApprovedToActiveTransitionAsync());

        BackgroundJob.Enqueue<IMedicationReminderJob>(
            job => job.ProcessAllRemindersAsync());

        BackgroundJob.Enqueue<IHealthEventJob>(
            job => job.ProcessAllHealthEventsAsync());
    }

    public static void ScheduleCriticalPriorityJobs()
    {
        // Critical priority jobs - every 2 minutes for more frequent processing
        RecurringJob.AddOrUpdate<IMedicationReminderJob>(
            "critical-medication-reminders",
            job => job.SendImmediateRemindersAsync(),
            "*/2 * * * *",
            TimeZoneInfo.Local,
            "critical");

        // Critical health event escalation - every 2 minutes
        RecurringJob.AddOrUpdate<IHealthEventJob>(
            "critical-health-events",
            job => job.EscalatePendingEventsAsync(),
            "*/2 * * * *",
            TimeZoneInfo.Local,
            "critical");
    }

    public static void ScheduleDevelopmentJobs()
    {
        // Lighter schedule for development environment
        RecurringJob.AddOrUpdate<IMedicationScheduleJob>(
            "dev-process-medications",
            job => job.ProcessTodayMedicationsAsync(),
            "*/2 * * * *",
            TimeZoneInfo.Local,
            "default");

        RecurringJob.AddOrUpdate<IHealthEventJob>(
            "dev-health-events",
            job => job.ProcessAllHealthEventsAsync(),
            "*/2 * * * *",
            TimeZoneInfo.Local,
            "default");

        RecurringJob.AddOrUpdate<IHealthEventJob>(
            "dev-health-event-reminders",
            job => job.SendReminderNotificationsAsync(),
            "*/5 * * * *",
            TimeZoneInfo.Local,
            "high");

        RecurringJob.AddOrUpdate<IHealthEventJob>(
            "dev-health-event-cleanup",
            job => job.CleanupOldCompletedEventsAsync(),
            "0 1 * * *",
            TimeZoneInfo.Local,
            "low");

        RecurringJob.AddOrUpdate<IMedicationReminderJob>(
            "dev-reminders",
            job => job.ProcessAllRemindersAsync(),
            Cron.Minutely,
            TimeZoneInfo.Local,
            "high");

        RecurringJob.AddOrUpdate<IMedicationReminderJob>(
            "dev-reminders-upcoming",
            job => job.SendUpcomingRemindersAsync(),
            "*/2 * * * *",
            TimeZoneInfo.Local,
            "high");

        RecurringJob.AddOrUpdate<IMedicationReminderJob>(
            "dev-overdue-alerts",
            job => job.SendOverdueAlertsAsync(),
            "*/5 * * * *",
            TimeZoneInfo.Local,
            "high");

        RecurringJob.AddOrUpdate<IMedicationReminderJob>(
            "dev-low-stock-alerts",
            job => job.CheckLowStockAlertsAsync(),
            "0 */4 * * *",
            TimeZoneInfo.Local,
            "default");

        RecurringJob.AddOrUpdate<IMedicationCleanupJob>(
            "dev-cleanup-expired",
            job => job.CleanupExpiredMedicationsAsync(),
            "30 1 * * *",
            TimeZoneInfo.Local,
            "low");

        RecurringJob.AddOrUpdate<IMedicationCleanupJob>(
            "dev-cleanup-notifications",
            job => job.CleanupOldNotificationsAsync(),
            "0 2 * * *",
            TimeZoneInfo.Local,
            "low");

        RecurringJob.AddOrUpdate<IMedicationCleanupJob>(
            "dev-cleanup-administration",
            job => job.CleanupOldAdministrationRecordsAsync(),
            "30 2 * * *",
            TimeZoneInfo.Local,
            "low");
    }

    public static void ScheduleJobsBasedOnEnvironment(string environment)
    {
        if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("🔧 Scheduling Development Hangfire jobs...");
            ScheduleDevelopmentJobs();
        }
        else
        {
            Console.WriteLine("🚀 Scheduling Production Hangfire jobs...");
            ScheduleAllRecurringJobs();
        }
    }

    public static void ScheduleFrequentJobs()
    {
        // For very time-sensitive operations - every minute
        RecurringJob.AddOrUpdate<IMedicationScheduleJob>(
            "frequent-medication-check",
            job => job.ProcessApprovedToActiveTransitionAsync(),
            Cron.Minutely,
            TimeZoneInfo.Local,
            "high");
    }

    public static void TestJobScheduling()
    {
        BackgroundJob.Enqueue(() => Console.WriteLine($"Test job executed at {DateTime.Now}"));
    }
}