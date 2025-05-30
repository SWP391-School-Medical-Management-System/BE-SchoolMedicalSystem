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
            .ForMember(dest => dest.Role, opt => opt.Ignore());

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

        CreateMap<ManagerExcelModel, CreateManagerRequest>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
            .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
            .ForMember(dest => dest.StaffCode, opt => opt.MapFrom(src => src.StaffCode));

        CreateMap<SchoolNurseExcelModel, CreateSchoolNurseRequest>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
            .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
            .ForMember(dest => dest.StaffCode, opt => opt.MapFrom(src => src.StaffCode))
            .ForMember(dest => dest.LicenseNumber, opt => opt.MapFrom(src => src.LicenseNumber))
            .ForMember(dest => dest.Specialization, opt => opt.MapFrom(src => src.Specialization));

        CreateMap<StudentExcelModel, CreateStudentRequest>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
            .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
            .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.StudentCode));

        CreateMap<ParentExcelModel, CreateParentRequest>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
            .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
            .ForMember(dest => dest.Relationship, opt => opt.MapFrom(src => src.Relationship));
    }
}