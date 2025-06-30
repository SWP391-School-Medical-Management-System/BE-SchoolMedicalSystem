using System;
using System.Collections.Generic;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccinationSessionResponse
{
    public class CreateWholeVaccinationSessionResponse
    {
        public Guid Id { get; set; }
        public Guid VaccineTypeId { get; set; }
        public string VaccineTypeName { get; set; }
        public string SessionName { get; set; }
        public string ResponsibleOrganizationName { get; set; }
        public string Location { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; }
        public string Posology { get; set; }
        public string SideEffect { get; set; }
        public string Contraindication { get; set; }
        public string Notes { get; set; }
        public Guid CreatedById { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Code { get; set; }
        public List<Guid> ClassIds { get; set; }
    }
}