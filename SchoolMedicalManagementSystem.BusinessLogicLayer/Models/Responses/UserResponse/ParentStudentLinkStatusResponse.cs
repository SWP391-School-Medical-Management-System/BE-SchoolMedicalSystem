namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;

public class ParentStudentLinkStatusResponse
{
    public Guid ParentId { get; set; }
    public string ParentName { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; }
    public bool IsLinked { get; set; }
    public Guid? CurrentParentId { get; set; }
    public string CurrentParentName { get; set; }
    public int TotalStudentsLinkedToParent { get; set; }
    public bool CanLink { get; set; }
    public bool CanUnlink { get; set; }
    public string UnlinkBlockReason { get; set; }
    public bool HasSevereMedicalConditions { get; set; }
}