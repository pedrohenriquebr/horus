namespace Horus.Modules.Core.Infra.Services.RateLimiter;

public class TokenBucketRateLimiter : IRateLimiter
{
    private readonly object _lock = new();
    private readonly int _maxBurst;
    private readonly double _tokensPerSecond;
    private DateTime _lastRefill;
    private double _tokens;

    public TokenBucketRateLimiter(double tokensPerSecond, int burst)
    {
        _tokensPerSecond = tokensPerSecond;
        _maxBurst = burst;
        _tokens = burst;
        _lastRefill = DateTime.UtcNow;
    }

    public bool TryAcquire()
    {
        lock (_lock)
        {
            RefillTokens();
            if (_tokens >= 1)
            {
                _tokens--;
                return true;
            }

            return false;
        }
    }

    public async Task WaitAsync()
    {
        while (!TryAcquire()) await Task.Delay(TimeSpan.FromMilliseconds(100));
    }

    private void RefillTokens()
    {
        var now = DateTime.UtcNow;
        var timePassed = (now - _lastRefill).TotalSeconds;
        var newTokens = timePassed * _tokensPerSecond;
        _tokens = Math.Min(_maxBurst, _tokens + newTokens);
        _lastRefill = now;
    }
}