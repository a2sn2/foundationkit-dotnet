using System.Text.Json;
using FoundationKit.Application.Capabilities;

namespace FoundationKit.Composer;

public enum StudioFieldType
{
    Text = 0,
    Integer = 1,
    Decimal = 2,
    Boolean = 3,
    Date = 4,
    DateTime = 5,
    Guid = 6,
    Reference = 7
}

public sealed record StudioFieldBlueprint(
    string Name,
    StudioFieldType Type = StudioFieldType.Text,
    bool Required = false,
    int MaximumLength = 200,
    bool Indexed = false,
    bool Unique = false,
    bool Filterable = false,
    bool Sortable = false,
    string? ReferenceResource = null);

public sealed record StudioResourceBlueprint(
    string Name,
    string Route,
    IReadOnlyList<StudioFieldBlueprint> Fields,
    bool Auditing = true,
    bool Authorization = true,
    bool Idempotency = true,
    bool Concurrency = true);

public sealed record StudioModuleBlueprint(
    string Name,
    IReadOnlyList<StudioResourceBlueprint> Resources);

public sealed record StudioProjectBlueprint(
    int SchemaVersion,
    string Name,
    string Profile,
    string FoundationMode,
    IReadOnlyList<string> SelectedFeatures,
    IReadOnlyList<StudioModuleBlueprint> Modules,
    IReadOnlyDictionary<string, string>? ProviderChoices = null)
{
    public static StudioProjectBlueprint CreateStarter(string name) => new(
        1,
        name,
        FoundationCapabilityProfiles.Standard,
        "standalone",
        [
            StudioFeatureIds.Security,
            StudioFeatureIds.Identity,
            StudioFeatureIds.Authorization,
            StudioFeatureIds.Auditing,
            StudioFeatureIds.Settings,
            StudioFeatureIds.FeatureManagement,
            StudioFeatureIds.Localization,
            StudioFeatureIds.Caching,
            StudioFeatureIds.Observability,
            StudioFeatureIds.Resilience
        ],
        [
            new StudioModuleBlueprint(
                "Core",
                [
                    new StudioResourceBlueprint(
                        "Record",
                        "records",
                        [
                            new StudioFieldBlueprint("Name", StudioFieldType.Text, Required: true, MaximumLength: 200, Indexed: true, Filterable: true, Sortable: true),
                            new StudioFieldBlueprint("IsActive", StudioFieldType.Boolean, Required: true)
                        ])
                ])
        ]);
}

public static class StudioFeatureIds
{
    public const string Validation = "validation";
    public const string WebApi = "web-api";
    public const string Blazor = "blazor";
    public const string Security = "security";
    public const string Identity = "identity";
    public const string Authorization = "authorization";
    public const string Auditing = "auditing";
    public const string Settings = "settings";
    public const string FeatureManagement = "feature-management";
    public const string Localization = "localization";
    public const string Organization = "organization";
    public const string MultiTenancy = "multi-tenancy";
    public const string Workflow = "workflow";
    public const string Approvals = "approvals";
    public const string Tasks = "tasks";
    public const string Notifications = "notifications";
    public const string Files = "files";
    public const string Documents = "documents";
    public const string Jobs = "jobs";
    public const string BackgroundWorkers = "background-workers";
    public const string Messaging = "messaging";
    public const string DistributedEventBus = "distributed-event-bus";
    public const string Webhooks = "webhooks";
    public const string Realtime = "realtime";
    public const string Caching = "caching";
    public const string Search = "search";
    public const string Reporting = "reporting";
    public const string Money = "money";
    public const string Numbering = "numbering";
    public const string Privacy = "privacy";
    public const string Retention = "retention";
    public const string Ai = "ai";
    public const string Observability = "observability";
    public const string Resilience = "http-resilience";
    public const string BlobStorage = "blob-storage";
    public const string DistributedLocking = "distributed-locking";
}

public enum StudioFeatureReadiness
{
    Generated = 0,
    ProviderReady = 1,
    Reference = 2,
    Planned = 3
}

public sealed record StudioProviderOption(
    string Id,
    string DisplayName,
    string Kind,
    IReadOnlyList<string> NuGetPackages,
    string? Notes = null);

public sealed record StudioFeatureDescriptor(
    string Id,
    string DisplayName,
    string Category,
    string Description,
    StudioFeatureReadiness Readiness,
    string? CapabilityId,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<StudioProviderOption> Providers,
    string DefaultProvider);

public static class ComposerStudioFeatureCatalog
{
    public const string NativeProviderId = "native";
    public const string FoundationProviderId = "foundationkit";
    public const string AbpProviderId = "abp-oss";
    public const string ConsumerProviderId = "consumer";

    private static readonly StudioProviderOption NativeProvider = new(
        NativeProviderId,
        ".NET / ASP.NET Core",
        "native",
        []);

    private static readonly StudioProviderOption FoundationProvider = new(
        FoundationProviderId,
        "FoundationKit",
        "foundationkit",
        []);

    private static readonly StudioProviderOption AbpCoreProvider = new(
        AbpProviderId,
        "ABP OSS 10.6",
        "abp",
        ["Volo.Abp.AspNetCore"],
        "ABP open-source framework provider. No ABP Commercial module is selected by Studio.");

    private static readonly StudioProviderOption ConsumerProvider = new(
        ConsumerProviderId,
        "Consumer implementation",
        "consumer",
        [],
        "The generated project exposes the boundary but the concrete implementation remains product-owned.");

    private static StudioProviderOption CreateAbpProvider(params string[] packages) => new(
        AbpProviderId,
        "ABP OSS 10.6",
        "abp",
        ["Volo.Abp.AspNetCore", .. packages],
        "ABP OSS provider surface. Durable stores, external transports, secrets and production topology remain explicit product/environment choices.");

    private static readonly StudioFeatureDescriptor[] Features =
    [
        F(StudioFeatureIds.Validation, "Validation", "Foundation", "Request and business-rule validation boundaries.", StudioFeatureReadiness.Generated, FoundationCapabilityIds.Validation, [], [FoundationProvider], FoundationProviderId),
        F(StudioFeatureIds.WebApi, "Web API", "Experience", "ASP.NET Core API, Problem Details, OpenAPI and FoundationKit API conventions.", StudioFeatureReadiness.Generated, FoundationCapabilityIds.WebApi, [StudioFeatureIds.Validation], [NativeProvider, FoundationProvider], FoundationProviderId),
        F(StudioFeatureIds.Blazor, "Blazor application", "Experience", "Generated Blazor WebAssembly application using FoundationKit.Blazor and Soft Orbit.", StudioFeatureReadiness.Generated, FoundationCapabilityIds.Blazor, [StudioFeatureIds.WebApi], [NativeProvider, FoundationProvider], FoundationProviderId),
        F(StudioFeatureIds.Security, "Security baseline", "Security", "ASP.NET Core authentication/authorization pipeline, response security and provider seams.", StudioFeatureReadiness.Generated, FoundationCapabilityIds.Security, [StudioFeatureIds.WebApi], [NativeProvider, FoundationProvider, AbpCoreProvider], NativeProviderId),
        F(StudioFeatureIds.Identity, "Identity", "Identity", "Current user and account/authentication provider surface.", StudioFeatureReadiness.ProviderReady, FoundationCapabilityIds.Identity, [StudioFeatureIds.Security], [NativeProvider, FoundationProvider, CreateAbpProvider("Volo.Abp.Security")], AbpProviderId),
        F(StudioFeatureIds.Authorization, "Permissions & Authorization", "Identity", "Role/permission/ownership authorization with optional ABP permission infrastructure.", StudioFeatureReadiness.ProviderReady, FoundationCapabilityIds.Authorization, [StudioFeatureIds.Identity], [NativeProvider, FoundationProvider, CreateAbpProvider("Volo.Abp.Authorization.Abstractions")], AbpProviderId),
        F(StudioFeatureIds.Auditing, "Audit logging", "Governance", "Audit events, context and sink integration.", StudioFeatureReadiness.Generated, FoundationCapabilityIds.Auditing, [], [FoundationProvider, CreateAbpProvider("Volo.Abp.Auditing")], FoundationProviderId),
        F(StudioFeatureIds.Settings, "Settings", "Platform", "Hierarchical application settings with optional ABP setting infrastructure.", StudioFeatureReadiness.ProviderReady, FoundationCapabilityIds.Settings, [], [FoundationProvider, CreateAbpProvider("Volo.Abp.Settings")], AbpProviderId),
        F(StudioFeatureIds.FeatureManagement, "Feature management", "Platform", "Runtime feature checks and feature configuration.", StudioFeatureReadiness.ProviderReady, FoundationCapabilityIds.FeatureManagement, [StudioFeatureIds.Settings], [FoundationProvider, CreateAbpProvider("Volo.Abp.Features")], AbpProviderId),
        F(StudioFeatureIds.Localization, "Localization", "Experience", "Culture, fallback and RTL/LTR foundations.", StudioFeatureReadiness.Reference, FoundationCapabilityIds.Localization, [], [NativeProvider, FoundationProvider, AbpCoreProvider], NativeProviderId),
        F(StudioFeatureIds.Organization, "Organization / Branches", "Business", "Organizations, branches, departments, teams and hierarchy vocabulary.", StudioFeatureReadiness.Planned, FoundationCapabilityIds.Organization, [StudioFeatureIds.Authorization], [FoundationProvider, ConsumerProvider], FoundationProviderId),
        F(StudioFeatureIds.MultiTenancy, "Multi-Tenancy", "Platform", "Tenant context and isolation with optional ABP multi-tenancy infrastructure.", StudioFeatureReadiness.ProviderReady, FoundationCapabilityIds.MultiTenancy, [StudioFeatureIds.Authorization], [CreateAbpProvider("Volo.Abp.MultiTenancy"), ConsumerProvider], AbpProviderId),
        F(StudioFeatureIds.Workflow, "Workflow", "Process", "State/trigger workflow definitions and execution boundaries.", StudioFeatureReadiness.Reference, FoundationCapabilityIds.Workflow, [StudioFeatureIds.Auditing], [FoundationProvider], FoundationProviderId),
        F(StudioFeatureIds.Approvals, "Approvals", "Process", "Maker-checker and approval/rejection processes.", StudioFeatureReadiness.Reference, FoundationCapabilityIds.Approvals, [StudioFeatureIds.Workflow, StudioFeatureIds.Authorization, StudioFeatureIds.Auditing], [FoundationProvider], FoundationProviderId),
        F(StudioFeatureIds.Tasks, "Tasks", "Process", "Assignable work items, priorities and lifecycle.", StudioFeatureReadiness.Planned, FoundationCapabilityIds.Tasks, [StudioFeatureIds.Identity], [FoundationProvider, ConsumerProvider], FoundationProviderId),
        F(StudioFeatureIds.Notifications, "Notifications", "Communication", "Channel-neutral notifications and transport provider seams.", StudioFeatureReadiness.Reference, FoundationCapabilityIds.Notifications, [], [FoundationProvider], FoundationProviderId),
        F(StudioFeatureIds.BlobStorage, "BLOB Storage", "Content", "BLOB abstraction with the concrete store selected by the product/environment.", StudioFeatureReadiness.ProviderReady, null, [], [CreateAbpProvider("Volo.Abp.BlobStoring"), ConsumerProvider], AbpProviderId),
        F(StudioFeatureIds.Files, "Files", "Content", "File metadata/access foundation backed by a selected BLOB provider.", StudioFeatureReadiness.ProviderReady, FoundationCapabilityIds.Files, [StudioFeatureIds.Authorization, StudioFeatureIds.BlobStorage], [FoundationProvider, ConsumerProvider], FoundationProviderId),
        F(StudioFeatureIds.Documents, "Documents", "Content", "Document metadata, classification, versioning and entity linkage.", StudioFeatureReadiness.Planned, FoundationCapabilityIds.Documents, [StudioFeatureIds.Files, StudioFeatureIds.Auditing], [FoundationProvider, ConsumerProvider], FoundationProviderId),
        F(StudioFeatureIds.Jobs, "Background Jobs", "Operations", "Immediate/delayed/scheduled work with ABP default background-job infrastructure available.", StudioFeatureReadiness.ProviderReady, FoundationCapabilityIds.Jobs, [], [NativeProvider, CreateAbpProvider("Volo.Abp.BackgroundJobs")], AbpProviderId),
        F(StudioFeatureIds.BackgroundWorkers, "Background Workers", "Operations", "Long-running and periodic hosted work.", StudioFeatureReadiness.ProviderReady, null, [], [NativeProvider, AbpCoreProvider], NativeProviderId),
        F(StudioFeatureIds.Messaging, "Messaging", "Integration", "Integration events plus outbox/inbox/retry/dead-letter vocabulary.", StudioFeatureReadiness.Planned, FoundationCapabilityIds.Messaging, [], [FoundationProvider, CreateAbpProvider("Volo.Abp.EventBus")], AbpProviderId),
        F(StudioFeatureIds.DistributedEventBus, "Distributed Event Bus", "Integration", "ABP event-bus abstraction with transport selected separately by the product.", StudioFeatureReadiness.ProviderReady, FoundationCapabilityIds.Messaging, [StudioFeatureIds.Messaging], [CreateAbpProvider("Volo.Abp.EventBus")], AbpProviderId),
        F(StudioFeatureIds.Webhooks, "Webhooks", "Integration", "Inbound/outbound webhook signing, replay and delivery history.", StudioFeatureReadiness.Planned, FoundationCapabilityIds.Webhooks, [StudioFeatureIds.Messaging, StudioFeatureIds.Security], [FoundationProvider, ConsumerProvider], FoundationProviderId),
        F(StudioFeatureIds.Realtime, "Realtime", "Communication", "Provider-neutral realtime delivery surface.", StudioFeatureReadiness.Planned, FoundationCapabilityIds.Realtime, [StudioFeatureIds.Authorization], [NativeProvider, ConsumerProvider], NativeProviderId),
        F(StudioFeatureIds.Caching, "Caching", "Data", "Native HybridCache with FoundationKit compatibility boundaries.", StudioFeatureReadiness.Generated, FoundationCapabilityIds.Caching, [], [NativeProvider, FoundationProvider], NativeProviderId),
        F(StudioFeatureIds.Search, "Search", "Data", "Search boundary for relational, full-text or external engines.", StudioFeatureReadiness.Planned, FoundationCapabilityIds.Search, [StudioFeatureIds.Authorization], [ConsumerProvider], ConsumerProviderId),
        F(StudioFeatureIds.Reporting, "Reporting", "Business", "Read-model/report filtering, grouping and export boundary.", StudioFeatureReadiness.Planned, FoundationCapabilityIds.Reporting, [StudioFeatureIds.Authorization], [FoundationProvider, ConsumerProvider], FoundationProviderId),
        F(StudioFeatureIds.Money, "Money", "Finance", "Currency-aware monetary values and explicit conversion boundaries.", StudioFeatureReadiness.Planned, FoundationCapabilityIds.Money, [], [FoundationProvider], FoundationProviderId),
        F(StudioFeatureIds.Numbering, "Business Numbering", "Business", "Human-friendly scoped sequences.", StudioFeatureReadiness.Planned, FoundationCapabilityIds.Numbering, [], [FoundationProvider], FoundationProviderId),
        F(StudioFeatureIds.Privacy, "Privacy", "Governance", "PII classification, masking, redaction, consent and anonymization hooks.", StudioFeatureReadiness.Planned, FoundationCapabilityIds.Privacy, [StudioFeatureIds.Auditing, StudioFeatureIds.Security], [FoundationProvider, ConsumerProvider], FoundationProviderId),
        F(StudioFeatureIds.Retention, "Retention", "Governance", "Retention, archive, deletion and anonymization scheduling.", StudioFeatureReadiness.Planned, FoundationCapabilityIds.Retention, [StudioFeatureIds.Jobs, StudioFeatureIds.Auditing], [FoundationProvider, ConsumerProvider], FoundationProviderId),
        F(StudioFeatureIds.Ai, "AI", "Intelligence", "Provider-neutral chat, embeddings, retrieval, tool and agent boundaries.", StudioFeatureReadiness.Planned, FoundationCapabilityIds.ArtificialIntelligence, [StudioFeatureIds.Observability], [ConsumerProvider], ConsumerProviderId),
        F(StudioFeatureIds.Observability, "Observability", "Operations", "Health, log, trace and metric conventions.", StudioFeatureReadiness.Reference, FoundationCapabilityIds.Observability, [], [NativeProvider, FoundationProvider], NativeProviderId),
        F(StudioFeatureIds.Resilience, "HTTP Resilience", "Operations", "Standard Microsoft.Extensions.Http.Resilience pipeline.", StudioFeatureReadiness.Generated, null, [], [NativeProvider], NativeProviderId),
        F(StudioFeatureIds.DistributedLocking, "Distributed Locking", "Operations", "Distributed-lock abstraction/provider surface for clustered workloads.", StudioFeatureReadiness.ProviderReady, null, [], [CreateAbpProvider("Volo.Abp.DistributedLocking"), ConsumerProvider], AbpProviderId)
    ];

    public static IReadOnlyList<StudioFeatureDescriptor> All => Features;

    public static StudioFeatureDescriptor Get(string id) =>
        Features.FirstOrDefault(feature => string.Equals(feature.Id, id, StringComparison.OrdinalIgnoreCase))
        ?? throw new ComposerGenerationException($"Unknown Studio feature '{id}'.");

    public static IReadOnlyList<StudioFeatureDescriptor> Resolve(
        IEnumerable<string> selected,
        IReadOnlyDictionary<string, string>? providerChoices = null)
    {
        ArgumentNullException.ThrowIfNull(selected);
        var resolved = new Dictionary<string, StudioFeatureDescriptor>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Visit(string id)
        {
            if (resolved.ContainsKey(id))
                return;
            if (!visiting.Add(id))
                throw new ComposerGenerationException($"Studio feature dependency cycle detected at '{id}'.");
            var feature = Get(id);
            foreach (var dependency in feature.Dependencies)
                Visit(dependency);
            visiting.Remove(id);
            resolved[id] = feature;
        }

        foreach (var id in selected.Where(id => !string.IsNullOrWhiteSpace(id)))
            Visit(id.Trim());

        ValidateProviderChoices(resolved.Values, providerChoices);
        return resolved.Values
            .OrderBy(feature => feature.Category, StringComparer.Ordinal)
            .ThenBy(feature => feature.DisplayName, StringComparer.Ordinal)
            .ToArray();
    }

    public static string ResolveProvider(
        StudioFeatureDescriptor feature,
        IReadOnlyDictionary<string, string>? providerChoices)
    {
        if (providerChoices is not null && providerChoices.TryGetValue(feature.Id, out var selected) && !string.IsNullOrWhiteSpace(selected))
            return selected.Trim();
        return feature.DefaultProvider;
    }

    public static IReadOnlyList<string> ResolveAbpPackages(
        IEnumerable<StudioFeatureDescriptor> features,
        IReadOnlyDictionary<string, string>? providerChoices)
    {
        var packages = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var feature in features)
        {
            var providerId = ResolveProvider(feature, providerChoices);
            var provider = feature.Providers.First(option => string.Equals(option.Id, providerId, StringComparison.OrdinalIgnoreCase));
            if (!string.Equals(provider.Kind, "abp", StringComparison.OrdinalIgnoreCase))
                continue;
            foreach (var package in provider.NuGetPackages)
                packages.Add(package);
        }
        return packages.ToArray();
    }

    private static void ValidateProviderChoices(
        IEnumerable<StudioFeatureDescriptor> features,
        IReadOnlyDictionary<string, string>? providerChoices)
    {
        if (providerChoices is null)
            return;
        var featureMap = features.ToDictionary(feature => feature.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var choice in providerChoices)
        {
            if (!featureMap.TryGetValue(choice.Key, out var feature))
                throw new ComposerGenerationException($"Provider choice targets unresolved Studio feature '{choice.Key}'.");
            if (!feature.Providers.Any(provider => string.Equals(provider.Id, choice.Value, StringComparison.OrdinalIgnoreCase)))
                throw new ComposerGenerationException($"Provider '{choice.Value}' is not supported by Studio feature '{choice.Key}'.");
        }
    }

    private static StudioFeatureDescriptor F(
        string id,
        string displayName,
        string category,
        string description,
        StudioFeatureReadiness readiness,
        string? capabilityId,
        IReadOnlyList<string> dependencies,
        IReadOnlyList<StudioProviderOption> providers,
        string defaultProvider) =>
        new(id, displayName, category, description, readiness, capabilityId, dependencies, providers, defaultProvider);
}

public static class ComposerStudioBlueprintCompiler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string Serialize(StudioProjectBlueprint blueprint)
    {
        ValidateBlueprint(blueprint);
        return JsonSerializer.Serialize(blueprint, JsonOptions);
    }

    public static StudioProjectBlueprint Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        try
        {
            var blueprint = JsonSerializer.Deserialize<StudioProjectBlueprint>(json, new JsonSerializerOptions(JsonOptions)
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new ComposerGenerationException("Studio blueprint is empty.");
            ValidateBlueprint(blueprint);
            return blueprint;
        }
        catch (JsonException exception)
        {
            throw new ComposerGenerationException($"Studio blueprint JSON is invalid: {exception.Message}", exception);
        }
    }

    public static StudioBlueprintCompilation Compile(StudioProjectBlueprint blueprint)
    {
        ValidateBlueprint(blueprint);
        var features = ComposerStudioFeatureCatalog.Resolve(blueprint.SelectedFeatures, blueprint.ProviderChoices);
        var capabilities = features
            .Select(feature => feature.CapabilityId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Concat([FoundationCapabilityIds.Kernel, FoundationCapabilityIds.Validation, FoundationCapabilityIds.WebApi, FoundationCapabilityIds.Blazor])
            .Concat(blueprint.Modules.SelectMany(module => module.Resources).Any(resource => resource.Idempotency)
                ? [FoundationCapabilityIds.Idempotency]
                : Array.Empty<string>())
            .Concat(blueprint.Modules.SelectMany(module => module.Resources).Any(resource => resource.Concurrency)
                ? [FoundationCapabilityIds.Concurrency]
                : Array.Empty<string>())
            .Distinct(StringComparer.OrdinalIgnoreCase)
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
                behaviors = BuildBehaviors(resource, features),
                fields = resource.Fields.Select(field => new
                {
                    name = field.Name,
                    type = "text",
                    required = field.Required,
                    maximumLength = field.Type == StudioFieldType.Text ? Math.Clamp(field.MaximumLength, 1, 4000) : 128,
                    indexed = field.Indexed,
                    unique = field.Unique,
                    filter = field.Type == StudioFieldType.Text && field.Filterable ? "exact" : "none",
                    sortable = field.Type == StudioFieldType.Text && field.Sortable
                }).ToArray(),
                api = new
                {
                    routePrefix = "api",
                    idempotency = resource.Idempotency ? "required" : "disabled",
                    concurrency = resource.Concurrency ? "require-if-match" : "application-policy",
                    maximumFilters = Math.Max(1, resource.Fields.Count(field => field.Type == StudioFieldType.Text && field.Filterable)),
                    maximumSorts = Math.Max(1, resource.Fields.Count(field => field.Type == StudioFieldType.Text && field.Sortable))
                }
            }).ToArray()
        }).ToArray();

        var manifestJson = JsonSerializer.Serialize(new
        {
            schemaVersion = 2,
            name = blueprint.Name.Trim(),
            profile = NormalizeProfile(blueprint.Profile),
            includeCapabilities = capabilities,
            excludeCapabilities = Array.Empty<string>(),
            providers = new[] { FoundationCapabilityIds.SqlServerProvider },
            modules
        }, JsonOptions);

        var manifest = ComposerManifestParser.Parse(manifestJson);
        var analysis = CompositionAnalyzer.Analyze(manifest);
        var abpPackages = ComposerStudioFeatureCatalog.ResolveAbpPackages(features, blueprint.ProviderChoices);
        return new StudioBlueprintCompilation(blueprint, features, manifestJson, analysis, abpPackages);
    }

    private static string[] BuildBehaviors(
        StudioResourceBlueprint resource,
        IReadOnlyList<StudioFeatureDescriptor> features)
    {
        var selected = features.Select(feature => feature.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var behaviors = new List<string> { "crud" };
        if (resource.Auditing && selected.Contains(StudioFeatureIds.Auditing)) behaviors.Add("auditing");
        if (resource.Authorization && selected.Contains(StudioFeatureIds.Authorization)) behaviors.Add("authorization");
        if (resource.Concurrency) behaviors.Add("concurrency");
        if (selected.Contains(StudioFeatureIds.Caching)) behaviors.Add("caching");
        if (selected.Contains(StudioFeatureIds.Security)) behaviors.Add("security");
        if (selected.Contains(StudioFeatureIds.Identity)) behaviors.Add("identity");
        if (selected.Contains(StudioFeatureIds.Workflow)) behaviors.Add("workflow");
        if (selected.Contains(StudioFeatureIds.Approvals)) behaviors.Add("approvals");
        if (selected.Contains(StudioFeatureIds.Notifications)) behaviors.Add("notifications");
        if (selected.Contains(StudioFeatureIds.Settings)) behaviors.Add("settings");
        if (selected.Contains(StudioFeatureIds.FeatureManagement)) behaviors.Add("feature-management");
        if (selected.Contains(StudioFeatureIds.Localization)) behaviors.Add("localization");
        return behaviors.ToArray();
    }

    private static string NormalizeProfile(string profile)
    {
        if (string.IsNullOrWhiteSpace(profile))
            return FoundationCapabilityProfiles.Standard;
        _ = FoundationCapabilityProfiles.Get(profile.Trim());
        return profile.Trim();
    }

    private static void ValidateBlueprint(StudioProjectBlueprint blueprint)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        if (blueprint.SchemaVersion != 1)
            throw new ComposerGenerationException($"Unsupported Studio blueprint schemaVersion '{blueprint.SchemaVersion}'. Expected 1.");
        if (string.IsNullOrWhiteSpace(blueprint.Name))
            throw new ComposerGenerationException("Studio project name is required.");
        if (blueprint.SelectedFeatures is null)
            throw new ComposerGenerationException("Studio selectedFeatures is required.");
        if (blueprint.Modules is null || blueprint.Modules.Count == 0)
            throw new ComposerGenerationException("Studio requires at least one module.");
        if (!string.Equals(blueprint.FoundationMode, "linked", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(blueprint.FoundationMode, "standalone", StringComparison.OrdinalIgnoreCase))
            throw new ComposerGenerationException("Studio foundationMode must be 'linked' or 'standalone'.");

        var resources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var module in blueprint.Modules)
        {
            if (string.IsNullOrWhiteSpace(module.Name) || module.Resources is null || module.Resources.Count == 0)
                throw new ComposerGenerationException("Every Studio module requires a name and at least one resource.");
            foreach (var resource in module.Resources)
            {
                if (string.IsNullOrWhiteSpace(resource.Name) || string.IsNullOrWhiteSpace(resource.Route) || resource.Fields is null || resource.Fields.Count == 0)
                    throw new ComposerGenerationException($"Studio module '{module.Name}' contains an incomplete resource.");
                if (!resources.Add(resource.Name))
                    throw new ComposerGenerationException($"Studio resource names must be globally unique for safe UI/reference generation. Duplicate '{resource.Name}'.");
                var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var field in resource.Fields)
                {
                    if (string.IsNullOrWhiteSpace(field.Name) || !fieldNames.Add(field.Name))
                        throw new ComposerGenerationException($"Studio resource '{resource.Name}' contains an invalid or duplicate field name.");
                    if (field.Type == StudioFieldType.Text && (field.MaximumLength < 1 || field.MaximumLength > 4000))
                        throw new ComposerGenerationException($"Studio text field '{resource.Name}.{field.Name}' maximumLength must be between 1 and 4000.");
                    if (field.Type == StudioFieldType.Reference && string.IsNullOrWhiteSpace(field.ReferenceResource))
                        throw new ComposerGenerationException($"Studio reference field '{resource.Name}.{field.Name}' requires referenceResource.");
                }
            }
        }

        foreach (var field in blueprint.Modules.SelectMany(module => module.Resources).SelectMany(resource => resource.Fields))
        {
            if (field.Type == StudioFieldType.Reference && !resources.Contains(field.ReferenceResource!))
                throw new ComposerGenerationException($"Studio reference field '{field.Name}' targets unknown resource '{field.ReferenceResource}'.");
        }

        _ = ComposerStudioFeatureCatalog.Resolve(blueprint.SelectedFeatures, blueprint.ProviderChoices);
    }
}

public sealed record StudioBlueprintCompilation(
    StudioProjectBlueprint Blueprint,
    IReadOnlyList<StudioFeatureDescriptor> Features,
    string ManifestJson,
    CompositionAnalysis Analysis,
    IReadOnlyList<string> AbpPackages);
