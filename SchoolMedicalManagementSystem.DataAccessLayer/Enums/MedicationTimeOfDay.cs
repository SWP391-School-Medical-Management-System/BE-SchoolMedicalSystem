namespace SchoolMedicalManagementSystem.DataAccessLayer.Enums;

/// <summary>
/// Thời điểm trong ngày uống thuốc (chỉ trong giờ học 7h-17h)
/// </summary>
public enum MedicationTimeOfDay
{
    Morning,            // 7:00 - Buổi sáng sớm
    AfterBreakfast,     // 8:30 - Sau bữa sáng
    MidMorning,         // 10:00 - Giữa buổi sáng
    BeforeLunch,        // 11:30 - Trước bữa trưa
    AfterLunch,         // 13:00 - Sau bữa trưa
    MidAfternoon,       // 14:30 - Giữa buổi chiều
    LateAfternoon,      // 16:00 - Cuối buổi chiều
    BeforeDismissal     // 16:30 - Trước khi tan học
}