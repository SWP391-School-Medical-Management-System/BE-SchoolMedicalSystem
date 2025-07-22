using AutoMapper;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthCheckRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthCheckResponse;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Mappers
{
    public class HealthCheckMappingProfile : Profile
    {
        public HealthCheckMappingProfile()
        {
            CreateMap<CreateWholeHealthCheckRequest, HealthCheck>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedById, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedById, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedDate, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Code, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.HealthCheckClasses, opt => opt.Ignore());

            CreateMap<UpdateHealthCheckRequest, HealthCheck>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedById, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedById, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedDate, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Code, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.HealthCheckClasses, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, member) => member != null));

            CreateMap<HealthCheck, HealthCheckResponse>()
                .ForMember(dest => dest.ClassIds, opt => opt.MapFrom(src => src.HealthCheckClasses.Select(c => c.ClassId).ToList()))
                .ForMember(dest => dest.TotalStudents, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedStudents, opt => opt.Ignore());

            CreateMap<HealthCheck, CreateWholeHealthCheckResponse>()
                .ForMember(dest => dest.ClassIds, opt => opt.MapFrom(src => src.HealthCheckClasses.Select(c => c.ClassId).ToList()));
        }
    }
}