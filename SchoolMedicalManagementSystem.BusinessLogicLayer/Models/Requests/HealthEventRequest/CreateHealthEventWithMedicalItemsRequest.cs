using SchoolMedicalManagementSystem.DataAccessLayer.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthEventRequest
{
    public class CreateHealthEventWithMedicalItemsRequest
    {
        public Guid UserId { get; set; } // ID học sinh
        public HealthEventType EventType { get; set; }
        public string Description { get; set; }
        public DateTime OccurredAt { get; set; } // Thời gian xảy ra
        public string Location { get; set; } // Địa điểm xảy ra
        public string? ActionTaken { get; set; } // Hành động đã thực hiện
        public string? Outcome { get; set; } // Kết quả xử lý
        public bool IsEmergency { get; set; } // Trường hợp khẩn cấp
        public Guid? RelatedMedicalConditionId { get; set; } // Tình trạng y tế liên quan      
        public string CurrentHealthStatus { get; set; } // Tình trạng sức khỏe hiện tại
        public string? ParentNotice { get; set; } // Lưu ý về nhà cho phụ huynh

        public List<MedicalItemUsageRequest> MedicalItemUsages { get; set; } = new List<MedicalItemUsageRequest>();
    }
    public class MedicalItemUsageRequest
    {
        public Guid MedicalItemId { get; set; } // ID thuốc/vật tư y tế
        public double Quantity { get; set; } // Số lượng sử dụng
        public string Notes { get; set; } // Ghi chú
        public DateTime UsedAt { get; set; } // Thời gian sử dụng
        public double? Dose { get; set; }
        public double? MedicalPerOnce { get; set; }
    }
}
