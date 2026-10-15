namespace PartnerExchange.Gateway.Domain;

public sealed record RetryPolicy(int MaxAttempts, TimeSpan BaseDelay, TimeSpan MaxDelay)
{
    public TimeSpan DelayFor(int attempt, double jitter)
    {
        var exponential = BaseDelay * Math.Pow(2, Math.Min(attempt -1, 30));
        var capped = exponential < MaxDelay ? exponential : MaxDelay;
        return capped / 2 + capped / 2 * jitter;
    }
}