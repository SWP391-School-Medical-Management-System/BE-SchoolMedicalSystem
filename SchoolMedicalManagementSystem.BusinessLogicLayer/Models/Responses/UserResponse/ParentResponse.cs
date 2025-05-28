namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;

public class ParentResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }
    public string Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string ProfileImageUrl { get; set; }
    public string Relationship { get; set; }
    
    // Children information
    public List<StudentSummaryResponse> Children { get; set; }
    public int ChildrenCount { get; set; }
    
    public DateTime? CreatedDate { get; set; }
    public DateTime? LastUpdatedDate { get; set; }

    public ParentResponse()
    {
        Children = new List<StudentSummaryResponse>();
        ChildrenCount = 0;
    }
}