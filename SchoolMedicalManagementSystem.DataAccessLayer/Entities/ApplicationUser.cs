namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

public class ApplicationUser : BaseEntity
{
    public string Username { get; set; } // Tên đăng nhập của người dùng
    public string PasswordHash { get; set; } // Mật khẩu đã được hash
    public string Email { get; set; } // Địa chỉ email
    public string PhoneNumber { get; set; } // Số điện thoại liên hệ
    public string FullName { get; set; } // Họ và tên đầy đủ
    public string Address { get; set; } // Địa chỉ nơi ở
    public DateTime? DateOfBirth { get; set; } // Ngày sinh
    public string? Gender { get; set; } // Giới tính
    public bool IsActive { get; set; } = false; // Trạng thái tài khoản (kích hoạt/không kích hoạt)
    public string? ProfileImageUrl { get; set; } // Đường dẫn đến ảnh đại diện

    public string? StaffCode { get; set; } // ID nhân viên (cho Admin/Manager/SchoolNurse)
    public string? LicenseNumber { get; set; } // Số giấy phép hành nghề (cho SchoolNurse)
    public string? Specialization { get; set; } // Chuyên môn (cho SchoolNurse)
    
    public string? StudentCode { get; set; } // Mã học sinh (cho Student)
    public Guid? ParentId { get; set; } // ID phụ huynh (cho Student - liên kết với User là parent)
    public string? Relationship { get; set; } // Quan hệ với học sinh: "Father", "Mother", "Guardian" (cho Parent)

    public virtual ICollection<UserRole> UserRoles { get; set; }
    public virtual ApplicationUser Parent { get; set; }                     // Liên kết đến phụ huynh (nếu là học sinh)
    public virtual ICollection<ApplicationUser> Children { get; set; }      // Danh sách con (nếu là phụ huynh)
    public virtual ICollection<StudentClass> StudentClasses { get; set; } 
    public virtual MedicalRecord MedicalRecord { get; set; }                // Hồ sơ y tế (nếu là học sinh)
    public virtual ICollection<HealthEvent> HandledHealthEvents { get; set; } // Các sự kiện y tế đã xử lý (nếu là y tá)
    public virtual ICollection<HealthCheck> ConductedHealthChecks { get; set; } // Các kiểm tra y tế đã thực hiện (nếu là y tá)
    public virtual ICollection<Notification> SentNotifications { get; set; }    // Thông báo đã gửi
    public virtual ICollection<Notification> ReceivedNotifications { get; set; } // Thông báo đã nhận
    public virtual ICollection<BlogPost> BlogPosts { get; set; }            // Bài viết blog (nếu là Admin/Manager/SchoolNurse)
    public virtual ICollection<BlogComment> BlogComments { get; set; }      // Bình luận trên blog
    public virtual ICollection<Report> GeneratedReports { get; set; }       // Báo cáo đã tạo (nếu là Admin/Manager/SchoolNurse)
    public virtual ICollection<StudentMedication> SentMedications { get; set; }      // Thuốc phụ huynh gửi
    public virtual ICollection<StudentMedication> ApprovedMedications { get; set; }  // Thuốc y tá phê duyệt
    public virtual ICollection<MedicationAdministration> MedicationAdministrations { get; set; } // Lịch sử cho uống thuốc
    public virtual ICollection<VaccinationSession> CreatedVaccinationSessions { get; set; }  // Tạo buổi tiêm 
    public virtual ICollection<VaccinationSession> ApprovedVaccinationSessions { get; set; }  // Sự đồng thuận để tạo buổi tiêm
    public virtual ICollection<VaccinationConsent> VaccinationConsents { get; set; }    //  Xin phép phụ huynh 
    public virtual ICollection<VaccinationConsent> ParentVaccinationConsents { get; set; }  // Sự đồng thuận của phụ huynh
    public virtual ICollection<VaccinationAssignment> VaccinationAssignments { get; set; }  // Phân công nhân viên y tế

}