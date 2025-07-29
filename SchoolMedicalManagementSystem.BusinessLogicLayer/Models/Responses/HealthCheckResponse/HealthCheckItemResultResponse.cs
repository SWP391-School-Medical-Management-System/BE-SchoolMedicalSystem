using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthCheckResponse
{
    public class HealthCheckItemResultResponse
    {
        public Guid Id { get; set; } // Id của HealthCheckResultItem hoặc HealthCheckResult
        public Guid UserId { get; set; }
        public string StudentName { get; set; }
        public Guid HealthCheckId { get; set; }
        public Guid HealthCheckItemId { get; set; }
        public string HealthCheckItemName { get; set; }
        public string Unit { get; set; }
        public string ResultStatus { get; set; } // NotChecked, Complete
        public List<HealthCheckResultItemResponse> ResultItems { get; set; }
    }

    public class HealthCheckResultItemResponse
    {
        public Guid Id { get; set; }
        public Guid HealthCheckResultId { get; set; }
        public Guid HealthCheckItemId { get; set; }
        public string HealthCheckItemName { get; set; }
        public double? Value { get; set; }
        public bool IsNormal { get; set; }
        public string Notes { get; set; }
    }
}
