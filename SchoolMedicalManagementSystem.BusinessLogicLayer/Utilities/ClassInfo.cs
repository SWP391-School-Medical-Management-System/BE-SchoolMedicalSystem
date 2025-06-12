namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Utilities;

public class ClassInfo
{
    public string Name { get; set; }
    public int Grade { get; set; }
    public int AcademicYear { get; set; }
    public Guid? ClassId { get; set; }
    public bool IsValid { get; set; } = true;
    public string ErrorMessage { get; set; } = string.Empty;
}