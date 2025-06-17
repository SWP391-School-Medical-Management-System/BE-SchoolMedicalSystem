using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;

namespace SchoolMedicalManagementSystem.DataAccessLayer.RepositoryContracts.IBaseUnitOfWork;

public interface IBaseUnitOfWork : IDisposable
{
    IBaseRepository<TEntity> GetRepositoryByEntity<TEntity>()
        where TEntity : BaseEntity;

    DbContext GetDbContext();
    
    IExecutionStrategy CreateExecutionStrategy();
    
    TRepository GetRepository<TRepository>()
        where TRepository : IBaseRepository;

    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default
    );

    Task<IDbContextTransaction> BeginTransactionWithoutRetryAsync(
        CancellationToken cancellationToken = default
    );

    Task CommitTransactionAsync(
        IDbContextTransaction transaction,
        CancellationToken cancellationToken = default
    );

    Task RollbackTransactionAsync(
        IDbContextTransaction transaction,
        CancellationToken cancellationToken = default
    );

    Task ExecuteInTransactionAsync(
        Func<Task> operation,
        CancellationToken cancellationToken = default
    );
}