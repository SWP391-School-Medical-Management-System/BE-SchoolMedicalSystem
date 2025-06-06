namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.MedicalRecordRequest;

public class UpdateMedicalRecordRequest
{
    public string? BloodType { get; set; }
    public double? Height { get; set; }
    public double? Weight { get; set; }
    public string? EmergencyContact { get; set; }
    public string? EmergencyContactPhone { get; set; }
}