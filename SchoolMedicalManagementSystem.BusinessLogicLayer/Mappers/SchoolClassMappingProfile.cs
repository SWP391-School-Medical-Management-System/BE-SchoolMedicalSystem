using AutoMapper;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.SchoolClassRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.SchoolClassResponse;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Mappers;

public class SchoolClassMappingProfile : Profile
{
    public SchoolClassMappingProfile()
    {
        CreateMap<CreateSchoolClassRequest, SchoolClass>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.StudentClasses, opt => opt.Ignore());

        CreateMap<SchoolClass, SchoolClassSummaryResponse>()
            .ForMember(dest => dest.StudentCount, opt => opt.Ignore())
            .ForMember(dest => dest.MaleStudentCount, opt => opt.Ignore())
            .ForMember(dest => dest.FemaleStudentCount, opt => opt.Ignore());

        CreateMap<UpdateSchoolClassRequest, SchoolClass>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.StudentClasses, opt => opt.Ignore());

        CreateMap<StudentClass, StudentSummaryResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Student.Id))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Student.FullName))
            .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.Student.StudentCode))
            .ForMember(dest => dest.CurrentClassName, opt => opt.MapFrom(src => src.SchoolClass.Name))
            .ForMember(dest => dest.CurrentGrade, opt => opt.MapFrom(src => src.SchoolClass.Grade))
            .ForMember(dest => dest.ClassCount, opt => opt.Ignore()) // Set trong service
            .ForMember(dest => dest.ClassNames, opt => opt.Ignore()) // Set trong service
            .ForMember(dest => dest.HasMedicalRecord, opt => opt.MapFrom(src => src.Student.MedicalRecord != null));

        CreateMap<AddStudentsToClassRequest, List<Guid>>()
            .ConvertUsing(src => src.StudentIds);

        CreateMap<SchoolClass, SchoolClassResponse>();
    }
}