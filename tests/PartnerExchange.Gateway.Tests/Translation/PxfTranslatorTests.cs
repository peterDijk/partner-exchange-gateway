using PartnerExchange.Contracts.Pxf;
using PartnerExchange.Gateway.Domain;
using PartnerExchange.Gateway.Tests.Domain;
using PartnerExchange.Gateway.Translation;

namespace PartnerExchange.Gateway.Tests.Translation;

public class PxfTranslatorTests
{
    [Fact]
    public void Report_from_the_future_is_not_accepted()
    {
        var report = Sample.Report();
        var pxf = PxfTranslator.ToPxf(
            report with { ObservedAt = Sample.T0 + TimeSpan.FromMinutes(10) }, 
            Sample.MessageId,
            "me", PxfVersion.V3, Sample.T0);
        Assert.Null(pxf.Value);
        Assert.Single(pxf.Errors);
    }

    [Fact]
    public void Round_trips_through_pxf_v2_text()
    {
        var report = Sample.Report();
        var pxf = PxfTranslator.ToPxf(report, Sample.MessageId, "NLGW", PxfVersion.V2, Sample.T0).Value;
        Assert.NotNull(pxf);

        Assert.True(PxfParser.TryParse(PxfWriter.Write(pxf), out var parsed, out _));
        Assert.Equal(report with { SpeedKnots = null }, PxfTranslator.FromPxf(parsed));
    }

    [Fact]
    public void V1_drops_altitude_and_readiness_and_speed_with_warnings()
    {
        var result = PxfTranslator.ToPxf(Sample.Report(), Sample.MessageId, "NLGW", PxfVersion.V1, Sample.T0);

        Assert.NotNull(result.Value);
        Assert.Null(result.Value.AltitudeMetres);
        Assert.Null(result.Value.SpeedKnots);
        Assert.Equal(3, result.Warnings.Count);
    }

    [Fact]
    public void V2_requires_altitude()
    {
        var result = PxfTranslator.ToPxf(Sample.Report() with { AltitudeMetres = null }, Sample.MessageId, "NLGW", PxfVersion.V2, Sample.T0);
        Assert.Null(result.Value);
        Assert.Contains("PXF v2 requires altitude", result.Errors);
    }

    [Fact]
    public void Rejects_callsigns_longer_than_pxf_allows()
    {
        var result = PxfTranslator.ToPxf(Sample.Report() with { Callsign = "FALCON1234" }, Sample.MessageId, "NLGW", PxfVersion.V2, Sample.T0);
        Assert.Null(result.Value);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void Rounds_coordinates_away_from_zero_and_warns()
    {
        var result = PxfTranslator.ToPxf(Sample.Report() with { Latitude = 12.345665m }, Sample.MessageId, "NLGW", PxfVersion.V2, Sample.T0);
        Assert.Equal(12.34567m, result.Value?.Latitude);
        Assert.Contains(result.Warnings, w => w.Contains("rounded", StringComparison.Ordinal));
    }
}