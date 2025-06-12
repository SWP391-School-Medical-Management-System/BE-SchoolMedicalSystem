namespace SchoolMedicalManagementSystem.DataAccessLayer.Enums;

public enum StudentMedicationStatus
{
    PendingApproval,  // Chờ phê duyệt
    Approved,         // Đã phê duyệt
    Rejected,         // Bị từ chối
    Active,           // Đang thực hiện
    Completed,        // Hoàn thành
    Discontinued      // Ngưng sử dụng
}