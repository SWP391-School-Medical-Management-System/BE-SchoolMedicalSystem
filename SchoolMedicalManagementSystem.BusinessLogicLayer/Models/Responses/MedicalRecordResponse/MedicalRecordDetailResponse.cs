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