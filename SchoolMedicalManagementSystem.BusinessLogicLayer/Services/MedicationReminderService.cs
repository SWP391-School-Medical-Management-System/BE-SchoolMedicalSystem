using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services;

public class MedicationReminderService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MedicationReminderService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public MedicationReminderService(
        IServiceProvider serviceProvider,
        ILogger<MedicationReminderService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MedicationReminderService started with 1-minute interval");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendUpcomingRemindersAsync(); // 5 phút trước
                await SendImmediateRemindersAsync(); // 1 phút trước  
                await SendOverdueAlertsAsync(); // Quá hạn
                await CheckLowStockAlertsAsync(); // Kiểm tra thuốc sắp hết
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in reminder processing");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task SendUpcomingRemindersAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            var currentTime = DateTime.Now;
            var reminderTime = currentTime.AddMinutes(5);
            var today = DateTime.Today;

            var todaySchedules = await unitOfWork.GetRepositoryByEntity<MedicationSchedule>()
                .GetQueryable()
                .Include(ms => ms.StudentMedication).ThenInclude(sm => sm.Student)
                .Include(ms => ms.StudentMedication).ThenInclude(sm => sm.Parent)
                .Where(ms => ms.Status == MedicationScheduleStatus.Pending &&
                             ms.ScheduledDate.Date == today &&
                             !ms.ReminderSent && !ms.IsDeleted)
                .Take(50)
                .ToListAsync();

            var upcomingSchedules = todaySchedules
                .Where(ms =>
                {
                    var scheduledDateTime = ms.ScheduledDate.Add(ms.ScheduledTime);
                    return scheduledDateTime >= currentTime && scheduledDateTime <= reminderTime;
                })
                .OrderBy(ms => ms.StudentMedication.Priority) // Sắp xếp theo priority
                .ToList();

            if (upcomingSchedules.Any())
            {
                await CreateReminderNotificationsAsync(upcomingSchedules, unitOfWork, "upcoming");
                _logger.LogInformation("Sent {Count} upcoming reminders", upcomingSchedules.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending upcoming reminders");
        }
    }

    private async Task SendImmediateRemindersAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            var currentTime = DateTime.Now;
            var immediateTime = currentTime.AddMinutes(1);
            var today = DateTime.Today;

            var todaySchedules = await unitOfWork.GetRepositoryByEntity<MedicationSchedule>()
                .GetQueryable()
                .Include(ms => ms.StudentMedication).ThenInclude(sm => sm.Student)
                .Include(ms => ms.StudentMedication).ThenInclude(sm => sm.Parent)
                .Where(ms => ms.Status == MedicationScheduleStatus.Pending &&
                             ms.ScheduledDate.Date == today &&
                             ms.ReminderCount < 2 && !ms.IsDeleted) // Chỉ gửi tối đa 2 lần
                .Take(30)
                .ToListAsync();

            var immediateSchedules = todaySchedules
                .Where(ms =>
                {
                    var scheduledDateTime = ms.ScheduledDate.Add(ms.ScheduledTime);
                    return scheduledDateTime >= currentTime && scheduledDateTime <= immediateTime;
                })
                .Where(ms => ms.StudentMedication.Priority >= MedicationPriority.High) // Chỉ thuốc quan trọng
                .ToList();

            if (immediateSchedules.Any())
            {
                await CreateReminderNotificationsAsync(immediateSchedules, unitOfWork, "immediate");
                _logger.LogInformation("Sent {Count} immediate reminders", immediateSchedules.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending immediate reminders");
        }
    }

    private async Task SendOverdueAlertsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var scheduleService = scope.ServiceProvider.GetRequiredService<IMedicationScheduleService>();

        try
        {
            var result = await scheduleService.AutoMarkOverdueSchedulesAsync();

            if (result.Success && result.Data.SuccessCount > 0)
            {
                _logger.LogInformation("Auto-marked {Count} overdue schedules", result.Data.SuccessCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in overdue processing");
        }
    }

    // Kiểm tra thuốc sắp hết
    private async Task CheckLowStockAlertsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            var lowStockMedications = await unitOfWork.GetRepositoryByEntity<StudentMedication>()
                .GetQueryable()
                .Include(sm => sm.Student)
                .Include(sm => sm.Parent)
                .Where(sm => sm.Status == StudentMedicationStatus.Active &&
                             sm.RemainingDoses <= 3 &&
                             !sm.LowStockAlertSent && !sm.IsDeleted)
                .Take(20)
                .ToListAsync();

            foreach (var medication in lowStockMedications)
            {
                await CreateLowStockNotificationAsync(medication, unitOfWork);

                medication.LowStockAlertSent = true;
                medication.LastUpdatedBy = "SYSTEM";
                medication.LastUpdatedDate = DateTime.Now;
            }

            if (lowStockMedications.Any())
            {
                await unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Sent {Count} low stock alerts", lowStockMedications.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking low stock");
        }
    }

    private async Task CreateReminderNotificationsAsync(List<MedicationSchedule> schedules,
        IUnitOfWork unitOfWork, string reminderType)
    {
        var notificationRepo = unitOfWork.GetRepositoryByEntity<Notification>();
        var notifications = new List<Notification>();

        foreach (var schedule in schedules)
        {
            // Thông báo cho Parent
            if (schedule.StudentMedication?.ParentId != null)
            {
                var urgencyText = GetUrgencyText(schedule.StudentMedication.Priority, reminderType);

                var parentNotification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = $"{urgencyText} - Nhắc nhở uống thuốc",
                    Content = $"Con em {schedule.StudentMedication.Student?.FullName} " +
                              $"cần uống thuốc '{schedule.StudentMedication.MedicationName}' " +
                              $"vào lúc {schedule.ScheduledTime:hh\\:mm}. " +
                              $"Liều lượng: {schedule.ScheduledDosage}. " +
                              $"Mức độ ưu tiên: {GetPriorityText(schedule.StudentMedication.Priority)}",
                    NotificationType = NotificationType.General,
                    SenderId = null,
                    RecipientId = schedule.StudentMedication.ParentId,
                    RequiresConfirmation = false,
                    IsRead = false,
                    CreatedDate = DateTime.Now,
                    EndDate = DateTime.Now.AddHours(4)
                };

                notifications.Add(parentNotification);
            }

            // Thông báo cho School Nurses với priority cao
            if (schedule.StudentMedication.Priority >= MedicationPriority.High)
            {
                var nurses = await GetActiveSchoolNursesAsync(unitOfWork);
                foreach (var nurse in nurses)
                {
                    var nurseNotification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        Title = $"🚨 {GetUrgencyText(schedule.StudentMedication.Priority, reminderType)}",
                        Content = $"Học sinh {schedule.StudentMedication?.Student?.FullName} " +
                                  $"({schedule.StudentMedication?.Student?.StudentCode}) " +
                                  $"cần uống thuốc QUAN TRỌNG '{schedule.StudentMedication?.MedicationName}' " +
                                  $"vào lúc {schedule.ScheduledTime:hh\\:mm}. " +
                                  $"Priority: {GetPriorityText(schedule.StudentMedication.Priority)}",
                        NotificationType = NotificationType.General,
                        SenderId = null,
                        RecipientId = nurse.Id,
                        RequiresConfirmation = false,
                        IsRead = false,
                        CreatedDate = DateTime.Now,
                        EndDate = DateTime.Now.AddHours(4)
                    };

                    notifications.Add(nurseNotification);
                }
            }

            schedule.ReminderSent = true;
            schedule.ReminderSentAt = DateTime.Now;
            schedule.ReminderCount++;
            schedule.LastUpdatedBy = "SYSTEM";
            schedule.LastUpdatedDate = DateTime.Now;
        }

        if (notifications.Any())
        {
            await notificationRepo.AddRangeAsync(notifications);
            await unitOfWork.SaveChangesAsync();
        }
    }

    private async Task CreateLowStockNotificationAsync(StudentMedication medication, IUnitOfWork unitOfWork)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            Title = "⚠️ Thuốc sắp hết - Cần bổ sung",
            Content = $"Thuốc '{medication.MedicationName}' của con em {medication.Student?.FullName} " +
                      $"chỉ còn {medication.RemainingDoses} liều. " +
                      $"Vui lòng chuẩn bị bổ sung thuốc để đảm bảo liên tục điều trị.",
            NotificationType = NotificationType.General,
            SenderId = null,
            RecipientId = medication.ParentId,
            RequiresConfirmation = true,
            IsRead = false,
            CreatedDate = DateTime.Now,
            EndDate = DateTime.Now.AddDays(7)
        };

        await unitOfWork.GetRepositoryByEntity<Notification>().AddAsync(notification);
    }

    private async Task<List<ApplicationUser>> GetActiveSchoolNursesAsync(IUnitOfWork unitOfWork)
    {
        return await unitOfWork.GetRepositoryByEntity<ApplicationUser>()
            .GetQueryable()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "SCHOOLNURSE") &&
                        u.IsActive && !u.IsDeleted)
            .ToListAsync();
    }

    private string GetUrgencyText(MedicationPriority priority, string reminderType)
    {
        return (priority, reminderType) switch
        {
            (MedicationPriority.Critical, "immediate") => "🚨 KHẨN CẤP",
            (MedicationPriority.Critical, _) => "🚨 RẤT QUAN TRỌNG",
            (MedicationPriority.High, "immediate") => "⚠️ QUAN TRỌNG",
            (MedicationPriority.High, _) => "⚠️ Ưu tiên cao",
            _ => "📅 Nhắc nhở"
        };
    }

    private string GetPriorityText(MedicationPriority priority)
    {
        return priority switch
        {
            MedicationPriority.Critical => "Rất quan trọng",
            MedicationPriority.High => "Quan trọng",
            MedicationPriority.Normal => "Bình thường",
            MedicationPriority.Low => "Thấp",
            _ => "Không xác định"
        };
    }
}