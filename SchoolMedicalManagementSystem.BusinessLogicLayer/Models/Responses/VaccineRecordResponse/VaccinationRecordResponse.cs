using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccineRecordResponse
{
    public class VaccinationRecordResponse
    {
        public Guid Id { get; set; }
        public string VaccinationTypeName { get; set; }
        public int DoseNumber { get; set; }
        public DateTime AdministeredDate { get; set; }
        public string AdministeredBy { get; set; }
        public string BatchNumber { get; set; }
        public string? Notes { get; set; }
    }
}
