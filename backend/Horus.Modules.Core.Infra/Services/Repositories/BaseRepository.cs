using System.Linq.Expressions;
using Horus.Modules.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Horus.Modules.Core.Infra.Services.Repositories;

public abstract class BaseRepository : IBaseRepository
{

    protected readonly DbContext _dbContext;

    protected BaseRepository(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected virtual IQueryable<T> Set<T>() where T : class
    {
        return _dbContext.Set<T>();
    }

    public async Task<bool> AnyAsync<T>() where T : class
    {
        return await Set<T>().AnyAsync();
    }

    public async Task<bool> AnyAsync<T>(Expression<Func<T, bool>> expr) where T : class
    {
        return await Set<T>().AnyAsync(expr);
    }

    public async Task<T> FirstAsync<T>() where T : class
    {
        return await Set<T>().FirstAsync();
    }

    public async Task<T> FirstAsync<T>(Expression<Func<T, bool>> expr) where T : class
    {
        return await Set<T>().FirstAsync(expr);
    }

    public async Task<T?> FirstOrDefaultAsync<T>() where T : class
    {
        return await Set<T>().FirstOrDefaultAsync();
    }

    public async Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> expr) where T : class
    {
        return await Set<T>().FirstOrDefaultAsync(expr);
    }

    public IQueryable<T> Query<T>() where T : class
    {
        return Set<T>();
    }

    public IQueryable<T> Query<T>(Expression<Func<T, bool>> expr) where T : class
    {
        return Set<T>().Where(expr);
    }


    public async Task<T> SingleAsync<T>() where T : class
    {
        return await Set<T>().SingleAsync();
    }

    public async Task<T> SingleAsync<T>(Expression<Func<T, bool>> expr) where T : class
    {
        return await Set<T>().SingleAsync(expr);
    }

    public async Task<T?> SingleOrDefaultAsync<T>() where T : class
    {
        return await Set<T>().SingleOrDefaultAsync();
    }

    public async Task<T?> SingleOrDefaultAsync<T>(Expression<Func<T, bool>> expr) where T : class
    {
        return await Set<T>().SingleOrDefaultAsync(expr);
    }
}