using AutoMapper;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicationStockResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationAdministrationResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationResponse;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using System.Text.Json;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Mappers;

public class StudentMedicationMappingProfile : Profile
{
    public StudentMedicationMappingProfile()
    {
        ConfigureRequestMappings();
        ConfigureResponseMappings();
        ConfigureAdministrationMappings();
        ConfigureMedicationStockMappings();
    }

    private void ConfigureRequestMappings()
    {
        CreateMap<CreateStudentMedicationRequest, StudentMedication>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ParentId, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovedById, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.RejectionReason, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovedAt, opt => opt.Ignore())
            .ForMember(dest => dest.SubmittedAt, opt => opt.Ignore())
            .ForMember(dest => dest.TotalDoses, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.RemainingDoses, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.MinStockThreshold, opt => opt.MapFrom(src => 3))
            .ForMember(dest => dest.LowStockAlertSent, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.AutoGenerateSchedule, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.RequireNurseConfirmation, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.SkipOnAbsence, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.SkipWeekends, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.ManagementNotes, opt => opt.Ignore())
            .ForMember(dest => dest.SkipDates, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Student, opt => opt.Ignore())
            .ForMember(dest => dest.Parent, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Administrations, opt => opt.Ignore())
            .ForMember(dest => dest.Schedules, opt => opt.Ignore())
            .ForMember(dest => dest.StockHistory, opt => opt.Ignore());

        CreateMap<CreateBulkStudentMedicationRequest.BulkMedicationDetails, StudentMedication>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.StudentId, opt => opt.Ignore())
                .ForMember(dest => dest.ParentId, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedById, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => StudentMedicationStatus.PendingApproval))
                .ForMember(dest => dest.RejectionReason, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedAt, opt => opt.Ignore())
                .ForMember(dest => dest.SubmittedAt, opt => opt.Ignore())
                .ForMember(dest => dest.TotalDoses, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.RemainingDoses, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.MinStockThreshold, opt => opt.MapFrom(src => 3))
                .ForMember(dest => dest.LowStockAlertSent, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.AutoGenerateSchedule, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.RequireNurseConfirmation, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.SkipOnAbsence, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.SkipWeekends, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.ManagementNotes, opt => opt.Ignore())
                .ForMember(dest => dest.SkipDates, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.LastUpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Student, opt => opt.Ignore())
                .ForMember(dest => dest.Parent, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Administrations, opt => opt.Ignore())
                .ForMember(dest => dest.Schedules, opt => opt.Ignore())
                .ForMember(dest => dest.StockHistory, opt => opt.Ignore())
                .ForMember(dest => dest.TimesOfDay, opt => opt.MapFrom(src => SerializeTimesOfDay(src.TimesOfDay)))
                .ForMember(dest => dest.QuantityUnit, opt => opt.MapFrom(src => src.QuantityUnit))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.FrequencyCount, opt => opt.MapFrom(src => src.FrequencyCount));

        CreateMap<UpdateStudentMedicationRequest, StudentMedication>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<UpdateMedicationManagementRequest, StudentMedication>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }

    private void ConfigureResponseMappings()
    {
        CreateMap<StudentMedication, StudentMedicationListResponse>()
            .ForMember(dest => dest.PriorityDisplayName, opt => opt.MapFrom(src => GetPriorityDisplayName(src.Priority)))
            .ForMember(dest => dest.StatusDisplayName, opt => opt.MapFrom(src => GetStatusDisplayName(src.Status)))
            .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student != null ? src.Student.FullName : ""))
            .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.Student != null ? src.Student.StudentCode : ""))
            .ForMember(dest => dest.ParentName, opt => opt.MapFrom(src => src.Parent != null ? src.Parent.FullName : ""))
            .ForMember(dest => dest.ApprovedByName, opt => opt.MapFrom(src => src.ApprovedBy != null ? src.ApprovedBy.FullName : null))
            .ForMember(dest => dest.TotalSchedules, opt => opt.MapFrom(src => src.Schedules != null ? src.Schedules.Count(s => !s.IsDeleted) : 0))
            .ForMember(dest => dest.TotalAdministrations, opt => opt.MapFrom(src => src.Administrations != null ? src.Administrations.Count(a => !a.IsDeleted) : 0))
            .ForMember(dest => dest.IsExpiringSoon, opt => opt.MapFrom(src => src.ExpiryDate <= DateTime.Today.AddDays(7)))
            .ForMember(dest => dest.IsLowStock, opt => opt.MapFrom(src => src.RemainingDoses <= src.MinStockThreshold));

        CreateMap<StudentMedication, StudentMedicationDetailResponse>()
            .ForMember(dest => dest.StatusDisplayName, opt => opt.MapFrom(src => GetStatusDisplayName(src.Status)))
            .ForMember(dest => dest.PriorityDisplayName, opt => opt.MapFrom(src => GetPriorityDisplayName(src.Priority)))
            .ForMember(dest => dest.TimesOfDayDisplayName, opt => opt.MapFrom(src => GetTimesOfDayDisplayName(src.TimesOfDay)))
            .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student != null ? src.Student.FullName : ""))
            .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.Student != null ? src.Student.StudentCode : ""))
            .ForMember(dest => dest.ParentName, opt => opt.MapFrom(src => src.Parent != null ? src.Parent.FullName : ""))
            .ForMember(dest => dest.ApprovedByName, opt => opt.MapFrom(src => src.ApprovedBy != null ? src.ApprovedBy.FullName : null))
            .ForMember(dest => dest.TotalSchedules, opt => opt.MapFrom(src => src.Schedules != null ? src.Schedules.Count(s => !s.IsDeleted) : 0))
            .ForMember(dest => dest.TotalAdministrations, opt => opt.MapFrom(src => src.Administrations != null ? src.Administrations.Count(a => !a.IsDeleted) : 0))
            .ForMember(dest => dest.TotalStockReceived, opt => opt.MapFrom(src => src.StockHistory != null ? src.StockHistory.Where(s => !s.IsDeleted).Sum(s => s.QuantityAdded) : 0))
            .ForMember(dest => dest.IsExpiringSoon, opt => opt.MapFrom(src => src.ExpiryDate <= DateTime.Today.AddDays(7)))
            .ForMember(dest => dest.IsLowStock, opt => opt.MapFrom(src => src.RemainingDoses <= src.MinStockThreshold))
            .ForMember(dest => dest.DaysUntilExpiry, opt => opt.MapFrom(src => (int)(src.ExpiryDate - DateTime.Today).TotalDays))
            .ForMember(dest => dest.TotalQuantitySent, opt => opt.MapFrom(src => src.QuantitySent))
            .ForMember(dest => dest.UsedDoses, opt => opt.MapFrom(src => src.TotalDoses - src.RemainingDoses))
            .ForMember(dest => dest.UsagePercentage, opt => opt.MapFrom(src => 
                src.TotalDoses > 0 ? Math.Round((double)(src.TotalDoses - src.RemainingDoses) / src.TotalDoses * 100, 2) : 0))
            .ForMember(dest => dest.IsStockAvailable, opt => opt.MapFrom(src => src.RemainingDoses > 0));

        CreateMap<StudentMedication, StudentMedicationResponse>()
            .ForMember(dest => dest.StatusDisplayName, opt => opt.MapFrom(src => GetStatusDisplayName(src.Status)))
            .ForMember(dest => dest.PriorityDisplayName, opt => opt.MapFrom(src => GetPriorityDisplayName(src.Priority)))
            .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student != null ? src.Student.FullName : ""))
            .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.Student != null ? src.Student.StudentCode : ""))
            .ForMember(dest => dest.ParentName, opt => opt.MapFrom(src => src.Parent != null ? src.Parent.FullName : ""))
            .ForMember(dest => dest.ApprovedByName, opt => opt.MapFrom(src => src.ApprovedBy != null ? src.ApprovedBy.FullName : null))
            .ForMember(dest => dest.FrequencyCount, opt => opt.MapFrom(src => src.FrequencyCount));

        CreateMap<StudentMedication, ParentMedicationResponse>()
            .ForMember(dest => dest.StatusDisplayName, opt => opt.MapFrom(src => GetStatusDisplayName(src.Status)))
            .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student != null ? src.Student.FullName : ""))
            .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.Student != null ? src.Student.StudentCode : ""))
            .ForMember(dest => dest.ApprovedByName, opt => opt.MapFrom(src => src.ApprovedBy != null ? src.ApprovedBy.FullName : null))
            .ForMember(dest => dest.IsLowStock, opt => opt.MapFrom(src => src.RemainingDoses <= src.MinStockThreshold))
            .ForMember(dest => dest.IsExpiringSoon, opt => opt.MapFrom(src => src.ExpiryDate <= DateTime.Today.AddDays(7)))
            .ForMember(dest => dest.TotalAdministrations, opt => opt.MapFrom(src => src.Administrations != null ? src.Administrations.Count(a => !a.IsDeleted) : 0))
            .ForMember(dest => dest.LastAdministeredAt, opt => opt.MapFrom(src => GetLastAdministeredAt(src.Administrations)));

        CreateMap<StudentMedication, PendingApprovalResponse>()
            .ForMember(dest => dest.PriorityDisplayName, opt => opt.MapFrom(src => GetPriorityDisplayName(src.Priority)))
            .ForMember(dest => dest.TimesOfDayDisplayName, opt => opt.MapFrom(src => GetTimesOfDayDisplayName(src.TimesOfDay)))
            .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student != null ? src.Student.FullName : ""))
            .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.Student != null ? src.Student.StudentCode : ""))
            .ForMember(dest => dest.ParentName, opt => opt.MapFrom(src => src.Parent != null ? src.Parent.FullName : ""))
            .ForMember(dest => dest.DaysWaiting, opt => opt.MapFrom(src => src.SubmittedAt.HasValue ? 
                (int)(DateTime.Now - src.SubmittedAt.Value).TotalDays : 0));

        CreateMap<StudentMedication, StudentMedicationResponseForRequest>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.RequestId, opt => opt.MapFrom(src => src.StudentMedicationRequestId))
            .ForMember(dest => dest.MedicationName, opt => opt.MapFrom(src => src.MedicationName))
            .ForMember(dest => dest.Dosage, opt => opt.MapFrom(src => src.Dosage))
            .ForMember(dest => dest.Purpose, opt => opt.MapFrom(src => src.Purpose))
            .ForMember(dest => dest.ExpiryDate, opt => opt.MapFrom(src => src.ExpiryDate))
            .ForMember(dest => dest.QuantitySent, opt => opt.MapFrom(src => src.QuantitySent))
            .ForMember(dest => dest.QuantityUnit, opt => opt.MapFrom(src => src.QuantityUnit))
            .ForMember(dest => dest.RejectionReason, opt => opt.MapFrom(src => src.RejectionReason))
            .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
            .ForMember(dest => dest.PriorityDisplayName, opt => opt.MapFrom(src => src.Priority.ToString()));

        CreateMap<StudentMedicationRequest, StudentMedicationRequestResponse>()
            .ForMember(dest => dest.StatusDisplayName, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.PriorityDisplayName, opt => opt.MapFrom(src => src.Priority.ToString()))
            .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.MedicationCount, opt => opt.MapFrom(src => src.MedicationsDetails.Count));

        CreateMap<StudentMedicationRequest, StudentMedicationRequestDetailResponse>()
            .ForMember(dest => dest.StatusDisplayName, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.PriorityDisplayName, opt => opt.MapFrom(src => src.Priority.ToString()))
            .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.Medications, opt => opt.MapFrom(src => src.MedicationsDetails));

        CreateMap<StudentMedicationUsageHistory, StudentMedicationUsageHistoryResponse>()
                .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student.FullName))
                .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.Student.StudentCode))
                .ForMember(dest => dest.AdministeredByName, opt => opt.MapFrom(src => src.Nurse.FullName))
                .ForMember(dest => dest.StatusDisplayName, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.QuantityReceive, opt => opt.MapFrom(src => src.StudentMedication.QuantityReceive));
    }

    
    private void ConfigureMedicationStockMappings()
    {
        CreateMap<MedicationStock, MedicationStockResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.StudentMedicationId, opt => opt.MapFrom(src => src.StudentMedicationId))
            .ForMember(dest => dest.QuantityAdded, opt => opt.MapFrom(src => src.QuantityAdded))
            .ForMember(dest => dest.QuantityUnit, opt => opt.MapFrom(src => src.QuantityUnit))
            .ForMember(dest => dest.ExpiryDate, opt => opt.MapFrom(src => src.ExpiryDate))
            .ForMember(dest => dest.DateAdded, opt => opt.MapFrom(src => src.DateAdded))
            .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes))
            .ForMember(dest => dest.IsInitialStock, opt => opt.MapFrom(src => src.IsInitialStock))
            .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.LastUpdatedDate, opt => opt.MapFrom(src => src.LastUpdatedDate))
            .ForMember(dest => dest.LastUpdatedBy, opt => opt.MapFrom(src => src.LastUpdatedBy))
            .ForMember(dest => dest.MedicationName, opt => opt.MapFrom(src => 
                src.StudentMedication != null ? src.StudentMedication.MedicationName : ""))
            .ForMember(dest => dest.Dosage, opt => opt.MapFrom(src => 
                src.StudentMedication != null ? src.StudentMedication.Dosage : ""))
            .ForMember(dest => dest.Purpose, opt => opt.MapFrom(src => 
                src.StudentMedication != null ? src.StudentMedication.Purpose : ""))
            .ForMember(dest => dest.MedicationStatus, opt => opt.MapFrom(src => 
                src.StudentMedication != null ? src.StudentMedication.Status : StudentMedicationStatus.PendingApproval))
            .ForMember(dest => dest.MedicationStatusDisplayName, opt => opt.MapFrom(src => 
                src.StudentMedication != null ? GetStatusDisplayName(src.StudentMedication.Status) : ""))
            .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => 
                src.StudentMedication != null ? src.StudentMedication.Priority : MedicationPriority.Normal))
            .ForMember(dest => dest.PriorityDisplayName, opt => opt.MapFrom(src => 
                src.StudentMedication != null ? GetPriorityDisplayName(src.StudentMedication.Priority) : ""))
            .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => 
                src.StudentMedication != null && src.StudentMedication.Student != null 
                    ? src.StudentMedication.Student.FullName : ""))
            .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => 
                src.StudentMedication != null && src.StudentMedication.Student != null 
                    ? src.StudentMedication.Student.StudentCode : ""))
            .ForMember(dest => dest.ParentName, opt => opt.MapFrom(src => 
                src.StudentMedication != null && src.StudentMedication.Parent != null 
                    ? src.StudentMedication.Parent.FullName : ""))
            .ForMember(dest => dest.TotalQuantitySent, opt => opt.MapFrom(src => 
                src.StudentMedication != null ? src.StudentMedication.QuantitySent : 0))
            .ForMember(dest => dest.RemainingDoses, opt => opt.MapFrom(src => 
                src.StudentMedication != null ? src.StudentMedication.RemainingDoses : 0))
            .ForMember(dest => dest.MinStockThreshold, opt => opt.MapFrom(src => 
                src.StudentMedication != null ? src.StudentMedication.MinStockThreshold : 0))
            .ForMember(dest => dest.TotalDoses, opt => opt.MapFrom(src => 
                src.StudentMedication != null ? src.StudentMedication.TotalDoses : 0))
            .ForMember(dest => dest.UsedDoses, opt => opt.MapFrom(src => 
                src.StudentMedication != null ? src.StudentMedication.TotalDoses - src.StudentMedication.RemainingDoses : 0))
            .ForMember(dest => dest.IsExpired, opt => opt.MapFrom(src => src.ExpiryDate < DateTime.Today))
            .ForMember(dest => dest.IsExpiringSoon, opt => opt.MapFrom(src => 
                src.ExpiryDate >= DateTime.Today && src.ExpiryDate <= DateTime.Today.AddDays(7)))
            .ForMember(dest => dest.DaysUntilExpiry, opt => opt.MapFrom(src => 
                (int)(src.ExpiryDate - DateTime.Today).TotalDays))
            .ForMember(dest => dest.IsLowStock, opt => opt.MapFrom(src => 
                src.StudentMedication != null && 
                src.StudentMedication.RemainingDoses <= src.StudentMedication.MinStockThreshold));
    }
    
    private void ConfigureAdministrationMappings()
    {
        CreateMap<MedicationAdministration, MedicationAdministrationResponse>()
            .ForMember(dest => dest.AdministeredByName, opt => opt.MapFrom(src =>
                src.AdministeredBy != null ? src.AdministeredBy.FullName : ""))
            .ForMember(dest => dest.MedicationName, opt => opt.MapFrom(src =>
                src.StudentMedication != null ? src.StudentMedication.MedicationName : ""))
            .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src =>
                src.StudentMedication != null && src.StudentMedication.Student != null
                    ? src.StudentMedication.Student.FullName : ""))
            .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src =>
                src.StudentMedication != null && src.StudentMedication.Student != null
                    ? src.StudentMedication.Student.StudentCode : ""))
            .ForMember(dest => dest.ParentName, opt => opt.MapFrom(src =>
                src.StudentMedication != null && src.StudentMedication.Parent != null
                    ? src.StudentMedication.Parent.FullName : ""))
            .ForMember(dest => dest.MedicationPriority, opt => opt.MapFrom(src =>
                src.StudentMedication != null ? src.StudentMedication.Priority : MedicationPriority.Normal))
            .ForMember(dest => dest.MedicationPurpose, opt => opt.MapFrom(src =>
                src.StudentMedication != null ? src.StudentMedication.Purpose : ""))
            .ForMember(dest => dest.RemainingDoses, opt => opt.MapFrom(src =>
                src.StudentMedication != null ? src.StudentMedication.RemainingDoses : 0))
            .ForMember(dest => dest.IsLowStock, opt => opt.MapFrom(src =>
                src.StudentMedication != null &&
                src.StudentMedication.RemainingDoses <= src.StudentMedication.MinStockThreshold));
    }

    #region Helper Methods

    private static string GetStatusDisplayName(StudentMedicationStatus status)
    {
        return status switch
        {
            StudentMedicationStatus.PendingApproval => "Chờ phê duyệt",
            StudentMedicationStatus.Approved => "Đã phê duyệt",
            StudentMedicationStatus.Rejected => "Bị từ chối",
            StudentMedicationStatus.Active => "Đang thực hiện",
            StudentMedicationStatus.Completed => "Hoàn thành",
            StudentMedicationStatus.Discontinued => "Ngưng sử dụng",
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

    private static string GetTimesOfDayDisplayName(string? timesOfDayJson)
    {
        if (string.IsNullOrEmpty(timesOfDayJson))
            return "Chưa thiết lập";
            
        try
        {
            // First try to deserialize as integers (numeric enum values)
            var timeOfDayInts = JsonSerializer.Deserialize<List<int>>(timesOfDayJson);
            if (timeOfDayInts != null && timeOfDayInts.Any())
            {
                var displayNames = new List<string>();
                foreach (var timeOfDayInt in timeOfDayInts)
                {
                    if (Enum.IsDefined(typeof(MedicationTimeOfDay), timeOfDayInt))
                    {
                        var timeOfDay = (MedicationTimeOfDay)timeOfDayInt;
                        displayNames.Add(GetTimeOfDayDisplayName(timeOfDay));
                    }
                }
                
                if (displayNames.Any())
                    return string.Join(", ", displayNames);
            }
            
            // If integer deserialization fails, try as strings
            var timeOfDayStrings = JsonSerializer.Deserialize<List<string>>(timesOfDayJson);
            if (timeOfDayStrings != null && timeOfDayStrings.Any())
            {
                var displayNames = new List<string>();
                foreach (var timeOfDayString in timeOfDayStrings)
                {
                    if (Enum.TryParse<MedicationTimeOfDay>(timeOfDayString, out var timeOfDay))
                    {
                        displayNames.Add(GetTimeOfDayDisplayName(timeOfDay));
                    }
                }
                
                if (displayNames.Any())
                    return string.Join(", ", displayNames);
            }
            
            return "Chưa thiết lập";
        }
        catch
        {
            return "Chưa thiết lập";
        }
    }
    
    private static string GetTimeOfDayDisplayName(MedicationTimeOfDay timeOfDay)
    {
        return timeOfDay switch
        {
            MedicationTimeOfDay.Morning => "Buổi sáng ",
            MedicationTimeOfDay.Noon => "Buổi trưa ",
            MedicationTimeOfDay.Afternoon => "Buổi chiều ",
            MedicationTimeOfDay.Evening => "Buổi tối ",
            _ => timeOfDay.ToString()
        };
    }

    private static DateTime? GetLastAdministeredAt(ICollection<MedicationAdministration>? administrations)
    {
        if (administrations == null || !administrations.Any()) return null;

        return administrations
            .Where(a => !a.IsDeleted)
            .OrderByDescending(a => a.AdministeredAt)
            .FirstOrDefault()?.AdministeredAt;
    }

    private string SerializeTimesOfDay(List<MedicationTimeOfDay>? timesOfDay)
    {
        return timesOfDay != null && timesOfDay.Any() ? JsonSerializer.Serialize(timesOfDay) : null;
    }

    private string SerializeSpecificTimes(List<TimeSpan>? specificTimes)
    {
        return specificTimes != null && specificTimes.Any() ? JsonSerializer.Serialize(specificTimes) : null;
    }

    #endregion
}