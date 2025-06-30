using AutoMapper;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicationScheduleResponse;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Mappers;

public class MedicationScheduleMappingProfile : Profile
{
    public MedicationScheduleMappingProfile()
    {
        CreateMap<MedicationSchedule, MedicationScheduleResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.StudentMedicationId, opt => opt.MapFrom(src => src.StudentMedicationId))
            .ForMember(dest => dest.ScheduledDate, opt => opt.MapFrom(src => src.ScheduledDate))
            .ForMember(dest => dest.ScheduledTime, opt => opt.MapFrom(src => src.ScheduledTime))
            .ForMember(dest => dest.ScheduledDosage, opt => opt.MapFrom(src => src.ScheduledDosage))

            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.StatusDisplayName, opt => opt.MapFrom(src => GetStatusDisplayName(src.Status)))
            .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
            .ForMember(dest => dest.PriorityDisplayName,
                opt => opt.MapFrom(src => GetPriorityDisplayName(src.Priority)))

            .ForMember(dest => dest.AdministrationId, opt => opt.MapFrom(src => src.AdministrationId))
            .ForMember(dest => dest.CompletedAt, opt => opt.MapFrom(src => src.CompletedAt))
            .ForMember(dest => dest.MissedAt, opt => opt.MapFrom(src => src.MissedAt))
            .ForMember(dest => dest.MissedReason, opt => opt.MapFrom(src => src.MissedReason))
            .ForMember(dest => dest.StudentPresent, opt => opt.MapFrom(src => src.StudentPresent))
            .ForMember(dest => dest.AttendanceCheckedAt, opt => opt.MapFrom(src => src.AttendanceCheckedAt))

            .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes))
            .ForMember(dest => dest.SpecialInstructions, opt => opt.MapFrom(src => src.SpecialInstructions))

            .ForMember(dest => dest.ReminderSent, opt => opt.MapFrom(src => src.ReminderSent))
            .ForMember(dest => dest.ReminderSentAt, opt => opt.MapFrom(src => src.ReminderSentAt))
            .ForMember(dest => dest.ReminderCount, opt => opt.MapFrom(src => src.ReminderCount))

            .ForMember(dest => dest.RequiresNurseConfirmation, opt => opt.MapFrom(src => src.RequiresNurseConfirmation))
            .ForMember(dest => dest.ConfirmedByNurseId, opt => opt.MapFrom(src => src.ConfirmedByNurseId))
            .ForMember(dest => dest.ConfirmedAt, opt => opt.MapFrom(src => src.ConfirmedAt))

            .ForMember(dest => dest.MedicationName, opt => opt.MapFrom(src =>
                src.StudentMedication != null ? src.StudentMedication.MedicationName : ""))
            .ForMember(dest => dest.MedicationPurpose, opt => opt.MapFrom(src =>
                src.StudentMedication != null ? src.StudentMedication.Purpose : ""))
            .ForMember(dest => dest.TimeOfDay, opt => opt.MapFrom(src =>
                GetTimeOfDayFromScheduledTime(src.ScheduledTime)))
            .ForMember(dest => dest.TimeOfDayDisplayName, opt => opt.MapFrom(src =>
                GetTimeOfDayDisplayName(GetTimeOfDayFromScheduledTime(src.ScheduledTime))))
            .ForMember(dest => dest.MedicationStartDate, opt => opt.MapFrom(src =>
                src.StudentMedication != null ? src.StudentMedication.StartDate : DateTime.MinValue))
            .ForMember(dest => dest.MedicationEndDate, opt => opt.MapFrom(src =>
                src.StudentMedication != null ? src.StudentMedication.EndDate : DateTime.MinValue))
            .ForMember(dest => dest.MedicationExpiryDate, opt => opt.MapFrom(src =>
                src.StudentMedication != null ? src.StudentMedication.ExpiryDate : DateTime.MinValue))

            .ForMember(dest => dest.StudentId, opt => opt.MapFrom(src =>
                src.StudentMedication != null ? src.StudentMedication.StudentId : Guid.Empty))
            .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src =>
                src.StudentMedication.Student != null ? src.StudentMedication.Student.FullName : ""))
            .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src =>
                src.StudentMedication.Student != null ? src.StudentMedication.Student.StudentCode : ""))
            .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src =>
                src.StudentMedication != null ? src.StudentMedication.ParentId : Guid.Empty))
            .ForMember(dest => dest.ParentName, opt => opt.MapFrom(src =>
                src.StudentMedication.Parent != null ? src.StudentMedication.Parent.FullName : ""))

            .ForMember(dest => dest.Administration, opt => opt.MapFrom(src =>
                src.Administration != null ? MapToAdministrationInfo(src.Administration) : null))

            .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
            .ForMember(dest => dest.LastUpdatedDate, opt => opt.MapFrom(src => src.LastUpdatedDate))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.LastUpdatedBy, opt => opt.MapFrom(src => src.LastUpdatedBy));
    }

    #region Helper Methods

    private static string GetStatusDisplayName(MedicationScheduleStatus status)
    {
        return status switch
        {
            MedicationScheduleStatus.Pending => "Chờ thực hiện",
            MedicationScheduleStatus.Completed => "Đã hoàn thành",
            MedicationScheduleStatus.Missed => "Đã bỏ lỡ",
            MedicationScheduleStatus.Cancelled => "Đã hủy",
            MedicationScheduleStatus.StudentAbsent => "Học sinh vắng mặt",
            _ => status.ToString()
        };
    }

    private static string GetPriorityDisplayName(MedicationPriority priority)
    {
        return priority switch
        {
            MedicationPriority.Low => "Thấp",
            MedicationPriority.Normal => "Bình thường",
            MedicationPriority.High => "Cao",
            MedicationPriority.Critical => "Rất quan trọng",
            _ => priority.ToString()
        };
    }

    private static string GetTimeOfDayDisplayName(MedicationTimeOfDay timeOfDay)
    {
        return timeOfDay switch
        {
            MedicationTimeOfDay.Morning => "Buổi sáng sớm (7:00)",
            MedicationTimeOfDay.AfterBreakfast => "Sau bữa sáng (8:30)",
            MedicationTimeOfDay.MidMorning => "Giữa buổi sáng (10:00)",
            MedicationTimeOfDay.BeforeLunch => "Trước bữa trưa (11:30)",
            MedicationTimeOfDay.AfterLunch => "Sau bữa trưa (13:00)",
            MedicationTimeOfDay.MidAfternoon => "Giữa buổi chiều (14:30)",
            MedicationTimeOfDay.LateAfternoon => "Cuối buổi chiều (16:00)",
            MedicationTimeOfDay.BeforeDismissal => "Trước khi tan học (16:30)",
            _ => timeOfDay.ToString()
        };
    }

    private static MedicationAdministrationInfo MapToAdministrationInfo(MedicationAdministration administration)
    {
        return new MedicationAdministrationInfo
        {
            Id = administration.Id,
            AdministeredAt = administration.AdministeredAt,
            ActualDosage = administration.ActualDosage,
            Notes = administration.Notes,
            StudentRefused = administration.StudentRefused,
            RefusalReason = administration.RefusalReason,
            SideEffectsObserved = administration.SideEffectsObserved,
            AdministeredById = administration.AdministeredById,
            AdministeredByName = administration.AdministeredBy?.FullName ?? ""
        };
    }

    private static MedicationTimeOfDay GetTimeOfDayFromScheduledTime(TimeSpan scheduledTime)
    {
        if (scheduledTime >= TimeSpan.FromHours(7) && scheduledTime < TimeSpan.FromHours(8.5))
            return MedicationTimeOfDay.Morning;
        if (scheduledTime >= TimeSpan.FromHours(8.5) && scheduledTime < TimeSpan.FromHours(10))
            return MedicationTimeOfDay.AfterBreakfast;
        if (scheduledTime >= TimeSpan.FromHours(10) && scheduledTime < TimeSpan.FromHours(11.5))
            return MedicationTimeOfDay.MidMorning;
        if (scheduledTime >= TimeSpan.FromHours(11.5) && scheduledTime < TimeSpan.FromHours(13))
            return MedicationTimeOfDay.BeforeLunch;
        if (scheduledTime >= TimeSpan.FromHours(13) && scheduledTime < TimeSpan.FromHours(14.5))
            return MedicationTimeOfDay.AfterLunch;
        if (scheduledTime >= TimeSpan.FromHours(14.5) && scheduledTime < TimeSpan.FromHours(16))
            return MedicationTimeOfDay.MidAfternoon;
        if (scheduledTime >= TimeSpan.FromHours(16) && scheduledTime < TimeSpan.FromHours(16.5))
            return MedicationTimeOfDay.LateAfternoon;
        return MedicationTimeOfDay.BeforeDismissal;
    }

    #endregion
}