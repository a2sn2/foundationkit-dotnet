using FoundationKit.Application.Capabilities;

namespace FoundationKit.Composer;

public sealed record CompositionEntry(
    CapabilityDescriptor Capability,
    IReadOnlyList<string> Reasons);

public sealed record CompositionAnalysis(
    ComposerManifest Manifest,
    IReadOnlyList<CompositionEntry> Entries,
    IReadOnlyList<CapabilityCompatibilityResult> CompatibilityResults,
    IReadOnlyList<string> Warnings)
{
    public bool IsStableOnly => Entries.All(entry => entry.Capability.Maturity == CapabilityMaturity.Stable);
}

public static class CompositionAnalyzer
{
    public static CompositionAnalysis Analyze(
        ComposerManifest manifest,
        CapabilityResolver? resolver = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        resolver ??= CapabilityResolver.CreateDefault();

        var catalog = FoundationCapabilityCatalog.All.ToDictionary(
            capability => capability.Id,
            StringComparer.OrdinalIgnoreCase);
        var profile = FoundationCapabilityProfiles.Get(manifest.Profile);

        ValidateManifestKinds(manifest, catalog);

        var resolved = manifest.ToProjectManifest().Resolve(resolver);
        ValidateProjectModelComposition(manifest, resolved);
        var compatibility = CapabilityCompatibility.Evaluate(
            resolved,
            manifest.ContractRequirements);
        var reasons = BuildReasons(manifest, profile, resolved, catalog);
        var entries = resolved
            .Select(capability => new CompositionEntry(
                capability,
                reasons.TryGetValue(capability.Id, out var capabilityReasons)
                    ? capabilityReasons.Order(StringComparer.OrdinalIgnoreCase).ToArray()
                    : ["dependency"]))
            .ToArray();

        var warnings = entries
            .Where(entry => entry.Capability.Maturity != CapabilityMaturity.Stable)
            .Select(entry =>
                $"Capability '{entry.Capability.Id}' maturity is {entry.Capability.Maturity}; " +
                "catalog selection does not mean the capability is fully generatable or production-ready.")
            .ToArray();

        return new CompositionAnalysis(manifest, entries, compatibility, warnings);
    }

    private static void ValidateManifestKinds(
        ComposerManifest manifest,
        Dictionary<string, CapabilityDescriptor> catalog)
    {
        foreach (var id in manifest.IncludeCapabilities.Concat(manifest.ExcludeCapabilities))
        {
            if (!catalog.TryGetValue(id, out var capability))
                throw new ComposerManifestException($"Unknown capability '{id}'.");

            if (capability.Kind == CapabilityKind.Provider)
            {
                throw new ComposerManifestException(
                    $"Provider '{id}' must be listed under 'providers', not capability include/exclude lists.");
            }

            if (capability.Kind == CapabilityKind.Tooling)
            {
                throw new ComposerManifestException(
                    $"Tooling capability '{id}' cannot be selected as a runtime project capability.");
            }
        }

        foreach (var id in manifest.ResourceCapabilityIds)
        {
            if (!catalog.TryGetValue(id, out var capability) ||
                capability.Kind is CapabilityKind.Provider or CapabilityKind.Tooling)
            {
                throw new ComposerManifestException(
                    $"Resource behavior maps to invalid runtime capability '{id}'.");
            }
        }

        foreach (var providerId in manifest.Providers)
        {
            if (!catalog.TryGetValue(providerId, out var provider))
                throw new ComposerManifestException($"Unknown provider '{providerId}'.");

            if (provider.Kind != CapabilityKind.Provider)
            {
                throw new ComposerManifestException(
                    $"'{providerId}' is not a provider capability and cannot be listed under 'providers'.");
            }
        }

        foreach (var requirement in manifest.ContractRequirements)
        {
            if (!catalog.ContainsKey(requirement.CapabilityId))
            {
                throw new ComposerManifestException(
                    $"Unknown capability contract '{requirement.CapabilityId}'.");
            }
        }
    }

    private static void ValidateProjectModelComposition(
        ComposerManifest manifest,
        IReadOnlyList<CapabilityDescriptor> resolved)
    {
        if (manifest.ProjectModel is null)
            return;

        var resolvedIds = resolved
            .Select(capability => capability.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!resolvedIds.Contains(FoundationCapabilityIds.WebApi))
        {
            throw new ComposerManifestException(
                "Schema v2 resources require the 'web-api' capability because the current executable Module/API Engine is HTTP based.");
        }
    }

    private static Dictionary<string, HashSet<string>> BuildReasons(
        ComposerManifest manifest,
        CapabilityProfile profile,
        IReadOnlyList<CapabilityDescriptor> resolved,
        Dictionary<string, CapabilityDescriptor> catalog)
    {
        var reasons = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var selectedIds = resolved.Select(capability => capability.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var id in profile.CapabilityIds)
        {
            if (selectedIds.Contains(id) && !manifest.ExcludeCapabilities.Contains(id, StringComparer.OrdinalIgnoreCase))
                AddReason(reasons, id, $"profile:{profile.Id}");
        }

        foreach (var id in manifest.IncludeCapabilities)
            AddReason(reasons, id, "explicit-include");

        foreach (var id in manifest.Providers)
            AddReason(reasons, id, "explicit-provider");

        if (manifest.ProjectModel is not null)
        {
            foreach (var module in manifest.ProjectModel.Modules)
            {
                foreach (var resource in module.Resources)
                {
                    foreach (var behavior in resource.Behaviors)
                    {
                        var capabilityId = MapBehaviorCapability(behavior);
                        if (capabilityId is not null && selectedIds.Contains(capabilityId))
                        {
                            AddReason(
                                reasons,
                                capabilityId,
                                $"resource:{module.Name}.{resource.Name}:{BehaviorName(behavior)}");
                        }
                    }
                }
            }
        }

        foreach (var parent in resolved)
        {
            foreach (var dependency in parent.Dependencies)
            {
                if (selectedIds.Contains(dependency) && catalog.ContainsKey(dependency))
                    AddReason(reasons, dependency, $"required-by:{parent.Id}");
            }
        }

        return reasons;
    }

    private static string? MapBehaviorCapability(ComposerResourceBehavior behavior) => behavior switch
    {
        ComposerResourceBehavior.Crud => null,
        ComposerResourceBehavior.Concurrency => null,
        ComposerResourceBehavior.Auditing => FoundationCapabilityIds.Auditing,
        ComposerResourceBehavior.Authorization => FoundationCapabilityIds.Authorization,
        ComposerResourceBehavior.Workflow => FoundationCapabilityIds.Workflow,
        ComposerResourceBehavior.Caching => FoundationCapabilityIds.Caching,
        ComposerResourceBehavior.Security => FoundationCapabilityIds.Security,
        ComposerResourceBehavior.Identity => FoundationCapabilityIds.Identity,
        ComposerResourceBehavior.Approvals => FoundationCapabilityIds.Approvals,
        ComposerResourceBehavior.Notifications => FoundationCapabilityIds.Notifications,
        ComposerResourceBehavior.Settings => FoundationCapabilityIds.Settings,
        ComposerResourceBehavior.FeatureManagement => FoundationCapabilityIds.FeatureManagement,
        ComposerResourceBehavior.Localization => FoundationCapabilityIds.Localization,
        _ => throw new InvalidOperationException($"Unsupported resource behavior '{behavior}'.")
    };

    private static string BehaviorName(ComposerResourceBehavior behavior) => behavior switch
    {
        ComposerResourceBehavior.FeatureManagement => "feature-management",
        _ => behavior.ToString().ToLowerInvariant()
    };

    private static void AddReason(
        Dictionary<string, HashSet<string>> reasons,
        string id,
        string reason)
    {
        if (!reasons.TryGetValue(id, out var capabilityReasons))
        {
            capabilityReasons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            reasons[id] = capabilityReasons;
        }

        capabilityReasons.Add(reason);
    }
}
