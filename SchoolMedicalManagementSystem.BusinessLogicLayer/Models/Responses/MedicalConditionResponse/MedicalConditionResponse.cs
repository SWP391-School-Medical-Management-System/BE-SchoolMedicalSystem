using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalCondition;

public class MedicalConditionResponse
{
    public Guid Id { get; set; }
    public Guid MedicalRecordId { get; set; }
    public string? StudentName { get; set; }
    public string? StudentCode { get; set; }
    public MedicalConditionType Type { get; set; }

    public string TypeDisplay => Type switch
    {
        MedicalConditionType.Allergy => "Dị ứng",
        MedicalConditionType.ChronicDisease => "Bệnh mãn tính",
        MedicalConditionType.MedicalHistory => "Lịch sử y tế",
        _ => "Không xác định"
    };

    public string Name { get; set; }
    public SeverityType? Severity { get; set; }

    public string? SeverityDisplay => Severity?.ToString() switch
    {
        "Mild" => "Nhẹ",
        "Moderate" => "Trung bình",
        "Severe" => "Nghiêm trọng",
        _ => null
    };

    public string? Reaction { get; set; } // Chỉ cho dị ứng
    public string? Treatment { get; set; } // Chỉ cho bệnh mãn tính và lịch sử y tế
    public string? Medication { get; set; }
    public DateTime? DiagnosisDate { get; set; } // Chỉ cho lịch sử y tế
    public string? Hospital { get; set; }
    public string? Doctor { get; set; }
    public string? Notes { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
}