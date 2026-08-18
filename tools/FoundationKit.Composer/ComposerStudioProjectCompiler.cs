using System.Text.Json;
using FoundationKit.Application.Capabilities;

namespace FoundationKit.Composer;

public static class ComposerStudioProjectCompiler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static StudioBlueprintCompilation Compile(StudioProjectBlueprint blueprint)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        // Reuse the strict Studio blueprint validator without using its legacy manifest projection.
        _ = ComposerStudioBlueprintCompiler.Serialize(blueprint);

        var features = ComposerStudioFeatureCatalog.Resolve(blueprint.SelectedFeatures, blueprint.ProviderChoices);
        var selectedCapabilities = features
            .Select(feature => feature.CapabilityId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Concat([
                FoundationCapabilityIds.Kernel,
                FoundationCapabilityIds.Validation,
                FoundationCapabilityIds.WebApi,
                FoundationCapabilityIds.Blazor
            ])
            .Concat(blueprint.Modules.SelectMany(module => module.Resources).Any(resource => resource.Idempotency)
                ? [FoundationCapabilityIds.Idempotency]
                : Array.Empty<string>())
            .Concat(blueprint.Modules.SelectMany(module => module.Resources).Any(resource => resource.Concurrency)
                ? [FoundationCapabilityIds.Concurrency]
                : Array.Empty<string>())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var capabilityClosure = ResolveCapabilityClosure(selectedCapabilities);
        var profile = FoundationCapabilityProfiles.Get(blueprint.Profile);
        var excludes = profile.CapabilityIds
            .Where(id => !capabilityClosure.Contains(id))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var modules = blueprint.Modules.Select(module => new
        {
            name = module.Name,
            resources = module.Resources.Select(resource => new
            {
                name = resource.Name,
                route = resource.Route,
                idType = "guid",
                behaviors = BuildExecutableBehaviors(resource, features),
                fields = resource.Fields.Select(field => new
                {
                    // Composer's proven executable schema remains text-based. Studio emits a valid
                    // canonical placeholder contract first, then specializes CLR/SQL/UI types after generation.
                    name = field.Name,
                    type = "text",
                    required = field.Required,
                    maximumLength = field.Type == StudioFieldType.Text ? Math.Clamp(field.MaximumLength, 1, 4000) : 128,
                    index = new
                    {
                        enabled = field.Indexed || field.Unique || field.Filterable || field.Sortable || field.Type == StudioFieldType.Reference,
                        unique = field.Unique
                    },
                    query = new
                    {
                        filter = field.Type == StudioFieldType.Text && field.Filterable ? "exact" : "none",
                        sortable = field.Type == StudioFieldType.Text && field.Sortable
                    }
                }).ToArray(),
                api = new
                {
                    routePrefix = "api",
                    idempotency = resource.Idempotency ? "required" : "disabled",
                    concurrency = resource.Concurrency ? "require-if-match" : "application-policy",
                    maximumFilters = resource.Fields.Any(field => field.Type == StudioFieldType.Text && field.Filterable)
                        ? Math.Min(25, resource.Fields.Count(field => field.Type == StudioFieldType.Text && field.Filterable))
                        : 0,
                    // The current CrudQueryPlan accepts one order selector per request even when multiple
                    // fields are declared sortable. Keep the API request bound at one sort expression.
                    maximumSorts = resource.Fields.Any(field => field.Type == StudioFieldType.Text && field.Sortable) ? 1 : 0
                }
            }).ToArray()
        }).ToArray();

        var manifestJson = JsonSerializer.Serialize(new
        {
            schemaVersion = 2,
            name = blueprint.Name.Trim(),
            profile = blueprint.Profile.Trim(),
            includeCapabilities = selectedCapabilities.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            excludeCapabilities = excludes,
            providers = new[] { FoundationCapabilityIds.SqlServerProvider },
            modules
        }, JsonOptions);

        var manifest = ComposerManifestParser.Parse(manifestJson);
        var analysis = CompositionAnalyzer.Analyze(manifest);
        var abpPackages = ComposerStudioFeatureCatalog.ResolveAbpPackages(features, blueprint.ProviderChoices);
        return new StudioBlueprintCompilation(blueprint, features, manifestJson, analysis, abpPackages);
    }

    private static HashSet<string> ResolveCapabilityClosure(IEnumerable<string> roots)
    {
        var catalog = FoundationCapabilityCatalog.All.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
        var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Visit(string id)
        {
            if (!resolved.Add(id))
                return;
            if (!catalog.TryGetValue(id, out var capability))
                throw new ComposerGenerationException($"Studio maps to unknown FoundationKit capability '{id}'.");
            foreach (var dependency in capability.Dependencies)
                Visit(dependency);
        }

        foreach (var root in roots)
            Visit(root);
        return resolved;
    }

    private static string[] BuildExecutableBehaviors(
        StudioResourceBlueprint resource,
        IReadOnlyList<StudioFeatureDescriptor> features)
    {
        var selected = features.Select(feature => feature.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var behaviors = new List<string> { "crud" };
        if (resource.Auditing && selected.Contains(StudioFeatureIds.Auditing))
            behaviors.Add("auditing");
        if (resource.Authorization && selected.Contains(StudioFeatureIds.Authorization))
            behaviors.Add("authorization");
        if (resource.Concurrency)
            behaviors.Add("concurrency");
        return behaviors.ToArray();
    }
}