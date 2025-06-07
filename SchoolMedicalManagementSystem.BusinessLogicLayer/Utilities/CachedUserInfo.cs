namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Utilities;

public class CachedUserInfo
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public string PasswordHash { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public List<string> Roles { get; set; }
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
}