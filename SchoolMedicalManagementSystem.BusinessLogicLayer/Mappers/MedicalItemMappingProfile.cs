using AutoMapper;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalItemRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalItemResponse;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Mappers;

public class MedicalItemMappingProfile : Profile
{
    public MedicalItemMappingProfile()
    {
        CreateMap<CreateMedicalItemRequest, MedicalItem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Code, opt => opt.Ignore())
            .ForMember(dest => dest.StartDate, opt => opt.Ignore())
            .ForMember(dest => dest.EndDate, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovalStatus, opt => opt.Ignore())
            .ForMember(dest => dest.RequestedById, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovedById, opt => opt.Ignore())
            .ForMember(dest => dest.RequestedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovedAt, opt => opt.Ignore())
            .ForMember(dest => dest.RejectedAt, opt => opt.Ignore())
            .ForMember(dest => dest.RejectionReason, opt => opt.Ignore())
            .ForMember(dest => dest.RequestedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Usages, opt => opt.Ignore());

        CreateMap<UpdateMedicalItemRequest, MedicalItem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Code, opt => opt.Ignore())
            .ForMember(dest => dest.StartDate, opt => opt.Ignore())
            .ForMember(dest => dest.EndDate, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovalStatus, opt => opt.Ignore())
            .ForMember(dest => dest.RequestedById, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovedById, opt => opt.Ignore())
            .ForMember(dest => dest.Justification, opt => opt.Ignore())
            .ForMember(dest => dest.RequestedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovedAt, opt => opt.Ignore())
            .ForMember(dest => dest.RejectedAt, opt => opt.Ignore())
            .ForMember(dest => dest.RejectionReason, opt => opt.Ignore())
            .ForMember(dest => dest.Priority, opt => opt.Ignore())
            .ForMember(dest => dest.IsUrgent, opt => opt.Ignore())
            .ForMember(dest => dest.RequestedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Usages, opt => opt.Ignore());

        CreateMap<MedicalItem, MedicalItemResponse>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.ApprovalStatus.ToString()))
            .ForMember(dest => dest.StatusDisplayName, opt => opt.Ignore())
            .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority.ToString()))
            .ForMember(dest => dest.PriorityDisplayName, opt => opt.Ignore())
            .ForMember(dest => dest.FormDisplayName, opt => opt.MapFrom(src => GetFormDisplayName(src.Form)))
            .ForMember(dest => dest.IsExpiringSoon, opt => opt.MapFrom(src =>
                src.ExpiryDate.HasValue && src.ExpiryDate.Value <= DateTime.Now.AddDays(30) &&
                src.ExpiryDate.Value > DateTime.Now))
            .ForMember(dest => dest.IsExpired, opt => opt.MapFrom(src =>
                src.ExpiryDate.HasValue && src.ExpiryDate.Value <= DateTime.Now))
            .ForMember(dest => dest.IsLowStock, opt => opt.MapFrom(src => src.Quantity <= 10))
            .ForMember(dest => dest.StatusText, opt => opt.MapFrom(src => GetStatusText(src)))
            .ForMember(dest => dest.RequestedByName, opt => opt.MapFrom(src =>
                src.RequestedBy != null ? src.RequestedBy.FullName : null))
            .ForMember(dest => dest.RequestedByStaffCode, opt => opt.MapFrom(src =>
                src.RequestedBy != null ? src.RequestedBy.StaffCode : null))
            .ForMember(dest => dest.ApprovedByName, opt => opt.MapFrom(src =>
                src.ApprovedBy != null ? src.ApprovedBy.FullName : null))
            .ForMember(dest => dest.ApprovedByStaffCode, opt => opt.MapFrom(src =>
                src.ApprovedBy != null ? src.ApprovedBy.StaffCode : null))
            .ForMember(dest => dest.CanApprove, opt => opt.Ignore())
            .ForMember(dest => dest.CanReject, opt => opt.Ignore())
            .ForMember(dest => dest.CanUse, opt => opt.Ignore());
    }

    private static string GetFormDisplayName(MedicationForm? form)
    {
        if (!form.HasValue) return "";

        return form.Value switch
        {
            MedicationForm.Tablet => "Viên",
            MedicationForm.Syrup => "Siro",
            MedicationForm.Injection => "Tiêm",
            MedicationForm.Cream => "Kem",
            MedicationForm.Drops => "Nhỏ giọt",
            MedicationForm.Inhaler => "Hít",
            MedicationForm.Other => "Khác",
            _ => form.ToString()
        };
    }

    private static string GetStatusText(MedicalItem item)
    {
        var statuses = new List<string>();

        if (item.ExpiryDate.HasValue && item.ExpiryDate.Value <= DateTime.Now)
        {
            statuses.Add("Hết hạn");
        }
        else if (item.ExpiryDate.HasValue && item.ExpiryDate.Value <= DateTime.Now.AddDays(30))
        {
            statuses.Add("Gần hết hạn");
        }

        if (item.Quantity <= 10)
        {
            statuses.Add("Tồn kho thấp");
        }

        if (item.Quantity == 0)
        {
            statuses.Add("Hết hàng");
        }

        return statuses.Any() ? string.Join(", ", statuses) : "Bình thường";
    }
}