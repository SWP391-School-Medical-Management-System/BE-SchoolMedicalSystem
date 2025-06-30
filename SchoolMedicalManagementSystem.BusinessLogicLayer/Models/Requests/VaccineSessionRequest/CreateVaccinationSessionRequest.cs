using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineSessionRequest
{
    public class CreateVaccinationSessionRequest
    {
        public Guid VaccineTypeId { get; set; }
        public string Location { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<Guid> ClassIds { get; set; }
        public string Notes { get; set; }
        public string SessionName { get; set; }
    }
}
