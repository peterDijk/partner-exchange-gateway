namespace PartnerExchange.Gateway.Domain;

public enum OperationalStatus
{
    FullyOperational,
    Degraded,
    OutOfService
}

public enum ReadinessLevel
{
    Immediate,
    High,
    Medium,
    Low
}

public sealed record UnitReport(
    string UnitId,
    string Callsign,
    decimal Latitude,
    decimal Longitude,
    int? AltitudeMeters,
    int? SpeedKnots,
    OperationalStatus Status,
    ReadinessLevel Readiness,
    DateTimeOffset ObservedAt);
