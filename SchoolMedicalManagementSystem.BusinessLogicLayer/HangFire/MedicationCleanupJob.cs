using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.HangFire;

public class MedicationCleanupJob : IMedicationCleanupJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MedicationCleanupJob> _logger;
    private readonly int _defaultRetentionDays = 30;

    public MedicationCleanupJob(
        IServiceProvider serviceProvider,
        ILogger<MedicationCleanupJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task CleanupExpiredDataAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var scheduleService = scope.ServiceProvider.GetRequiredService<IMedicationScheduleService>();

        try
        {
            _logger.LogInformation("Starting comprehensive medication data cleanup");

            // Cleanup old schedules (30 days old)
            var scheduleResult = await scheduleService.CleanupOldSchedulesAsync(_defaultRetentionDays);
            if (scheduleResult.Success)
            {
                _logger.LogInformation("Cleaned up {Count} old medication schedules",
                    scheduleResult.Data.RecordsDeleted);
            }

            // Cleanup expired medications (30 days after end date)
            await CleanupExpiredMedicationsAsync();

            // Cleanup old notifications (30 days old)
            await CleanupOldNotificationsAsync();

            // Cleanup old administration records
            await CleanupOldAdministrationRecordsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup process");
            throw;
        }
    }

    public async Task CleanupExpiredMedicationsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            var cutoffDate = DateTime.Today.AddDays(-_defaultRetentionDays);

            var expiredMedications = await unitOfWork.GetRepositoryByEntity<StudentMedication>()
                .GetQueryable()
                .Where(sm => sm.EndDate < cutoffDate &&
                             (sm.Status == StudentMedicationStatus.Completed ||
                              sm.Status == StudentMedicationStatus.Discontinued) &&
                             !sm.IsDeleted)
                .Take(200)
                .ToListAsync();

            var deletedCount = 0;

            foreach (var medication in expiredMedications)
            {
                var hasActiveSchedules = await unitOfWork.GetRepositoryByEntity<MedicationSchedule>()
                    .GetQueryable()
                    .AnyAsync(ms => ms.StudentMedicationId == medication.Id &&
                                    !ms.IsDeleted &&
                                    ms.Status == MedicationScheduleStatus.Pending);

                if (hasActiveSchedules) continue;

                medication.IsDeleted = true;
                medication.LastUpdatedBy = "SYSTEM_CLEANUP";
                medication.LastUpdatedDate = DateTime.Now;
                deletedCount++;
            }

            if (deletedCount > 0)
            {
                await unitOfWork.SaveChangesAsync();
            }

            _logger.LogInformation("Cleaned up {Count} expired medications", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired medications");
            throw;
        }
    }

    public async Task CleanupOldNotificationsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            var cutoffDate = DateTime.Today.AddDays(-_defaultRetentionDays);

            var oldNotifications = await unitOfWork.GetRepositoryByEntity<Notification>()
                .GetQueryable()
                .Where(n => n.CreatedDate < cutoffDate &&
                            n.IsRead == true &&
                            n.EndDate < DateTime.Now &&
                            !n.IsDeleted)
                .Take(500)
                .ToListAsync();

            var deletedCount = 0;

            foreach (var notification in oldNotifications)
            {
                notification.IsDeleted = true;
                notification.LastUpdatedBy = "SYSTEM_CLEANUP";
                notification.LastUpdatedDate = DateTime.Now;
                deletedCount++;
            }

            if (deletedCount > 0)
            {
                await unitOfWork.SaveChangesAsync();
            }

            _logger.LogInformation("Cleaned up {Count} old notifications", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old notifications");
            throw;
        }
    }

    public async Task CleanupOldAdministrationRecordsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            var cutoffDate = DateTime.Today.AddDays(-_defaultRetentionDays);

            var oldAdministrations = await unitOfWork.GetRepositoryByEntity<MedicationAdministration>()
                .GetQueryable()
                .Where(ma => ma.AdministeredAt < cutoffDate && !ma.IsDeleted)
                .Take(300)
                .ToListAsync();

            var deletedCount = 0;

            foreach (var administration in oldAdministrations)
            {
                administration.IsDeleted = true;
                administration.LastUpdatedBy = "SYSTEM_CLEANUP";
                administration.LastUpdatedDate = DateTime.Now;
                deletedCount++;
            }

            if (deletedCount > 0)
            {
                await unitOfWork.SaveChangesAsync();
            }

            _logger.LogInformation("Cleaned up {Count} old administration records", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old administration records");
            throw;
        }
    }
}