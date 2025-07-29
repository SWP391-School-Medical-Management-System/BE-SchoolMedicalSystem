using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities
{
    public class VitalSignRecord : BaseEntity
    {
        public Guid MedicalRecordId { get; set; } // References MedicalRecord (formerly Health_Profiles)
        public Guid? HealthCheckId { get; set; }
        public double? BloodPressure { get; set; } // Huyết áp  (mmHg)
        public double? HeartRate { get; set; }     // Nhịp tim (bpm)
        public DateTime CheckDate { get; set; }
        public string? Comments { get; set; }
        public Guid RecordedBy { get; set; } // Medical staff who recorded the data

        public virtual MedicalRecord MedicalRecord { get; set; }
        public virtual HealthCheck? HealthCheck { get; set; }
        public virtual ApplicationUser RecordedByUser { get; set; }
    }
}
