using Volo.Abp.Settings;

namespace FoundationKit.Settings;

/// <summary>
/// Resolves FoundationKit setting requests through ABP's current tenant/user setting context.
/// Explicit FoundationKit scope ordering is intentionally delegated to ABP for this provider.
/// </summary>
public sealed class AbpSettingReader : ISettingReader
{
    private static readonly SettingScope ProviderScope = new("provider", "abp-current-context");
    private readonly ISettingProvider _provider;
    private readonly Func<string, string> _nameMap;

    public AbpSettingReader(
        ISettingProvider provider,
        Func<string, string>? nameMap = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _nameMap = nameMap ?? (static key => key);
    }

    public async ValueTask<ResolvedSetting?> ResolveAsync(
        string key,
        SettingResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var providerName = _nameMap(key.Trim());
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        var value = await _provider.GetOrNullAsync(providerName).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return value is null
            ? null
            : new ResolvedSetting(key.Trim(), value, ProviderScope);
    }
}
