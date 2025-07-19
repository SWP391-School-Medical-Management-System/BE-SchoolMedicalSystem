using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities
{
    public class PhysicalRecord : BaseEntity
    {
        public Guid MedicalRecordId { get; set; } // References MedicalRecord (formerly Health_Profiles)
        public Guid? HealthCheckId { get; set; }
        public decimal Height { get; set; } // e.g., 130.50 cm
        public decimal Weight { get; set; } // e.g., 25.00 kg
        public decimal BMI { get; set; } // e.g., 14.7 BMI = W/ [(H)2]
        public DateTime CheckDate { get; set; }
        public string? Comments { get; set; }
        public Guid RecordedBy { get; set; } // Medical staff who recorded the data

        public virtual MedicalRecord MedicalRecord { get; set; }
        public virtual HealthCheck? HealthCheck { get; set; }
        public virtual ApplicationUser RecordedByUser { get; set; }
    }
}
