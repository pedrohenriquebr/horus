namespace Horus.Modules.Shared.Contracts.Events;

public sealed record ApplicationAccessedEvent(
    DateTime Datetime,
    string Url,
    string? Username,
    string Method,
    string RemoteIpAddress
);