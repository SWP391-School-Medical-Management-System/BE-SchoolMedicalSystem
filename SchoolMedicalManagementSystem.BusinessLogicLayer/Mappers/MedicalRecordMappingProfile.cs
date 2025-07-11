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
        CreateVisionMappings();
        CreateHearingMappings();
        CreatePhysicalMappings();
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
            .ForMember(dest => dest.VaccinationRecords, opt => opt.Ignore())
            .ForMember(dest => dest.VisionRecords, opt => opt.Ignore())
            .ForMember(dest => dest.HearingRecords, opt => opt.Ignore())
            .ForMember(dest => dest.PhysicalRecords, opt => opt.Ignore());

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
            .ForMember(dest => dest.VisionRecords, opt => opt.Ignore())
            .ForMember(dest => dest.HearingRecords, opt => opt.Ignore())
            .ForMember(dest => dest.PhysicalRecords, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, member) => member != null));

        CreateMap<MedicalRecord, MedicalRecordResponse>()
            .ForMember(dest => dest.StudentName, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.StudentCode, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.AllergyCount, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.ChronicDiseaseCount, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.NeedsUpdate, opt => opt.Ignore()); // Set in service;

        CreateMap<MedicalRecord, MedicalRecordDetailResponse>()
            .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student != null ? src.Student.FullName : ""))
            .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.Student != null ? src.Student.StudentCode : ""))
            .ForMember(dest => dest.MedicalConditions, opt => opt.MapFrom(src => src.MedicalConditions.Where(mc => !mc.IsDeleted)))
            .ForMember(dest => dest.VaccinationRecords, opt => opt.MapFrom(src => src.VaccinationRecords.Where(vr => !vr.IsDeleted)))
            .ForMember(dest => dest.NeedsUpdate, opt => opt.MapFrom(src => src.LastUpdatedDate == null || src.LastUpdatedDate < DateTime.Now.AddMonths(-6)));
        // Vision, Hearing, Physical sẽ được xử lý thủ công trong MapToMedicalRecordDetailResponse
    }

    private void CreateVisionMappings()
    {
        CreateMap<VisionRecord, VisionRecordResponse>()
            .ForMember(dest => dest.LeftEye, opt => opt.MapFrom(src => src.LeftEye))
            .ForMember(dest => dest.RightEye, opt => opt.MapFrom(src => src.RightEye))
            .ForMember(dest => dest.CheckDate, opt => opt.MapFrom(src => src.CheckDate))
            .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments))
            .ForMember(dest => dest.RecordedBy, opt => opt.MapFrom(src => src.RecordedBy));
    }

    private void CreateHearingMappings()
    {
        CreateMap<HearingRecord, HearingRecordResponse>()
            .ForMember(dest => dest.LeftEar, opt => opt.MapFrom(src => src.LeftEar))
            .ForMember(dest => dest.RightEar, opt => opt.MapFrom(src => src.RightEar))
            .ForMember(dest => dest.CheckDate, opt => opt.MapFrom(src => src.CheckDate))
            .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments))
            .ForMember(dest => dest.RecordedBy, opt => opt.MapFrom(src => src.RecordedBy));
    }

    private void CreatePhysicalMappings()
    {
        CreateMap<PhysicalRecord, PhysicalRecordResponse>()
            .ForMember(dest => dest.Height, opt => opt.MapFrom(src => src.Height))
            .ForMember(dest => dest.Weight, opt => opt.MapFrom(src => src.Weight))
            .ForMember(dest => dest.BMI, opt => opt.MapFrom(src => src.BMI))
            .ForMember(dest => dest.CheckDate, opt => opt.MapFrom(src => src.CheckDate))
            .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments))
            .ForMember(dest => dest.RecordedBy, opt => opt.MapFrom(src => src.RecordedBy));
    }

    private void CreateVaccinationMappings()
    {
        CreateMap<VaccinationRecord, VaccinationRecordResponse>()
            .ForMember(dest => dest.VaccinationTypeName, opt => opt.MapFrom(src => src.VaccinationType.Name));
    }
}