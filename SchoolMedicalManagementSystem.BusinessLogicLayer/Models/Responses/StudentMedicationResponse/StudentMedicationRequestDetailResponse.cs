using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationResponse
{
    public class StudentMedicationRequestDetailResponse
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public Guid ParentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string ParentName { get; set; } = string.Empty;
        public MedicationPriority Priority { get; set; }
        public string PriorityDisplayName { get; set; } = string.Empty;
        public StudentMedicationStatus Status { get; set; }
        public string StatusDisplayName { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public List<StudentMedicationResponse> MedicationsDetails { get; set; } = new();
    }
}
