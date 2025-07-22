using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest
{
    public class AdministerMedicationRequest
    {
        public StatusUsage Status { get; set; } // Trạng thái sử dụng: Used, Skipped, Missed
        public string? DosageUsed { get; set; } // Liều lượng sử dụng (cho Bottle)
        public string? Note { get; set; } // Ghi chú (bắt buộc nếu Skipped hoặc Missed)
        public bool IsMakeupDose { get; set; } // Có phải là liều uống bù không
        public DateTime? AdministeredTime { get; set; }  // Thời gian uống
    }
}
