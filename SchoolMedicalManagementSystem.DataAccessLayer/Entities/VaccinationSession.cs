using System.ComponentModel.DataAnnotations;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Quản lý thông tin về một buổi tiêm vaccine
/// </summary>
public class VaccinationSession : BaseEntity
{
    public Guid VaccineTypeId { get; set; }        // Foreign Key đến VaccinationType
    public string SessionName { get; set; }        // Tên của buổi tiêm
    public string ResponsibleOrganizationName { get; set; }  // Tên tổ chức chịu trách nhiệm
    public string Location { get; set; }           // Địa điểm tiêm
    public DateTime StartTime { get; set; }        // Thời gian bắt đầu
    public DateTime EndTime { get; set; }          // Thời gian kết thúc
    public string Status { get; set; }             // Trạng thái: PendingApproval, WaitingForParentConsent, Scheduled, Completed, Cancelled
    public Guid CreatedById { get; set; }          // Foreign Key đến ApplicationUser (School Nurse)
    public Guid? ApprovedById { get; set; }        // Foreign Key đến ApplicationUser (Manager), nullable
    public DateTime? ApprovedDate { get; set; }    // Thời gian duyệt, nullable
    public string SideEffect {  get; set; }        // Tác dụng phụ
    public string Contraindication { get; set; }   // Chống chỉ định
    public string Notes { get; set; }              // Ghi chú, tùy chọn


    public virtual VaccinationType VaccineType { get; set; }      // Quan hệ với loại vaccine
    public virtual ApplicationUser CreatedBy { get; set; }        // Quan hệ với School Nurse
    public virtual ApplicationUser ApprovedBy { get; set; }       // Quan hệ với Manager
    public virtual ICollection<VaccinationSessionClass> Classes { get; set; } // Quan hệ với các lớp
    public virtual ICollection<VaccinationConsent> Consents { get; set; }    // Quan hệ với đồng ý của phụ huynh
    public virtual ICollection<VaccinationAssignment> Assignments { get; set; } // Quan hệ với phân công Nurse
    public virtual ICollection<VaccinationRecord> Records { get; set; } // Quan hệ với hồ sơ tiêm chủng
}