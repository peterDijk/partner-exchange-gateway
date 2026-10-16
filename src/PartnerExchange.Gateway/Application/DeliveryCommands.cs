using PartnerExchange.Gateway.Domain;
using PartnerExchange.Gateway.Storage;

namespace PartnerExchange.Gateway.Application;

public sealed class DeliveryCommands(IEventStore store)
{
    private const int MaxConflictRetries = 3;

    public async Task<Decision> HandleAsync(
        Guid messageId,
        DeliveryCommand command,
        DateTimeOffset now,
        RetryPolicy policy,
        CancellationToken ct)
    {
        var streamId = StreamIds.Delivery(messageId);

        for (var attempt = 1; ; attempt++)
        {
            var history = await store.ReadStreamAsync(streamId, ct);
            var state = DeliveryAggregate.Fold(history.Select(r => (DeliveryEvent)r.Event));
            var decision = DeliveryAggregate.Decide(state, command, now, policy);

            if (decision is not Decision.Emit emit)
            {
                return decision;
            }

            try
            {
                await store.AppendAsync(streamId, history.Count - 1, emit.Events, ct);
            }
            catch (ConcurrencyConflictException) when (attempt < MaxConflictRetries)
            {
                // Other append happening, try again from the top (read) again
            }
        }

    }
}