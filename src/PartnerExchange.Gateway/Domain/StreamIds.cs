using System.Security.Cryptography;
using System.Text;

namespace PartnerExchange.Gateway.Domain;

public static class StreamIds
{
    public const string Rejections = "rejections";
    public static string Delivery(Guid messageId) => 
        $"delivery-{messageId:N}";
    public static string Inbound(string partnerId, Guid messageId) => 
        $"inbound-{partnerId}-{messageId:N}";
    public static string Idempotency(string key) =>
        $"idempotency-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))}";

}
