using System.Runtime.CompilerServices;

using Microsoft.Extensions.Options;
using PartnerExchange.Contracts.Pxf;
using PartnerExchange.Gateway.Api;
using PartnerExchange.Gateway.Domain;
using PartnerExchange.Gateway.Translation;

namespace PartnerExchange.Gateway.Application;

public abstract record SubmitOutcome
{
    private SubmitOutcome() { }

    public sealed record Accepted(Guid MessageId, IReadOnlyList<string> Warnings) : SubmitOutcome;

    public sealed record UnknownPartner(string PartnerId) : SubmitOutcome;

    public sealed record Untranslatable(IReadOnlyList<string> Errors) : SubmitOutcome;
}

public sealed class ReportIntake(
    DeliveryCommands commands,
    RejectionLog rejections,
    IOptions<GatewayOptions> options,
    TimeProvider time)
{
    public async Task<SubmitOutcome> SubmitAsync(SubmitReportRequest request, CancellationToken ct)
    {
        var gateway = options.Value;
        if (!gateway.Partners.TryGetValue(request.PartnerId, out var partner))
        {
            await rejections.RecordAsync("national", request.PartnerId, [$"Unknown partner '{request.PartnerId}'."], ct);
            return new SubmitOutcome.UnknownPartner(request.PartnerId);
        }

        RetryPolicy policy = new(gateway.Resilience.MaxAttempts, gateway.Resilience.BaseDelay, gateway.Resilience.MaxDelay);

        var now = time.GetUtcNow();
        var messageId = Guid.CreateVersion7(now);
        var report = request.ToUnitReport();

        var translation = PxfTranslator.ToPxf(report, messageId, gateway.Originator, partner.Version, now);
        if (translation.Value is not { } pxf)
        {
            await rejections.RecordAsync("national", request.PartnerId, translation.Errors, ct);
            return new SubmitOutcome.Untranslatable(translation.Errors);
        }

        var command = new AcceptReport(
            messageId, request.PartnerId, partner.Version, PxfWriter.Write(pxf), report, translation.Warnings);
        var decision = await commands.HandleAsync(messageId, command, now, policy, ct);
        if (decision is Decision.Reject reject)
            throw new InvalidOperationException(reject.Reason);

        return new SubmitOutcome.Accepted(messageId, translation.Warnings);
    }
}
