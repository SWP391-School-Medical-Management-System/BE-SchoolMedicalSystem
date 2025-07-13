using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalRecordRequest
{
    public class UpdateMedicalRecordByParentRequest
    {
        public string BloodType { get; set; } // Nhóm máu
        public string EmergencyContact { get; set; } // Tên người liên hệ khẩn cấp
        public string EmergencyContactPhone { get; set; } // SĐT liên hệ khẩn cấp

        // VisionRecord
        public decimal? LeftEye { get; set; } // Thay đổi giá trị nếu cần
        public decimal? RightEye { get; set; }
        public DateTime? CheckDate { get; set; } // Ngày kiểm tra
        public string Comments { get; set; } // Ghi chú

        // HearingRecord
        public string LeftEar { get; set; } // Trạng thái tai trái
        public string RightEar { get; set; } // Trạng thái tai phải
        public DateTime? CheckDateHearing { get; set; } // Ngày kiểm tra thính lực
        public string CommentsHearing { get; set; } // Ghi chú thính lực

        // PhysicalRecord
        public decimal? Height { get; set; } // Chiều cao
        public decimal? Weight { get; set; } // Cân nặng
        public DateTime? CheckDatePhysical { get; set; } // Ngày kiểm tra thể chất
        public string CommentsPhysical { get; set; } // Ghi chú thể chất
    }
}
