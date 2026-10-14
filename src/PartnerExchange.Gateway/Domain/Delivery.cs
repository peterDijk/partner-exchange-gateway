using PartnerExchange.Contracts.Pxf;

namespace PartnerExchange.Gateway.Domain;

public enum DeliveryStatus
{
    Pending,
    RetryScheduled,
    Delivered,
    DeadLettered
}

public sealed record Delivery(
    Guid MessageId,
    string PartnerId,
    PxfVersion Version,
    string Payload,
    IReadOnlyList<string> Warnings,
    DeliveryStatus Status,
    int Attempts,
    DateTimeOffset? NextAttemptAt,
    string? LastError,
    DateTimeOffset AcceptedAt,
    DateTimeOffset? DeliveredAt
);

