using PartnerExchange.Contracts.Pxf;

namespace PartnerExchange.Contracts.Tests.Pxf;

internal static class Samples
{
    public static readonly PxfMessage V2 = new(
        PxfVersion.V2,
        Guid.ParseExact("0192f1a07c3e7a1b9d4e2f60a8b3c5d7", "N"),
        new DateTimeOffset(2026, 10, 11, 10, 15, 0, TimeSpan.Zero),
        "NLGW", "BLUE-ALPHA-01", "FALCON11",
        12.3456m, -45.6789m, 150,
        PxfStatus.Operational, 'A');

    public static readonly PxfMessage V1 = new(
        PxfVersion.V1,
        Guid.ParseExact("0192f1a07c3e7a1b9d4e2f60a8b3c5d8", "N"),
        new DateTimeOffset(2026, 10, 11, 10, 15, 0, TimeSpan.Zero),
        "NLGW", "BLUE-BRAVO-02", "HAWK7",
        -7.6543m, 101.2345m, null,
        PxfStatus.Limited, null);

    public static string Golden(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Golden", name));
}
