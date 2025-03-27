namespace Horus.Modules.Core.Application.Services;

public interface IChatSessionHub
{
    public Task NotifyTitleChangedAsync(string title);
}