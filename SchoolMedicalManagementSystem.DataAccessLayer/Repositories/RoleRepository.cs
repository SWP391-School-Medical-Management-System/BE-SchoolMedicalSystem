using Microsoft.EntityFrameworkCore;
using SchoolMedicalManagementSystem.DataAccessLayer.Context;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.RepositoryContracts.IUserRepository;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Repositories.UserRepository;

public class RoleRepository : BaseRepository<Role>, IRoleRepository
{
    private readonly ApplicationDbContext _context;

    public RoleRepository(ApplicationDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<Role> GetRoleByNameAsync(string roleName)
    {
        return await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
    }
}