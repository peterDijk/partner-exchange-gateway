using PartnerExchange.Contracts.Pxf;
using PartnerExchange.Gateway.Domain;

namespace PartnerExchange.Gateway.Tests.Domain;

internal static class Sample
{
    public static readonly DateTimeOffset T0 = new(2026, 10, 6, 10, 0, 0, TimeSpan.Zero);
    public static readonly Guid MessageId = Guid.Parse("0192f1a0-7c3e-7a1b-9d4e-2f60a8b3c5d7");

    public static UnitReport Report() => new(
        "BLUE-ALPHA-01", "FALCON11", 12.3456m, -45.6789m, 150, 32,
        OperationalStatus.FullyOperational, ReadinessLevel.Immediate,
        new DateTimeOffset(2026, 10, 6, 9, 59, 0, TimeSpan.Zero));

    public static ReportAccepted Accepted() =>
        new(MessageId, "ALPHA", PxfVersion.V2, "PXF 2\n...", Report(), [], T0);
}
