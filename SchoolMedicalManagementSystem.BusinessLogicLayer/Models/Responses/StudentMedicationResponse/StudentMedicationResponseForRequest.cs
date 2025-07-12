using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.StudentMedicationResponse
{
    public class StudentMedicationResponseForRequest
    {
        public Guid Id { get; set; }                // ID của StudentMedication
        public Guid RequestId { get; set; }         // Liên kết với StudentMedicationRequestId

        public string MedicationName { get; set; }  // Tên thuốc
        public string Dosage { get; set; }          // Liều lượng
        public string? Purpose { get; set; }        // Mục đích sử dụng
        public DateTime ExpiryDate { get; set; }    // Ngày hết hạn
        public int QuantitySent { get; set; }       // Số lượng gửi
        public string? QuantityUnit { get; set; }   // Đơn vị số lượng

        public string? RejectionReason { get; set; } // Lý do từ chối (nếu có)

        public MedicationPriority Priority { get; set; } // Ưu tiên
        public string PriorityDisplayName { get; set; }  // Hiển thị tên ưu tiên
    }
}
