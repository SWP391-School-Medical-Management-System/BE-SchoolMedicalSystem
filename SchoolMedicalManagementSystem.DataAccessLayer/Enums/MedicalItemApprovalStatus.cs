namespace SchoolMedicalManagementSystem.DataAccessLayer.Enums;

public enum MedicalItemApprovalStatus
{
    Pending,      // Chờ phê duyệt
    Approved,     // Đã phê duyệt
    Rejected,     // Bị từ chối
    Draft         // Bản nháp (School Nurse có thể chỉnh sửa)
}