using System.Collections.ObjectModel;

namespace Horus.Modules.Core.Domain.Entities;

public abstract class BaseEntityWithMetadata : BaseEntity
{ 
    public IReadOnlyDictionary<string, object> Metadata {  get; protected set; } = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());

    public void SetMetadata(Dictionary<string, object> metadata)
    {
        Metadata = metadata.AsReadOnly();
    }
}