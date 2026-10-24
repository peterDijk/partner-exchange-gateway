using PartnerExchange.Contracts.Pxf;
using PartnerExchange.Gateway.Domain;

namespace PartnerExchange.Gateway.Api;

public sealed record SubmitReportRequest(
    string PartnerId,
    string UnitId,
    string Callsign,
    decimal Latitude,
    decimal Longitude,
    int? AltitudeMetres,
    int? SpeedKnots,
    OperationalStatus Status,
    ReadinessLevel? Readiness,
    DateTimeOffset ObservedAt)
{
    public UnitReport ToUnitReport() =>
        new(UnitId, Callsign, Latitude, Longitude, AltitudeMetres, SpeedKnots, Status, Readiness, ObservedAt);
}

public sealed record SubmitReportResponse(Guid MessageId, IReadOnlyList<string> Warnings);

public sealed record DeliveryResponse(
    Guid MessageId,
    string PartnerId,
    PxfVersion PxfVersion,
    DeliveryStatus Status,
    int Attempts,
    DateTimeOffset? NextAttemptAt,
    string? LastError,
    IReadOnlyList<string> Warnings,
    DateTimeOffset AcceptedAt,
    DateTimeOffset? DeliveredAt)
{
    public static DeliveryResponse From(Delivery d) => new(
        d.MessageId, d.PartnerId, d.Version, d.Status, d.Attempts,
        d.NextAttemptAt, d.LastError, d.Warnings, d.AcceptedAt, d.DeliveredAt);
}
