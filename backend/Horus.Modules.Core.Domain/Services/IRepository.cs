using Horus.Modules.Shared.Contracts.SharedKernel;

namespace Horus.Modules.Core.Domain.Services;

public interface IRepository<TEntity, TKey>
    where TEntity : IAggregateRoot
    where TKey : IComparable
{
    public TEntity? GetById(TKey key);
    public Task Save(TEntity entity);
}