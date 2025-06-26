namespace SchoolMedicalManagementSystem.DataAccessLayer.Enums;

/// <summary>
/// Loại tần suất uống thuốc
/// </summary>
public enum MedicationFrequencyType
{
    Daily,              // Hàng ngày
    EveryOtherDay,      // Cách ngày
    Weekly,             // Hàng tuần
    BiWeekly,           // 2 tuần/lần
    Monthly,            // Hàng tháng
    AsNeeded,           // Khi cần thiết
    SpecificDays        // Những ngày cụ thể (T2, T4, T6)
}