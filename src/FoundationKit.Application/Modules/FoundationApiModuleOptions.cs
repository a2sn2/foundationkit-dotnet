namespace FoundationKit.Application.Modules;

public sealed record FoundationApiModuleOptions(
    string RoutePrefix,
    FoundationApiIdempotencyMode Idempotency,
    FoundationApiConcurrencyMode Concurrency,
    int MaximumFilters,
    int MaximumSorts,
    string? RateLimitPolicyName)
{
    public static FoundationApiModuleOptions Default { get; } = new(
        "api",
        FoundationApiIdempotencyMode.Disabled,
        FoundationApiConcurrencyMode.ApplicationPolicy,
        10,
        5,
        null);
}

public sealed class FoundationApiModuleOptionsBuilder
{
    public string RoutePrefix { get; set; } = FoundationApiModuleOptions.Default.RoutePrefix;

    public FoundationApiIdempotencyMode Idempotency { get; set; } = FoundationApiModuleOptions.Default.Idempotency;

    public FoundationApiConcurrencyMode Concurrency { get; set; } = FoundationApiModuleOptions.Default.Concurrency;

    public int MaximumFilters { get; set; } = FoundationApiModuleOptions.Default.MaximumFilters;

    public int MaximumSorts { get; set; } = FoundationApiModuleOptions.Default.MaximumSorts;

    public string? RateLimitPolicyName { get; set; }

    internal FoundationApiModuleOptions Build()
    {
        var routePrefix = NormalizeRoutePrefix(RoutePrefix);
        if (!Enum.IsDefined(Idempotency))
            throw new ArgumentOutOfRangeException(nameof(Idempotency), Idempotency, "Unknown API idempotency mode.");
        if (!Enum.IsDefined(Concurrency))
            throw new ArgumentOutOfRangeException(nameof(Concurrency), Concurrency, "Unknown API concurrency mode.");
        if (MaximumFilters is < 0 or > 25)
            throw new ArgumentOutOfRangeException(nameof(MaximumFilters), "Maximum filters must be between 0 and 25.");
        if (MaximumSorts is < 0 or > 10)
            throw new ArgumentOutOfRangeException(nameof(MaximumSorts), "Maximum sorts must be between 0 and 10.");

        var rateLimitPolicy = string.IsNullOrWhiteSpace(RateLimitPolicyName)
            ? null
            : NormalizeToken(RateLimitPolicyName, nameof(RateLimitPolicyName), 96);

        return new FoundationApiModuleOptions(
            routePrefix,
            Idempotency,
            Concurrency,
            MaximumFilters,
            MaximumSorts,
            rateLimitPolicy);
    }

    private static string NormalizeRoutePrefix(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var route = value.Trim().Trim('/').ToLowerInvariant();
        if (route.Length is 0 or > 48)
            throw new ArgumentException("API route prefix must contain between 1 and 48 characters.", nameof(value));

        var segments = route.Split('/');
        if (segments.Any(segment =>
                segment.Length == 0 ||
                !char.IsAsciiLetterOrDigit(segment[0]) ||
                !char.IsAsciiLetterOrDigit(segment[^1]) ||
                segment.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-')))
        {
            throw new ArgumentException(
                "API route prefix segments must start/end with an ASCII letter or digit and may contain only letters, digits, and '-'.",
                nameof(value));
        }

        return route;
    }

    private static string NormalizeToken(string value, string parameterName, int maximumLength)
    {
        var token = value.Trim();
        if (token.Length is 0 || token.Length > maximumLength || token.Any(char.IsControl))
            throw new ArgumentException("API option value is empty, too long, or contains control characters.", parameterName);
        return token;
    }
}
