using System.Text.Json.Serialization;
using PartnerExchange.Gateway;
using PartnerExchange.Gateway.Api;
using PartnerExchange.Gateway.Application;
using PartnerExchange.Gateway.Projections;
using PartnerExchange.Gateway.Storage;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = 64 * 1024);

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddOptions<GatewayOptions>()
    .BindConfiguration(GatewayOptions.Section)
    .Validate(o => o.Originator.Length is > 0 and <= 8, "Gateway:Originator must be 1-8 characters.")
    .Validate(o => o.Partners.Count > 0, "Configure at least one partner under Gateway:Partners.")
    .Validate(o => o.Partners.Values.All(p => p.Endpoint is not null), "Every partner needs an Endpoint.")
    .ValidateOnStart();

// One instance per projection, exposed both as itself (for queries) and as IProjection (for the store).
builder.Services.AddSingleton<DeliveryProjection>();
builder.Services.AddSingleton<IProjection>(sp => sp.GetRequiredService<DeliveryProjection>());
builder.Services.AddSingleton<RejectionProjection>();
builder.Services.AddSingleton<IProjection>(sp => sp.GetRequiredService<RejectionProjection>());

builder.Services.AddSingleton<IEventStore, InMemoryEventStore>();
builder.Services.AddSingleton<DeliveryCommands>();
builder.Services.AddSingleton<RejectionLog>();
builder.Services.AddSingleton<ReportIntake>();
builder.Services.AddSingleton<ReportSubmissionValidator>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapOpenApi();
app.MapScalarApiReference();
app.MapReportEndpoints();

app.Run();
