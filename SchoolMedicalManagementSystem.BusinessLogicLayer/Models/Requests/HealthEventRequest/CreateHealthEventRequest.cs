using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.HealthEventRequest;

public class CreateHealthEventRequest
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
}