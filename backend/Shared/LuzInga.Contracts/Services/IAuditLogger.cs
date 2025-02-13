using Horus.Modules.Shared.Contracts.Events;

namespace Horus.Modules.Shared.Contracts.Services;

public interface IAuditLogger
{
    public Task LogRecent(ApplicationAccessedEvent data);
    public Task LogRecent(string request, object requestData, object? responseData);
}