using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthCheckResponse
{
    public class HealthCheckNurseAssignmentStatusResponse
    {
        public Guid NurseId { get; set; }
        public string NurseName { get; set; }
        public bool IsAssigned { get; set; }
        public List<Guid> AssignedHealthCheckItemIds { get; set; } = new List<Guid>();
        public List<string> AssignedHealthCheckItemNames { get; set; } = new List<string>();
    }
}
