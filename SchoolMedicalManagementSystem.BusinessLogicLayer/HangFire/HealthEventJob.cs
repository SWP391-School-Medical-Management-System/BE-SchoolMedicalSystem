using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.HangFire;

public class HealthEventJob : IHealthEventJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HealthEventJob> _logger;

    private readonly TimeSpan _escalationThreshold = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _reminderThreshold = TimeSpan.FromMinutes(10);
    private readonly TimeSpan _cleanupThreshold = TimeSpan.FromHours(1);
    private readonly TimeSpan _escalationDuplicateCheck = TimeSpan.FromMinutes(15);
    private readonly TimeSpan _reminderDuplicateCheck = TimeSpan.FromMinutes(30);

    public HealthEventJob(
        IServiceProvider serviceProvider,
        ILogger<HealthEventJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new int[] { 30, 60, 120 })]
    public async Task ProcessAllHealthEventsAsync()
    {
        try
        {
            if (!await IsDatabaseAvailableAsync())
            {
                _logger.LogDebug("Database not available, skipping health event processing");
                return;
            }

            await EscalatePendingEventsAsync();
            await SendReminderNotificationsAsync();
            await CleanupOldCompletedEventsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessAllHealthEventsAsync");
            throw;
        }
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task EscalatePendingEventsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            var cutoffTime = DateTime.Now.Subtract(_escalationThreshold);

            var pendingEvents = await unitOfWork.GetRepositoryByEntity<HealthEvent>()
                .GetQueryable()
                .Include(he => he.Student)
                .Where(he => he.Status == HealthEventStatus.Pending &&
                             he.HandledById == null &&
                             he.CreatedDate.HasValue &&
                             he.CreatedDate.Value <= cutoffTime &&
                             !he.IsDeleted)
                .Take(10)
                .ToListAsync();

            if (pendingEvents.Any())
            {
                _logger.LogInformation("Found {Count} pending events for escalation", pendingEvents.Count);

                var managers = await unitOfWork.GetRepositoryByEntity<ApplicationUser>()
                    .GetQueryable()
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "MANAGER") &&
                                u.IsActive && !u.IsDeleted)
                    .ToListAsync();

                if (!managers.Any())
                {
                    _logger.LogWarning("No managers found for escalation");
                    return;
                }

                foreach (var healthEvent in pendingEvents)
                {
                    var hasRecentEscalation = await CheckRecentEscalationNotificationAsync(unitOfWork, healthEvent.Id);

                    if (!hasRecentEscalation)
                    {
                        await CreateEscalationNotificationsAsync(unitOfWork, healthEvent, managers);

                        var createdDate = healthEvent.CreatedDate ?? DateTime.Now;
                        var duration = DateTime.Now - createdDate;

                        _logger.LogWarning("ESCALATED: Event {EventId} for student {StudentName} - " +
                                           "Created: {CreatedDate}, Escalated after: {Duration}",
                            healthEvent.Id,
                            healthEvent.Student?.FullName,
                            createdDate,
                            duration);
                    }
                }

                await unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Escalation processing completed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating pending events");
            throw;
        }
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task SendReminderNotificationsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            var reminderCutoff = DateTime.Now.Subtract(_reminderThreshold);

            var longRunningEvents = await unitOfWork.GetRepositoryByEntity<HealthEvent>()
                .GetQueryable()
                .Include(he => he.Student)
                .Include(he => he.HandledBy)
                .Where(he => he.Status == HealthEventStatus.InProgress &&
                             he.AssignedAt.HasValue &&
                             he.AssignedAt.Value <= reminderCutoff &&
                             !he.IsDeleted)
                .Take(10)
                .ToListAsync();

            if (longRunningEvents.Any())
            {
                _logger.LogInformation("Found {Count} long-running events for reminders", longRunningEvents.Count);

                foreach (var healthEvent in longRunningEvents)
                {
                    if (healthEvent.HandledBy != null)
                    {
                        var hasRecentReminder = await CheckRecentReminderNotificationAsync(unitOfWork, healthEvent.Id);

                        if (!hasRecentReminder)
                        {
                            await CreateReminderNotificationAsync(unitOfWork, healthEvent);

                            var duration = DateTime.Now - (healthEvent.AssignedAt ?? DateTime.Now);
                            _logger.LogInformation("REMINDER sent for event {EventId} to nurse {NurseName} - " +
                                                   "In progress for: {Duration}",
                                healthEvent.Id,
                                healthEvent.HandledBy.FullName,
                                duration);
                        }
                    }
                }

                await unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Reminder processing completed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reminder notifications");
            throw;
        }
    }

    [AutomaticRetry(Attempts = 1)]
    public async Task CleanupOldCompletedEventsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            var cleanupCutoff = DateTime.Now.Subtract(_cleanupThreshold);

            var oldNotifications = await unitOfWork.GetRepositoryByEntity<Notification>()
                .GetQueryable()
                .Where(n => n.HealthEvent != null &&
                            n.HealthEvent.Status == HealthEventStatus.Completed &&
                            n.HealthEvent.CompletedAt.HasValue &&
                            n.HealthEvent.CompletedAt.Value <= cleanupCutoff &&
                            !n.IsDeleted)
                .Take(50)
                .ToListAsync();

            if (oldNotifications.Any())
            {
                _logger.LogInformation("Cleaning up {Count} old health event notifications", oldNotifications.Count);

                foreach (var notification in oldNotifications)
                {
                    notification.IsDeleted = true;
                    notification.LastUpdatedDate = DateTime.Now;
                    notification.LastUpdatedBy = "SYSTEM";
                }

                await unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Health event cleanup completed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old completed events");
            throw;
        }
    }
    
    private async Task<bool> IsDatabaseAvailableAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await unitOfWork.GetRepositoryByEntity<HealthEvent>()
                .GetQueryable()
                .Take(1)
                .AnyAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Database connectivity check failed: {Message}", ex.Message);
            return false;
        }
    }

    private async Task<bool> CheckRecentEscalationNotificationAsync(IUnitOfWork unitOfWork, Guid healthEventId)
    {
        try
        {
            var recentEscalationCutoff = DateTime.Now.Subtract(_escalationDuplicateCheck);

            return await unitOfWork.GetRepositoryByEntity<Notification>()
                .GetQueryable()
                .AnyAsync(n => n.HealthEventId == healthEventId &&
                               n.Title.Contains("cần can thiệp") &&
                               n.CreatedDate >= recentEscalationCutoff &&
                               !n.IsDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking recent escalation notification for event {EventId}", healthEventId);
            return false;
        }
    }

    private async Task<bool> CheckRecentReminderNotificationAsync(IUnitOfWork unitOfWork, Guid healthEventId)
    {
        try
        {
            var recentReminderCutoff = DateTime.Now.Subtract(_reminderDuplicateCheck);

            return await unitOfWork.GetRepositoryByEntity<Notification>()
                .GetQueryable()
                .AnyAsync(n => n.HealthEventId == healthEventId &&
                               n.Title.Contains("đang xử lý") &&
                               n.CreatedDate >= recentReminderCutoff &&
                               !n.IsDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking recent reminder notification for event {EventId}", healthEventId);
            return false;
        }
    }

    private async Task CreateEscalationNotificationsAsync(
        IUnitOfWork unitOfWork,
        HealthEvent healthEvent,
        List<ApplicationUser> managers)
    {
        var notificationRepo = unitOfWork.GetRepositoryByEntity<Notification>();

        foreach (var manager in managers)
        {
            var waitTime = DateTime.Now - (healthEvent.CreatedDate ?? DateTime.Now);

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = $"Sự kiện y tế cần can thiệp - {healthEvent.Student?.FullName}",
                Content = $"Sự kiện y tế #{healthEvent.Code} của học sinh {healthEvent.Student?.FullName} " +
                          $"({healthEvent.Student?.StudentCode}) đã chờ xử lý quá lâu.{Environment.NewLine}{Environment.NewLine}" +
                          $"Vị trí: {healthEvent.Location}{Environment.NewLine}" +
                          $"Thời gian xảy ra: {healthEvent.OccurredAt:dd/MM/yyyy HH:mm:ss}{Environment.NewLine}" +
                          $"Thời gian chờ: {waitTime:hh\\:mm\\:ss}{Environment.NewLine}" +
                          $"Mô tả: {healthEvent.Description}{Environment.NewLine}" +
                          $"Mức độ: {(healthEvent.IsEmergency ? "KHẨN CẤP" : "Bình thường")}{Environment.NewLine}{Environment.NewLine}" +
                          "Vui lòng phân công ngay cho School Nurse phù hợp.",
                NotificationType = NotificationType.HealthEvent,
                SenderId = null,
                RecipientId = manager.Id,
                HealthEventId = healthEvent.Id,
                RequiresConfirmation = true,
                IsRead = false,
                IsConfirmed = false,
                CreatedDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(2)
            };

            await notificationRepo.AddAsync(notification);
        }

        _logger.LogInformation("Created escalation notifications for {ManagerCount} managers, event {EventId}",
            managers.Count, healthEvent.Id);
    }

    private async Task CreateReminderNotificationAsync(IUnitOfWork unitOfWork, HealthEvent healthEvent)
    {
        var notificationRepo = unitOfWork.GetRepositoryByEntity<Notification>();
        var processingDuration = DateTime.Now - (healthEvent.AssignedAt ?? DateTime.Now);
        var startTime = healthEvent.AssignedAt ?? healthEvent.CreatedDate ?? DateTime.Now;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            Title = $"Sự kiện y tế đang xử lý - {healthEvent.Student?.FullName}",
            Content = $"Bạn đang xử lý sự kiện y tế của học sinh {healthEvent.Student?.FullName} " +
                      $"({healthEvent.Student?.StudentCode}) đã {processingDuration:hh\\:mm\\:ss}.{Environment.NewLine}{Environment.NewLine}" +
                      $"Vị trí: {healthEvent.Location}{Environment.NewLine}" +
                      $"Bắt đầu xử lý: {startTime:dd/MM/yyyy HH:mm:ss}{Environment.NewLine}" +
                      $"Thời gian xử lý: {processingDuration:hh\\:mm\\:ss}{Environment.NewLine}" +
                      $"Mô tả: {healthEvent.Description}{Environment.NewLine}{Environment.NewLine}" +
                      "Vui lòng cập nhật tiến trình hoặc hoàn thành xử lý.",
            NotificationType = NotificationType.HealthEvent,
            SenderId = null,
            RecipientId = healthEvent.HandledById.Value,
            HealthEventId = healthEvent.Id,
            RequiresConfirmation = false,
            IsRead = false,
            IsConfirmed = false,
            CreatedDate = DateTime.Now,
            EndDate = DateTime.Now.AddHours(1)
        };

        await notificationRepo.AddAsync(notification);

        _logger.LogInformation("Created reminder notification for nurse {NurseId}, event {EventId}",
            healthEvent.HandledById.Value, healthEvent.Id);
    }
}