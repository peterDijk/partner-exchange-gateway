using PartnerExchange.Gateway.Application;
using PartnerExchange.Gateway.Domain;
using PartnerExchange.Gateway.Storage;
using PartnerExchange.Gateway.Tests.Domain;

namespace PartnerExchange.Gateway.Tests.Application;

public class DeliveryCommandsTests
{
    private sealed class InterferingStore(IEventStore inner, Func<Task> interfere) : IEventStore
    {
        private bool _interfered;

        public int Reads { get; private set; }

        public Task<IReadOnlyList<RecordedEvent>> ReadStreamAsync(string streamId, CancellationToken ct = default)
        {
            Reads++;
            return inner.ReadStreamAsync(streamId, ct);
        }

        public IAsyncEnumerable<RecordedEvent> ReadAllAsync(long fromPosition, CancellationToken ct = default) =>
            inner.ReadAllAsync(fromPosition, ct);

        public async Task<long> AppendAsync(
            string streamId, long expectedVersion, IReadOnlyList<DomainEvent> events, CancellationToken ct = default)
        {
            if (!_interfered)
            {
                _interfered = true;
                await interfere();
            }
            return await inner.AppendAsync(streamId, expectedVersion, events, ct);
        }
    }

    private static readonly RetryPolicy Policy =
        new(MaxAttempts: 3, TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(10));

    [Fact]
    public async Task Redecides_on_fresh_state_after_a_conflict()
    {
        var inner = new InMemoryEventStore([]);
        var stream = StreamIds.Delivery(Sample.MessageId);
        await inner.AppendAsync(stream, ExpectedVersion.NoStream, [Sample.Accepted()]);

        var store = new InterferingStore(inner, () =>
            inner.AppendAsync(stream, 0, [new DeliveryAttemptFailed(Sample.MessageId, 1, "503", Sample.T0, Sample.T0)]));
        var commands = new DeliveryCommands(store);

        var decision = await commands.HandleAsync(Sample.MessageId, new RecordSuccess(), Sample.T0, Policy, CancellationToken.None);

        Assert.Equal(2, store.Reads); // should have tried a second time
        var delivered = Assert.IsType<ReportDelivered>(Assert.Single(Assert.IsType<Decision.Emit>(decision).Events));
        Assert.Equal(2, delivered.Attempt);   // there was a DeliveryAttemptFailed, now this succeed should be attempt 2
    }
}
