namespace SchoolMedicalManagementSystem.DataAccessLayer.Enums;

public enum AppointmentStatus
{
    Scheduled,      // Đã lên lịch
    Completed,      // Đã hoàn thành
    Cancelled,      // Đã hủy
    Rescheduled,    // Đã đổi lịch
    Pending         // Chờ xác nhận
}