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
            .ForMember(dest => dest.StudentName, opt => opt.Ignore())
            .ForMember(dest => dest.StudentCode, opt => opt.Ignore());
    }
}