using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities
{
    public class VisionRecord : BaseEntity
    {
        public Guid MedicalRecordId { get; set; } // References MedicalRecord (formerly Health_Profiles)
        public Guid? HealthCheckId { get; set; }
        public decimal? LeftEye { get; set; } // e.g., 4.0
        public decimal? RightEye { get; set; } // e.g., 4.0
        public DateTime CheckDate { get; set; }
        public string? Comments { get; set; } // e.g., Suspected myopia
        public Guid RecordedBy { get; set; } // Medical staff who recorded the data

        public virtual MedicalRecord MedicalRecord { get; set; }
        public virtual HealthCheck? HealthCheck { get; set; }
        public virtual ApplicationUser RecordedByUser { get; set; }
    }
}
