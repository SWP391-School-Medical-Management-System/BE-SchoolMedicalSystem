using SchoolMedicalManagementSystem.DataAccessLayer.RepositoryContracts.IBaseUnitOfWork;
using SchoolMedicalManagementSystem.DataAccessLayer.RepositoryContracts.IUserRepository;

namespace SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;

public interface IUnitOfWork : IBaseUnitOfWork
{
    public IUserRepository UserRepository { get; }
    public IRoleRepository RoleRepository { get; }
    public IUserRoleRepository UserRoleRepository { get; }
    public ISchoolClassRepository SchoolClassRepository { get; }
}