using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationResponse
{
    public class StudentMedicationUsageHistoryResponse
    {
        public Guid Id { get; set; }
        public Guid StudentMedicationId { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentCode { get; set; }
        public string ClassName { get; set; }
        public DateTime? UsageDate { get; set; }
        public string DosageUse { get; set; }
        public StatusUsage Status { get; set; }
        public string StatusDisplayName { get; set; }
        public string? Reason { get; set; }
        public string? Note { get; set; }
        public Guid AdministeredBy { get; set; }
        public string AdministeredByName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int QuantityReceive { get; set; } // Số lượng thuốc còn lại      
        public string MedicationName { get; set; }
        public DateTime? AdministeredTime { get; set; }
        public string? AdministeredPeriod { get; set; }
    }
}
