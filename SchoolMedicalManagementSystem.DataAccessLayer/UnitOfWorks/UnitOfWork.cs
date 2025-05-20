using SchoolMedicalManagementSystem.DataAccessLayer.Context;
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

}