using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities
{
    public class HealthCheckAssignment : BaseEntity
    {
        public Guid HealthCheckId { get; set; }
        public Guid HealthCheckItemId { get; set; }
        public Guid NurseId { get; set; }
        public DateTime AssignedDate { get; set; }

        public virtual HealthCheck HealthCheck { get; set; }
        public virtual ICollection<HealthCheckItem> HealthCheckItems { get; set; }
        public virtual ApplicationUser Nurse { get; set; }     
    }
}
