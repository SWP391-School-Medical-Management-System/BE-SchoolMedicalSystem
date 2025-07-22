using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthCheckRequest
{
    public class AssignNurseToHealthCheckRequest
    {
        public Guid HealthCheckId { get; set; }
        public List<NurseAssignmentRequest> Assignments { get; set; }
    }
    public class NurseAssignmentRequest
    {
        [Required]
        public Guid HealthCheckItemId { get; set; }

        [Required]
        public Guid NurseId { get; set; }
    }
}
