namespace Horus.Modules.Core.Domain.Entities;

public interface IAuditableEntity
{
    DateTime CreatedAt { get; }
    DateTime? UpdatedAt { get; }
}