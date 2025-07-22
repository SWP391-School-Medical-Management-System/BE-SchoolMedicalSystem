using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthCheckResponse
{
    public class ClassNurseAssignmentHealthCheck
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; }
        public Guid? NurseId { get; set; }
        public string NurseName { get; set; }
    }
}
