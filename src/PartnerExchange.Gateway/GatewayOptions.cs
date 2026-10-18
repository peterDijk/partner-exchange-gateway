using PartnerExchange.Contracts.Pxf;

namespace PartnerExchange.Gateway;

public sealed class GatewayOptions
{
    public const string Section = "Gateway";

    public string Originator { get; init; } = "";
    public Dictionary<string, PartnerOptions> Partners { get; init; } = new(StringComparer.Ordinal);
    public ResilienceOptions Resilience { get; init; } = new();
    public string? JournalPath { get; init; }
}

public sealed class PartnerOptions
{
    public Uri Endpoint { get; init; } = default!;
    public PxfVersion Version { get; init; } = PxfVersion.V3;
}

public sealed class ResilienceOptions
{
    public int MaxAttempts { get; init; } = 6;
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromMinutes(1);
    public int BreakerFailureThreshold { get; init; } = 3;
    public TimeSpan BreakerOpenDuration { get; init; } = TimeSpan.FromSeconds(30);
}