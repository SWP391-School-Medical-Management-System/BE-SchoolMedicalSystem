using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolMedicalManagementSystem.BusinessLogicLayer.ServiceContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.HangFire;

public class MedicationScheduleJob : IMedicationScheduleJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MedicationScheduleJob> _logger;

    public MedicationScheduleJob(
        IServiceProvider serviceProvider,
        ILogger<MedicationScheduleJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ProcessTodayMedicationsAsync()
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
    public async Task ProcessTomorrowMedicationsAsync()
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
    public async Task ProcessNewlyApprovedMedicationsAsync()
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
                        _logger.LogInformation(
                            "Generated full schedule for newly approved medication {MedicationName} - {Count} schedules",
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

    public async Task ProcessApprovedToActiveTransitionAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            var today = DateTime.Today;

            var medicationsToActivate = await unitOfWork.GetRepositoryByEntity<StudentMedication>()
                .GetQueryable()
                .Where(sm => sm.Status == StudentMedicationStatus.Approved &&
                             sm.StartDate.HasValue && sm.EndDate.HasValue &&
                             sm.StartDate.Value.Date <= today &&
                             sm.EndDate.Value.Date >= today &&
                             !sm.IsDeleted)
                .Take(50)
                .ToListAsync();

            if (!medicationsToActivate.Any()) return;

            _logger.LogInformation("Found {Count} approved medications ready to activate",
                medicationsToActivate.Count);

            foreach (var medication in medicationsToActivate)
            {
                try
                {
                    medication.Status = StudentMedicationStatus.Active;
                    medication.LastUpdatedBy = "SYSTEM";
                    medication.LastUpdatedDate = DateTime.Now;

                    _logger.LogInformation(
                        "Activated medication {MedicationId} for student {StudentId} - {MedicationName}",
                        medication.Id, medication.StudentId, medication.MedicationName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error activating medication {MedicationId}", medication.Id);
                }
            }

            await unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessApprovedToActiveTransitionAsync");
        }
    }

    #region Helper Methods

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
                _logger.LogWarning(ex, "Error parsing SkipDates for medication {MedicationId}, ignoring",
                    medication.Id);
            }
        }

        return true;
    }

    #endregion
}