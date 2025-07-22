using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthCheckRequest
{
    public class SaveHearingCheckRequest
    {
        public Guid StudentId { get; set; }
        public string LeftEar { get; set; } // "Normal", "Impaired"
        public string RightEar { get; set; } // "Normal", "Impaired"
        public string Comments { get; set; } // Ghi chú
    }
}
