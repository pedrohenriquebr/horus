namespace Horus.Modules.Core.Domain.Entities;

public class AuditLog : BaseEntityWithMetadata
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Dictionary<string, object> EventData { get; set; } = new();
    public string? UserId { get; set; }
}