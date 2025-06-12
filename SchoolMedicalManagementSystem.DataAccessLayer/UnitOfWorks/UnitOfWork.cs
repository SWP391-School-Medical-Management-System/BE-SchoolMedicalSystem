using SchoolMedicalManagementSystem.DataAccessLayer.Context;
using SchoolMedicalManagementSystem.DataAccessLayer.Repositories.UserRepository;
using SchoolMedicalManagementSystem.DataAccessLayer.RepositoryContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.RepositoryContracts.IUserRepository;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;

namespace SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks;

public class UnitOfWork : BaseUnitOfWork<ApplicationDbContext>, IUnitOfWork
{
    public UnitOfWork(ApplicationDbContext context, IServiceProvider serviceProvider)
        : base(context, serviceProvider)
    {
    }

    public IUserRepository UserRepository => GetRepository<IUserRepository>();
    public IRoleRepository RoleRepository => GetRepository<IRoleRepository>();
    public IUserRoleRepository UserRoleRepository => GetRepository<IUserRoleRepository>();
    public ISchoolClassRepository SchoolClassRepository => GetRepository<ISchoolClassRepository>();
    public IMedicalRecordRepository MedicalRecordRepository => GetRepository<IMedicalRecordRepository>();
    public IMedicalConditionRepository MedicalConditionRepository => GetRepository<IMedicalConditionRepository>();
    public IHealthEventRepository HealthEventRepository => GetRepository<IHealthEventRepository>();
    public IStudentMedicationRepository StudentMedicationRepository => GetRepository<IStudentMedicationRepository>();
}