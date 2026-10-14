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

public sealed record RecordSuccess : DeliveryCommand;
public sealed record RecordFailure(string Reason, bool Permanent,
    double Jitter) : DeliveryCommand;
public sealed record RequestReplay : DeliveryCommand;