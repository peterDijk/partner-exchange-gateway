using System.Collections.Concurrent;
using PartnerExchange.Gateway.Domain;
using PartnerExchange.Gateway.Storage;

namespace PartnerExchange.Gateway.Projections;

public sealed class DeliveryProjection : IProjection
{
    private readonly ConcurrentDictionary<Guid, Delivery> _deliveries = new();

    public void Apply(RecordedEvent recorded)
    {
        if (recorded.Event is not DeliveryEvent e) return;
        _deliveries.TryGetValue(e.MessageId, out var current);
        _deliveries[e.MessageId] = DeliveryAggregate.Apply(current, e);
        // this is using the full aggregate as read model
        // for simplicity in this project. Normally I would use a
        // separate read-model that omits certain fields, the payload for example.
        // might add this here later
    }

    public Delivery? Get(Guid messageId) =>
        _deliveries.TryGetValue(messageId, out var delivery)
            ? delivery : null;
    
    public IReadOnlyList<Delivery> List(DeliveryStatus? status = null) =>
        _deliveries.Values
            .Where(d => status is null || d.Status == status)
            .OrderBy(d => d.AcceptedAt)
            .ToList();
    
    public IReadOnlyList<Delivery> PendingFor(string partnerId) =>
        _deliveries.Values
            .Where(d => d.PartnerId == partnerId 
                && d.Status is DeliveryStatus.Pending 
                    or DeliveryStatus.RetryScheduled)
            .OrderBy(d => d.NextAttemptAt)
            .ToList();
}
