using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthCheckItemRequest
{
    public class UpdateHealthCheckItemRequest
    {
        public Guid? HealthCheckId { get; set; }
        public string Name { get; set; }
        public HealthCheckItemName? Categories { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }
        public double? MinValue { get; set; }
        public double? MaxValue { get; set; }
    }
}
