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
            $"Cannot apply {e.GetType().Name} to {state?.Status.ToString() ?? "a new"} delivery"),
    };

    public static Delivery? Fold(IEnumerable<DeliveryEvent> events) =>
        events.Aggregate((Delivery?)null, (state, e) => Apply(state, e));

    public static Decision? Decide(Delivery? state, DeliveryCommand command, DateTimeOffset now, RetryPolicy policy) =>
        (state, command) switch
        {
            (null, AcceptReport cmd) => new Decision.Emit([
                new ReportAccepted(cmd.MessageId, cmd.PartnerId, cmd.Version,
                    cmd.Payload, cmd.Report, cmd.Warnings, now)
            ]),

            (null, _) => new Decision.Reject("delivery doesn't exist"),

            ({ Status: DeliveryStatus.Pending or DeliveryStatus.RetryScheduled } s, RecordSuccess) =>
                new Decision.Emit([new ReportDelivered(s.MessageId, s.Attempts + 1, now)]),

            ({ } s, RecordSuccess) => new Decision.Reject($"Status is {s.Status}, can not record success"),

            ({ Status: DeliveryStatus.Pending or DeliveryStatus.RetryScheduled } s, RecordFailure cmd)
                when cmd.Permanent || s.Attempts + 1 >= policy.MaxAttempts =>
                new Decision.Emit([new DeliveryDeadLettered(s.MessageId, s.Attempts + 1, cmd.Reason, now)]),

            ({ Status: DeliveryStatus.Pending or DeliveryStatus.RetryScheduled } s, RecordFailure cmd) =>
                new Decision.Emit([new DeliveryAttemptFailed(
                        s.MessageId, s.Attempts + 1, cmd.Reason, now,
                        now + policy.DelayFor(s.Attempts + 1, cmd.Jitter))]),

            ({ } s, RecordFailure) => new Decision.Reject($"Status is {s.Status}, can not record failure"),

            ({ Status: DeliveryStatus.DeadLettered } s, RequestReplay) =>
                new Decision.Emit([new DeliveryReplayRequested(s.MessageId, now)]),

            ({ } s, RequestReplay) => new Decision.Reject($"Can only be requested for deadlettered deliveries, current status is {s.Status}"),

            _ => throw new InvalidOperationException("unknown command")
        };
}