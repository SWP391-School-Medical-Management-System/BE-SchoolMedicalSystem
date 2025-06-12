using AutoMapper;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.NotificationResponse;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Mappers;

public class NotificationMappingProfile : Profile
{
    public NotificationMappingProfile()
    {
        CreateMap<Notification, NotificationResponse>();
    }
}