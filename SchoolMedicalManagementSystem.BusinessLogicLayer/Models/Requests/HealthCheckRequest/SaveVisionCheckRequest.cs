using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthCheckRequest
{
    public class SaveVisionCheckRequest
    {
        public Guid StudentId { get; set; }
        public Guid HealthCheckItemId { get; set; } // ID của HealthCheckItem cho mắt trái
        public decimal? Value { get; set; }        // Kết quả đo mắt trái (có thể null nếu chưa khám)
        public string? Comments { get; set; }      // Ghi chú (ví dụ: "Nghi ngờ cận thị")
    }
}
