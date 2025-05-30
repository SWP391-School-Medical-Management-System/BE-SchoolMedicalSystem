using SchoolMedicalManagementSystem.DataAccessLayer.Context;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.RepositoryContracts.IUserRepository;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Repositories.UserRepository;

public class SchoolClassRepository : BaseRepository<SchoolClass>, ISchoolClassRepository
{
    private readonly ApplicationDbContext _context;

    public SchoolClassRepository(ApplicationDbContext context)
        : base(context)
    {
        _context = context;
    }
}