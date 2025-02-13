namespace Horus.Modules.Core.Infra.Services.RateLimiter;

public interface IRateLimiter
{
    Task WaitAsync();
    bool TryAcquire();
}