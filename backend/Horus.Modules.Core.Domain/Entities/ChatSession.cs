using Horus.Modules.Core.Domain.Events;

namespace Horus.Modules.Core.Domain.Entities;

public class ChatSession : BaseEntityWithMetadata
{
    public Guid Id { get; private set; }
    public Guid? ProjectId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public DateTime? EndedAt { get; private set; }

    public virtual Project? Project { get; set; } = null;
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    
    // for ef core
    public ChatSession(Guid id, Guid? projectId, string title, DateTime? endedAt)
    {
        Id = id;
        ProjectId = projectId;
        Title = title;
        EndedAt = endedAt;
    }
    
    // for crud generator
    public ChatSession(Guid id, Guid? projectId, string title, DateTime? endedAt,
        IReadOnlyDictionary<string, object> metadata)
    {
        Id = id;
        ProjectId = projectId;
        Title = title;
        EndedAt = endedAt;
        this.Metadata = metadata;
    }

    protected ChatSession()
    {
    }
    
    public void UpdateTitle(string title)
    {
        Title = title;
    }

    public void UpdateMetadata(IReadOnlyDictionary<string, object> metadata)
    {
        Metadata = metadata;
    }

    public void UpdateEndedAt(DateTime? endedAt)
    {
        EndedAt = endedAt;
    }

    public void UpdateProjectId(Guid? projectId)
    {
        ProjectId = projectId;
    }

    public ChatMessage AddNewMessage(string role, string content)
    {
        if(Messages.Count == 0)
            AddDomainEvent(new ChatSessionCreatedEvent(this));
        
        var chatMessage = new ChatMessage(Guid.NewGuid(), this.Id, role, content, null, []);
        this.Messages.Add(chatMessage);
        // AddDomainEvent(new ChatMessaageAddedEvent(chatMessage));
        return chatMessage;
    }
}