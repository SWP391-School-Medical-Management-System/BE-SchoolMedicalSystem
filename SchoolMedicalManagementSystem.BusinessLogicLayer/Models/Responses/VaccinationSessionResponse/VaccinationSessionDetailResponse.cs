using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.VaccinationSessionResponse
{
    public class VaccinationSessionDetailResponse
    {
        public Guid Id { get; set; }
        public Guid VaccineTypeId { get; set; }
        public string VaccineTypeName { get; set; }
        public string Location { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; }
        public string SessionName { get; set; }
        public string Notes { get; set; }
        public List<Guid> ClassIds { get; set; }
        public int TotalConsents { get; set; }
        public int ConfirmedConsents { get; set; }
        public int PendingConsents { get; set; }
        public int DeclinedConsents { get; set; }
        public string SideEffect { get; set; }              
        public string Contraindication { get; set; }
        public string ResponsibleOrganizationName { get; set; }
        public List<ClassNurseAssignment> ClassNurseAssignments { get; set; }
    }
    public class ClassNurseAssignment
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; }
        public Guid? NurseId { get; set; }
        public string NurseName { get; set; }
    }
}
