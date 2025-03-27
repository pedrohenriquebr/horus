using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Domain.Entities;
using Horus.Modules.Core.Domain.Events;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Horus.Modules.Core.Application.DomainEventsHandlers;

public class ChatSessionCreatedEventHandler : INotificationHandler<ChatSessionCreatedEvent>
{
    private readonly DbContext _context;
    private readonly IHubContext<ChatSessionHub> _hub;
    private readonly ILlmProvider _llmProvider;

    public ChatSessionCreatedEventHandler(DbContext context, IHubContext<ChatSessionHub> hub, ILlmProvider llmProvider)
    {
        _context = context;
        _hub = hub;
        _llmProvider = llmProvider;
    }

    public async Task Handle(ChatSessionCreatedEvent notification, CancellationToken cancellationToken)
    {
        var chatSession = await _context.Set<ChatSession>()
            .FirstOrDefaultAsync(x => x.Id == notification.ChatSession.Id, cancellationToken: cancellationToken);

        if (chatSession == null)
            return;

        var message = chatSession.Messages.FirstOrDefault(d => d.Role == "user");
        var response = await _llmProvider.GenerateTextAsync(
            "Based on this first message of the user, suggest a title for the chat session. The title must be short and concise. Do not add any other text. the first message is: " + message?.Content,
            new Dictionary<string, object>()
            {
                ["text"] = "you must respond with a title only, no other text"
            },
            null);
        
        var newTitle = response.Trim();
        chatSession.UpdateTitle(newTitle);

        await _hub.Clients.All.SendAsync("ReceiveTitleChanged", chatSession.Id, newTitle, cancellationToken: cancellationToken);
    }
}