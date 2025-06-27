using AutoMapper;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineRecordRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineRequest;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccineRecordResponse;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Mappers
{
    public class VaccinationRecordMappingProfile : Profile
    {
        public VaccinationRecordMappingProfile()
        {
            CreateVaccinationRecordMappings();
        }
        private void CreateVaccinationRecordMappings()
        {
            CreateMap<CreateVaccinationRecordRequest, VaccinationRecord>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalRecordId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.LastUpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Code, opt => opt.Ignore())
                .ForMember(dest => dest.StartDate, opt => opt.Ignore())
                .ForMember(dest => dest.EndDate, opt => opt.Ignore())
                .ForMember(dest => dest.Student, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalRecord, opt => opt.Ignore())
                .ForMember(dest => dest.VaccinationType, opt => opt.Ignore())
                .ForMember(dest => dest.Notifications, opt => opt.Ignore());

            CreateMap<UpdateVaccinationRecordRequest, VaccinationRecord>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalRecordId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.LastUpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Code, opt => opt.Ignore())
                .ForMember(dest => dest.StartDate, opt => opt.Ignore())
                .ForMember(dest => dest.EndDate, opt => opt.Ignore())
                .ForMember(dest => dest.Student, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalRecord, opt => opt.Ignore())
                .ForMember(dest => dest.VaccinationType, opt => opt.Ignore())
                .ForMember(dest => dest.Notifications, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, member) => member != null));

            CreateMap<VaccinationRecord, VaccinationRecordResponse>()
                .ForMember(dest => dest.VaccinationTypeName, opt => opt.MapFrom(src => src.VaccinationType.Name))
                .ForMember(dest => dest.VaccinationStatus, opt => opt.MapFrom(src => src.VaccinationStatus));

        }
    }
}
