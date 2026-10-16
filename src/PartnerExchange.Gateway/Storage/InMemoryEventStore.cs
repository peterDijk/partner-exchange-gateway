using System.Collections.Immutable;

using PartnerExchange.Gateway.Domain;

namespace PartnerExchange.Gateway.Storage;

public sealed class InMemoryEventStore : IEventStore
{
    private readonly Lock _gate = new();
    // creating the dict with StringComparer.Ordinal makes the key-string compare exact, case-sensitive and culture-invariant
    // it compares UTF-16 code units directly and uses a matching hash
    private ImmutableDictionary<string, ImmutableArray<RecordedEvent>> _streams = 
        ImmutableDictionary.Create<string, ImmutableArray<RecordedEvent>>(StringComparer.Ordinal);

    private readonly IProjection[] _projections;
    private long _lastPosition = -1;

    public InMemoryEventStore(IEnumerable<IProjection> projections)
    {
        _projections = projections.ToArray();
    }

    public Task<IReadOnlyList<RecordedEvent>> ReadStreamAsync(string streamId, CancellationToken ct = default)
    {
        lock (_gate)
        {
            IReadOnlyList<RecordedEvent> snapshot = _streams.TryGetValue(streamId, out var stream)
                ? stream
                : ImmutableArray<RecordedEvent>.Empty;
            return Task.FromResult(snapshot);
        }
    }

    public Task<long> AppendAsync(string streamId,
        long expectedVersion, IReadOnlyList<DomainEvent> events,
        CancellationToken ct = default)
    {
        lock (_gate)
        {
            var existing = _streams.TryGetValue(streamId, out var s)
                ? s
                : ImmutableArray<RecordedEvent>.Empty;

            long currentVerion = existing.Length == 0
                ? ExpectedVersion.NoStream
                : existing.Length - 1;
            
            if (expectedVersion != ExpectedVersion.Any && expectedVersion != currentVerion)
            {
                throw new ConcurrencyConflictException(streamId, expectedVersion, currentVerion);
            }

            var recorded = new RecordedEvent[events.Count];
            for (var i = 0; i < events.Count; i++)
                recorded[i] = new RecordedEvent(
                    streamId, currentVerion + 1 + i,
                    _lastPosition + 1 + i, events[i]);
            
            var updated = existing.AddRange(recorded);
            _streams = _streams.SetItem(streamId, updated);
            _lastPosition += recorded.Length;

            foreach (var r in recorded)
                foreach (var projection in _projections)
                    projection.Apply(r);
            
            return Task.FromResult(currentVerion + recorded.Length);
        }
    }
}