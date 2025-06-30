using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccinationSessionResponse
{
    public class ParentConsentStatusResponse
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public Guid ParentId { get; set; }
        public string ParentName { get; set; }
        public string ConsentStatus { get; set; } // Confirmed, Declined, Pending
        public DateTime? ResponseDate { get; set; }
    }
}
