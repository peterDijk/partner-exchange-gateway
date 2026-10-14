namespace PartnerExchange.Gateway.Translation;

public sealed record TranslationResult<T>(
    T? Value,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings) where T : class;