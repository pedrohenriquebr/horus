using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Horus.Modules.Core.Domain.Events;

namespace Horus.Modules.Core.Domain.Entities;

public abstract class BaseEntity : IAuditableEntity
{
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; } = DateTime.UtcNow;

    protected void MarkAsUpdated() => UpdatedAt = DateTime.UtcNow;
    
    [JsonIgnore]
    private readonly List<BaseEvent> _domainEvents = new();

    [NotMapped]
    [JsonIgnore]
    public IReadOnlyCollection<BaseEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(BaseEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    protected void RemoveDomainEvent(BaseEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}