namespace SchoolMedicalManagementSystem.DataAccessLayer.Enums;

/// <summary>
/// Mức độ ưu tiên thuốc
/// </summary>
public enum MedicationPriority
{
    Low,        // Thấp - có thể trễ vài giờ
    Normal,     // Bình thường - đúng giờ
    High,       // Cao - cần đúng giờ
    Critical    // Rất quan trọng - không được bỏ lỡ
}