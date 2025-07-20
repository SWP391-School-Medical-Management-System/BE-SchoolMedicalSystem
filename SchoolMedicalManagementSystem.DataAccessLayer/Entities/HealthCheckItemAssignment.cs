using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities
{
    public class HealthCheckItemAssignment : BaseEntity
    {
        public Guid HealthCheckId { get; set; }
        public Guid HealthCheckItemId { get; set; }

        public virtual HealthCheck HealthCheck { get; set; }
        public virtual HealthCheckItem HealthCheckItem { get; set; }
    }
}
