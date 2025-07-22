using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthCheckResponse
{
    public class StudentConsentStatusHealthCheckResponse
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentCode { get; set; }
        public List<string> ClassNames { get; set; }
        public string Status { get; set; }
    }
}
