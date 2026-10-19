using PartnerExchange.Contracts.Pxf;
using PartnerExchange.Gateway.Domain;

namespace PartnerExchange.Gateway.Translation;

public static class PxfTranslator
{
    private const int PxfCallsignMax = 8;
    private const int PxfDecimals = 5;

    public static TranslationResult<PxfMessage> ToPxf(
        UnitReport report, Guid messageId, string originator, PxfVersion version, DateTimeOffset now)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (report.Callsign.Length > PxfCallsignMax)
        {
            errors.Add($"Callsign '{report.Callsign}' exceeds the PXF limit of {PxfCallsignMax} characters.");
        }

        var altitude = report.AltitudeMetres;
        var speed = report.SpeedKnots;
        char? readiness = report.Readiness is { } r ? ToPxfReadiness(r) : null;


        if (version == PxfVersion.V1)
        {
            if (altitude is not null) warnings.Add("PXF v1 has no altitude field; altitude dropped");
            if (readiness is not null) warnings.Add("PXF v1 has no readiness field; readiness is dropped");
            if (speed is not null) warnings.Add("PXF v1 has no speed field; speed is dropped");
            altitude = null;
            readiness = null;
            speed = null;
        }
        else if (version == PxfVersion.V2)
        {
            if (altitude is null) errors.Add("PXF v2 requires altitude");
            if (readiness is null) errors.Add("PXF v2 requires readiness");
            if (speed is not null)
            {
                warnings.Add("PXF v2 has no speed field; speed is dropped");
                speed = null;
            }
        }
        else
        {
            if (speed is null) errors.Add("PXF v3 requires speed");
        }

        var lat = Math.Round(report.Latitude, PxfDecimals, MidpointRounding.AwayFromZero);
        var lon = Math.Round(report.Longitude, PxfDecimals, MidpointRounding.AwayFromZero);
        if (lat != report.Latitude || lon != report.Longitude)
            warnings.Add($"Coordinates rounded to {PxfDecimals} decimals");
        
        if (report.ObservedAt > now + TimeSpan.FromMinutes(5))
        {
            errors.Add("Observed at can't be in the future");
        }

        if (errors.Count > 0)
            // With errors we cant have a valid PxfMessage, so return null
            return new(null, errors, warnings);

        var message = new PxfMessage(
            version, messageId, TruncateToSeconds(report.ObservedAt), originator,
            report.UnitId, report.Callsign, lat, lon, altitude, speed, ToPxfStatus(report.Status), readiness);

        return new(message, errors, warnings);
    }

    public static UnitReport FromPxf(PxfMessage m) => new(
        m.UnitId,
        m.Callsign,
        m.Latitude,
        m.Longitude,
        m.AltitudeMetres,
        m.SpeedKnots,
        FromPxfStatus(m.Status),
        m.Readiness is { } r ? FromPxfReadiness(r) : null,
        m.ObservedAt
    );

    private static PxfStatus ToPxfStatus(OperationalStatus s) => s switch
    {
        OperationalStatus.FullyOperational => PxfStatus.Operational,
        OperationalStatus.Degraded => PxfStatus.Limited,
        OperationalStatus.OutOfService => PxfStatus.NonOperational,
        _ => throw new ArgumentOutOfRangeException(nameof(s), s, "unsupported domain status")
    };

    private static OperationalStatus FromPxfStatus(PxfStatus s) => s switch
    {
        PxfStatus.Operational => OperationalStatus.FullyOperational,
        PxfStatus.Limited => OperationalStatus.Degraded,
        PxfStatus.NonOperational => OperationalStatus.OutOfService,
        _ => throw new ArgumentOutOfRangeException(nameof(s), s, "unsupported pxf status")
    };

    private static char ToPxfReadiness(ReadinessLevel r) => r switch
    {
        ReadinessLevel.Immediate => 'A',
        ReadinessLevel.High => 'B',
        ReadinessLevel.Medium => 'C',
        ReadinessLevel.Low => 'D',
        _ => throw new ArgumentOutOfRangeException(nameof(r), r, "unsupported readiness")
    };

    private static ReadinessLevel FromPxfReadiness(char c) => c switch
    {
        'A' => ReadinessLevel.Immediate,
        'B' => ReadinessLevel.High,
        'C' => ReadinessLevel.Medium,
        'D' => ReadinessLevel.Low,
        _ => throw new ArgumentOutOfRangeException(nameof(c), c, null),
    };

    private static DateTimeOffset TruncateToSeconds(DateTimeOffset t) =>
        new(t.Ticks - t.Ticks % TimeSpan.TicksPerSecond, t.Offset);
}