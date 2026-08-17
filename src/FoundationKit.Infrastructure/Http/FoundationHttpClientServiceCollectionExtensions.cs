using Microsoft.Extensions.DependencyInjection;

namespace FoundationKit.Infrastructure.Http;

public static class FoundationHttpClientServiceCollectionExtensions
{
    public const string DefaultClientName = "FoundationKit.Resilient";

    /// <summary>
    /// Registers an HttpClient using the native .NET standard resilience pipeline
    /// (rate limiting, total/request timeout, retry and circuit breaker defaults).
    /// </summary>
    public static IHttpClientBuilder AddFoundationResilientHttpClient(
        this IServiceCollection services,
        string name = DefaultClientName,
        Action<HttpClient>? configureClient = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var builder = configureClient is null
            ? services.AddHttpClient(name)
            : services.AddHttpClient(name, configureClient);

        builder.AddStandardResilienceHandler();
        return builder;
    }
}
