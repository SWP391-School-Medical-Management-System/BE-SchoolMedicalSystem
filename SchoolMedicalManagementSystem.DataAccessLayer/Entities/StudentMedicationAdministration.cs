namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Lịch sử cho học sinh uống thuốc
/// </summary>
public class StudentMedicationAdministration : BaseEntity
{
    public Guid StudentMedicationId { get; set; }   // ID thuốc học sinh
    public Guid? AdministeredById { get; set; }      // ID School Nurse cho uống
    public DateTime AdministeredAt { get; set; }    // Thời gian cho uống
    public string ActualDosage { get; set; }        // Liều lượng thực tế
    public string Notes { get; set; }               // Ghi chú
    public bool StudentRefused { get; set; }        // Học sinh từ chối uống
    public string? RefusalReason { get; set; }      // Lý do từ chối
    
    public virtual StudentMedication StudentMedication { get; set; }
    public virtual ApplicationUser AdministeredBy { get; set; }
}