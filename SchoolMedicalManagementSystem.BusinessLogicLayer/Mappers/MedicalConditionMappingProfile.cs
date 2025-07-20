using AutoMapper;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalConditionRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalCondition;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Mappers;

public class MedicalConditionMappingProfile : Profile
{
    public MedicalConditionMappingProfile()
    {
        CreateMedicalConditionMappings();
    }

    private void CreateMedicalConditionMappings()
    {
        CreateMap<CreateMedicalConditionRequest, MedicalCondition>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Code, opt => opt.Ignore())
            .ForMember(dest => dest.StartDate, opt => opt.Ignore())
            .ForMember(dest => dest.EndDate, opt => opt.Ignore())
            .ForMember(dest => dest.MedicalRecord, opt => opt.Ignore());

        CreateMap<UpdateMedicalConditionRequest, MedicalCondition>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.MedicalRecordId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Code, opt => opt.Ignore())
            .ForMember(dest => dest.StartDate, opt => opt.Ignore())
            .ForMember(dest => dest.EndDate, opt => opt.Ignore())
            .ForMember(dest => dest.MedicalRecord, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, member) => member != null));

        CreateMap<MedicalCondition, MedicalConditionResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.MedicalRecordId, opt => opt.MapFrom(src => src.MedicalRecordId))
            //.ForMember(dest => dest.HealthCheckId, opt => opt.MapFrom(src => src.HealthCheckId))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Severity, opt => opt.MapFrom(src => src.Severity))
            .ForMember(dest => dest.Reaction, opt => opt.MapFrom(src => src.Reaction))
            .ForMember(dest => dest.Treatment, opt => opt.MapFrom(src => src.Treatment))
            .ForMember(dest => dest.Medication, opt => opt.MapFrom(src => src.Medication))
            .ForMember(dest => dest.DiagnosisDate, opt => opt.MapFrom(src => src.DiagnosisDate))
            .ForMember(dest => dest.Hospital, opt => opt.MapFrom(src => src.Hospital))
            .ForMember(dest => dest.Doctor, opt => opt.MapFrom(src => src.Doctor))
            .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes));
            //.ForMember(dest => dest.SystolicBloodPressure, opt => opt.MapFrom(src => src.SystolicBloodPressure))
            //.ForMember(dest => dest.DiastolicBloodPressure, opt => opt.MapFrom(src => src.DiastolicBloodPressure))
            //.ForMember(dest => dest.HeartRate, opt => opt.MapFrom(src => src.HeartRate));
    }
}