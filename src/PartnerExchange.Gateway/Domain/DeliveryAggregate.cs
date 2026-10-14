namespace PartnerExchange.Gateway.Domain;

public static class DeliveryAggregate
{
    public static Delivery Apply(Delivery? state, DeliveryEvent e) => (state, e) switch
    {
        (null, ReportAccepted a) => new Delivery(
            a.MessageId, a.PartnerId, a.Version,
            a.Payload, a.Warnings, DeliveryStatus.Pending,
            Attempts: 0, NextAttemptAt: null, LastError: null, a.AcceptedAt, DeliveredAt: null),
            (Delivery s, DeliveryAttemptFailed f) => s with
            {
                Status = DeliveryStatus.RetryScheduled,
                Attempts = f.Attempt,
                NextAttemptAt = f.NextAttemptAt,
                LastError = f.Reason
            },
            (Delivery s, ReportDelivered d) => s with
            {
                Status = DeliveryStatus.Delivered,
                Attempts = d.Attempt,
                NextAttemptAt = null,
                DeliveredAt = d.DeliveredAt,
            },
            (Delivery s, DeliveryDeadLettered x) => s with
            {
                Status = DeliveryStatus.DeadLettered,
                Attempts = x.Attempts,
                NextAttemptAt = null,
                LastError = x.Reason,
            },
            (Delivery s, DeliveryReplayRequested) => s with
            {
                Status = DeliveryStatus.Pending,
                Attempts = 0,
                NextAttemptAt = null,
            },
        _ => throw new InvalidOperationException(
            $"Cannon apply {e.GetType().Name} to {state?.Status.ToString() ?? "a new"} delivery"),
    };

    public static Delivery? Fold(IEnumerable<DeliveryEvent> events) =>
        events.Aggregate((Delivery?)null, (state, e) => Apply(state,e));


}