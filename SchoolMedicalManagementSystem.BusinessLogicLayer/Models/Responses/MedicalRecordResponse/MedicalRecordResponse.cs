namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses.MedicalRecordResponse;

public class MedicalRecordResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    
    // Student Information (chỉ những gì cần thiết)
    public string StudentName { get; set; }
    public string StudentCode { get; set; }
    
    // Basic Medical Information
    public string BloodType { get; set; }
    public double Height { get; set; }
    public double Weight { get; set; }
    public double? BMI { get; set; }
    public string EmergencyContact { get; set; }
    public string EmergencyContactPhone { get; set; }
    
    // Medical Condition Summary (chỉ số lượng)
    public int AllergyCount { get; set; }
    public int ChronicDiseaseCount { get; set; }
    
    // Hiển thị danh sách học sinh có hồ sơ y tế cần cập nhật
    // Nhắc nhở kiểm tra sức khỏe định kỳ
    public bool NeedsUpdate { get; set; }
    
    // Audit Information (chỉ cần thiết)
    public DateTime? CreatedDate { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
}
