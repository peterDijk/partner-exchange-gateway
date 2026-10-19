using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PartnerExchange.Gateway.Api;
using PartnerExchange.Gateway.Domain;

namespace PartnerExchange.Gateway.Tests.Support;

public static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}

public static class Requests
{
    public static SubmitReportRequest ValidAlpha() => new(
        "ALPHA", "BLUE-ALPHA-01", "FALCON11", 12.3456m, -45.6789m, 150, 32,
        OperationalStatus.FullyOperational, ReadinessLevel.Immediate,
        new DateTimeOffset(2026, 10,19, 9, 59, 0, TimeSpan.Zero));
}

public static class GatewayClientExtensions
{
    public static Task<HttpResponseMessage> PostReportAsync(this HttpClient client, SubmitReportRequest request) =>
        client.PostAsJsonAsync("/reports", request, TestJson.Options);
    
    public static async Task<Guid> SubmitAsync(this HttpClient client, SubmitReportRequest request)
    {
        var response = await client.PostReportAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<SubmitReportResponse>(TestJson.Options);
        return body!.MessageId;
    }

    public static async Task<DeliveryResponse> GetDeliveryAsync(this HttpClient client, Guid messageId) =>
        (await client.GetFromJsonAsync<DeliveryResponse>($"/deliveries/{messageId}", TestJson.Options))!;
}
