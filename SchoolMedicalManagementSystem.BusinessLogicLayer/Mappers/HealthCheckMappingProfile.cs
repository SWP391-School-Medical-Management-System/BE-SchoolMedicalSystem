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
                .ForMember(dest => dest.Classes, opt => opt.MapFrom(src => src.HealthCheckClasses
                    .Select(c => new ClassInfoHealth
                    {
                        Id = c.ClassId,
                        Name = c.SchoolClass != null ? c.SchoolClass.Name : null
                    })
                    .Where(c => c.Name != null)
                    .ToList()))
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

            CreateMap<HearingRecord, HearingRecordResponseHealth>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.MedicalRecordId, opt => opt.MapFrom(src => src.MedicalRecordId))
                .ForMember(dest => dest.HealthCheckId, opt => opt.MapFrom(src => src.HealthCheckId))
                .ForMember(dest => dest.LeftEar, opt => opt.MapFrom(src => src.LeftEar))
                .ForMember(dest => dest.RightEar, opt => opt.MapFrom(src => src.RightEar))
                .ForMember(dest => dest.CheckDate, opt => opt.MapFrom(src => src.CheckDate))
                .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments))
                .ForMember(dest => dest.RecordedBy, opt => opt.MapFrom(src => src.RecordedBy));

            CreateMap<PhysicalRecord, PhysicalRecordResponseHealth>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.MedicalRecordId, opt => opt.MapFrom(src => src.MedicalRecordId))
                .ForMember(dest => dest.HealthCheckId, opt => opt.MapFrom(src => src.HealthCheckId))
                .ForMember(dest => dest.Height, opt => opt.MapFrom(src => src.Height))
                .ForMember(dest => dest.Weight, opt => opt.MapFrom(src => src.Weight))
                .ForMember(dest => dest.BMI, opt => opt.MapFrom(src => src.BMI))
                .ForMember(dest => dest.CheckDate, opt => opt.MapFrom(src => src.CheckDate))
                .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments))
                .ForMember(dest => dest.RecordedBy, opt => opt.MapFrom(src => src.RecordedBy));

            CreateMap<VitalSignRecord, VitalSignRecordResponseHealth>()
               .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
               .ForMember(dest => dest.MedicalRecordId, opt => opt.MapFrom(src => src.MedicalRecordId))
               .ForMember(dest => dest.HealthCheckId, opt => opt.MapFrom(src => src.HealthCheckId))
               .ForMember(dest => dest.BloodPressure, opt => opt.MapFrom(src => src.BloodPressure))
               .ForMember(dest => dest.HeartRate, opt => opt.MapFrom(src => src.HeartRate))
               .ForMember(dest => dest.CheckDate, opt => opt.MapFrom(src => src.CheckDate))
               .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments))
               .ForMember(dest => dest.RecordedBy, opt => opt.MapFrom(src => src.RecordedBy));

        }
    }
}