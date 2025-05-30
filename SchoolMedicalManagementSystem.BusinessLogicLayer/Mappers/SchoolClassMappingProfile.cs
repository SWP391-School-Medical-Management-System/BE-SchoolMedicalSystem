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
            .ForMember(dest => dest.Students, opt => opt.Ignore());

        CreateMap<UpdateSchoolClassRequest, SchoolClass>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Students, opt => opt.Ignore());

        CreateMap<SchoolClass, SchoolClassResponse>()
            .ForMember(dest => dest.StudentCount,
                opt => opt.MapFrom(src => src.Students != null ? src.Students.Count(s => !s.IsDeleted) : 0))
            .ForMember(dest => dest.MaleStudentCount,
                opt => opt.MapFrom(src =>
                    src.Students != null ? src.Students.Count(s => !s.IsDeleted && s.Gender == "Male") : 0))
            .ForMember(dest => dest.FemaleStudentCount,
                opt => opt.MapFrom(src =>
                    src.Students != null ? src.Students.Count(s => !s.IsDeleted && s.Gender == "Female") : 0))
            .ForMember(dest => dest.MalePercentage,
                opt => opt.MapFrom(src => CalculatePercentage(src.Students, "Male")))
            .ForMember(dest => dest.FemalePercentage,
                opt => opt.MapFrom(src => CalculatePercentage(src.Students, "Female")))
            .ForMember(dest => dest.Students,
                opt => opt.MapFrom(src =>
                    src.Students != null ? src.Students.Where(s => !s.IsDeleted) : new List<ApplicationUser>()));

        CreateMap<SchoolClass, SchoolClassSummaryResponse>()
            .ForMember(dest => dest.StudentCount,
                opt => opt.MapFrom(src => src.Students != null ? src.Students.Count(s => !s.IsDeleted) : 0))
            .ForMember(dest => dest.MaleStudentCount,
                opt => opt.MapFrom(src =>
                    src.Students != null ? src.Students.Count(s => !s.IsDeleted && s.Gender == "Male") : 0))
            .ForMember(dest => dest.FemaleStudentCount,
                opt => opt.MapFrom(src =>
                    src.Students != null ? src.Students.Count(s => !s.IsDeleted && s.Gender == "Female") : 0));

        CreateMap<ApplicationUser, StudentSummaryResponse>()
            .ForMember(dest => dest.ClassName, opt => opt.MapFrom(src => src.Class != null ? src.Class.Name : null))
            .ForMember(dest => dest.Grade, opt => opt.MapFrom(src => src.Class != null ? src.Class.Grade : (int?)null))
            .ForMember(dest => dest.HasMedicalRecord, opt => opt.MapFrom(src => src.MedicalRecord != null));
    }

    private static double CalculatePercentage(ICollection<ApplicationUser> students, string gender)
    {
        if (students == null || !students.Any(s => !s.IsDeleted))
            return 0;

        var totalStudents = students.Count(s => !s.IsDeleted);
        var genderCount = students.Count(s => !s.IsDeleted && s.Gender == gender);

        return totalStudents > 0 ? Math.Round((double)genderCount / totalStudents * 100, 2) : 0;
    }
}