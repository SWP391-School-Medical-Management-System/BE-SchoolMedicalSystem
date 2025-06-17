namespace SchoolMedicalManagementSystem.DataAccessLayer.Enums;

/// <summary>
/// Thời điểm trong ngày uống thuốc
/// </summary>
public enum MedicationTimeOfDay
{
    BeforeBreakfast,    // Trước bữa sáng
    AfterBreakfast,     // Sau bữa sáng
    BeforeLunch,        // Trước bữa trưa
    AfterLunch,         // Sau bữa trưa
    BeforeDinner,       // Trước bữa tối
    AfterDinner,        // Sau bữa tối
    BeforeBed,          // Trước khi ngủ
    SpecificTime        // Giờ cụ thể
}