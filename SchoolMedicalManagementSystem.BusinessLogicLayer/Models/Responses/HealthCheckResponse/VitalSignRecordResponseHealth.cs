using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthCheckResponse
{
    public class VitalSignRecordResponseHealth
    {
        public Guid Id { get; set; }
        public Guid MedicalRecordId { get; set; }
        public Guid? HealthCheckId { get; set; }
        public double? BloodPressure { get; set; } // Huyết áp (mmHg)
        public double? HeartRate { get; set; }     // Nhịp tim (bpm)
        public DateTime CheckDate { get; set; }
        public string? Comments { get; set; }
        public Guid RecordedBy { get; set; }
    }
}
