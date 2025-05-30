namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.SchoolClassRequest;

public class CreateSchoolClassRequest
{
    public string Name { get; set; }
    public int Grade { get; set; }
    public int AcademicYear { get; set; }
}