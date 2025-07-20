using AutoMapper;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthCheckItemRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthCheckItemResponse;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Mappers
{
    public class HealthCheckItemMappingProfile : Profile
    {
        public HealthCheckItemMappingProfile()
        {
            CreateMap<CreateHealthCheckItemRequest, HealthCheckItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.LastUpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.HealthCheckItemAssignments, opt => opt.Ignore())
                .ForMember(dest => dest.ResultItems, opt => opt.Ignore());

            CreateMap<UpdateHealthCheckItemRequest, HealthCheckItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.LastUpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.HealthCheckItemAssignments, opt => opt.Ignore())
                .ForMember(dest => dest.ResultItems, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, member) => member != null));

            CreateMap<HealthCheckItem, HealthCheckItemResponse>()
                .ForMember(dest => dest.HealthCheckIds, opt => opt.MapFrom(src =>
                    src.HealthCheckItemAssignments.Where(hcia => !hcia.IsDeleted).Select(hcia => hcia.HealthCheckId).ToList()));
        }
    }
}