using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthCheckResponse
{
    public class VisionRecordResponseHealth
    {
        public Guid Id { get; set; }
        public Guid MedicalRecordId { get; set; }
        public Guid? HealthCheckId { get; set; }
        public decimal? LeftEye { get; set; }
        public decimal? RightEye { get; set; }
        public DateTime CheckDate { get; set; }
        public string? Comments { get; set; }
        public Guid RecordedBy { get; set; }
    }
}
