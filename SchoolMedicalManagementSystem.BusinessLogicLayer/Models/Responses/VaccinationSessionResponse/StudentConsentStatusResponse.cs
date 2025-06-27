using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccinationSessionResponse
{
    public class StudentConsentStatusResponse
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public string Status { get; set; }
        public DateTime? ResponseDate { get; set; }
        public string VaccinationStatus { get; set; }
    }
}
