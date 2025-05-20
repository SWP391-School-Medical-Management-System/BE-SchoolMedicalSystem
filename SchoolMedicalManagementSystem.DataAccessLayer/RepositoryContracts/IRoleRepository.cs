using SchoolMedicalManagementSystem.DataAccessLayer.Entities;

namespace SchoolMedicalManagementSystem.DataAccessLayer.RepositoryContracts.IUserRepository;

public interface IRoleRepository : IBaseRepository<Role>
{
    Task<Role> GetRoleByNameAsync(string roleName);
}