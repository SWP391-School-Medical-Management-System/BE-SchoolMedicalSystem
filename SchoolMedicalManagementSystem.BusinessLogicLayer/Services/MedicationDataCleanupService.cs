using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.BaseResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;
using Microsoft.EntityFrameworkCore;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicationScheduleResponse;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.BackgroundServices;

/// <summary>
/// Background Service for periodic cleanup of old medication-related data
/// </summary>
public class MedicationDataCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MedicationDataCleanupService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6);
    private readonly int _defaultRetentionDays = 30;

    public MedicationDataCleanupService(
        IServiceProvider serviceProvider,
        ILogger<MedicationDataCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MedicationDataCleanupService started with 6-hour interval");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Chạy cleanup vào lúc ít hoạt động (2:00 AM hoặc 8:00 PM)
                var currentHour = DateTime.Now.Hour;
                if (currentHour == 2 || currentHour == 20)
                {
                    await CleanupExpiredDataAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in data cleanup process");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CleanupExpiredDataAsync()
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
            var medicationResult = await CleanupExpiredMedicationsAsync(unitOfWork);
            if (medicationResult.Success)
            {
                _logger.LogInformation("Cleaned up {Count} expired student medications",
                    medicationResult.Data.RecordsDeleted);
            }

            // Cleanup old notifications (30 days old)
            var notificationResult = await CleanupOldNotificationsAsync(unitOfWork);
            if (notificationResult.Success)
            {
                _logger.LogInformation("Cleaned up {Count} old notifications",
                    notificationResult.Data.RecordsDeleted);
            }

            // Cleanup old administration records
            var administrationResult = await CleanupOldAdministrationRecordsAsync(unitOfWork);
            if (administrationResult.Success)
            {
                _logger.LogInformation("Cleaned up {Count} old administration records",
                    administrationResult.Data.RecordsDeleted);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup process");
        }
    }

    private async Task<BaseResponse<CleanupOperationResponse>> CleanupExpiredMedicationsAsync(IUnitOfWork unitOfWork)
    {
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
                // Kiểm tra không còn schedules pending
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

            var response = new CleanupOperationResponse
            {
                RecordsProcessed = expiredMedications.Count,
                RecordsDeleted = deletedCount,
                CleanupDate = DateTime.Now
            };

            return BaseResponse<CleanupOperationResponse>.SuccessResult(response,
                $"Cleaned up {deletedCount} expired medications");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired medications");
            return BaseResponse<CleanupOperationResponse>.ErrorResult($"Error cleaning medications: {ex.Message}");
        }
    }

    private async Task<BaseResponse<CleanupOperationResponse>> CleanupOldNotificationsAsync(IUnitOfWork unitOfWork)
    {
        try
        {
            var cutoffDate = DateTime.Today.AddDays(-_defaultRetentionDays);

            var oldNotifications = await unitOfWork.GetRepositoryByEntity<Notification>()
                .GetQueryable()
                .Where(n => n.CreatedDate < cutoffDate &&
                            n.IsRead == true && // Chỉ xóa thông báo đã đọc
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

            var response = new CleanupOperationResponse
            {
                RecordsProcessed = oldNotifications.Count,
                RecordsDeleted = deletedCount,
                CleanupDate = DateTime.Now
            };

            return BaseResponse<CleanupOperationResponse>.SuccessResult(response,
                $"Cleaned up {deletedCount} old notifications");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old notifications");
            return BaseResponse<CleanupOperationResponse>.ErrorResult($"Error cleaning notifications: {ex.Message}");
        }
    }

    // Cleanup old administration records
    private async Task<BaseResponse<CleanupOperationResponse>> CleanupOldAdministrationRecordsAsync(
        IUnitOfWork unitOfWork)
    {
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

            var response = new CleanupOperationResponse
            {
                RecordsProcessed = oldAdministrations.Count,
                RecordsDeleted = deletedCount,
                CleanupDate = DateTime.Now
            };

            return BaseResponse<CleanupOperationResponse>.SuccessResult(response,
                $"Cleaned up {deletedCount} old administration records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old administration records");
            return BaseResponse<CleanupOperationResponse>.ErrorResult(
                $"Error cleaning administration records: {ex.Message}");
        }
    }
}