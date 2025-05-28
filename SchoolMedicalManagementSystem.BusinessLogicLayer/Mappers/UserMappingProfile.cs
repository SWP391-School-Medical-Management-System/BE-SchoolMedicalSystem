using AutoMapper;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Mappers;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<CreateManagerRequest, ApplicationUser>();
        CreateMap<UpdateManagerRequest, ApplicationUser>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<CreateSchoolNurseRequest, ApplicationUser>();
        CreateMap<UpdateSchoolNurseRequest, ApplicationUser>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<CreateStudentRequest, ApplicationUser>();
        CreateMap<UpdateStudentRequest, ApplicationUser>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<CreateParentRequest, ApplicationUser>();
        CreateMap<UpdateParentRequest, ApplicationUser>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<ApplicationUser, ManagerResponse>();

        CreateMap<ApplicationUser, SchoolNurseResponse>();

        CreateMap<ApplicationUser, StaffUserResponse>()
            .ForMember(dest => dest.Role, opt => opt.Ignore()); // Set manually in service

        CreateMap<ApplicationUser, StudentResponse>()
            .ForMember(dest => dest.HasMedicalRecord, opt => opt.MapFrom(src => src.MedicalRecord != null))
            .ForMember(dest => dest.ClassName, opt => opt.MapFrom(src => src.Class != null ? src.Class.Name : null))
            .ForMember(dest => dest.Grade, opt => opt.MapFrom(src => src.Class != null ? src.Class.Grade : (int?)null))
            .ForMember(dest => dest.AcademicYear,
                opt => opt.MapFrom(src => src.Class != null ? src.Class.AcademicYear : (int?)null))
            .ForMember(dest => dest.ParentName,
                opt => opt.MapFrom(src => src.Parent != null ? src.Parent.FullName : null))
            .ForMember(dest => dest.ParentPhone,
                opt => opt.MapFrom(src => src.Parent != null ? src.Parent.PhoneNumber : null))
            .ForMember(dest => dest.ParentRelationship,
                opt => opt.MapFrom(src => src.Parent != null ? src.Parent.Relationship : null))
            .ForMember(dest => dest.BloodType,
                opt => opt.MapFrom(src => src.MedicalRecord != null ? src.MedicalRecord.BloodType : null))
            .ForMember(dest => dest.Height,
                opt => opt.MapFrom(src => src.MedicalRecord != null ? src.MedicalRecord.Height : (double?)null))
            .ForMember(dest => dest.Weight,
                opt => opt.MapFrom(src => src.MedicalRecord != null ? src.MedicalRecord.Weight : (double?)null))
            .ForMember(dest => dest.EmergencyContact,
                opt => opt.MapFrom(src => src.MedicalRecord != null ? src.MedicalRecord.EmergencyContact : null))
            .ForMember(dest => dest.EmergencyContactPhone,
                opt => opt.MapFrom(src => src.MedicalRecord != null ? src.MedicalRecord.EmergencyContactPhone : null));

        CreateMap<ApplicationUser, ParentResponse>()
            .ForMember(dest => dest.ChildrenCount,
                opt => opt.MapFrom(src => src.Children != null ? src.Children.Count(c => !c.IsDeleted) : 0))
            .ForMember(dest => dest.Children,
                opt => opt.MapFrom(src =>
                    src.Children != null
                        ? src.Children.Where(c => !c.IsDeleted).ToList()
                        : new List<ApplicationUser>()));

        CreateMap<ApplicationUser, StudentSummaryResponse>()
            .ForMember(dest => dest.ClassName, opt => opt.MapFrom(src => src.Class != null ? src.Class.Name : null))
            .ForMember(dest => dest.Grade, opt => opt.MapFrom(src => src.Class != null ? src.Class.Grade : (int?)null))
            .ForMember(dest => dest.HasMedicalRecord, opt => opt.MapFrom(src => src.MedicalRecord != null));

        CreateMap<Role, string>()
            .ConvertUsing(role => role.Name);

        CreateMap<UserRole, string>()
            .ConvertUsing(userRole => userRole.Role.Name);
    }
}