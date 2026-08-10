namespace FoundationKit.Application.Modules;

public static class FoundationModuleCapabilityRules
{
    private static readonly FoundationModuleCapability[] OrderedCapabilities =
    [
        FoundationModuleCapability.Crud,
        FoundationModuleCapability.Auditing,
        FoundationModuleCapability.Authorization,
        FoundationModuleCapability.Concurrency,
        FoundationModuleCapability.Workflow,
        FoundationModuleCapability.Caching,
        FoundationModuleCapability.Security,
        FoundationModuleCapability.Identity,
        FoundationModuleCapability.Approvals,
        FoundationModuleCapability.Notifications,
        FoundationModuleCapability.Settings,
        FoundationModuleCapability.FeatureManagement,
        FoundationModuleCapability.Localization
    ];

    public static FoundationModuleCapability Expand(FoundationModuleCapability declared)
    {
        ValidateKnown(declared);
        var effective = declared;

        bool changed;
        do
        {
            var before = effective;

            if (effective.HasFlag(FoundationModuleCapability.Identity))
                effective |= FoundationModuleCapability.Security;

            if (effective.HasFlag(FoundationModuleCapability.Authorization))
                effective |= FoundationModuleCapability.Identity | FoundationModuleCapability.Security;

            if (effective.HasFlag(FoundationModuleCapability.Workflow))
                effective |= FoundationModuleCapability.Auditing;

            if (effective.HasFlag(FoundationModuleCapability.Approvals))
            {
                effective |=
                    FoundationModuleCapability.Workflow |
                    FoundationModuleCapability.Authorization |
                    FoundationModuleCapability.Auditing;
            }

            if (effective.HasFlag(FoundationModuleCapability.FeatureManagement))
                effective |= FoundationModuleCapability.Settings;

            changed = effective != before;
        }
        while (changed);

        return effective;
    }

    public static IReadOnlyList<FoundationModuleCapability> Enumerate(
        FoundationModuleCapability capabilities)
    {
        ValidateKnown(capabilities);
        return OrderedCapabilities
            .Where(capability => (capabilities & capability) == capability)
            .ToArray();
    }

    public static IReadOnlyList<string> Names(FoundationModuleCapability capabilities) =>
        Enumerate(capabilities).Select(capability => capability.ToString()).ToArray();

    public static void ValidateKnown(FoundationModuleCapability capabilities)
    {
        var known = OrderedCapabilities.Aggregate(
            FoundationModuleCapability.None,
            static (current, capability) => current | capability);
        if ((capabilities & ~known) != FoundationModuleCapability.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capabilities),
                capabilities,
                "Module capabilities contain unknown flag values.");
        }
    }
}

public sealed record FoundationModuleCompositionSnapshot(
    string Name,
    string Route,
    string ApiRoute,
    IReadOnlyList<string> DeclaredCapabilities,
    IReadOnlyList<string> EffectiveCapabilities,
    FoundationApiModuleOptions Api);

public static class FoundationModuleComposition
{
    public static FoundationModuleCompositionSnapshot Describe(IFoundationModuleDefinition module)
    {
        ArgumentNullException.ThrowIfNull(module);
        return new FoundationModuleCompositionSnapshot(
            module.Name,
            module.Route,
            $"/{module.Api.RoutePrefix}/{module.Route}",
            FoundationModuleCapabilityRules.Names(module.DeclaredCapabilities),
            FoundationModuleCapabilityRules.Names(module.Capabilities),
            module.Api);
    }
}
