using SchoolMedicalManagementSystem.DataAccessLayer.Context;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWork.Interfaces;

namespace SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWork;

public class UnitOfWork : BaseUnitOfWork<ApplicationDbContext>, IUnitOfWork
{
    public UnitOfWork(ApplicationDbContext context, IServiceProvider serviceProvider)
        : base(context, serviceProvider)
    {
    }
}