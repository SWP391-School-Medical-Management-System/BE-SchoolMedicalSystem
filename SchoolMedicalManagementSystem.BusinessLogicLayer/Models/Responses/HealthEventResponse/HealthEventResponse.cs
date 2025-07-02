using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.HealthEventResponse;

public class HealthEventResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? HandledById { get; set; }
    public HealthEventType EventType { get; set; }
    public string EventTypeDisplayName { get; set; }
    public string Description { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Location { get; set; }
    public string ActionTaken { get; set; }
    public string Code { get; set; }
    public string? Outcome { get; set; }
    public bool IsEmergency { get; set; }
    public Guid? RelatedMedicalConditionId { get; set; }
    public DateTime? CreatedDate { get; set; }
    public bool CanTakeOwnership { get; set; }
    public bool CanComplete { get; set; }
    public string StudentName { get; set; }
    public string StudentCode { get; set; }
    public string HandledByName { get; set; }
    public string RelatedMedicalConditionName { get; set; }
    public string EmergencyStatusText { get; set; } // "Khẩn cấp" / "Bình thường"
    public string CurrentHealthStatus { get; set; }   // Tình trạng sức khoẻ hiện tại (Nhịp tim, thân nhiệt, huyết áp, nhịp thở)
    public string? ParentNotice { get; set; }   // Lưu ý về nhà cho phụ huynh
    public List<HealthEventMedicalItemResponse> MedicalItemDetails { get; set; } = new List<HealthEventMedicalItemResponse>();
}
public class HealthEventMedicalItemResponse
{
    public string StudentName { get; set; }
    public string StudentClass { get; set; }
    public string? NurseName { get; set; }
    public string? MedicationName { get; set; }
    public double? MedicationQuantity { get; set; }
    public string? MedicationDosage { get; set; }
    public double? SupplyQuantity { get; set; }
}