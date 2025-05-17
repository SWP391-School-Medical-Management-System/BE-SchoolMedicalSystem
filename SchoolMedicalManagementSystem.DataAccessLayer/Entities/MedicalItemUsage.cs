namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Sử dụng thuốc và vật tư y tế
/// </summary>
public class MedicalItemUsage : BaseEntity
{
    public Guid MedicalItemId { get; set; }    // ID thuốc/vật tư y tế
    public Guid HealthEventId { get; set; }    // ID sự kiện y tế
    public double Quantity { get; set; }       // Số lượng sử dụng
    public string Notes { get; set; }          // Ghi chú
    public DateTime UsedAt { get; set; }       // Thời gian sử dụng
    public Guid UsedById { get; set; }         // ID người sử dụng (y tá)
    
    public virtual MedicalItem MedicalItem { get; set; } // Thuốc/vật tư được sử dụng
    public virtual HealthEvent HealthEvent { get; set; } // Sự kiện y tế
    public virtual ApplicationUser UsedBy { get; set; }  // Y tá sử dụng thuốc/vật tư
}