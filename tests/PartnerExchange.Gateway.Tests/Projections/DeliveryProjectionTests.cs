using PartnerExchange.Gateway.Domain;
using PartnerExchange.Gateway.Projections;
using PartnerExchange.Gateway.Storage;
using PartnerExchange.Gateway.Tests.Domain;

namespace PartnerExchange.Gateway.Tests.Projections;

public class DeliveryProjectionTests
{
    [Fact]
    public async Task Tracks_delivery_status_through_the_store()
    {
        var projection = new DeliveryProjection();
        var store = new InMemoryEventStore([projection]);
        var stream = StreamIds.Delivery(Sample.MessageId);

        await store.AppendAsync(stream, ExpectedVersion.NoStream, [Sample.Accepted()]);
        var sampleProjection = projection.Get(Sample.MessageId);
        Assert.Equal(DeliveryStatus.Pending, sampleProjection?.Status);
        Assert.Single(projection.PendingFor("ALPHA"));

        await store.AppendAsync(stream, 0, [new ReportDelivered(Sample.MessageId, 1, Sample.T0)]);
        Assert.Equal(DeliveryStatus.Delivered, projection.Get(Sample.MessageId)?.Status);
        Assert.Empty(projection.PendingFor("ALPHA"));
    }

    [Fact]
    public async Task A_rebuilt_projection_mirrors_existing()
    {
        var originalProjection = new DeliveryProjection();
        var store = new InMemoryEventStore([originalProjection]);
        var stream = StreamIds.Delivery(Sample.MessageId);

        await store.AppendAsync(stream, ExpectedVersion.NoStream, [Sample.Accepted()]);
        await store.AppendAsync(stream, 0, [new ReportDelivered(Sample.MessageId, 1, Sample.T0)]);

        var allEvents = await store.ReadAllAsync(-1).ToListAsync();
        var newProjection = new DeliveryProjection();
        foreach (var e in allEvents)
        {
            newProjection.Apply(e);
        }

        Assert.Equal(originalProjection.Get(Sample.MessageId), newProjection.Get(Sample.MessageId));
        Assert.Equivalent(originalProjection.List(), newProjection.List());
    }
}