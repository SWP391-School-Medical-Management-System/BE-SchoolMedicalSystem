using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Tình trạng y tế của học sinh
/// </summary>
public class MedicalCondition : BaseEntity
{
    public Guid MedicalRecordId { get; set; }  // ID hồ sơ y tế của học sinh
    public Guid? HealthCheckId { get; set; }
    public MedicalConditionType Type { get; set; }  // Loại tình trạng: "Allergy" (Dị ứng), "ChronicDisease" (Bệnh mãn tính), "MedicalHistory" (Lịch sử y tế)
    public string Name { get; set; }  // Tên tình trạng, bệnh hoặc dị ứng (ví dụ: "Hen suyễn", "Dị ứng lạc", "Gãy tay")
    public SeverityType? Severity { get; set; }    // Mức độ nghiêm trọng: Mild (Nhẹ), Moderate (Trung bình), Severe (Nghiêm trọng)
    public string? Reaction { get; set; }       // Phản ứng khi bị dị ứng (ví dụ: "Phát ban", "Khó thở") - chỉ cho dị ứng
    public string Treatment { get; set; }      // Phương pháp điều trị
    public string Medication { get; set; }     // Thuốc điều trị (ví dụ: "Paracetamol", "Ventolin")
    public DateTime? DiagnosisDate { get; set; } // Ngày chẩn đoán (cho lịch sử y tế)
    public string Hospital { get; set; }       // Bệnh viện điều trị
    public string Doctor { get; set; }         // Bác sĩ điều trị
    public string Notes { get; set; }          // Ghi chú bổ sung
    public double? SystolicBloodPressure { get; set; } // Huyết áp tâm thu (mmHg)
    public double? DiastolicBloodPressure { get; set; } // Huyết áp tâm trương (mmHg)
    public double? HeartRate { get; set; }     // Nhịp tim (bpm)

    public virtual MedicalRecord MedicalRecord { get; set; } // Hồ sơ y tế chứa tình trạng này
    public virtual HealthCheck? HealthCheck { get; set; }
}