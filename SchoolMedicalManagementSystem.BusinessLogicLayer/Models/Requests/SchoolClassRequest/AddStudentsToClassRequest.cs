namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.SchoolClassRequest;

public class AddStudentsToClassRequest
{
    public List<Guid> StudentIds { get; set; } = new List<Guid>();
}