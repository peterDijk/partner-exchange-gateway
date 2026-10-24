using PartnerExchange.Gateway.Domain;
using PartnerExchange.Gateway.Storage;

namespace PartnerExchange.Gateway.Application;

public abstract record ClaimResult
{
    private ClaimResult() { }

    public sealed record Claimed() : ClaimResult;
    public sealed record Duplicate(Guid MessageId, IReadOnlyList<string> Warnings) : ClaimResult;
    public sealed record Conflict() : ClaimResult;
}
public sealed class IdempotencyGuard(
    IEventStore store)
{
    // if this stream is growing too much and/ or audit
    // is not needed, this should go to a key:value store 
    // with expiry configured
    public async Task<ClaimResult> TryClaimAsync(
        string key, string bodyHash, Guid messageId,
        IReadOnlyList<string> warnings, DateTimeOffset now, CancellationToken ct)
    {
        var stream = StreamIds.Idempotency(key);
        try
        {
            await store.AppendAsync(stream, ExpectedVersion.NoStream, 
                [new SubmissionClaimed(key, messageId, bodyHash, warnings, now)], ct);
            return new ClaimResult.Claimed();
        }
        catch (ConcurrencyConflictException)
        {
            var events = await store.ReadStreamAsync(stream, ct);
            var claim = events.Select(e => e.Event).OfType<SubmissionClaimed>().Single();
            return claim.BodyHash == bodyHash
                ? new ClaimResult.Duplicate(claim.MessageId, claim.Warnings)
                : new ClaimResult.Conflict();
        }
    }
}
