using System.Data;

namespace Horus.Modules.Core.Domain;

public interface IHorusContext
{
    public IDbConnection Connection { get; }
}

public interface IUnitOfWork
{
    public void BeginTransaction();
    public Task CommitTransactionAsync();
    public Task RollbackAsync();
}