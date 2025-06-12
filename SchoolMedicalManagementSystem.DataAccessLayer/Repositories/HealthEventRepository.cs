using SchoolMedicalManagementSystem.DataAccessLayer.Context;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.RepositoryContracts;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Repositories;

public class HealthEventRepository : BaseRepository<HealthEvent>, IHealthEventRepository
{
    private readonly ApplicationDbContext _context;

    public HealthEventRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }
}