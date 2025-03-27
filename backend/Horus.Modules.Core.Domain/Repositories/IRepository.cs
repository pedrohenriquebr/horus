using Horus.Modules.Core.Domain.Entities;

namespace Horus.Modules.Core.Domain.Repositories;

public interface IRepository<T,K> : IBaseRepository
    where T : BaseEntity
    where K : IComparable
{
    
    Task<T?> GetAsync(K id);
    Task<T> InsertAsync(T data);
    Task<T> UpdateAsync(T data);
    Task DeleteAsync(K id);
}