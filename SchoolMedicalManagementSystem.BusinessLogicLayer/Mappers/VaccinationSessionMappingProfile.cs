using AutoMapper;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineSessionRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccinationSessionResponse;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Mappers
{
    public class VaccinationSessionMappingProfile : Profile
    {
        public VaccinationSessionMappingProfile()
        {
            CreateVaccinationSessionMappings();
        }

        private void CreateVaccinationSessionMappings()
        {
            // Ánh xạ từ CreateVaccinationSessionRequest sang VaccinationSession
            CreateMap<CreateVaccinationSessionRequest, VaccinationSession>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedById, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedById, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedDate, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Code, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.VaccineType, opt => opt.Ignore())
                .ForMember(dest => dest.Classes, opt => opt.Ignore());

            // Ánh xạ từ CreateWholeVaccinationSessionRequest sang VaccinationSession
            CreateMap<CreateWholeVaccinationSessionRequest, VaccinationSession>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedById, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedById, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedDate, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Code, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.VaccineType, opt => opt.Ignore())
                .ForMember(dest => dest.Classes, opt => opt.Ignore());

            // Ánh xạ từ UpdateVaccinationSessionRequest sang VaccinationSession
            CreateMap<UpdateVaccinationSessionRequest, VaccinationSession>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedById, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedById, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedDate, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Code, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.VaccineType, opt => opt.Ignore())
                .ForMember(dest => dest.Classes, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, member) => member != null));

            // Ánh xạ từ VaccinationSession sang VaccinationSessionResponse
            CreateMap<VaccinationSession, VaccinationSessionResponse>()
                .ForMember(dest => dest.VaccineTypeName, opt => opt.MapFrom(src => src.VaccineType.Name))
                .ForMember(dest => dest.Classes, opt => opt.MapFrom(src => src.Classes.Select(c => new ClassInfo { Id = c.ClassId, Name = c.SchoolClass.Name }).ToList()));

            // Ánh xạ từ VaccinationSession sang CreateWholeVaccinationSessionResponse
            CreateMap<VaccinationSession, CreateWholeVaccinationSessionResponse>()
                .ForMember(dest => dest.VaccineTypeName, opt => opt.MapFrom(src => src.VaccineType.Name))
                .ForMember(dest => dest.ClassIds, opt => opt.MapFrom(src => src.Classes.Select(c => c.ClassId).ToList()));

            // Ánh xạ từ VaccinationConsent sang ParentConsentStatusResponse
            CreateMap<VaccinationConsent, ParentConsentStatusResponse>()
                .ForMember(dest => dest.StudentId, opt => opt.MapFrom(src => src.StudentId))
                .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student != null ? src.Student.FullName : "Không xác định"))
                .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src => src.ParentId == Guid.Empty ? Guid.Empty : src.ParentId))
                .ForMember(dest => dest.ParentName, opt => opt.MapFrom(src => src.Parent != null ? src.Parent.FullName : "Không xác định"))
                .ForMember(dest => dest.ConsentStatus, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.ResponseDate, opt => opt.MapFrom(src => src.ResponseDate));

            // Ánh xạ từ VaccinationRecord sang StudentVaccinationResultResponse
            CreateMap<VaccinationRecord, StudentVaccinationResultResponse>()
                .ForMember(dest => dest.StudentId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student != null ? src.Student.FullName : "Không xác định"))
                .ForMember(dest => dest.VaccinationRecordId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.VaccinationTypeId, opt => opt.MapFrom(src => src.VaccinationTypeId))
                .ForMember(dest => dest.VaccinationTypeName, opt => opt.MapFrom(src => src.VaccinationType != null ? src.VaccinationType.Name : "Không xác định"))
                .ForMember(dest => dest.DoseNumber, opt => opt.MapFrom(src => src.DoseNumber))
                .ForMember(dest => dest.AdministeredDate, opt => opt.MapFrom(src => src.AdministeredDate))
                .ForMember(dest => dest.AdministeredBy, opt => opt.MapFrom(src => src.AdministeredByUser != null ? src.AdministeredByUser.FullName : src.AdministeredBy ?? "Không xác định"))
                .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes))
                .ForMember(dest => dest.VaccinationStatus, opt => opt.MapFrom(src => src.VaccinationStatus))
                .ForMember(dest => dest.Symptoms, opt => opt.MapFrom(src => src.Symptoms))
                .ForMember(dest => dest.NoteAfterSession, opt => opt.MapFrom(src => src.NoteAfterSession)) 
                .ForMember(dest => dest.ClassName, opt => opt.Ignore());

        }
    }
}