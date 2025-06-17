namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Lịch sử cho uống thuốc tại trường
/// </summary>
public class MedicationAdministration : BaseEntity
{
    public Guid StudentMedicationId { get; set; }   // ID thuốc học sinh
    public Guid AdministeredById { get; set; }      // ID y tá cho uống thuốc
    public DateTime AdministeredAt { get; set; }    // Thời gian cho uống
    
    public string ActualDosage { get; set; }         // Liều lượng thực tế đã cho
    public string? Notes { get; set; }               // Ghi chú
    public bool StudentRefused { get; set; }         // Học sinh có từ chối không
    public string? RefusalReason { get; set; }       // Lý do từ chối
    public string? SideEffectsObserved { get; set; } // Tác dụng phụ quan sát được
    
    // Navigation Properties
    public virtual StudentMedication StudentMedication { get; set; } // Thuốc học sinh
    public virtual ApplicationUser AdministeredBy { get; set; }      // Y tá cho uống thuốc
    public virtual ICollection<MedicationSchedule> CompletedSchedules { get; set; } = new List<MedicationSchedule>(); // Các lịch trình được hoàn thành
}
