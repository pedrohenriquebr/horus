namespace Horus.Modules.Shared.Contracts.ValueObjects;

public sealed record AuditLog(
    DateTime DateTime,
    string? Url,
    string? UserName,
    string? UserId,
    string? HttpMethod,
    string? RemoteIp,
    bool IsError,
    string? ErrorMessage
);