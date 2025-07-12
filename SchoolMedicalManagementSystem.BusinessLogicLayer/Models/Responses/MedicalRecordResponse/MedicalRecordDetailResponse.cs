using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalCondition;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalRecordResponse;

public class MedicalRecordDetailResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    // Student Information (tối thiểu)
    public string StudentName { get; set; }
    public string StudentCode { get; set; }

    // Basic Medical Information
    public string BloodType { get; set; }
    public string EmergencyContact { get; set; }
    public string EmergencyContactPhone { get; set; }

    // New fields as arrays (latest first)
    public List<VisionRecordResponse> VisionRecords { get; set; } = new();
    public List<HearingRecordResponse> HearingRecords { get; set; } = new();
    public List<PhysicalRecordResponse> PhysicalRecords { get; set; } = new();

    // Medical Conditions (chi tiết)
    public List<MedicalConditionResponse> MedicalConditions { get; set; } = new();

    // Vaccination Records (tóm tắt)
    public List<VaccinationRecordResponse> VaccinationRecords { get; set; } = new();

    // Update Status
    public bool NeedsUpdate { get; set; }

    // Audit Information
    public DateTime? CreatedDate { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
}

// Response models for Vision, Hearing, Physical
public class VisionRecordResponse
{
    public decimal LeftEye { get; set; }      // Thị lực mắt trái
    public decimal RightEye { get; set; }     // Thị lực mắt phải
    public DateTime CheckDate { get; set; }   // Ngày kiểm tra
    public string? Comments { get; set; }     // Ghi chú
    public Guid RecordedBy { get; set; }      // Người ghi nhận
}

public class HearingRecordResponse
{
    public string LeftEar { get; set; }       // Thính lực tai trái
    public string RightEar { get; set; }      // Thính lực tai phải
    public DateTime CheckDate { get; set; }   // Ngày kiểm tra
    public string? Comments { get; set; }     // Ghi chú
    public Guid RecordedBy { get; set; }      // Người ghi nhận
}

public class PhysicalRecordResponse
{
    public decimal Height { get; set; }       // Chiều cao (cm)
    public decimal Weight { get; set; }       // Cân nặng (kg)
    public decimal BMI { get; set; }          // Chỉ số BMI
    public DateTime CheckDate { get; set; }   // Ngày kiểm tra
    public string? Comments { get; set; }     // Ghi chú
    public Guid RecordedBy { get; set; }      // Người ghi nhận
}