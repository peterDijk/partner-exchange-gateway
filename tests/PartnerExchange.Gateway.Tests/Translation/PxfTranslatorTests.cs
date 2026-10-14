using PartnerExchange.Contracts.Pxf;
using PartnerExchange.Gateway.Domain;
using PartnerExchange.Gateway.Tests.Domain;
using PartnerExchange.Gateway.Translation;

namespace PartnerExchange.Gateway.Tests.Translation;

public class PxfTranslatorTests
{
    // public static readonly DateTimeOffset T0 = new(2026, 10, 6, 10, 0, 0, TimeSpan.Zero);

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
}