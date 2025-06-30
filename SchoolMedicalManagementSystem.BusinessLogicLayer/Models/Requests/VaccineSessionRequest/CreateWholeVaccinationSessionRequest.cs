using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineSessionRequest
{
    public class CreateWholeVaccinationSessionRequest
    {
        [Required]
        public Guid VaccineTypeId { get; set; }

        [Required]
        [StringLength(100)]
        public string VaccineTypeName { get; set; }

        [Required]
        [StringLength(100)]
        public string SessionName { get; set; }

        [Required]
        [StringLength(100)]
        public string ResponsibleOrganizationName { get; set; }

        [Required]
        [StringLength(200)]
        public string Location { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Required]
        [StringLength(100)]
        public string Posology { get; set; }

        [StringLength(500)]
        public string SideEffect { get; set; }

        [StringLength(500)]
        public string Contraindication { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; }

        [Required]
        public List<Guid> ClassIds { get; set; }
    }
}