using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PartnerExchange.Contracts.Pxf;

public static partial class PxfParser
{
    public const int MaxMessageBytes = 1024;
    private const string TimestampFormat = "yyyyMMdd'T'HHmmss'Z'";

    public static bool TryParse(
        string text,
        [NotNullWhen(true)] out PxfMessage? message,
        out IReadOnlyList<PxfError> errors)
    {
        message = null;
        var problems = new List<PxfError>();
        errors = problems;

        if (text.Length > MaxMessageBytes || !Ascii.IsValid(text))
        {
            problems.Add(new PxfError(0, "SIZE", $"Message must be ASCII and at most {MaxMessageBytes} bytes."));
            return false;
        }

        var lines = text.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n');
        if (lines.Length != 6)
        {
            problems.Add(new PxfError(0, "STRUCTURE", $"Expected 6 lines, got {lines.Length}"));
            return false;
        }

        if (ParseVersion(lines[0], problems) is not { } version)
        {
            return false;
        }

        var header = ParseHeader(lines[1], problems);
        var unit = ParseUnit(lines[2], problems);
        var position = ParsePosition(lines[3], problems);
        var status = ParseStatus(lines[4], problems);
        ParseTrailer(lines[5], lines[..5], problems);
    }

    private sealed record HeaderSegment(Guid MessageId, DateTimeOffset ObservedAt, string Originator);
    private sealed record UnitSegment(string UnitId, string Callsign);
    private sealed record PositionSegment(decimal Latitude, decimal Longitude, int? AltitudeMeters);
    private sealed record StatusSegment(PxfStatus Status, char? Readiness);

    private static PxfVersion? ParseVersion(string line, List<PxfError> problems)
    {
        var fields = Fields(line, 1, "PXF", 1, problems);
        if (fields is null) return null;

        PxfVersion? version = fields[0] switch
        {
            "1" => PxfVersion.V1,
            "2" => PxfVersion.V2,
            _ => null
        };
        Check(version is not null, problems, 1, "VERSION", $"Unsupported PXF version '{fields[0]}'.");
        return version;
    }

    private static HeaderSegment? ParseHeader(string line, List<PxfError> problems)
    {
        const int lineNo = 2;
        var fields = Fields(line, lineNo, "H", 3, problems);
        if (fields is null) return null;

        var ok = Check(
            Guid.TryParseExact(fields[0], "N", out var messageId),
            problems,
            lineNo,
            "H.ID",
            "Message id must be 32 hex digits.") &
            Check(
                DateTimeOffset.TryParseExact(
                    fields[1],
                    TimestampFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var observedAt),
                problems,
                lineNo,
                "H.TIME",
                "Observed-at must be yyyyMMddTHHmmssZ.") &
            Check(
                OriginatorPattern().IsMatch(fields[2]),
                problems,
                lineNo,
                "H.ORIGIN",
                "Originator must be 1-8 chars of A-Z or 0-9.");
        
        return ok ? new HeaderSegment(messageId, observedAt, fields[2]) : null;
    }

    private static UnitSegment? ParseUnit(string line, List<PxfError> problems)
    {
        const int lineNo = 3;
        var fields = Fields(line, lineNo, "U", 2, problems);
        if (fields is null) return null;

        var ok = Check(
            UnitIdPattern().IsMatch(fields[0]),
            problems,
            lineNo,
            "U.ID",
            "Unit-id must be 1-16 chars of A-Z or 0-9") &
            Check(
                CallsignPattern().IsMatch(fields[1]),
                problems,
                lineNo,
                "U.CALLSIGN",
                "Callsign must be 1-8 chars of A- or 0-9");
        
        return ok ? new UnitSegment(fields[0], fields[1]) : null;
    }

    private static PositionSegment? ParsePosition(string line, List<PxfError> problems)
    {
        throw new NotImplementedException();
    }

    private static StatusSegment? ParseStatus(string line, List<PxfError> problems)
    {
        throw new NotImplementedException();
    }

    private static void ParseTrailer(string line, string[] bodyLines, List<PxfError> problems)
    {
        const int lineNo = 6;
        var fields = Fields(line, lineNo, "Z", 1, problems);
        if (fields is null) return;

        var expected = PxfChecksum.Compute(
            string.Concat(bodyLines.Select(l => l + "\n")));
        Check(fields[0] == expected, problems, lineNo, "Z.CHECKSUM", $"Checksum mismatch: expected {expected}, got {fields[0]}.");
    }

    private static string[]? Fields(string line, int lineNo, string tag, int count, List<PxfError> problems)
    {
        var parts = line.Split(' ');
        var tagCheck = Check(parts[0] == tag, problems, lineNo, "TAG", $"Expected segment '{tag}', got '{parts[0]}'.");
        var fieldsCheck = Check(parts.Length - 1 == count, problems, lineNo, "FIELDS", $"Segment '{tag}' expects {count} field(s), got {parts.Length - 1}.");
        return !tagCheck || !fieldsCheck ? null : parts[1..];
    }

    private static bool Check(bool condition, List<PxfError> problems, int line, string code, string message)
    {
        if (!condition) problems.Add(new PxfError(line, code, message));
        return condition;
    }

    [GeneratedRegex("^[A-Z0-9]{1,8}$")] private static partial Regex OriginatorPattern();
    [GeneratedRegex("^[A-Z0-9-]{1,16}$")] private static partial Regex UnitIdPattern();
    [GeneratedRegex("^[A-Z0-9]{1,8}$")] private static partial Regex CallsignPattern();
    [GeneratedRegex(@"^[+-][0-9]{2}\.[0-9]{5}$")] private static partial Regex LatitudePattern();
    [GeneratedRegex(@"^[+-][0-9]{3}\.[0-9]{5}$")] private static partial Regex LongitudePattern();
    [GeneratedRegex("^[0-9]{4}$")] private static partial Regex AltitudePattern();
}