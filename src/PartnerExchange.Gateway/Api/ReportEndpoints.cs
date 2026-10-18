using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PartnerExchange.Gateway.Application;
using PartnerExchange.Gateway.Domain;
using PartnerExchange.Gateway.Projections;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace PartnerExchange.Gateway.Api;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/reports", SubmitReport)
            .Accepts<SubmitReportRequest>("application/json")
            .WithSummary("Submit a national report for delivery to a coalition partner.");
        app.MapGet("/deliveries/{messageId:guid}", GetDelivery);
        app.MapGet("/deliveries", ListDeliveries);
        app.MapGet("/rejections", ListRejections);
        return app;
    }

    private static async Task<Results<Accepted<SubmitReportResponse>, ValidationProblem, ProblemHttpResult>> SubmitReport(
        [FromBody] JsonElement body,
        ReportSubmissionValidator validator,
        ReportIntake intake,
        RejectionLog rejections,
        IOptions<JsonOptions> json,
        CancellationToken ct)
    {
        var errors = validator.Validate(body);
        if (errors.Count > 0)
        {
            var reasons = errors.SelectMany(e => e.Value.Select(m => $"{e.Key}: {m}")).ToList();
            await rejections.RecordAsync("national", null, reasons, ct);
            return TypedResults.ValidationProblem(errors);
        }

        SubmitReportRequest request;
        try
        {
            request = body.Deserialize<SubmitReportRequest>(json.Value.SerializerOptions)
                ?? throw new JsonException("Body is null.");
        }
        catch (JsonException ex)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["$"] = [ex.Message] });
        }

        var outcome = await intake.SubmitAsync(request, ct);
        return outcome switch
        {
            SubmitOutcome.Accepted a => TypedResults.Accepted(
                $"/deliveries/{a.MessageId}", new SubmitReportResponse(a.MessageId, a.Warnings)),
            SubmitOutcome.UnknownPartner u => TypedResults.Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Unknown partner",
                detail: $"No partner '{u.PartnerId}' is configured."),
            SubmitOutcome.Untranslatable t => TypedResults.Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Report cannot be represented in the partner's PXF version",
                extensions: new Dictionary<string, object?> { ["errors"] = t.Errors }),
            _ => throw new UnreachableException(),
        };
    }

    private static Results<Ok<DeliveryResponse>, NotFound> GetDelivery(Guid messageId, DeliveryProjection deliveries) =>
        deliveries.Get(messageId) is { } d
            ? TypedResults.Ok(DeliveryResponse.From(d))
            : TypedResults.NotFound();

    private static Ok<List<DeliveryResponse>> ListDeliveries(DeliveryStatus? status, DeliveryProjection deliveries) =>
        TypedResults.Ok(deliveries.List(status).Select(DeliveryResponse.From).ToList());

    private static Ok<IReadOnlyList<MessageRejected>> ListRejections(RejectionProjection rejections) =>
        TypedResults.Ok(rejections.Latest(100));
}
