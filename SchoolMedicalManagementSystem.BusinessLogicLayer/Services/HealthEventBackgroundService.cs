using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services;

public class HealthEventBackgroundService : BackgroundService
{
    private readonly ILogger<HealthEventBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;

    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(15); // Check mỗi 15 giây
    private readonly TimeSpan _escalationThreshold = TimeSpan.FromSeconds(30); // Escalate sau 30 giây ⚡
    private readonly TimeSpan _reminderThreshold = TimeSpan.FromMinutes(2); // Reminder sau 2 phút
    private readonly TimeSpan _cleanupThreshold = TimeSpan.FromMinutes(5); // Cleanup sau 5 phút

    // Check interval and thresholds
    private readonly TimeSpan _escalationDuplicateCheck = TimeSpan.FromMinutes(3); // 3 phút cho escalation
    private readonly TimeSpan _reminderDuplicateCheck = TimeSpan.FromMinutes(2); // 2 phút cho reminder

    private double GetTotalSeconds(TimeSpan timeSpan)
    {
        return timeSpan.Days * 86400 + timeSpan.Hours * 3600 + timeSpan.Minutes * 60 + timeSpan.Seconds +
               timeSpan.Milliseconds / 1000.0;
    }

    private double GetTotalMinutes(TimeSpan timeSpan)
    {
        return GetTotalSeconds(timeSpan) / 60.0;
    }

    private DateTime GetSafeDateTime(DateTime? nullableDateTime, DateTime fallback = default)
    {
        return nullableDateTime ?? (fallback == default ? DateTime.Now : fallback);
    }

    private TimeSpan GetSafeDuration(DateTime? startDateTime, DateTime? endDateTime = null)
    {
        var start = GetSafeDateTime(startDateTime);
        var end = endDateTime ?? DateTime.Now;
        return end - start;
    }

    public HealthEventBackgroundService(
        ILogger<HealthEventBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessHealthEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Health Event Background Service");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Health Event Background Service stopped");
    }

    private async Task ProcessHealthEventsAsync(CancellationToken cancellationToken)
    {
        await EscalatePendingEventsAsync(cancellationToken);
        await SendReminderNotificationsAsync(cancellationToken);
        await CleanupOldCompletedEventsAsync(cancellationToken);
    }

    /// <summary>
    /// Escalate pending events to managers after threshold time
    /// </summary>
    private async Task EscalatePendingEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var cutoffTime = DateTime.Now.Subtract(_escalationThreshold);

            var pendingEvents = await unitOfWork.GetRepositoryByEntity<HealthEvent>()
                .GetQueryable()
                .Include(he => he.Student)
                .Where(he => he.Status == HealthEventStatus.Pending &&
                             he.HandledById == null &&
                             he.CreatedDate.HasValue &&
                             he.CreatedDate.Value <= cutoffTime &&
                             !he.IsDeleted)
                .ToListAsync(cancellationToken);

            if (pendingEvents.Any())
            {
                var managers = await unitOfWork.GetRepositoryByEntity<ApplicationUser>()
                    .GetQueryable()
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "MANAGER") &&
                                u.IsActive && !u.IsDeleted)
                    .ToListAsync(cancellationToken);

                foreach (var healthEvent in pendingEvents)
                {
                    var hasRecentEscalation = await CheckRecentEscalationNotificationAsync(unitOfWork, healthEvent.Id);

                    if (!hasRecentEscalation)
                    {
                        await CreateEscalationNotificationsAsync(unitOfWork, healthEvent, managers);

                        var createdDate = healthEvent.CreatedDate ?? DateTime.Now;
                        var duration = DateTime.Now - createdDate;

                        _logger.LogWarning("ESCALATED: Event {EventId} for student {StudentName} - " +
                                           "Created: {CreatedDate}, Escalated after: {Duration} seconds",
                            healthEvent.Id,
                            healthEvent.Student?.FullName,
                            createdDate,
                            GetTotalSeconds(duration));
                    }
                    else
                    {
                        _logger.LogDebug("Skipping escalation for event {EventId} - recent escalation already sent",
                            healthEvent.Id);
                    }
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            else
            {
                _logger.LogDebug("No pending events found for escalation");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating pending events");
        }
    }

    /// <summary>
    /// Send reminder notifications for long-running events
    /// </summary>
    private async Task SendReminderNotificationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var reminderCutoff = DateTime.Now.Subtract(_reminderThreshold);

            var longRunningEvents = await unitOfWork.GetRepositoryByEntity<HealthEvent>()
                .GetQueryable()
                .Include(he => he.Student)
                .Include(he => he.HandledBy)
                .Where(he => he.Status == HealthEventStatus.InProgress &&
                             he.AssignedAt.HasValue &&
                             he.AssignedAt.Value <= reminderCutoff &&
                             !he.IsDeleted)
                .ToListAsync(cancellationToken);

            if (longRunningEvents.Any())
            {
                _logger.LogInformation("Found {Count} long-running events (> {Threshold})",
                    longRunningEvents.Count, _reminderThreshold);
            }

            foreach (var healthEvent in longRunningEvents)
            {
                if (healthEvent.HandledBy != null)
                {
                    await CreateReminderNotificationAsync(unitOfWork, healthEvent);

                    var duration = GetSafeDuration(healthEvent.AssignedAt, DateTime.Now);
                    _logger.LogInformation("REMINDER sent for event {EventId} to nurse {NurseName} - " +
                                           "In progress for: {Duration} seconds",
                        healthEvent.Id,
                        healthEvent.HandledBy.FullName,
                        GetTotalSeconds(duration));
                }
            }

            if (longRunningEvents.Any())
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Sent {Count} reminder notifications", longRunningEvents.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reminder notifications");
        }
    }

    /// <summary>
    /// Clean up old completed event notifications
    /// </summary>
    private async Task CleanupOldCompletedEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var cleanupCutoff = DateTime.Now.Subtract(_cleanupThreshold);

            var oldNotifications = await unitOfWork.GetRepositoryByEntity<Notification>()
                .GetQueryable()
                .Where(n => n.HealthEvent != null &&
                            n.HealthEvent.Status == HealthEventStatus.Completed &&
                            n.HealthEvent.CompletedAt.HasValue &&
                            n.HealthEvent.CompletedAt.Value <= cleanupCutoff &&
                            !n.IsDeleted)
                .ToListAsync(cancellationToken);

            if (oldNotifications.Any())
            {
                _logger.LogInformation("Found {Count} old notifications for cleanup (> {Threshold})",
                    oldNotifications.Count, _cleanupThreshold);

                foreach (var notification in oldNotifications)
                {
                    notification.IsDeleted = true;
                    notification.LastUpdatedDate = DateTime.Now;
                    notification.LastUpdatedBy = "SYSTEM";
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Cleaned up {Count} old notifications", oldNotifications.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old completed events");
        }
    }

    // Check for recent escalation notifications to prevent duplicates
    private async Task<bool> CheckRecentEscalationNotificationAsync(IUnitOfWork unitOfWork, Guid healthEventId)
    {
        try
        {
            var notificationRepo = unitOfWork.GetRepositoryByEntity<Notification>();
            var recentEscalationCutoff = DateTime.Now.Subtract(_escalationDuplicateCheck);

            return await notificationRepo.GetQueryable()
                .AnyAsync(n => n.HealthEventId == healthEventId &&
                               n.Title.Contains("cần can thiệp") && // Check escalation notification
                               n.CreatedDate >= recentEscalationCutoff &&
                               !n.IsDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking recent escalation notification for event {EventId}", healthEventId);
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
            var waitTimeSeconds = GetTotalSeconds(DateTime.Now - (healthEvent.CreatedDate ?? DateTime.Now));

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = $"Sự kiện y tế cần can thiệp - {healthEvent.Student?.FullName}",
                Content = $"Sự kiện y tế #{healthEvent.Code} của học sinh {healthEvent.Student?.FullName} " +
                          $"({healthEvent.Student?.StudentCode}) đã chờ xử lý quá lâu.{Environment.NewLine}{Environment.NewLine}" +
                          $"Vị trí: {healthEvent.Location}{Environment.NewLine}" +
                          $"Thời gian xảy ra: {healthEvent.OccurredAt:dd/MM/yyyy HH:mm:ss}{Environment.NewLine}" +
                          $"Thời gian chờ: {waitTimeSeconds:F0} giây{Environment.NewLine}" +
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

        var recentReminderCutoff = DateTime.Now.Subtract(_reminderDuplicateCheck);

        var recentReminder = await notificationRepo.GetQueryable()
            .AnyAsync(n => n.HealthEventId == healthEvent.Id &&
                           n.Title.Contains("đang xử lý") && // Check reminder notification
                           n.CreatedDate >= recentReminderCutoff &&
                           !n.IsDeleted);

        if (recentReminder)
        {
            _logger.LogDebug(
                "Skipping reminder for health event {HealthEventId} - recent reminder already sent within {Duration}",
                healthEvent.Id, _reminderDuplicateCheck);
            return;
        }

        var processingDuration = GetSafeDuration(healthEvent.AssignedAt, DateTime.Now);
        var durationMinutes = GetTotalMinutes(processingDuration);
        var reminderMinutes = GetTotalMinutes(_reminderThreshold);
        var startTime = GetSafeDateTime(healthEvent.AssignedAt, GetSafeDateTime(healthEvent.CreatedDate, DateTime.Now));

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            Title = $"Sự kiện y tế đang xử lý - {healthEvent.Student?.FullName}",
            Content = $"Bạn đang xử lý sự kiện y tế của học sinh {healthEvent.Student?.FullName} " +
                      $"({healthEvent.Student?.StudentCode}) đã {durationMinutes:F1} phút.{Environment.NewLine}{Environment.NewLine}" +
                      $"Vị trí: {healthEvent.Location}{Environment.NewLine}" +
                      $"Bắt đầu xử lý: {startTime:dd/MM/yyyy HH:mm:ss}{Environment.NewLine}" +
                      $"Thời gian xử lý: {durationMinutes:F1} phút{Environment.NewLine}" +
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