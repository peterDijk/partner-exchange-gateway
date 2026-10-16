using PartnerExchange.Gateway.Domain;
using PartnerExchange.Gateway.Storage;
using PartnerExchange.Gateway.Tests.Domain;

namespace PartnerExchange.Gateway.Tests.Storage;

public class InMemoryEventStoreTests
{
    private const string Stream = "delivery-test";

    private static ReportDelivered Delivered(int attempt) => new(Sample.MessageId, attempt, Sample.T0);

    [Fact]
    public async Task Appends_and_reads_back_in_order_with_versions()
    {
        var store = new InMemoryEventStore([]);

        Assert.Equal(0, await store.AppendAsync(Stream, ExpectedVersion.NoStream, [Sample.Accepted()]));
        Assert.Equal(1, await store.AppendAsync(Stream, 0, [Delivered(1)]));

        var events = await store.ReadStreamAsync(Stream);
        Assert.Equal(new long[] { 0, 1 }, events.Select(e => e.StreamVersion));
        Assert.IsType<ReportDelivered>(events[1].Event);
    }

    [Fact]
    public async Task Rejects_a_stale_expected_version()
    {
        var store = new InMemoryEventStore([]);
        await store.AppendAsync(Stream, ExpectedVersion.NoStream, [Sample.Accepted()]);

        var conflict = await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => store.AppendAsync(Stream, ExpectedVersion.NoStream, [Delivered(1)]));
        Assert.Equal(0, conflict.Actual);
    }

    [Fact]
    public async Task Concurrent_appends_at_the_same_version_have_exactly_one_winner()
    {
        var store = new InMemoryEventStore([]);
        await store.AppendAsync(Stream, ExpectedVersion.NoStream, [Sample.Accepted()]);

        var attempts = Enumerable.Range(0, 64)
            .Select(i => Task.Run(async () =>
            {
                try
                {
                    await store.AppendAsync(Stream, 0, [Delivered(i)]);
                    return true;
                }
                catch (ConcurrencyConflictException)
                {
                    return false;
                }
            })).ToArray();
        
        var results = await Task.WhenAll(attempts);

        Assert.Single(results, won => won);
        Assert.Equal(2, (await store.ReadStreamAsync(Stream)).Count);
    }

    [Fact]
    public async Task Read_snapshots_are_not_affected_by_later_appends()
    {
        var store = new InMemoryEventStore([]);
        await store.AppendAsync(Stream, ExpectedVersion.NoStream, [Sample.Accepted()]);

        var snapshot = await store.ReadStreamAsync(Stream);
        await store.AppendAsync(Stream, 0, [Delivered(1)]);

        Assert.Single(snapshot);
    }

    [Fact]
    public async Task Any_skips_the_version_check()
    {
        var store = new InMemoryEventStore([]);
        await store.AppendAsync(StreamIds.Rejections, ExpectedVersion.Any, [Sample.Accepted()]);
        await store.AppendAsync(StreamIds.Rejections, ExpectedVersion.Any, [Sample.Accepted()]);
        Assert.Equal(2, (await store.ReadStreamAsync(StreamIds.Rejections)).Count);
    }
}
