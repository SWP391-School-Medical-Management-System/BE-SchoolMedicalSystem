using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;
using Microsoft.EntityFrameworkCore;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.BackgroundServices;

/// <summary>
/// Background Service tự động tạo lịch trình uống thuốc khi StudentMedication được approve
/// </summary>
public class MedicationScheduleGeneratorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MedicationScheduleGeneratorService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

    public MedicationScheduleGeneratorService(
        IServiceProvider serviceProvider,
        ILogger<MedicationScheduleGeneratorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MedicationScheduleGeneratorService started with 30-second interval");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessTodayMedicationsAsync();
                await ProcessTomorrowMedicationsAsync();
                await ProcessNewlyApprovedMedicationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in schedule generation");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    // Xử lý medication cần schedule cho hôm nay
    private async Task ProcessTodayMedicationsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var scheduleService = scope.ServiceProvider.GetRequiredService<IMedicationScheduleService>();

        try
        {
            var today = DateTime.Today;
            
            var medicationsNeedSchedule = await unitOfWork.GetRepositoryByEntity<StudentMedication>()
                .GetQueryable()
                .Include(sm => sm.Schedules)
                .Where(sm => sm.Status == StudentMedicationStatus.Active &&
                             sm.AutoGenerateSchedule &&
                             sm.StartDate <= today &&
                             sm.EndDate >= today &&
                             !sm.IsDeleted &&
                             !sm.Schedules.Any(s => s.ScheduledDate.Date == today && !s.IsDeleted))
                .Take(10)
                .ToListAsync();

            if (!medicationsNeedSchedule.Any()) return;

            _logger.LogInformation("Found {Count} medications needing today's schedule generation", 
                medicationsNeedSchedule.Count);

            foreach (var medication in medicationsNeedSchedule)
            {
                try
                {
                    var result = await scheduleService.GenerateSchedulesForMedicationAsync(
                        medication.Id, today, today);

                    if (result.Success)
                    {
                        _logger.LogInformation("Generated {Count} schedules for medication {MedicationName}",
                            result.Data.SuccessCount, medication.MedicationName);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to generate schedules for {MedicationName}: {Error}",
                            medication.MedicationName, result.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating schedules for medication {MedicationId}", medication.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessTodayMedicationsAsync");
        }
    }

    // Xử lý medication cần schedule cho ngày mai (chỉ chạy sau 18h)
    private async Task ProcessTomorrowMedicationsAsync()
    {
        // Chỉ tạo schedule cho ngày mai sau 18h
        if (DateTime.Now.Hour < 18) return;

        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var scheduleService = scope.ServiceProvider.GetRequiredService<IMedicationScheduleService>();

        try
        {
            var tomorrow = DateTime.Today.AddDays(1);

            var medicationsNeedTomorrowSchedule = await unitOfWork.GetRepositoryByEntity<StudentMedication>()
                .GetQueryable()
                .Include(sm => sm.Schedules)
                .Where(sm => sm.Status == StudentMedicationStatus.Active &&
                             sm.AutoGenerateSchedule &&
                             sm.StartDate <= tomorrow &&
                             sm.EndDate >= tomorrow &&
                             !sm.IsDeleted &&
                             !sm.Schedules.Any(s => s.ScheduledDate.Date == tomorrow && !s.IsDeleted))
                .Take(10)
                .ToListAsync();

            foreach (var medication in medicationsNeedTomorrowSchedule)
            {
                // Kiểm tra đơn giản có nên tạo schedule cho ngày mai không
                if (ShouldCreateScheduleForDate(medication, tomorrow))
                {
                    var result = await scheduleService.GenerateSchedulesForMedicationAsync(
                        medication.Id, tomorrow, tomorrow);

                    if (result.Success)
                    {
                        _logger.LogInformation("Generated tomorrow schedule for {MedicationName}", 
                            medication.MedicationName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating tomorrow schedules");
        }
    }

    // Xử lý medication vừa được approve (trong 10 phút gần đây)
    private async Task ProcessNewlyApprovedMedicationsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var scheduleService = scope.ServiceProvider.GetRequiredService<IMedicationScheduleService>();

        try
        {
            var today = DateTime.Today;
            var tenMinutesAgo = DateTime.Now.AddMinutes(-10);
            
            var recentlyApproved = await unitOfWork.GetRepositoryByEntity<StudentMedication>()
                .GetQueryable()
                .Include(sm => sm.Schedules)
                .Where(sm => sm.Status == StudentMedicationStatus.Active &&
                             sm.ApprovedAt.HasValue &&
                             sm.ApprovedAt.Value >= tenMinutesAgo &&
                             sm.AutoGenerateSchedule &&
                             sm.StartDate <= today &&
                             sm.EndDate >= today &&
                             !sm.IsDeleted)
                .Take(5)
                .ToListAsync();

            foreach (var medication in recentlyApproved)
            {
                try
                {
                    var result = await scheduleService.GenerateSchedulesForMedicationAsync(
                        medication.Id, today, medication.EndDate);

                    if (result.Success)
                    {
                        _logger.LogInformation("Generated full schedule for newly approved medication {MedicationName} - {Count} schedules",
                            medication.MedicationName, result.Data.SuccessCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating schedule for newly approved medication {MedicationId}", 
                        medication.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessNewlyApprovedMedicationsAsync");
        }
    }

    private bool ShouldCreateScheduleForDate(StudentMedication medication, DateTime date)
    {
        if (medication.SkipWeekends && 
            (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday))
            return false;

        if (!string.IsNullOrEmpty(medication.SkipDates))
        {
            try
            {
                var skipDates = System.Text.Json.JsonSerializer.Deserialize<List<string>>(medication.SkipDates);
                var dateString = date.ToString("yyyy-MM-dd");
                if (skipDates?.Contains(dateString) == true)
                    return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing SkipDates for medication {MedicationId}, ignoring", medication.Id);
            }
        }

        return true;
    }
}