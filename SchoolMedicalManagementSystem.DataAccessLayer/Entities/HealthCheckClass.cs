using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities
{
    public class HealthCheckClass : BaseEntity
    {
        public Guid HealthCheckId { get; set; }            // Foreign Key đến HealthCheck
        public Guid ClassId { get; set; }              // Foreign Key đến SchoolClass

        public virtual HealthCheck HealthCheck { get; set; } // Quan hệ với buổi khám
        public virtual SchoolClass SchoolClass { get; set; }   // Quan hệ với lớp học
    }
}
