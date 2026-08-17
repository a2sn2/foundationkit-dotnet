using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace FoundationKit.Caching;

/// <summary>
/// Typed cache operations backed by the native .NET HybridCache implementation.
/// </summary>
public interface IValueCache
{
    ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        TimeSpan? expiration = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    ValueTask SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);

    ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);
}

public sealed class HybridValueCache(HybridCache cache) : IValueCache
{
    public ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        TimeSpan? expiration = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);

        return cache.GetOrCreateAsync(
            key,
            factory,
            CreateOptions(expiration),
            tags,
            cancellationToken);
    }

    public ValueTask SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return cache.SetAsync(
            key,
            value,
            CreateOptions(expiration),
            tags,
            cancellationToken);
    }

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return cache.RemoveAsync(key, cancellationToken);
    }

    public ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        return cache.RemoveByTagAsync(tag, cancellationToken);
    }

    private static HybridCacheEntryOptions? CreateOptions(TimeSpan? expiration)
        => expiration is null
            ? null
            : new HybridCacheEntryOptions { Expiration = expiration };
}

public static class FoundationHybridCacheServiceCollectionExtensions
{
    public static IServiceCollection AddFoundationHybridCache(
        this IServiceCollection services,
        Action<HybridCacheOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is null)
        {
            services.AddHybridCache();
        }
        else
        {
            services.AddHybridCache(configure);
        }

        services.AddSingleton<IValueCache, HybridValueCache>();
        return services;
    }
}
