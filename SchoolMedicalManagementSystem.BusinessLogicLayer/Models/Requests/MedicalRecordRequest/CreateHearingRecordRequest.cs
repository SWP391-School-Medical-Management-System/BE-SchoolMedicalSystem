using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalRecordRequest
{
    public class CreateHearingRecordRequest
    {
        public string LeftEar { get; set; } // Trạng thái tai trái
        public string RightEar { get; set; } // Trạng thái tai phải
        public DateTime? CheckDateHearing { get; set; } // Ngày kiểm tra thính lực
        public string CommentsHearing { get; set; } // Ghi chú thính lực

    }
}
