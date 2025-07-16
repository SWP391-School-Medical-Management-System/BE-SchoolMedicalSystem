using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.StudentMedicationRequest
{
    public class UpdateQuantityReceivedRequest
    {
        public List<MedicationQuantityUpdate> Medications { get; set; } // Danh sách thuốc cần cập nhật

        public class MedicationQuantityUpdate
        {
            public Guid MedicationId { get; set; } // ID của StudentMedication
            public int QuantityReceived { get; set; } // Số lượng nhận được
            public string? Notes { get; set; } // Ghi chú tùy chọn
        }
    }
}
