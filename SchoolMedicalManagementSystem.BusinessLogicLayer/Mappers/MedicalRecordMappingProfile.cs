using AutoMapper;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalRecordRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalRecordResponse;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Mappers;

public class MedicalRecordMappingProfile : Profile
{
    public MedicalRecordMappingProfile()
    {
        CreateMedicalRecordMappings();
        CreateVaccinationMappings();
    }

    private void CreateMedicalRecordMappings()
    {
        CreateMap<CreateMedicalRecordRequest, MedicalRecord>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Code, opt => opt.Ignore())
            .ForMember(dest => dest.StartDate, opt => opt.Ignore())
            .ForMember(dest => dest.EndDate, opt => opt.Ignore())
            .ForMember(dest => dest.Student, opt => opt.Ignore())
            .ForMember(dest => dest.MedicalConditions, opt => opt.Ignore())
            .ForMember(dest => dest.VaccinationRecords, opt => opt.Ignore());

        CreateMap<UpdateMedicalRecordRequest, MedicalRecord>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Code, opt => opt.Ignore())
            .ForMember(dest => dest.StartDate, opt => opt.Ignore())
            .ForMember(dest => dest.EndDate, opt => opt.Ignore())
            .ForMember(dest => dest.Student, opt => opt.Ignore())
            .ForMember(dest => dest.MedicalConditions, opt => opt.Ignore())
            .ForMember(dest => dest.VaccinationRecords, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, member) => member != null));

        CreateMap<MedicalRecord, MedicalRecordResponse>()
            .ForMember(dest => dest.StudentName, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.StudentCode, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.AllergyCount, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.ChronicDiseaseCount, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.NeedsUpdate, opt => opt.Ignore()); // Set in service

        CreateMap<MedicalRecord, MedicalRecordDetailResponse>()
            .ForMember(dest => dest.StudentName, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.StudentCode, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.MedicalConditions, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.VaccinationRecords, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.NeedsUpdate, opt => opt.Ignore()); // Set in service
    }

    private void CreateVaccinationMappings()
    {
        CreateMap<VaccinationRecord, VaccinationRecordResponse>()
            .ForMember(dest => dest.VaccinationTypeName, opt => opt.MapFrom(src => src.VaccinationType.Name));
    }
}