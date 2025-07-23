using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthCheckResponse
{
    public class StudentConsentStatusHealthCheckResponse
    {
        public Guid ClassId { get; set; }          // ID của lớp
        public string ClassName { get; set; }      // Tên lớp
        public int TotalStudents { get; set; }     // Tổng số học sinh trong lớp
        public int PendingCount { get; set; }      // Số lượng trạng thái Pending
        public int ConfirmedCount { get; set; }    // Số lượng trạng thái Confirmed
        public int DeclinedCount { get; set; }     // Số lượng trạng thái Declined
        public List<StudentConsentDetailHealthResponse> Students { get; set; } // Danh sách học sinh
    }

    public class StudentConsentDetailHealthResponse
    {
        public Guid StudentId { get; set; }        // ID của học sinh
        public string StudentName { get; set; }    // Tên học sinh
        public string Status { get; set; }         // Trạng thái đồng ý: Pending, Confirmed, Declined
        public DateTime? ResponseDate { get; set; } // Thời gian phản hồi, nullable      
    }
}
