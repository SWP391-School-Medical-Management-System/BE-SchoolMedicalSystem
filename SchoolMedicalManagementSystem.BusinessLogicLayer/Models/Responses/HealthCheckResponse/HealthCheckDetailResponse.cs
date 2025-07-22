using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccinationSessionResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthCheckResponse
{
    public class HealthCheckDetailResponse
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
        public List<Guid> ClassIds { get; set; }
        public int TotalConsents { get; set; }
        public int ConfirmedConsents { get; set; }
        public int PendingConsents { get; set; }
        public int DeclinedConsents { get; set; }
        public List<ItemNurseAssignmentHealthCheck> ItemNurseAssignments { get; set; }
        public List<HealthCheckItemResponseDetail> HealthCheckItems { get; set; }
    }

    public class ItemNurseAssignmentHealthCheck
    {
        public Guid HealthCheckItemId { get; set; }
        public string HealthCheckItemName { get; set; }
        public Guid? NurseId { get; set; }
        public string NurseName { get; set; }
    }

    public class HealthCheckItemResponseDetail
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
    }

}
