using System.Linq.Expressions;

namespace Horus.Modules.Core.Domain.Repositories;

public interface IBaseRepository
{
    IQueryable<T> Query<T>() where T : class;
    IQueryable<T> Query<T>(Expression<Func<T, bool>> expr) where T : class;

    Task<T?> FirstOrDefaultAsync<T>() where T : class;
    Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> expr) where T : class;

    Task<T> FirstAsync<T>() where T : class;
    Task<T> FirstAsync<T>(Expression<Func<T, bool>> expr) where T : class;

    Task<T?> SingleOrDefaultAsync<T>() where T : class;
    Task<T?> SingleOrDefaultAsync<T>(Expression<Func<T, bool>> expr) where T : class;

    Task<T> SingleAsync<T>() where T : class;
    Task<T> SingleAsync<T>(Expression<Func<T, bool>> expr) where T : class;

    Task<bool> AnyAsync<T>() where T : class;
    Task<bool> AnyAsync<T>(Expression<Func<T, bool>> expr) where T : class;
}