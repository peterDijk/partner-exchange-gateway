using System.Text.Json;
using Json.Schema;

namespace PartnerExchange.Gateway.Api;

public sealed class ReportSubmissionValidator
{
    private static readonly EvaluationOptions Options = new()
    {
        OutputFormat = OutputFormat.List,
        RequireFormatValidation = true,
    };

    private static readonly JsonSchema Schema = Load();

    public Dictionary<string, string[]> Validate(JsonElement instance)
    {
        var results = Schema.Evaluate(instance, Options);
        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        if (!results.IsValid)
            Collect(results, errors);
        return errors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray(), StringComparer.Ordinal);
    }

    private static JsonSchema Load()
    {
        using var stream = typeof(ReportSubmissionValidator).Assembly
            .GetManifestResourceStream("report-submission.schema.json")
            ?? throw new InvalidOperationException("Embedded schema 'report-submission.schema.json' not found.");
        using var reader = new StreamReader(stream);
        return JsonSchema.FromText(reader.ReadToEnd());
    }

    private static void Collect(EvaluationResults node, Dictionary<string, List<string>> errors)
    {
        if (node.Errors is { Count: > 0 })
        {
            var key = node.InstanceLocation.ToString() is { Length: > 0 } pointer ? pointer : "$";
            if (!errors.TryGetValue(key, out var list))
                errors[key] = list = [];
            list.AddRange(node.Errors.Values);
        }

        foreach (var child in node.Details ?? [])
            Collect(child, errors);
    }
}
