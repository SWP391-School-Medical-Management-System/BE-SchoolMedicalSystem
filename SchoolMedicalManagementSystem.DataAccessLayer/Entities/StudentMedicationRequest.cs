using System;
using System.Collections.Generic;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities
{
    /// <summary>
    /// Yêu cầu gửi thuốc cho học sinh, đại diện cho một request tổng với nhiều loại thuốc
    /// </summary>
    public class StudentMedicationRequest : BaseEntity
    {
        public Guid StudentId { get; set; }             // ID học sinh
        public Guid ParentId { get; set; }              // ID phụ huynh gửi yêu cầu
        public Guid? ApprovedById { get; set; }         // ID y tá phê duyệt (nếu có)

        public StudentMedicationStatus Status { get; set; } // Trạng thái của request
        public string? RejectionReason { get; set; }     // Lý do từ chối (nếu có)
        public DateTime? SubmittedAt { get; set; }       // Thời gian gửi yêu cầu
        public DateTime? ApprovedAt { get; set; }        // Thời gian phê duyệt (nếu có)
        public MedicationPriority Priority { get; set; } // Mức độ ưu tiên (lấy cao nhất từ danh sách thuốc)

        public string StudentName { get; set; }         // Tên học sinh
        public string StudentCode { get; set; }         // Mã học sinh
        public string ParentName { get; set; }          // Tên phụ huynh
        public string? ApprovedByName { get; set; }     // Tên y tá phê duyệt (nếu có)

        public DateTime CreatedDate { get; set; }       // Thời gian tạo request
        public DateTime? LastUpdatedDate { get; set; }  // Thời gian cập nhật lần cuối

        // Navigation Properties
        public virtual ApplicationUser Student { get; set; }        // Học sinh
        public virtual ApplicationUser Parent { get; set; }         // Phụ huynh
        public virtual ApplicationUser ApprovedBy { get; set; }     // Y tá phê duyệt (nếu có)
        public virtual ICollection<StudentMedication> MedicationsDetails { get; set; }   // Liên kết với các bản ghi thuốc chi tiết
    }
}