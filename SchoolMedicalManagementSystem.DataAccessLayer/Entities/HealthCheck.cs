namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Thông tin về đợt kiểm tra sức khỏe định kỳ cho học sinh
/// </summary>
public class HealthCheck : BaseEntity
{
    public string Title { get; set; }          // Tiêu đề kiểm tra sức khỏe (ví dụ: "Kiểm tra sức khỏe học kỳ 1")
    public string Description { get; set; }    // Mô tả chi tiết đợt kiểm tra
    public string ResponsibleOrganizationName { get; set; }     // Tổ chức chịu trách nhiệm
    public DateTime ScheduledDate { get; set; } // Ngày lên lịch kiểm tra
    public DateTime StartTime { get; set; }     // Bắt đầu
    public DateTime EndTime { get; set; }       // Thời gian kết thúc
    public string Location { get; set; }        // Địa điểm kiểm tra
    public string Status { get; set; }          // Trạng thái: PendingApproval, WaitingForParentConsent, Scheduled, Completed, Cancelled)
    public Guid CreatedById { get; set; }        // Foreign Key đến ApplicationUser (School Nurse)
    public Guid? ApprovedById { get; set; }        // Foreign Key đến ApplicationUser (Manager), nullable
    public string? DeclineReason { get; set; }     // Lí do từ chối buổi tiêm
    public DateTime? ApprovedDate { get; set; }    // Thời gian duyệt, nullable
    public Guid? ConductedById { get; set; }   // ID của y tá thực hiện
    public string Notes { get; set; }           // Ghi chú
    
    public virtual ApplicationUser ConductedBy { get; set; }            // Y tá thực hiện kiểm tra
    public virtual ICollection<HealthCheckItemAssignment> HealthCheckItemAssignments { get; set; }
    public virtual ICollection<HealthCheckResult> Results { get; set; }  // Kết quả kiểm tra
    public virtual ICollection<Notification> Notifications { get; set; } // Thông báo kiểm tra
    public virtual ICollection<PhysicalRecord> PhysicalRecords { get; set; }
    public virtual ICollection<HearingRecord> HearingRecords { get; set; }
    public virtual ICollection<VisionRecord> VisionRecords { get; set; }
    public virtual ICollection<VitalSignRecord> VitalSignRecords { get; set; }
    public virtual ICollection<MedicalCondition> MedicalConditions { get; set; }
    public virtual ICollection<HealthCheckClass> HealthCheckClasses { get; set; }
    public virtual ICollection<HealthCheckAssignment> HealthCheckAssignments { get; set; }
    public virtual ICollection<HealthCheckConsent> HealthCheckConsents { get; set; }
}