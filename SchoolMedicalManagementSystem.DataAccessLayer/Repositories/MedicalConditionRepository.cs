using SchoolMedicalManagementSystem.DataAccessLayer.Context;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.Repositories;
using SchoolMedicalManagementSystem.DataAccessLayer.Repositories.UserRepository;

namespace SchoolMedicalManagementSystem.DataAccessLayer.RepositoryContracts.IUserRepository;

public class MedicalConditionRepository : BaseRepository<MedicalCondition>, IMedicalConditionRepository
{
    private readonly ApplicationDbContext _context;

    public MedicalConditionRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }
}