using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Horus.Modules.Shared.Contracts.SharedKernel;

public interface IEntity
{
    public IReadOnlyCollection<BaseEvent> DomainEvents { get; }
    public void ClearDomainEvents();
}

public abstract class BaseEntity<Tkey> : IEntity
    where Tkey : IComparable
{
    [JsonIgnore] private readonly List<BaseEvent> _domainEvents = new();

    public Tkey Id { get; protected set; }

    [NotMapped] public IReadOnlyCollection<BaseEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    protected void AddDomainEvent(BaseEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    protected void RemoveDomainEvent(BaseEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }
}

public interface IAggregateRoot
{
}