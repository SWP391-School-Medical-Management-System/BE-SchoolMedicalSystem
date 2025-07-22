using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthCheckRequest
{
    public class ReAssignNurseToHealthCheckRequest
    {
        public List<NurseAssignmentRequest> Assignments { get; set; }
    }
}
