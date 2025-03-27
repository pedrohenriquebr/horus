using Horus.Modules.Core.Domain.Entities;

namespace Horus.Modules.Core.Domain.Events;

public class DocumentAddedEvent : BaseEvent
{
    public DocumentAddedEvent(Document document)
    {
        this.Document = document;
    }

    public DocumentAddedEvent()
    {
        
    }
    
    public Document Document { get; set; }
}

public class DocumentStartedChunking : BaseEvent
{
    public DocumentStartedChunking(Document document)
    {
        this.Document = document;
    }

    public DocumentStartedChunking()
    {
        
    }
    
    public Document Document { get; init; }
}

public class DocumentFinishedChunkingEvent : BaseEvent
{
    public DocumentFinishedChunkingEvent(Document document)
    {
        this.Document = document;
    }

    public Document Document { get; init; }
}


public class DocumentChunkCreatedEvent : BaseEvent
{
    public DocumentChunkCreatedEvent(DocumentChunk documentChunk)
    {
        this.DocumentChunk = documentChunk;
    }
    public DocumentChunk DocumentChunk { get; set; }
}

public class DocumentChunkEmbeddedEvent : BaseEvent
{
    public DocumentChunkEmbeddedEvent(DocumentChunk documentChunk)
    {
        this.DocumentChunk = documentChunk;
    }
    public DocumentChunk DocumentChunk { get; init; }
}


public class DocumentChunkEmbeddingStartedEvent : BaseEvent
{
    public DocumentChunkEmbeddingStartedEvent(DocumentChunk documentChunk)
    {
        this.DocumentChunk = documentChunk;
    }

    public DocumentChunk DocumentChunk { get; init; }

}

public class ChatMessaageAddedEvent : BaseEvent
{
    public ChatMessaageAddedEvent(ChatMessage chatMessage)
    {
        this.ChatMessage = chatMessage;
    }

    public ChatMessage ChatMessage { get; set; }
}


public class ChatSessionCreatedEvent : BaseEvent
{
    public ChatSessionCreatedEvent(ChatSession chatSession)
    {
        this.ChatSession = chatSession;
    }

    public ChatSession ChatSession { get; set; }
}

