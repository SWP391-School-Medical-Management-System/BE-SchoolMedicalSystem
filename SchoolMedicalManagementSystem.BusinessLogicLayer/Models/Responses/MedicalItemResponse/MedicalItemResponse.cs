using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalItemResponse;

public class MedicalItemResponse
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string? Dosage { get; set; }
    public MedicationForm? Form { get; set; }
    public string FormDisplayName { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int Quantity { get; set; }
    public string Unit { get; set; }
    public string Justification { get; set; }
    public string Status { get; set; }
    public string StatusDisplayName { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string Priority { get; set; }
    public string PriorityDisplayName { get; set; }
    public bool IsUrgent { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsExpiringSoon { get; set; }
    public bool IsExpired { get; set; }
    public bool IsLowStock { get; set; }
    public string StatusText { get; set; }

    // Requester info
    public string RequestedByName { get; set; }
    public string RequestedByStaffCode { get; set; }

    // Approver info
    public string? ApprovedByName { get; set; }
    public string? ApprovedByStaffCode { get; set; }

    // Permission flags
    public bool CanApprove { get; set; } // Manager có thể approve
    public bool CanReject { get; set; } // Manager có thể reject
    public bool CanUse { get; set; } // Có thể sử dụng (approved)
}