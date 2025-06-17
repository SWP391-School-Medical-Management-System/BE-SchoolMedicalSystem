using SchoolMedicalManagementSystem.DataAccessLayer.Repositories.UserRepository;
using SchoolMedicalManagementSystem.DataAccessLayer.RepositoryContracts;
using SchoolMedicalManagementSystem.DataAccessLayer.RepositoryContracts.IBaseUnitOfWork;
using SchoolMedicalManagementSystem.DataAccessLayer.RepositoryContracts.IUserRepository;

namespace SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;

public interface IUnitOfWork : IBaseUnitOfWork
{
    public IUserRepository UserRepository { get; }
    public IRoleRepository RoleRepository { get; }
    public IUserRoleRepository UserRoleRepository { get; }
    public ISchoolClassRepository SchoolClassRepository { get; }
    public IMedicalRecordRepository MedicalRecordRepository { get; }
    public IMedicalConditionRepository MedicalConditionRepository { get; }
    public IHealthEventRepository HealthEventRepository { get; }
    public IStudentMedicationRepository StudentMedicationRepository { get; }
    public IBlogPostRepository BlogPostRepository { get; }
    public IBlogCommentRepository BlogCommentRepository { get; }
}