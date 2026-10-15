namespace PartnerExchange.Gateway.Domain;

public abstract record Decision
{
    private Decision() { }

    public sealed record Emit(IReadOnlyList<DeliveryEvent> Events) : Decision;

    public sealed record Reject(string Reason) : Decision;
}