using AutoMapper;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalItemUsageRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalItemUsageResponse;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Mappers;

public class MedicalItemUsageMappingProfile : Profile
{
    public MedicalItemUsageMappingProfile()
    {
        CreateMap<CreateMedicalItemUsageRequest, MedicalItemUsage>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UsedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UsedById, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Code, opt => opt.Ignore())
            .ForMember(dest => dest.StartDate, opt => opt.Ignore())
            .ForMember(dest => dest.EndDate, opt => opt.Ignore())
            .ForMember(dest => dest.MedicalItem, opt => opt.Ignore())
            .ForMember(dest => dest.HealthEvent, opt => opt.Ignore())
            .ForMember(dest => dest.UsedBy, opt => opt.Ignore());

        CreateMap<MedicalItemUsage, MedicalItemUsageResponse>()
            .ForMember(dest => dest.MedicalItemName,
                opt => opt.MapFrom(src => src.MedicalItem != null ? src.MedicalItem.Name : ""))
            .ForMember(dest => dest.MedicalItemType,
                opt => opt.MapFrom(src => src.MedicalItem != null ? src.MedicalItem.Type : ""))
            .ForMember(dest => dest.Unit,
                opt => opt.MapFrom(src => src.MedicalItem != null ? src.MedicalItem.Unit : ""))
            .ForMember(dest => dest.HealthEventDescription,
                opt => opt.MapFrom(src => src.HealthEvent != null ? src.HealthEvent.Description : ""))
            .ForMember(dest => dest.StudentName,
                opt => opt.MapFrom(src =>
                    src.HealthEvent != null && src.HealthEvent.Student != null ? src.HealthEvent.Student.FullName : ""))
            .ForMember(dest => dest.StudentCode,
                opt => opt.MapFrom(src =>
                    src.HealthEvent != null && src.HealthEvent.Student != null
                        ? src.HealthEvent.Student.StudentCode
                        : ""))
            .ForMember(dest => dest.UsedByName,
                opt => opt.MapFrom(src => src.UsedBy != null ? src.UsedBy.FullName : ""))
            .ForMember(dest => dest.IsCorrection,
                opt => opt.MapFrom(src => src.Notes != null && src.Notes.Contains("ĐIỀU CHỈNH:")))
            .ForMember(dest => dest.IsReturn,
                opt => opt.MapFrom(src => src.Notes != null && src.Notes.Contains("HOÀN TRẢ:")))
            .ForMember(dest => dest.UsageType, opt => opt.MapFrom(src => GetUsageType(src)));

        CreateMap<CorrectMedicalItemUsageRequest, CreateMedicalItemUsageRequest>()
            .ConvertUsing(src => src.CorrectedData);
    }

    private static string GetUsageType(MedicalItemUsage usage)
    {
        if (usage.Notes != null)
        {
            if (usage.Notes.Contains("ĐIỀU CHỈNH:"))
                return "Correction";
            if (usage.Notes.Contains("HOÀN TRẢ:"))
                return "Return";
        }

        if (usage.Quantity < 0)
            return "Return";

        return "Normal";
    }
}