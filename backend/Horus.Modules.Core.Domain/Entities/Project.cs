namespace Horus.Modules.Core.Domain.Entities;

public class Project : BaseEntityWithMetadata
{
    public Guid Id { get; private set; }
    public string Name { get;  private set; }
    public string SystemPrompt { get;  private set; }
    public virtual ICollection<Document> Documents { get;  } = new List<Document>();
    public virtual ICollection<ChatSession> ChatSessions { get;  } = new List<ChatSession>();
    
    
    //for ef core
    public Project(Guid id, string name, string systemPrompt)
    {
        Id = id;
        Name = name;
        SystemPrompt = systemPrompt;
    }
    
    
    // for crud generator
    public Project(Guid id, string name, string systemPrompt, IReadOnlyDictionary<string, object> metadata)
    {
        Id = id;
        Name = name;
        SystemPrompt = systemPrompt;
        this.Metadata = metadata;
    }

    private Project()
    {
    }

    public void UpdateSystemPrompt(string systemPrompt)
    {
        SystemPrompt = systemPrompt;
    }

    public void UpdateName(string name)
    {
        Name = name;
    }

    public void UpdateMetadata(IReadOnlyDictionary<string, object> metadata)
    {
        Metadata = metadata;
    }
}