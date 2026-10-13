using PartnerExchange.Contracts.Pxf;

namespace PartnerExchange.Contracts.Tests.Pxf;

public class PxfParserTests
{
    [Fact]
    public void Parses_vs_golden_file()
    {
        Assert.True(
            PxfParser.TryParse(
                Samples.Golden("v2-report.pxf"),
                out var message, out var errors),
            string.Join(";", errors));
        Assert.Equal(Samples.V2, message);
    }

    [Fact]
    public void Parses_v1_golden_file()
    {
        Assert.True(PxfParser.TryParse(Samples.Golden("v1-report.pxf"), out var message, out var errors), string.Join("; ", errors));
        Assert.Equal(Samples.V1, message);
    }

    [Fact]
    public void Accepts_crlf_line_endings()
    {
        var crlf = Samples.Golden("v2-report.pxf").ReplaceLineEndings("\r\n");
        Assert.True(PxfParser.TryParse(crlf, out _, out _));
    }

    [Fact]
    public void Rejects_a_corrupted_message_via_the_checksum()
    {
        var corrupted = Samples.Golden("v2-report.pxf").Replace("FALCON11", "FALCON12");
        Assert.False(PxfParser.TryParse(corrupted, out _, out var errors));
        Assert.Contains(errors, e => e.Code == "Z.CHECKSUM");
    }

    [Fact]
    public void Collects_all_field_errors_instead_of_stopping_at_the_first()
    {
        var bad = Samples.Golden("v2-report.pxf")
            .Replace("FALCON11", "falcon11")
            .Replace("+12.34560", "+91.00000");
        Assert.False(PxfParser.TryParse(bad, out _, out var errors));
        Assert.Contains(errors, e => e.Code == "U.CALLSIGN");
        Assert.Contains(errors, e => e.Code == "P.LAT");
    }

    [Theory]
    [InlineData("PXF 4")]
    [InlineData("PXF  2")]
    [InlineData("pxf 2")]
    public void Rejects_unknown_or_malformed_version_lines(string firstLine)
    {
        var lines = Samples.Golden("v2-report.pxf").Split('\n');
        lines[0] = firstLine;
        Assert.False(PxfParser.TryParse(string.Join('\n', lines), out _, out _));
    }

    [Fact]
    public void Rejects_a_v1_message_with_a_v2_position_line()
    {
        var mixed = Samples.Golden("v1-report.pxf").Replace("+101.23450", "+101.23450 0150");
        Assert.False(PxfParser.TryParse(mixed, out _, out var errors));
        Assert.Contains(errors, e => e.Code == "FIELDS");
    }

    [Fact]
    public void Rejects_non_ascii_input()
    {
        Assert.False(PxfParser.TryParse(Samples.Golden("v2-report.pxf").Replace("NLGW", "NLGŴ"), out _, out var errors));
        Assert.Contains(errors, e => e.Code == "SIZE");
    }
}