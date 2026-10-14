using PartnerExchange.Contracts.Pxf;

namespace PartnerExchange.Gateway.Domain;

public abstract record DomainCommand;

public abstract record DeliveryCommand : DomainCommand;

public sealed record AcceptReport(
    Guid MessageId,
    string PartnerId,
    PxfVersion Version,
    string Payload,
    UnitReport Report,
    IReadOnlyList<string> Warnings
) : DeliveryCommand;

public sealed record RecordSuccess(DateTimeOffset now) : DeliveryCommand;
public sealed record RecordFailure(string reason, bool permanent, DateTimeOffset now,
    RetryPolicy policy, double jitter) : DeliveryCommand;
public sealed record RequestReplay(DateTimeOffset now) : DeliveryCommand;