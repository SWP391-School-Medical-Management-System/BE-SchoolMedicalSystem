using SchoolMedicalManagementSystem.DataAccessLayer.Context;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.RepositoryContracts;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Repositories;

public class MedicalItemRepository : BaseRepository<MedicalItem>, IMedicalItemRepository
{
    private readonly ApplicationDbContext _context;

    public MedicalItemRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }
}