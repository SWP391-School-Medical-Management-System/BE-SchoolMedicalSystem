using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalRecordRequest
{
    public class CreateVisionRecordRequest
    {      
        public decimal? LeftEye { get; set; } // Thị lực mắt trái
        public decimal? RightEye { get; set; } // Thị lực mắt phải
        public DateTime? CheckDate { get; set; } // Ngày kiểm tra
        public string Comments { get; set; } // Ghi chú
    }
}
