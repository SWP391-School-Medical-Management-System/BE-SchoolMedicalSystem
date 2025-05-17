namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Hồ sơ tiêm chủng của học sinh
/// </summary>
public class VaccinationRecord : BaseEntity
{
    public Guid UserId { get; set; }               // ID học sinh
    public Guid MedicalRecordId { get; set; }      // ID hồ sơ y tế
    public Guid VaccinationTypeId { get; set; }    // ID loại vắc-xin
    public int DoseNumber { get; set; }            // Số mũi tiêm (1, 2, 3)
    public DateTime AdministeredDate { get; set; } // Ngày tiêm
    public string AdministeredBy { get; set; }     // Người tiêm
    public string BatchNumber { get; set; }        // Số lô vắc-xin
    public string Notes { get; set; }              // Ghi chú
    
    public virtual ApplicationUser Student { get; set; }         // Học sinh được tiêm
    public virtual MedicalRecord MedicalRecord { get; set; }     // Hồ sơ y tế
    public virtual VaccinationType VaccinationType { get; set; } // Loại vắc-xin
    public virtual ICollection<Notification> Notifications { get; set; } // Thông báo tiêm chủng
}
