using Microsoft.AspNetCore.SignalR;

namespace Horus.Modules.Core.Application.Services;

public class ChatSessionHub : Microsoft.AspNetCore.SignalR.Hub, IChatSessionHub
{
    public Task NotifyTitleChangedAsync(string title)
    {
        return Clients.All.SendAsync("ReceiveTitleChanged", title);
    }
}