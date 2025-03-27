using Horus.Modules.Core.Application.Common.Helpers;
using Horus.Modules.Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Horus.Modules.Core.Application.Common;


// Query genérica para GetAll
public class GetAllQuery<TEntity> : IRequest<object>
{
    public int Offset { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Sort { get; set; }
    public IDictionary<string, object> Filters { get; set; } = new Dictionary<string, object>();
}
public class GenericGetAllQueryHandler<TEntity> : IRequestHandler<GetAllQuery<TEntity>, object>
    where TEntity : BaseEntity
{
    private readonly DbContext _dbContext;

    public GenericGetAllQueryHandler(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<object> Handle(GetAllQuery<TEntity> request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<TEntity>().AsNoTracking().AsQueryable();

        foreach (var filter in request.Filters)
        {
            var prop = typeof(TEntity).GetProperty(filter.Key);
            if (prop != null)
            {
                query = query.Where(e => EF.Property<object>(e, filter.Key).Equals(filter.Value));
            }
        }

        if (!string.IsNullOrEmpty(request.Sort))
        {
            var prop = typeof(TEntity).GetProperty(request.Sort);
            if (prop != null)
            {
                query = query.OrderBy(e => EF.Property<object>(e, request.Sort));
            }
            else
            {
                var keyProperty = EntityHelper.GetKeyProperty<TEntity>(_dbContext);
                if (keyProperty == null)
                    throw new InvalidOperationException($"Key property not found for entity {typeof(TEntity).Name}");

                query = query.OrderBy(e => EF.Property<object>(e, keyProperty.Name));
            }
        }

        var total = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling((double)total / request.PageSize);
        var items = await query
            .Skip((request.Offset - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new
        {
            Items = items, Total = total, Page = request.Offset, PageSize = request.PageSize, TotalPages = totalPages
        };
    }
}