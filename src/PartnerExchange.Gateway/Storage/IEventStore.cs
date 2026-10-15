using PartnerExchange.Gateway.Domain;

namespace PartnerExchange.Gateway.Storage;

public interface IEventStore
{
    // async because of possiblity to later add EventstoreDB(KurrentDB)/ Marten
    Task<IReadOnlyList<RecordedEvent>> ReadStreamAsync(
        string streamId, CancellationToken ct = default);

    Task<long> AppendAsync(string streamId, long expectedVersion,
        IReadOnlyList<DomainEvent> events, CancellationToken ct = default);
}

public interface IProjection
{
    void Apply(RecordedEvent recorded);
}