using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities
{
    public class HearingRecord : BaseEntity
    {
        public Guid MedicalRecordId { get; set; } // References MedicalRecord (formerly Health_Profiles)
        public string LeftEar { get; set; } // e.g., normal, impaired
        public string RightEar { get; set; } // e.g., normal, impaired
        public DateTime CheckDate { get; set; }
        public string? Comments { get; set; }
        public Guid RecordedBy { get; set; } // Medical staff who recorded the data

        public virtual MedicalRecord MedicalRecord { get; set; }
        public virtual ApplicationUser RecordedByUser { get; set; }
    }
}
