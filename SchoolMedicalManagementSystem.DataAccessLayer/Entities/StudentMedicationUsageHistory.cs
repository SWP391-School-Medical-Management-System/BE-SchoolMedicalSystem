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
        public Guid StudentMedicationId { get; set; }
        public Guid StudentId { get; set; }             
        public DateTime? UsageDate { get; set; }
        public string DosageUse { get; set; }
        public StatusUsage Status { get; set; }    
        public string? Reason { get; set; }    
        public string? Note { get; set; }   
        public Guid AdministeredBy { get; set; }    
        public DateTime? CreatedAt { get; set; }
        public DateTime? AdministeredTime { get; set; }

        public virtual ApplicationUser Nurse {  get; set; }      
        public virtual ApplicationUser Student { get; set; }        
        public virtual StudentMedication StudentMedication { get; set; }   
       
    }
}
