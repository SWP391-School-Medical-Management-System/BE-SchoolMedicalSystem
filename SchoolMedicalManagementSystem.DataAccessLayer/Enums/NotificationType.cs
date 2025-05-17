namespace SchoolMedicalManagementSystem.DataAccessLayer.Enums;

public enum NotificationType
{
    HealthCheck, // Thông báo kiểm tra sức khỏe
    HealthEvent, // Thông báo sự kiện y tế (ốm đau, tai nạn)
    Vaccination, // Thông báo tiêm chủng
    Appointment, // Thông báo lịch hẹn tư vấn
    General // Thông báo chung
}