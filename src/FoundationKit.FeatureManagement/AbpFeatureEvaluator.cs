using Volo.Abp.Features;

namespace FoundationKit.FeatureManagement;

/// <summary>
/// Evaluates FoundationKit features through ABP's current feature context while
/// preserving FoundationKit's provider-neutral decision contract.
/// </summary>
public sealed class AbpFeatureEvaluator : IFeatureEvaluator
{
    private readonly IFeatureChecker _checker;
    private readonly Func<string, string> _nameMap;

    public AbpFeatureEvaluator(
        IFeatureChecker checker,
        Func<string, string>? nameMap = null)
    {
        _checker = checker ?? throw new ArgumentNullException(nameof(checker));
        _nameMap = nameMap ?? (static id => id);
    }

    public async ValueTask<FeatureDecision> EvaluateAsync(
        FeatureDefinition feature,
        FeatureEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(feature);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var providerName = _nameMap(feature.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        var enabled = await _checker.IsEnabledAsync(providerName).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return new FeatureDecision(
            feature.Id,
            enabled,
            FeatureDecisionSource.Provider,
            null);
    }
}
