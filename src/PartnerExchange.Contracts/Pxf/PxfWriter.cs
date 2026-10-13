using System.Globalization;

namespace PartnerExchange.Contracts.Pxf;

public static class PxfWriter
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Write(PxfMessage message)
    {
        string[] lines =
        [
            string.Create(Inv, $"PXF {(int)message.Version}"),
            string.Create(Inv,$"H {message.MessageId:N} " +
                $"{message.ObservedAt.UtcDateTime:yyyyMMdd'T'HHmmss'Z'} " +
                $"{message.Originator}"),
            $"U {message.UnitId} {message.Callsign}",
            PositionLine(message),
            StatusLine(message)
        ];

        var body = string.Concat(lines.Select(line => line + "\n"));
        return body + $"Z {PxfChecksum.Compute(body)}\n";
    }

    private static string PositionLine(PxfMessage m)
    {
        var lat = m.Latitude.ToString("+00.00000;-00.00000", Inv);
        var lon = m.Longitude.ToString("+000.00000;-000.00000", Inv);
        return m.Version switch
        {
            PxfVersion.V1 => $"P {lat} {lon}",
            PxfVersion.V2 => string.Create(Inv, $"P {lat} {lon} {Require(m.AltitudeMetres, "altitude"):0000}"),
            PxfVersion.V3 => string.Create(Inv, $"P {lat} {lon} {Require(m.AltitudeMetres, "altitude"):0000} {Require(m.SpeedKnots, "speed"):000}"),
            _ => throw new NotSupportedException($"Unsupported PXF version {m.Version}")
        };
    }

    private static string StatusLine(PxfMessage m) => m.Version switch
    {
        PxfVersion.V1 => $"S {PxfCodes.Status.ToV1(m.Status)}",
        >= PxfVersion.V2 => $"S {PxfCodes.Status.ToV2(m.Status)} {Require(m.Readiness, "readiness")}",
        _ => throw new ArgumentOutOfRangeException(nameof(m), m.Version, "Unknown PXF version")
    };

    private static T Require<T>(T? value, string field) where T : struct =>
        value ?? throw new ArgumentException($"PXF requires {field}", nameof(value));
}