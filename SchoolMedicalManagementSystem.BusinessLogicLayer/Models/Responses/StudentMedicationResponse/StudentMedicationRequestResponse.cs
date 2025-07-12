using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationResponse
{
    public class StudentMedicationRequestResponse
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public Guid ParentId { get; set; }
        public string StudentName { get; set; }
        public string StudentCode { get; set; }
        public string ParentName { get; set; }
        public string? ApprovedByName { get; set; }
        public StudentMedicationStatus Status { get; set; }
        public string StatusDisplayName { get; set; }
        public string PriorityDisplayName { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string Code { get; set; }
        public int MedicationCount { get; set; }
    }
}
