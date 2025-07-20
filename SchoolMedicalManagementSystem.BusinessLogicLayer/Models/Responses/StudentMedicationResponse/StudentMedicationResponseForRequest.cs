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
        public DateTime ExpiryDate { get; set; }    // Ngày hết hạn
        public DateTime? StartDate { get; set; }    // Ngày bắt đầu dùng thuốc
        public int QuantitySent { get; set; }       // Số lượng gửi
        public QuantityUnitEnum? QuantityUnit { get; set; } // Đơn vị (viên, chai, gói)
        public int QuantityReceived { get; set; }   // Số lượng nhận được
        public StudentMedicationStatus Status { get; set; } // Trạng thái
        public ReceivedMedication Received { get; set; }
        public MedicationPriority Priority { get; set; } // Ưu tiên
        public string PriorityDisplayName { get; set; }  // Hiển thị tên ưu tiên
        public string Instructions { get; set; }    // Hướng dẫn sử dụng
        public int FrequencyCount { get; set; }       // Tần suất
        public string SpecialNotes { get; set; }    // Ghi chú đặc biệt
        public List<string> TimesOfDay { get; set; } // Thời gian trong ngày (danh sách chuỗi)
    }
}
