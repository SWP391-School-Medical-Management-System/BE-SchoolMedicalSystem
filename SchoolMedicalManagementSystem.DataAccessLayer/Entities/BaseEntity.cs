using System.ComponentModel.DataAnnotations;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

public abstract class BaseEntity
{
    [Key]
    public Guid Id { get; set; }

    public string? Code { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? LastUpdatedBy { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
}