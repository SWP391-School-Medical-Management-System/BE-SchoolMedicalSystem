using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities
{
    /// <summary>
    /// Thuốc học sinh - quản lý việc gửi thuốc cho học sinh tại trường
    /// </summary>
    public class StudentMedication : BaseEntity
    {
        public Guid StudentId { get; set; }             // ID học sinh
        public Guid ParentId { get; set; }              // ID phụ huynh gửi thuốc
        public Guid? ApprovedById { get; set; }         // ID y tá phê duyệt

        // Thông tin thuốc cơ bản
        public string MedicationName { get; set; }      // Tên thuốc
        public string Dosage { get; set; }              // Liều lượng (1 viên, 100ml)
        public string Instructions { get; set; }        // Hướng dẫn sử dụng
        public string? Frequency { get; set; }           // Số ngày cần dùng
        public int? FrequencyCount { get; set; }        // 3, 2 - số lần uống/ngày   3 lần 
        public string? FrequencyUnit { get; set; }      // "lần/ngày"
        public DateTime ExpiryDate { get; set; }        // Ngày hết hạn thuốc        
        public string? Purpose { get; set; }            // Mục đích sử dụng
        public string? SideEffects { get; set; }        // Tác dụng phụ
        public string? StorageInstructions { get; set; } // Hướng dẫn bảo quản

        // Thông tin từ parent
        public string? DoctorName { get; set; }         // Tên bác sĩ kê đơn
        public string? Hospital { get; set; }           // Bệnh viện/Phòng khám
        public DateTime? PrescriptionDate { get; set; } // Ngày kê đơn
        public string? PrescriptionNumber { get; set; } // Số đơn thuốc
        public DateTime? StartDate { get; set; }         // Ngày bắt đầu dùng thuốc
        public int QuantitySent { get; set; }           // Số lượng Parent gửi        9
        public QuantityUnitEnum? QuantityUnit { get; set; }       // Đơn vị (viên, chai, gói)
        public string? SpecialNotes { get; set; }       // Ghi chú từ Parent
        public string? EmergencyContactInstructions { get; set; } // Hướng dẫn khẩn cấp

        // Cài đặt quản lý (do School Nurse thiết lập)
        public int TotalDoses { get; set; } = 0;           // Y tá tính từ quantity và dosage
        public int RemainingDoses { get; set; } = 0;       // Y tá theo dõi
        public int MinStockThreshold { get; set; } = 3;    // Y tá thiết lập
        public bool AutoGenerateSchedule { get; set; } = true;
        public bool RequireNurseConfirmation { get; set; } = false;
        public bool SkipOnAbsence { get; set; } = true;
        public bool LowStockAlertSent { get; set; } = false;
        public string? ManagementNotes { get; set; }       // Ghi chú quản lý từ y tá
        public bool SkipWeekends { get; set; } = false;    // Bỏ qua cuối tuần
        public string? SpecificTimes { get; set; }         // JSON: ["08:00", "12:00", "18:00"]
        public string? TimesOfDay { get; set; }            // JSON: ["Morning", "AfterLunch", "LateAfternoon"]
        public string? SkipDates { get; set; }             // JSON: ["2024-12-25", "2024-01-01"]
        public ReceivedMedication Received { get; set; }    // Trạng thái nhận thuốc từ Parent
        public int QuantityReceive { get; set; } = 0;      // Nhận được bao nhiêu thuốc từ Parent

        public StudentMedicationStatus Status { get; set; } // Trạng thái
        public string? RejectionReason { get; set; }    // Lý do từ chối (nếu có)
        public DateTime? ApprovedAt { get; set; }       // Thời gian phê duyệt
        public DateTime? SubmittedAt { get; set; }      // Thời gian gửi yêu cầu
        public MedicationPriority Priority { get; set; } = MedicationPriority.Normal;

        // Khóa ngoại liên kết với request
        public Guid StudentMedicationRequestId { get; set; } // Khóa ngoại tới StudentMedicationRequest
        [ForeignKey(nameof(StudentMedicationRequestId))]
        public virtual StudentMedicationRequest Request { get; set; } // Navigation property tới request

        // Navigation Properties
        public virtual ApplicationUser Student { get; set; }        // Học sinh
        public virtual ApplicationUser Parent { get; set; }         // Phụ huynh
        public virtual ApplicationUser ApprovedBy { get; set; }     // Người phê duyệt
        public virtual ICollection<MedicationAdministration> Administrations { get; set; } // Lịch sử cho uống thuốc
        public virtual ICollection<MedicationSchedule> Schedules { get; set; } = new List<MedicationSchedule>(); // Lịch trình uống thuốc
        public virtual ICollection<MedicationStock> StockHistory { get; set; } = new List<MedicationStock>(); // Lịch sử gửi thuốc
        public virtual ICollection<StudentMedicationUsageHistory> UsageHistory { get; set; } = new List<StudentMedicationUsageHistory>();
    }
}