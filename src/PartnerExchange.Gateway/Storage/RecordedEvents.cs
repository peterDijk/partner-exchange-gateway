using PartnerExchange.Gateway.Domain;

namespace PartnerExchange.Gateway.Storage;

public sealed record RecordedEvent(string StreamId, 
    long StreamVersion, long GlobalPosition, DomainEvent Event);

public static class ExpectedVersion
{
    public const long Any = -2;
    public const long NoStream = -1;
}

public sealed class ConcurrencyConflictException(
    string streamId, long expected, long actual) : Exception(
        $"Stream '{streamId}' expected version {expected} but was at {actual}.")
{
    public string StreamId { get; } = streamId;
    public long Expected { get;} = expected;
    public long Actual { get; } = actual;
}