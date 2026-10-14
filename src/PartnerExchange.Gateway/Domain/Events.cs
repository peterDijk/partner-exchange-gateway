using System.Text.Json.Serialization;
using PartnerExchange.Contracts.Pxf;

namespace PartnerExchange.Gateway.Domain;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ReportAccepted), "report-accepted")]
[JsonDerivedType(typeof(DeliveryAttemptFailed), "delivery-attempt-failed")]
[JsonDerivedType(typeof(ReportDelivered), "report-delivered")]
[JsonDerivedType(typeof(DeliveryDeadLettered), "delivery-dead-lettered")]
[JsonDerivedType(typeof(DeliveryReplayRequested), "delivery-replay-requested")]
[JsonDerivedType(typeof(InboundReportAccepted), "inbound-report-accepted")]
[JsonDerivedType(typeof(MessageRejected), "message-rejected")]

public abstract record DomainEvent;

public abstract record DeliveryEvent(Guid MessageId) : DomainEvent;

public sealed record ReportAccepted(
    Guid MessageId,
    string PartnerId,
    PxfVersion Version,
    string Payload,
    UnitReport Report,
    IReadOnlyList<string> Warnings,
    DateTimeOffset AcceptedAt
) : DeliveryEvent(MessageId);

public sealed record DeliveryAttemptFailed(
    Guid MessageId,
    int Attempt,
    string Reason,
    DateTimeOffset FailedAt,
    DateTimeOffset NextAttemptAt
) : DeliveryEvent(MessageId);

public sealed record ReportDelivered(
    Guid MessageId,
    int Attempt,
    DateTimeOffset DeliveredAt
) : DeliveryEvent(MessageId);

public sealed record DeliveryDeadLettered(
    Guid MessageId,
    int Attempts,
    string Reason,
    DateTimeOffset At
) : DeliveryEvent(MessageId);

public sealed record DeliveryReplayRequested(
    Guid MessageId,
    DateTimeOffset At
) : DeliveryEvent(MessageId);

public sealed record InboundReportAccepted(
    string PartnerId,
    Guid MessageId,
    PxfVersion Version,
    UnitReport Report,
    DateTimeOffset ReceivedAt
) : DomainEvent;

public sealed record MessageRejected(
    Guid RejectionId,
    string Direction,
    string? PartnerId,
    IReadOnlyList<string> Reasons,
    DateTimeOffset At
) : DomainEvent;
