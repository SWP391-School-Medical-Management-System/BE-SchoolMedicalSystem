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
            .ForMember(dest => dest.EventTypeDisplayName, opt => opt.Ignore())
            .ForMember(dest => dest.StudentName, opt => opt.Ignore())
            .ForMember(dest => dest.StudentCode, opt => opt.Ignore())
            .ForMember(dest => dest.HandledByName, opt => opt.Ignore())
            .ForMember(dest => dest.RelatedMedicalConditionName, opt => opt.Ignore())
            .ForMember(dest => dest.EmergencyStatusText, opt => opt.Ignore())
            .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code));
    }
}