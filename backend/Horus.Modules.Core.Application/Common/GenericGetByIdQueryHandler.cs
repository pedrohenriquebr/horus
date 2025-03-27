using Horus.Modules.Core.Application.Common.Helpers;
using Horus.Modules.Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Horus.Modules.Core.Application.Common;
// Query genérica para GetById
public class GetByIdQuery<TEntity> : IRequest<object>
{
    public Guid Id { get; set; }
}

public class GenericGetByIdQueryHandler<TEntity> : IRequestHandler<GetByIdQuery<TEntity>, object>
    where TEntity : BaseEntity
{
    private readonly DbContext _dbContext;

    public GenericGetByIdQueryHandler(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<object> Handle(GetByIdQuery<TEntity> request, CancellationToken cancellationToken)
    {
        var entity = await EntityHelper.FindEntityByKey<TEntity>(_dbContext, request.Id, cancellationToken);
        return entity;
    }
}