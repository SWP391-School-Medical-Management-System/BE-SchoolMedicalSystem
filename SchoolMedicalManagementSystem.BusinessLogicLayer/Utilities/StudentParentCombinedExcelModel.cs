namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Utilities;

public class StudentParentCombinedExcelModel
{
    // Thông tin học sinh
    public string StudentUsername { get; set; }
    public string StudentEmail { get; set; }
    public string StudentFullName { get; set; }
    public string StudentAddress { get; set; }
    public string StudentGender { get; set; }
    public DateTime? StudentDateOfBirth { get; set; }
    public string StudentPhoneNumber { get; set; }
    public string StudentCode { get; set; }

    // Class information
    public string ClassNames { get; set; }
    public List<string> ClassList { get; set; } = new List<string>();
    public List<ClassInfo> ClassInfoList { get; set; } = new List<ClassInfo>();
    public List<ClassEnrollmentResult> ClassEnrollmentResults { get; set; } = new List<ClassEnrollmentResult>();

    // Thông tin phụ huynh
    public string ParentFullName { get; set; }
    public string ParentPhoneNumber { get; set; }
    public string ParentEmail { get; set; }
    public string ParentAddress { get; set; }
    public string ParentGender { get; set; }
    public DateTime? ParentDateOfBirth { get; set; }
    public string ParentRelationship { get; set; }
    public string LinkageType { get; set; } = "";

    // Validation
    public bool IsValid { get; set; } = true;
    public string ErrorMessage { get; set; } = string.Empty;

    // Processing flags
    public bool IsParentExisting { get; set; } = false; // Phụ huynh đã tồn tại trong hệ thống
    public Guid? ExistingParentId { get; set; } // ID phụ huynh nếu đã tồn tại
    public string GeneratedParentUsername { get; set; } // Username tự sinh cho phụ huynh
}