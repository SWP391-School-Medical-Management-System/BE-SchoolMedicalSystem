using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthCheckResponse
{
    public class HealthCheckResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ResponsibleOrganizationName { get; set; }
        public string Location { get; set; }
        public DateTime ScheduledDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public List<ClassInfoHealth> Classes { get; set; }
        public int ApprovedStudents { get; set; }
        public int TotalStudents { get; set; }
    }
    public class ClassInfoHealth
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
