using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalRecordRequest
{
    public class CreatePhysicalRecordRequest
    {       
        public decimal? Height { get; set; } // Chiều cao
        public decimal? Weight { get; set; } // Cân nặng
        public DateTime? CheckDate { get; set; } // Ngày kiểm tra thể chất
        public string Comments { get; set; } // Ghi chú thể chất
    }
}
