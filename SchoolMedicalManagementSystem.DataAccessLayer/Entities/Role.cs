namespace SchoolMedicalManagementSystem.DataAccessLayer.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } // Tên vai trò: "ADMIN", "MANAGER", "SCHOOLNURSE", "PARENT", "STUDENT"

    public virtual ICollection<UserRole> UserRoles { get; set; }
}