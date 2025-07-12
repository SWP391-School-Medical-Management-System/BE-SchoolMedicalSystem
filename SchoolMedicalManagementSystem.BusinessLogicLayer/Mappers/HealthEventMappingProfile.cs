using AutoMapper;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthEventRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthEventResponse;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Mappers;

public class HealthEventMappingProfile : Profile
{
    public HealthEventMappingProfile()
    {
        CreateMap<CreateHealthEventRequest, HealthEvent>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.HandledById, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Code, opt => opt.Ignore())
            .ForMember(dest => dest.StartDate, opt => opt.Ignore())
            .ForMember(dest => dest.EndDate, opt => opt.Ignore())
            .ForMember(dest => dest.Student, opt => opt.Ignore())
            .ForMember(dest => dest.HandledBy, opt => opt.Ignore())
            .ForMember(dest => dest.MedicalItemsUsed, opt => opt.Ignore())
            .ForMember(dest => dest.Notifications, opt => opt.Ignore())
            .ForMember(dest => dest.RelatedMedicalCondition, opt => opt.Ignore())
            .ForMember(dest => dest.Appointments, opt => opt.Ignore());

        CreateMap<UpdateHealthEventRequest, HealthEvent>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.HandledById, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Code, opt => opt.Ignore())
            .ForMember(dest => dest.StartDate, opt => opt.Ignore())
            .ForMember(dest => dest.EndDate, opt => opt.Ignore())
            .ForMember(dest => dest.Student, opt => opt.Ignore())
            .ForMember(dest => dest.HandledBy, opt => opt.Ignore())
            .ForMember(dest => dest.MedicalItemsUsed, opt => opt.Ignore())
            .ForMember(dest => dest.Notifications, opt => opt.Ignore())
            .ForMember(dest => dest.RelatedMedicalCondition, opt => opt.Ignore())
            .ForMember(dest => dest.Appointments, opt => opt.Ignore());

        CreateMap<HealthEvent, HealthEventResponse>()
            .ForMember(dest => dest.EventTypeDisplayName, opt => opt.MapFrom(src => src.EventType.ToString()))
            .ForMember(dest => dest.StudentName, opt => opt.Ignore())
            .ForMember(dest => dest.StudentCode, opt => opt.Ignore())
            .ForMember(dest => dest.StudentClass, opt => opt.MapFrom(src =>
            src.Student != null &&
            src.Student.StudentClasses != null &&
            src.Student.StudentClasses.Any() &&
            src.Student.StudentClasses.FirstOrDefault().SchoolClass != null
                ? src.Student.StudentClasses.FirstOrDefault().SchoolClass.Name
                : "Unknown"))

            .ForMember(dest => dest.HandledByName, opt => opt.Ignore())
            .ForMember(dest => dest.RelatedMedicalConditionName, opt => opt.Ignore())
            .ForMember(dest => dest.EmergencyStatusText, opt => opt.Ignore())
            .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.MedicalItemDetails, opt => opt.MapFrom(src => src.HealthEventMedicalItems)); // Thay MedicalItemsUsed bằng HealthEventMedicalItems

        CreateMap<CreateHealthEventWithMedicalItemsRequest, HealthEvent>()
             .ForMember(dest => dest.Id, opt => opt.Ignore())
             .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
             .ForMember(dest => dest.EventType, opt => opt.MapFrom(src => src.EventType))
             .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
             .ForMember(dest => dest.OccurredAt, opt => opt.MapFrom(src => src.OccurredAt))
             .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location))
             .ForMember(dest => dest.ActionTaken, opt => opt.MapFrom(src => src.ActionTaken))
             .ForMember(dest => dest.Outcome, opt => opt.MapFrom(src => src.Outcome))
             .ForMember(dest => dest.IsEmergency, opt => opt.MapFrom(src => src.IsEmergency))
             .ForMember(dest => dest.RelatedMedicalConditionId, opt => opt.MapFrom(src => src.RelatedMedicalConditionId)) // Giữ nguyên ánh xạ nullable
             .ForMember(dest => dest.CurrentHealthStatus, opt => opt.MapFrom(src => src.CurrentHealthStatus))
             .ForMember(dest => dest.ParentNotice, opt => opt.MapFrom(src => src.ParentNotice))
             .ForMember(dest => dest.HandledById, opt => opt.Ignore())
             .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
             .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
             .ForMember(dest => dest.LastUpdatedDate, opt => opt.Ignore())
             .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore())
             .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
             .ForMember(dest => dest.Code, opt => opt.Ignore())
             .ForMember(dest => dest.StartDate, opt => opt.Ignore())
             .ForMember(dest => dest.EndDate, opt => opt.Ignore())
             .ForMember(dest => dest.Student, opt => opt.Ignore())
             .ForMember(dest => dest.HandledBy, opt => opt.Ignore())
             .ForMember(dest => dest.MedicalItemsUsed, opt => opt.Ignore())
             .ForMember(dest => dest.Notifications, opt => opt.Ignore())
             .ForMember(dest => dest.RelatedMedicalCondition, opt => opt.Ignore())
             .ForMember(dest => dest.Appointments, opt => opt.Ignore());

        CreateMap<MedicalItemUsageRequest, MedicalItemUsage>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.HealthEventId, opt => opt.Ignore())
            .ForMember(dest => dest.UsedById, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.MedicalItem, opt => opt.Ignore())
            .ForMember(dest => dest.HealthEvent, opt => opt.Ignore())
            .ForMember(dest => dest.UsedBy, opt => opt.Ignore());

        CreateMap<HealthEventMedicalItem, HealthEventMedicalItemResponse>()
            .ForMember(dest => dest.NurseName, opt => opt.MapFrom(src => src.NurseName))
            .ForMember(dest => dest.MedicationName, opt => opt.MapFrom(src => src.MedicationName))
            .ForMember(dest => dest.MedicationQuantity, opt => opt.MapFrom(src => src.MedicationQuantity))
            .ForMember(dest => dest.MedicationDosage, opt => opt.MapFrom(src => src.MedicationDosage))
            .ForMember(dest => dest.Dose, opt => opt.MapFrom(src => src.Dose))
            .ForMember(dest => dest.MedicalPerOnce, opt => opt.MapFrom(src => src.MedicalPerOnce))
            .ForMember(dest => dest.SupplyQuantity, opt => opt.MapFrom(src => src.SupplyQuantity));
    }
}