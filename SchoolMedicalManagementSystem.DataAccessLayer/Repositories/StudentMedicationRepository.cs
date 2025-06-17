using SchoolMedicalManagementSystem.DataAccessLayer.Context;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.RepositoryContracts;

namespace SchoolMedicalManagementSystem.DataAccessLayer.Repositories;

public class StudentMedicationRepository : BaseRepository<MedicationAdministration>, IStudentMedicationRepository
{
    private readonly ApplicationDbContext _context;

    public StudentMedicationRepository(ApplicationDbContext context)
        : base(context)
    {
        _context = context;
    }
}