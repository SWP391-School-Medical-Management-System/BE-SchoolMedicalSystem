using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities
{
    public class StudentMedicationUsageHistory : BaseEntity
    {
        public Guid StudentMedicationId { get; set; }   // ID thuốc
        public Guid StudentId { get; set; }             
        public DateTime? UsageDate { get; set; }
        public string DosageUse { get; set; }
        public StatusUsage Status { get; set; }    
        public string? Reason { get; set; }  
        public string? Note { get; set; }
        public Guid AdministeredBy { get; set; }
        public DateTime? CreateAt { get; set; }

        public virtual ApplicationUser Nurse {  get; set; }     // Y tá phụ trách 
        public virtual ApplicationUser Student { get; set; }
        public virtual StudentMedication StudentMedications { get; set; }
       
    }
}
