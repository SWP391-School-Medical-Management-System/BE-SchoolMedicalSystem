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
                .ForMember(dest => dest.ClassNames, opt => opt.MapFrom(src => src.HealthCheckClasses.Select(c => c.SchoolClass != null ? c.SchoolClass.Name : "").ToList()))
                .ForMember(dest => dest.TotalStudents, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedStudents, opt => opt.Ignore());

            CreateMap<HealthCheck, CreateWholeHealthCheckResponse>()
                .ForMember(dest => dest.ClassIds, opt => opt.MapFrom(src => src.HealthCheckClasses.Select(c => c.ClassId).ToList()));

            CreateMap<VisionRecord, VisionRecordResponseHealth>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.MedicalRecordId, opt => opt.MapFrom(src => src.MedicalRecordId))
                .ForMember(dest => dest.HealthCheckId, opt => opt.MapFrom(src => src.HealthCheckId))
                .ForMember(dest => dest.LeftEye, opt => opt.MapFrom(src => src.LeftEye))
                .ForMember(dest => dest.RightEye, opt => opt.MapFrom(src => src.RightEye))
                .ForMember(dest => dest.CheckDate, opt => opt.MapFrom(src => src.CheckDate))
                .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments))
                .ForMember(dest => dest.RecordedBy, opt => opt.MapFrom(src => src.RecordedBy));
        }
    }
}