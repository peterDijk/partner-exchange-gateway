namespace PartnerExchange.Gateway.Domain;

public static class StreamIds
{
    public const string Rejections = "rejections";
    public static string Delivery(Guid messageId) => 
        $"delivery-{messageId:N}";
    public static string Inbound(string partnerId, Guid messageId) => 
        $"inbound-{partnerId}-{messageId:N}";

}
