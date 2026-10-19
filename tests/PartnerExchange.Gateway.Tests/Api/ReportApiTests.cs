using System.Net;
using System.Text;

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

using PartnerExchange.Gateway.Tests.Support;

namespace PartnerExchange.Gateway.Tests.Api;

public class ReportApiTests
{
    [Fact]
    public async Task Valid_report_is_accepted_and_visible()
    {
        await using var factory = new GatewayFactory();
        var client = factory.CreateClient();

        var response = await client.PostReportAsync(Requests.ValidAlpha());

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var messageId = Guid.Parse(response.Headers.Location!.ToString().Split('/')[^1]);
        var delivery = await client.GetDeliveryAsync(messageId);
        Assert.Equal("ALPHA", delivery.PartnerId);
        Assert.Empty(delivery.Warnings);
    }

    [Fact]
    public async Task Report_for_a_v1_partner_is_accepted_with_lossy_translation_warnings()
    {
        await using var factory = new GatewayFactory();
        var client = factory.CreateClient();

        var messageId = await client.SubmitAsync(Requests.ValidAlpha() with { PartnerId = "BRAVO" });
        Assert.Equal(3, (await client.GetDeliveryAsync(messageId)).Warnings.Count); // dropped: altitude, readiness, speed 
    }

    [Theory]
    [InlineData("""{ "partnerId": "ALPHA" }""")]
    [InlineData("""{ "partnerId": "ALPHA", "unitId": "U1", "callsign": "C1", "latitude": 95, "longitude": 0, "status": "Degraded", "observedAt": "2026-10-06T10:00:00Z" }""")]
    [InlineData("""{ "partnerId": "ALPHA", "unitId": "U1", "callsign": "C1", "latitude": 1, "longitude": 0, "status": "Degraded", "observedAt": "2026-10-06T10:00:00Z", "extra": true }""")]
    [InlineData("""{ "partnerId": "ALPHA", "unitId": "U1", "callsign": "C1", "latitude": 1, "longitude": 0, "status": "Degraded", "observedAt": "yesterday" }""")]
    public async Task Schema_violations_return_400_problem_details(string json)
    {
        await using var factory = new GatewayFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/reports", new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Callsign_valid_nationally_but_too_long_for_pxf_is_422_and_recorded()
    {
        await using var factory = new GatewayFactory();
        var client = factory.CreateClient();

        var response = await client.PostReportAsync(Requests.ValidAlpha() with { Callsign = "FALCON1234" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var rejections = await client.GetStringAsync("/rejections");
        Assert.Contains("FALCON1234", rejections, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unknown_partner_is_422()
    {
        await using var factory = new GatewayFactory();
        var client = factory.CreateClient();

        var response = await client.PostReportAsync(Requests.ValidAlpha() with { PartnerId = "CHARLIE" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_delivery_is_404()
    {
        await using var factory = new GatewayFactory();
        var response = await factory.CreateClient().GetAsync($"/deliveries/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Serves_an_openapi_document()
    {
        await using var factory = new GatewayFactory();
        var document = await factory.CreateClient().GetStringAsync("/openapi/v1.json");
        Assert.Contains("\"/reports\"", document, StringComparison.Ordinal);
    }
}
