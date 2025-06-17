namespace SchoolMedicalManagementSystem.DataAccessLayer.Enums;

/// <summary>
/// Trạng thái lịch trình uống thuốc
/// </summary>
public enum MedicationScheduleStatus
{
    Pending, // Chờ thực hiện
    Completed, // Đã hoàn thành
    Missed, // Đã bỏ lỡ
    Cancelled, // Đã hủy (do học sinh nghỉ học, thuốc hết hạn, etc.)
    StudentAbsent // Học sinh vắng mặt
}