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
                .ForMember(dest => dest.VaccineTypeName, opt => opt.MapFrom(src => src.VaccineType.Name));

            // Ánh xạ từ VaccinationSession sang CreateWholeVaccinationSessionResponse
            CreateMap<VaccinationSession, CreateWholeVaccinationSessionResponse>()
                .ForMember(dest => dest.VaccineTypeName, opt => opt.MapFrom(src => src.VaccineType.Name))
                .ForMember(dest => dest.ClassIds, opt => opt.MapFrom(src => src.Classes.Select(c => c.ClassId).ToList()));
        }
    }
}
