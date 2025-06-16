using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineRecordRequest
{
    public class UpdateVaccinationRecordRequest
    {
        public Guid VaccinationTypeId { get; set; }
        public int DoseNumber { get; set; }
        public DateTime AdministeredDate { get; set; }
        public string AdministeredBy { get; set; }
        public string BatchNumber { get; set; }
        public string Notes { get; set; }
    }
}
