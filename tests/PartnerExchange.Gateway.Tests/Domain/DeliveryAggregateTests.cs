using PartnerExchange.Contracts.Pxf;
using PartnerExchange.Gateway.Domain;

namespace PartnerExchange.Gateway.Tests.Domain;

public class DeliveryAggregateTests
{
    private static readonly RetryPolicy Policy =
        new(MaxAttempts: 3, TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(10));
    private static readonly DateTimeOffset T0 = Sample.T0;

    private static Delivery? Given(params DeliveryEvent[] history) =>
        DeliveryAggregate.Fold(history);
    
    private static T SingleEvent<T>(Decision? decision) where T : DeliveryEvent =>
        Assert.IsType<T>(
            Assert.Single(
                Assert.IsType<Decision.Emit>(decision).Events
            )
        );
    
    [Fact]
    public void Accepting_a_new_report_emits_ReportAccepted()
    {
        var cmd = new AcceptReport(Sample.MessageId, "ALPHA",
            PxfVersion.V2, "payload", Sample.Report(), []);
        var accepted = SingleEvent<ReportAccepted>(
            DeliveryAggregate.Decide(null, cmd, T0, Policy));
        Assert.Equal(T0, accepted.AcceptedAt);
    }

    [Fact]
    public void Accepting_twice_is_rejected()
    {
        var cmd = new AcceptReport(Sample.MessageId, "ALPHA",
            PxfVersion.V2, "payload", Sample.Report(), []);
        Assert.IsType<Decision.Reject>(
            DeliveryAggregate.Decide(Given(Sample.Accepted()), cmd, T0, Policy));
    }

    [Fact]
    public void Failure_schedules_a_retry_with_backoff()
    {
        var cmd = new RecordFailure("503", Permanent: false, Jitter: 1.0);
        var failed = SingleEvent<DeliveryAttemptFailed>(
            DeliveryAggregate.Decide(Given(Sample.Accepted()), cmd, T0, Policy)
        );
        Assert.Equal(1, failed.Attempt);
        Assert.Equal(T0 + TimeSpan.FromSeconds(1), failed.NextAttemptAt);
    }

    [Fact]
    public void Permanent_failure_dead_letters_immediately()
    {
        var cmd = new RecordFailure("503", Permanent: true, Jitter: 0.5);
        var dead = SingleEvent<DeliveryDeadLettered>(
            DeliveryAggregate.Decide(Given(Sample.Accepted()), cmd, T0, Policy));
        Assert.Equal(1, dead.Attempts);
    }

    [Fact]
    public void Exhausting_attempts_dead_letters()
    {
        var state = Given(
            Sample.Accepted(),
            new DeliveryAttemptFailed(Sample.MessageId, 1, "503", T0, T0),
            new DeliveryAttemptFailed(Sample.MessageId, 2, "503", T0, T0));
        var cmd = new RecordFailure("503", Permanent: false, Jitter: 1.0);
    
        var dead = SingleEvent<DeliveryDeadLettered>(
            DeliveryAggregate.Decide(state, cmd, T0, Policy)
        );
        Assert.Equal(3, dead.Attempts);
    }

    [Fact]
    public void Success_after_a_retry_counts_the_attempt()
    {
        var state = Given(Sample.Accepted(),
            new DeliveryAttemptFailed(Sample.MessageId, 1, "503", T0, T0));
        var cmd = new RecordSuccess();
        var delivered = SingleEvent<ReportDelivered>(
            DeliveryAggregate.Decide(state, cmd, T0, Policy)
        );
        Assert.Equal(2, delivered.Attempt);
    }

    [Fact]
    public void Delivered_is_terminal()
    {
        var state = Given(Sample.Accepted(),
            new ReportDelivered(Sample.MessageId, 1, T0));
        var cmd = new RecordFailure("x", false, 0.1);
        Assert.IsType<Decision.Reject>(
            DeliveryAggregate.Decide(state, cmd, T0, Policy)
        );
        Assert.IsType<Decision.Reject>(
            DeliveryAggregate.Decide(state, new RequestReplay(), T0, Policy)
        );
    }

    [Fact]
    public void Replay_resets_a_dead_letter_to_pending_with_0_attemps()
    {
        var state = Given(Sample.Accepted(),
            new DeliveryDeadLettered(Sample.MessageId, 3, "503", T0));
        var replay = SingleEvent<DeliveryReplayRequested>(
            DeliveryAggregate.Decide(state, new RequestReplay() ,T0, Policy));
        var after = DeliveryAggregate.Apply(state, replay);

        Assert.Equal(DeliveryStatus.Pending, after.Status);
        Assert.Equal(0, after.Attempts);
    }

    [Fact]
    public void Applying_event_to_wrong_state_throws()
    {
        Assert.Throws<InvalidOperationException>(() => 
            DeliveryAggregate.Apply(null,
                new ReportDelivered(Sample.MessageId, 1, T0)
                )
        );
    }

    [Fact]
    public void Replay_gives_a_fresh_budget_then_dead_letters_again()
    {
        var state = Given(
            Sample.Accepted(),
            new DeliveryDeadLettered(Sample.MessageId, 3, "503", T0));
        var request = SingleEvent<DeliveryReplayRequested>(
            DeliveryAggregate.Decide(state, new RequestReplay(), T0, Policy));
        var requested = DeliveryAggregate.Apply(state, request);

        var fail1 = SingleEvent<DeliveryAttemptFailed>(
            DeliveryAggregate.Decide(requested, new RecordFailure("503", false, 0.1), T0, Policy));
        var failed1 = DeliveryAggregate.Apply(requested, fail1);

        var fail2 = SingleEvent<DeliveryAttemptFailed>(
            DeliveryAggregate.Decide(failed1, new RecordFailure("503", false, 0.1), T0, Policy));
        var failed2 = DeliveryAggregate.Apply(failed1, fail2);

        var fail3 = SingleEvent<DeliveryDeadLettered>(
            DeliveryAggregate.Decide(failed2, new RecordFailure("503", false, 0.1), T0, Policy));
        var failed3 = DeliveryAggregate.Apply(failed2, fail3);
        
        Assert.Equal(DeliveryStatus.DeadLettered, failed3.Status);
        Assert.Equal(3, failed3.Attempts);
    }

    
}