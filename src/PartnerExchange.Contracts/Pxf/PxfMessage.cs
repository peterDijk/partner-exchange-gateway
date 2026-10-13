namespace PartnerExchange.Contracts.Pxf;

public enum PxfVersion
{
    V1 = 1,
    V2 = 2,
    V3 = 3,
}

public enum PxfStatus
{
    Operational,
    Limited,
    NonOperational
}

public sealed record PxfMessage(
    PxfVersion Version,
    Guid MessageId,
    DateTimeOffset ObservedAt,
    string Originator,
    string UnitId,
    string Callsign,
    decimal Latitude,
    decimal Longitude,
    int? AltitudeMetres,
    int? SpeedKnots,
    PxfStatus Status,
    char? Readiness);

public sealed record PxfError(int Line, string Code, string Message)
{
    public override string ToString() => $"line {Line}: [{Code}] {Message}";
}
