using System.Globalization;
using PartnerExchange.Contracts.Pxf;

namespace PartnerExchange.Contracts.Tests.Pxf;

public class PxfWriterTests
{
    // [Theory]
    // [InlineData("v1-report.pxf")]
    // [InlineData("v2-report.pxf")]
    // public void Writes_golden_files_byte_for_byte(string file)
    // {
    //     var golden = Samples.Golden(file);
    // Assert.True(PxfParser.TryParse(golden, out var parsed, out _));
    // Assert.NotNull(parsed);
    // Assert.Equal(golden, PxfWriter.Write(parsed));
    // }

    [Fact]
    public void Output_does_not_depend_on_the_current_culture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("nl-NL");
            Assert.Equal(Samples.Golden("v2-report.pxf"), PxfWriter.Write(Samples.V2));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Refuses_a_v2_message_without_altitude()
    {
        Assert.Throws<ArgumentException>(() => PxfWriter.Write(Samples.V2 with { AltitudeMetres = null }));
    }
}