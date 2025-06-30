using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccinationSessionResponse
{
    public class VaccinationSessionResponse
    {
        public Guid Id { get; set; }
        public Guid VaccineTypeId { get; set; }
        public string VaccineTypeName { get; set; }
        public string Location { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; }
        public string SessionName { get; set; }
        public string Notes { get; set; }
    }
}
