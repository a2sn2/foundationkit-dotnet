using FoundationKit.Settings;

namespace FoundationKit.FeatureManagement;

public static class FeatureId
{
    public const int MaximumLength = 120;

    public static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > MaximumLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                $"Feature ID cannot exceed {MaximumLength} characters.");
        }

        if (!char.IsLetterOrDigit(normalized[0])
            || normalized.Any(character =>
                !(char.IsLetterOrDigit(character)
                  || character is '.' or ':' or '-' or '_')))
        {
            throw new ArgumentException(
                "Feature ID must start with a letter or digit and contain only letters, digits, '.', ':', '-', or '_'.",
                nameof(value));
        }

        return normalized;
    }
}

public sealed record FeatureDefinition
{
    public FeatureDefinition(
        string id,
        bool defaultEnabled = false,
        string? description = null)
    {
        Id = FeatureId.Normalize(id);
        DefaultEnabled = defaultEnabled;

        if (string.IsNullOrWhiteSpace(description))
        {
            Description = null;
            return;
        }

        var normalizedDescription = description.Trim();
        if (normalizedDescription.Length > 500)
        {
            throw new ArgumentOutOfRangeException(
                nameof(description),
                "Feature description cannot exceed 500 characters.");
        }

        Description = normalizedDescription;
    }

    public string Id { get; }

    public bool DefaultEnabled { get; }

    public string? Description { get; }

    public override string ToString() => Id;
}

public enum FeatureDecisionSource
{
    Default,
    Setting,
    InvalidSetting,
    Provider
}

public sealed record FeatureDecision(
    string FeatureId,
    bool IsEnabled,
    FeatureDecisionSource Source,
    SettingScope? MatchedScope)
{
    public override string ToString() => $"{FeatureId}:{IsEnabled}";
}

public sealed class FeatureEvaluationContext
{
    public static FeatureEvaluationContext Global { get; } =
        new(SettingResolutionContext.Global);

    public FeatureEvaluationContext(SettingResolutionContext settings)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public SettingResolutionContext Settings { get; }
}

public interface IFeatureEvaluator
{
    ValueTask<FeatureDecision> EvaluateAsync(
        FeatureDefinition feature,
        FeatureEvaluationContext context,
        CancellationToken cancellationToken = default);
}

public sealed class SettingBackedFeatureEvaluator(ISettingReader settings) : IFeatureEvaluator
{
    private readonly ISettingReader _settings = settings
        ?? throw new ArgumentNullException(nameof(settings));

    public async ValueTask<FeatureDecision> EvaluateAsync(
        FeatureDefinition feature,
        FeatureEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(feature);
        ArgumentNullException.ThrowIfNull(context);

        var resolved = await _settings.ResolveAsync(
            GetEnabledSettingKey(feature.Id),
            context.Settings,
            cancellationToken).ConfigureAwait(false);

        if (resolved is null)
        {
            return new FeatureDecision(
                feature.Id,
                feature.DefaultEnabled,
                FeatureDecisionSource.Default,
                null);
        }

        if (bool.TryParse(resolved.Value.Trim(), out var enabled))
        {
            return new FeatureDecision(
                feature.Id,
                enabled,
                FeatureDecisionSource.Setting,
                resolved.Scope);
        }

        return new FeatureDecision(
            feature.Id,
            false,
            FeatureDecisionSource.InvalidSetting,
            resolved.Scope);
    }

    public static string GetEnabledSettingKey(string featureId) =>
        SettingKey.Normalize($"features.{FeatureId.Normalize(featureId)}.enabled");
}
