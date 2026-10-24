using PartnerExchange.Gateway.Domain;
using PartnerExchange.Gateway.Storage;

namespace PartnerExchange.Gateway.Application;

public sealed class RejectionLog(IEventStore store, TimeProvider time)
{
    public Task RecordAsync(string direction, string? partnerId, IReadOnlyList<string> reasons, CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var rejected = new MessageRejected(Guid.CreateVersion7(now), direction, partnerId, reasons, now);
        return store.AppendAsync(StreamIds.Rejections, ExpectedVersion.Any, [rejected], ct);
    }
}
