using AutoMapper;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Mappers;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<AdminCreateUserRequest, ApplicationUser>()
            .ForMember(dest => dest.StaffCode, opt => opt.MapFrom(src => src.StaffCode))
            .ForMember(dest => dest.LicenseNumber, opt => opt.MapFrom(src => src.LicenseNumber))
            .ForMember(dest => dest.Specialization, opt => opt.MapFrom(src => src.Specialization));

        CreateMap<AdminUpdateUserRequest, ApplicationUser>()
            .ForMember(dest => dest.StaffCode, opt => opt.MapFrom(src => src.StaffCode))
            .ForMember(dest => dest.LicenseNumber, opt => opt.MapFrom(src => src.LicenseNumber))
            .ForMember(dest => dest.Specialization, opt => opt.MapFrom(src => src.Specialization));

        CreateMap<ManagerCreateUserRequest, ApplicationUser>()
            .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.StudentCode))
            .ForMember(dest => dest.ClassId, opt => opt.MapFrom(src => src.ClassId))
            .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src => src.ParentId))
            .ForMember(dest => dest.Relationship, opt => opt.MapFrom(src => src.Relationship));

        CreateMap<ManagerUpdateUserRequest, ApplicationUser>()
            .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.StudentCode))
            .ForMember(dest => dest.ClassId, opt => opt.MapFrom(src => src.ClassId))
            .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src => src.ParentId))
            .ForMember(dest => dest.Relationship, opt => opt.MapFrom(src => src.Relationship));

        CreateMap<ApplicationUser, UserResponse>();
    }
}