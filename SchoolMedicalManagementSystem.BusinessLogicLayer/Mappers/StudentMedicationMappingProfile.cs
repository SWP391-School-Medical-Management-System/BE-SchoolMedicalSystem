using AutoMapper;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationAdministrationResponse;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationResponse;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Mappers;

public class StudentMedicationMappingProfile : Profile
{
    public StudentMedicationMappingProfile()
    {
        ConfigureStudentMedicationMappings();
        ConfigureStudentMedicationAdministrationMappings();
    }

    private void ConfigureStudentMedicationMappings()
    {
        CreateMap<CreateStudentMedicationRequest, StudentMedication>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ParentId, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.ApprovedById, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore()) // Set to PendingApproval in service
            .ForMember(dest => dest.RejectionReason, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovedAt, opt => opt.Ignore())
            .ForMember(dest => dest.SubmittedAt, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.Code, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.StartDate, opt => opt.Ignore()) // BaseEntity StartDate, not medication StartDate
            .ForMember(dest => dest.EndDate, opt => opt.Ignore()) // BaseEntity EndDate, not medication EndDate
            .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Student, opt => opt.Ignore())
            .ForMember(dest => dest.Parent, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Administrations, opt => opt.Ignore())
            .ForMember(dest => dest.Notifications, opt => opt.Ignore());

        CreateMap<UpdateStudentMedicationRequest, StudentMedication>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<StudentMedication, StudentMedicationResponse>()
            .ForMember(dest => dest.StatusDisplayName, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.StudentName, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.StudentCode, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.ParentName, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.ApprovedByName, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.CanApprove, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.CanAdminister, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.IsExpiringSoon, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.IsExpired, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.IsActive, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.DaysUntilExpiry, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.AdministrationCount, opt => opt.Ignore()) // Set in service
            .AfterMap((src, dest, context) =>
            {
                if (src.Student != null)
                {
                    dest.StudentName = src.Student.FullName ?? "";
                    dest.StudentCode = src.Student.StudentCode ?? "";
                }

                if (src.Parent != null)
                {
                    dest.ParentName = src.Parent.FullName ?? "";
                }

                if (src.ApprovedBy != null)
                {
                    dest.ApprovedByName = src.ApprovedBy.FullName ?? "";
                }
            });
    }

    private void ConfigureStudentMedicationAdministrationMappings()
    {
        CreateMap<AdministerMedicationRequest, StudentMedicationAdministration>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.StudentMedicationId, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.AdministeredById, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.AdministeredAt, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.Code, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.StartDate, opt => opt.Ignore()) // BaseEntity field
            .ForMember(dest => dest.EndDate, opt => opt.Ignore()) // BaseEntity field
            .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.StudentMedication, opt => opt.Ignore())
            .ForMember(dest => dest.AdministeredBy, opt => opt.Ignore());

        CreateMap<StudentMedicationAdministration, StudentMedicationAdministrationResponse>()
            .ForMember(dest => dest.MedicationName, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.StudentName, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.AdministeredByName, opt => opt.Ignore()) // Set in service
            .AfterMap((src, dest, context) =>
            {
                if (src.StudentMedication != null)
                {
                    dest.MedicationName = src.StudentMedication.MedicationName ?? "";

                    if (src.StudentMedication.Student != null)
                    {
                        dest.StudentName = src.StudentMedication.Student.FullName ?? "";
                    }
                }

                if (src.AdministeredBy != null)
                {
                    dest.AdministeredByName = src.AdministeredBy.FullName ?? "";
                }
            });
    }
}