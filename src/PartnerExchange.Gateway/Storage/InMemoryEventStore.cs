using Microsoft.AspNetCore.Mvc.TagHelpers;

using PartnerExchange.Gateway.Domain;

namespace PartnerExchange.Gateway.Storage;

public sealed class InMemoryEventStore : IEventStore
{
    private readonly Lock _gate = new();
    // creating the dict with StringComparer.Ordinal makes the key-string compare exact, case-sensitive and culture-invariant
    // it compares UTF-16 code units directly and uses a matching hash
    private readonly Dictionary<string, List<RecordedEvent>> _streams = new(StringComparer.Ordinal);

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
                ? stream.ToArray()
                : [];
            return Task.FromResult(snapshot);
        }
    }

    public Task<long> AppendAsync(string streamId,
        long expectedVersion, IReadOnlyList<DomainEvent> events,
        CancellationToken ct = default)
    {
        lock (_gate)
        {
            _streams.TryGetValue(streamId, out var stream);
            long currentVerion = stream is null
                ? ExpectedVersion.NoStream
                : stream.Count - 1;
            
            if (expectedVersion != ExpectedVersion.Any && expectedVersion != currentVerion)
            {
                throw new ConcurrencyConflictException(streamId, expectedVersion, currentVerion);
            }

            var recorded = new RecordedEvent[events.Count];
            for (var i = 0; i < events.Count; i++)
                recorded[i] = new RecordedEvent(
                    streamId, currentVerion + 1 + i,
                    _lastPosition + 1 + i, events[i]);
            
            if (stream is null)
            {
                stream = [];
                _streams.Add(streamId, stream);
            }
            stream.AddRange(recorded);
            _lastPosition += recorded.Length;

            foreach (var r in recorded)
                foreach (var projection in _projections)
                    projection.Apply(r);
            
            return Task.FromResult(currentVerion + recorded.Length);
        }
    }
}