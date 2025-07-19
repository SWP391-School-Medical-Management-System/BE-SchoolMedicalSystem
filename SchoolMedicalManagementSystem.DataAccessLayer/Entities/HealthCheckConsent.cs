using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities
{
    public class HealthCheckConsent : BaseEntity
    {
        public Guid HealthCheckId { get; set; }        // Foreign Key đến HealthCheck
        public Guid StudentId { get; set; }        // Foreign Key đến ApplicationUser (học sinh)
        public Guid ParentId { get; set; }         // Foreign Key đến ApplicationUser (phụ huynh)
        public string Status { get; set; }         // Trạng thái: Pending, Confirmed, Declined
        public DateTime? ResponseDate { get; set; } // Thời gian phản hồi, nullable
        public string ConsentFormUrl { get; set; } // Đường dẫn đến form đồng ý, tùy chọn

        public virtual HealthCheck HealthCheck { get; set; } // Quan hệ với buổi khám
        public virtual ApplicationUser Student { get; set; }   // Quan hệ với học sinh
        public virtual ApplicationUser Parent { get; set; }    // Quan hệ với phụ huynh
    }
}
