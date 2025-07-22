using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthCheckRequest
{
    public class UpdateHealthCheckRequest
    {
        public List<Guid> HealthCheckItemIds { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ResponsibleOrganizationName { get; set; }
        public string Location { get; set; }
        public DateTime ScheduledDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Notes { get; set; }
        public List<Guid> ClassIds { get; set; }
    }
}
