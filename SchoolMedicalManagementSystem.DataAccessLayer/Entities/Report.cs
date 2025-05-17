using SchoolMedicalManagementSystem.DataAccessLayer.Enums;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

/// <summary>
/// Báo cáo y tế
/// </summary>
public class Report : BaseEntity
{
    public string Title { get; set; }       // Tiêu đề báo cáo
    public string Description { get; set; } // Mô tả báo cáo
    public ReportType ReportType { get; set; } // Loại báo cáo: HealthCheckSummary, MedicationUsage, HealthEventStatistics
    public DateTime StartPeriod { get; set; } // Thời gian bắt đầu báo cáo
    public DateTime EndPeriod { get; set; }   // Thời gian kết thúc báo cáo
    public Guid GeneratedById { get; set; }   // ID người tạo báo cáo
    public ReportFormat ReportFormat { get; set; } // Định dạng báo cáo: PDF, Excel, Word
    public string StoragePath { get; set; }   // Đường dẫn lưu trữ báo cáo
    
    public virtual ApplicationUser GeneratedBy { get; set; } // Người tạo báo cáo
}
