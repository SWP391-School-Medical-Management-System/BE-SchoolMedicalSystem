using SchoolMedicalManagementSystem.DataAccessLayer.Context;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.RepositoryContracts.IUserRepository;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Repositories.UserRepository;

public class UserRoleRepository : BaseRepository<UserRole>, IUserRoleRepository
{
    private readonly ApplicationDbContext _context;

    public UserRoleRepository(ApplicationDbContext context)
        : base(context)
    {
        _context = context;
    }
}