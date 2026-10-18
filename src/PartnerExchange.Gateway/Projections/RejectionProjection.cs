using System.Collections.Concurrent;
using PartnerExchange.Gateway.Domain;
using PartnerExchange.Gateway.Storage;

namespace PartnerExchange.Gateway.Projections;

public sealed class RejectionProjection : IProjection
{
    private readonly ConcurrentQueue<MessageRejected> _rejections = new();

    public void Apply(RecordedEvent recorded)
    {
        if (recorded.Event is MessageRejected rejected)
            _rejections.Enqueue(rejected);
    }

    public IReadOnlyList<MessageRejected> Latest(int max) => _rejections.Reverse().Take(max).ToList();
}
