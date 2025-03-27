using FluentValidation;
using Horus.Modules.Core.Application.Common.Helpers;
using Horus.Modules.Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Horus.Modules.Core.Application.Common;

// Comando genérico para Delete
public class DeleteCommand<TEntity> : IRequest
{
    public Guid Id { get; set; }
}
public class GenericDeleteCommandHandler<TEntity> : IRequestHandler<DeleteCommand<TEntity>>
    where TEntity : BaseEntity
{
    private readonly DbContext _dbContext;

    public GenericDeleteCommandHandler(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(DeleteCommand<TEntity> request, CancellationToken cancellationToken)
    {
        var entity = await EntityHelper.FindEntityByKey<TEntity>(_dbContext, request.Id, cancellationToken);
        if (entity == null)
            throw new ValidationException("Entity not found");

        _dbContext.Set<TEntity>().Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}