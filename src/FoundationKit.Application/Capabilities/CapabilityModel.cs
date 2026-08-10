namespace FoundationKit.Application.Capabilities;

public enum CapabilityKind
{
    Kernel,
    Optional,
    Provider,
    Tooling
}

public enum CapabilityMaturity
{
    Stable,
    Preview,
    ReferenceOnly,
    Planned
}

public sealed record CapabilityDescriptor(
    string Id,
    string DisplayName,
    CapabilityKind Kind,
    CapabilityMaturity Maturity,
    string Category,
    string Description,
    IReadOnlyList<string> Dependencies);

public sealed record CapabilityProfile(
    string Id,
    string DisplayName,
    string Description,
    IReadOnlyList<string> CapabilityIds);

public static class FoundationCapabilityIds
{
    public const string Kernel = "kernel";
    public const string Validation = "validation";
    public const string WebApi = "web-api";
    public const string Blazor = "blazor";
    public const string Observability = "observability";
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
    public const string Messaging = "messaging";
    public const string Webhooks = "webhooks";
    public const string Realtime = "realtime";
    public const string Caching = "caching";
    public const string Search = "search";
    public const string Reporting = "reporting";
    public const string Idempotency = "idempotency";
    public const string Concurrency = "concurrency";
    public const string Money = "money";
    public const string Numbering = "numbering";
    public const string Privacy = "privacy";
    public const string Retention = "retention";
    public const string ArtificialIntelligence = "ai";
    public const string SqlServerProvider = "provider-sqlserver";
    public const string RedisProvider = "provider-redis";
    public const string SmtpProvider = "provider-smtp";
    public const string CliTooling = "tooling-cli";
    public const string WorkbenchTooling = "tooling-workbench";
}

public static class FoundationCapabilityCatalog
{
    private static readonly IReadOnlyList<string> None = Array.Empty<string>();

    private static readonly CapabilityDescriptor[] Descriptors =
    [
        D(FoundationCapabilityIds.Kernel, "Kernel", CapabilityKind.Kernel, CapabilityMaturity.Stable, "Foundation", "Domain and application primitives used by every FoundationKit composition.", None),
        D(FoundationCapabilityIds.Validation, "Validation", CapabilityKind.Optional, CapabilityMaturity.Stable, "Foundation", "Reusable validation and business-rule boundaries.", [FoundationCapabilityIds.Kernel]),
        D(FoundationCapabilityIds.WebApi, "Web API", CapabilityKind.Optional, CapabilityMaturity.Stable, "Experience", "HTTP result mapping, Problem Details, correlation, request-pipeline helpers, and reusable endpoint conventions.", [FoundationCapabilityIds.Kernel]),
        D(FoundationCapabilityIds.Blazor, "Blazor", CapabilityKind.Optional, CapabilityMaturity.Stable, "Experience", "Reusable typed API, error, async-state, and ViewModel primitives for Blazor consumers.", [FoundationCapabilityIds.Kernel]),
        D(FoundationCapabilityIds.Observability, "Observability", CapabilityKind.Optional, CapabilityMaturity.Preview, "Operations", "Logging, correlation, health, trace, and metric conventions with provider wiring kept explicit.", [FoundationCapabilityIds.Kernel]),
        D(FoundationCapabilityIds.Security, "Security", CapabilityKind.Optional, CapabilityMaturity.Preview, "Security", "Reusable reverse-proxy, rate-partition, MFA-assurance, and security conventions.", [FoundationCapabilityIds.WebApi]),
        D(FoundationCapabilityIds.Identity, "Identity", CapabilityKind.Optional, CapabilityMaturity.ReferenceOnly, "Identity", "Provider-neutral account policy, notification, security-event, and step-up requirement contracts.", [FoundationCapabilityIds.Security]),
        D(FoundationCapabilityIds.Authorization, "Authorization", CapabilityKind.Optional, CapabilityMaturity.ReferenceOnly, "Identity", "Role, permission, policy, ownership, and scoped authorization primitives.", [FoundationCapabilityIds.Identity]),
        D(FoundationCapabilityIds.Auditing, "Auditing", CapabilityKind.Optional, CapabilityMaturity.ReferenceOnly, "Governance", "Bounded provider-neutral audit events, context, recording, and sink contracts.", [FoundationCapabilityIds.Kernel]),
        D(FoundationCapabilityIds.Settings, "Settings", CapabilityKind.Optional, CapabilityMaturity.ReferenceOnly, "Platform", "Provider-neutral hierarchical setting resolution with deterministic scope/source precedence.", [FoundationCapabilityIds.Kernel]),
        D(FoundationCapabilityIds.FeatureManagement, "Feature Management", CapabilityKind.Optional, CapabilityMaturity.ReferenceOnly, "Platform", "Deterministic settings-backed Boolean feature decisions with explicit defaults and fail-closed invalid configuration.", [FoundationCapabilityIds.Settings]),
        D(FoundationCapabilityIds.Localization, "Localization", CapabilityKind.Optional, CapabilityMaturity.ReferenceOnly, "Experience", "Bounded culture metadata, RTL/LTR directionality, deterministic fallback, and opaque time-zone identity.", [FoundationCapabilityIds.Kernel]),
        D(FoundationCapabilityIds.Organization, "Organization", CapabilityKind.Optional, CapabilityMaturity.Planned, "Business", "Organizations, branches, departments, teams, positions, and reporting hierarchy.", [FoundationCapabilityIds.Kernel]),
        D(FoundationCapabilityIds.MultiTenancy, "Multi-Tenancy", CapabilityKind.Optional, CapabilityMaturity.Planned, "Platform", "Tenant context and isolation contracts without forcing a storage topology.", [FoundationCapabilityIds.Authorization]),
        D(FoundationCapabilityIds.Workflow, "Workflow", CapabilityKind.Optional, CapabilityMaturity.ReferenceOnly, "Process", "Deterministic state/trigger transition definitions and resolution with bounded audit intent.", [FoundationCapabilityIds.Auditing]),
        D(FoundationCapabilityIds.Approvals, "Approvals", CapabilityKind.Optional, CapabilityMaturity.ReferenceOnly, "Process", "Approve/reject decisions, permission gate, maker-checker policy, workflow resolution, and audit intent.", [FoundationCapabilityIds.Workflow, FoundationCapabilityIds.Authorization, FoundationCapabilityIds.Auditing]),
        D(FoundationCapabilityIds.Tasks, "Tasks", CapabilityKind.Optional, CapabilityMaturity.Planned, "Process", "Assignable work items, priorities, due dates, and lifecycle tracking.", [FoundationCapabilityIds.Kernel]),
        D(FoundationCapabilityIds.Notifications, "Notifications", CapabilityKind.Optional, CapabilityMaturity.ReferenceOnly, "Communication", "Bounded channel-neutral notification message, sender, and delivery-result contracts.", [FoundationCapabilityIds.Kernel]),
        D(FoundationCapabilityIds.Files, "Files", CapabilityKind.Optional, CapabilityMaturity.Planned, "Content", "Provider-neutral file storage, metadata, integrity, and access contracts.", [FoundationCapabilityIds.Authorization]),
        D(FoundationCapabilityIds.Documents, "Documents", CapabilityKind.Optional, CapabilityMaturity.Planned, "Content", "Document metadata, classification, versioning, and entity linkage.", [FoundationCapabilityIds.Files, FoundationCapabilityIds.Auditing]),
        D(FoundationCapabilityIds.Jobs, "Background Jobs", CapabilityKind.Optional, CapabilityMaturity.Planned, "Operations", "Immediate, delayed, scheduled, and recurring work contracts.", [FoundationCapabilityIds.Kernel]),
        D(FoundationCapabilityIds.Messaging, "Messaging", CapabilityKind.Optional, CapabilityMaturity.Planned, "Integration", "Integration events, outbox/inbox boundaries, retry, and dead-letter concepts.", [FoundationCapabilityIds.Kernel]),
        D(FoundationCapabilityIds.Webhooks, "Webhooks", CapabilityKind.Optional, CapabilityMaturity.Planned, "Integration", "Inbound/outbound webhook signing, replay, retry, and delivery-history contracts.", [FoundationCapabilityIds.Messaging, FoundationCapabilityIds.Security]),
        D(FoundationCapabilityIds.Realtime, "Realtime", CapabilityKind.Optional, CapabilityMaturity.Planned, "Communication", "Provider-neutral realtime event delivery contracts.", [FoundationCapabilityIds.Authorization]),
        D(FoundationCapabilityIds.Caching, "Caching", CapabilityKind.Optional, CapabilityMaturity.ReferenceOnly, "Data", "Bounded byte-cache contracts with explicit TTL, hit/miss/remove semantics and an in-memory reference provider.", [FoundationCapabilityIds.Kernel]),
        D(FoundationCapabilityIds.Search, "Search", CapabilityKind.Optional, CapabilityMaturity.Planned, "Data", "Provider-neutral search contracts for relational, full-text, and external engines.", [FoundationCapabilityIds.Authorization]),
        D(FoundationCapabilityIds.Reporting, "Reporting", CapabilityKind.Optional, CapabilityMaturity.Planned, "Business", "Report definitions, filtering, grouping, and export boundaries.", [FoundationCapabilityIds.Authorization]),
        D(FoundationCapabilityIds.Idempotency, "Idempotency", CapabilityKind.Optional, CapabilityMaturity.Planned, "Reliability", "Reusable duplicate-write reservation, completion, and replay contracts.", [FoundationCapabilityIds.WebApi]),
        D(FoundationCapabilityIds.Concurrency, "Concurrency", CapabilityKind.Optional, CapabilityMaturity.ReferenceOnly, "Reliability", "Provider-neutral optimistic-concurrency policy and conflict conventions with EF translation proof.", [FoundationCapabilityIds.Kernel]),
        D(FoundationCapabilityIds.Money, "Money", CapabilityKind.Optional, CapabilityMaturity.Planned, "Finance", "Currency-aware money values and explicit conversion boundaries.", [FoundationCapabilityIds.Kernel]),
        D(FoundationCapabilityIds.Numbering, "Numbering", CapabilityKind.Optional, CapabilityMaturity.Planned, "Business", "Business-friendly sequences with prefixes, periods, and explicit scope.", [FoundationCapabilityIds.Kernel]),
        D(FoundationCapabilityIds.Privacy, "Privacy", CapabilityKind.Optional, CapabilityMaturity.Planned, "Governance", "PII classification, masking, redaction, consent, and anonymization hooks.", [FoundationCapabilityIds.Auditing, FoundationCapabilityIds.Security]),
        D(FoundationCapabilityIds.Retention, "Retention", CapabilityKind.Optional, CapabilityMaturity.Planned, "Governance", "Retention, archive, deletion, and anonymization scheduling contracts.", [FoundationCapabilityIds.Jobs, FoundationCapabilityIds.Auditing]),
        D(FoundationCapabilityIds.ArtificialIntelligence, "AI", CapabilityKind.Optional, CapabilityMaturity.Planned, "Intelligence", "Provider-neutral chat, embeddings, retrieval, tool/agent, and AI-control boundaries.", [FoundationCapabilityIds.Observability]),
        D(FoundationCapabilityIds.SqlServerProvider, "SQL Server Provider", CapabilityKind.Provider, CapabilityMaturity.ReferenceOnly, "Provider", "SQL Server reference integration owned outside the provider-neutral kernel.", [FoundationCapabilityIds.Kernel]),
        D(FoundationCapabilityIds.RedisProvider, "Redis Provider", CapabilityKind.Provider, CapabilityMaturity.Planned, "Provider", "Redis adapter for caching and related distributed primitives.", [FoundationCapabilityIds.Caching]),
        D(FoundationCapabilityIds.SmtpProvider, "SMTP Provider", CapabilityKind.Provider, CapabilityMaturity.ReferenceOnly, "Provider", "Narrow SMTP transport adapter over the Notifications contract.", [FoundationCapabilityIds.Notifications]),
        D(FoundationCapabilityIds.CliTooling, "FoundationKit CLI", CapabilityKind.Tooling, CapabilityMaturity.ReferenceOnly, "Tooling", "Composer tooling for strict manifest validation, capability/profile discovery, dependency explanation, contract compatibility, deterministic project generation, and the interactive questionnaire; the visual Workbench composer remains future work.", [FoundationCapabilityIds.Kernel]),
        D(FoundationCapabilityIds.WorkbenchTooling, "FoundationKit Workbench", CapabilityKind.Tooling, CapabilityMaturity.ReferenceOnly, "Tooling", "Executable Core architecture and SQL reference consumer; a visual project composer remains a future UX layer.", [FoundationCapabilityIds.Kernel])
    ];

    public static IReadOnlyList<CapabilityDescriptor> All => Descriptors;

    private static CapabilityDescriptor D(
        string id,
        string displayName,
        CapabilityKind kind,
        CapabilityMaturity maturity,
        string category,
        string description,
        IReadOnlyList<string> dependencies) =>
        new(id, displayName, kind, maturity, category, description, dependencies);
}

public static class FoundationCapabilityProfiles
{
    public const string Minimal = "minimal";
    public const string Standard = "standard";
    public const string Enterprise = "enterprise";
    public const string Fintech = "fintech";
    public const string SaaS = "saas";
    public const string InternalBusiness = "internal-business";
    public const string PublicPortal = "public-portal";

    private static readonly CapabilityProfile[] Profiles =
    [
        new(Minimal, "Minimal", "Small API/service foundation with validation and operational visibility.",
            [FoundationCapabilityIds.Kernel, FoundationCapabilityIds.Validation, FoundationCapabilityIds.WebApi, FoundationCapabilityIds.Observability]),
        new(Standard, "Standard", "General business-system baseline with identity, security, audit, settings, notifications, files vocabulary, and localization.",
            [FoundationCapabilityIds.Kernel, FoundationCapabilityIds.Validation, FoundationCapabilityIds.WebApi, FoundationCapabilityIds.Observability, FoundationCapabilityIds.Security, FoundationCapabilityIds.Identity, FoundationCapabilityIds.Authorization, FoundationCapabilityIds.Auditing, FoundationCapabilityIds.Settings, FoundationCapabilityIds.Notifications, FoundationCapabilityIds.Files, FoundationCapabilityIds.Localization]),
        new(Enterprise, "Enterprise", "Standard baseline plus organization/process/automation/messaging/reporting vocabulary.",
            [FoundationCapabilityIds.Kernel, FoundationCapabilityIds.Validation, FoundationCapabilityIds.WebApi, FoundationCapabilityIds.Observability, FoundationCapabilityIds.Security, FoundationCapabilityIds.Identity, FoundationCapabilityIds.Authorization, FoundationCapabilityIds.Auditing, FoundationCapabilityIds.Settings, FoundationCapabilityIds.Notifications, FoundationCapabilityIds.Files, FoundationCapabilityIds.Localization, FoundationCapabilityIds.Organization, FoundationCapabilityIds.Workflow, FoundationCapabilityIds.Approvals, FoundationCapabilityIds.Tasks, FoundationCapabilityIds.Jobs, FoundationCapabilityIds.Messaging, FoundationCapabilityIds.FeatureManagement, FoundationCapabilityIds.Reporting, FoundationCapabilityIds.Idempotency, FoundationCapabilityIds.Concurrency]),
        new(Fintech, "Fintech", "Enterprise baseline plus finance/privacy/numbering/retention vocabulary.",
            [FoundationCapabilityIds.Kernel, FoundationCapabilityIds.Validation, FoundationCapabilityIds.WebApi, FoundationCapabilityIds.Observability, FoundationCapabilityIds.Security, FoundationCapabilityIds.Identity, FoundationCapabilityIds.Authorization, FoundationCapabilityIds.Auditing, FoundationCapabilityIds.Settings, FoundationCapabilityIds.Notifications, FoundationCapabilityIds.Files, FoundationCapabilityIds.Localization, FoundationCapabilityIds.Organization, FoundationCapabilityIds.Workflow, FoundationCapabilityIds.Approvals, FoundationCapabilityIds.Tasks, FoundationCapabilityIds.Jobs, FoundationCapabilityIds.Messaging, FoundationCapabilityIds.FeatureManagement, FoundationCapabilityIds.Reporting, FoundationCapabilityIds.Idempotency, FoundationCapabilityIds.Concurrency, FoundationCapabilityIds.Money, FoundationCapabilityIds.Privacy, FoundationCapabilityIds.Numbering, FoundationCapabilityIds.Retention]),
        new(SaaS, "SaaS", "Standard baseline plus tenancy, feature management, async integration, caching, and search vocabulary.",
            [FoundationCapabilityIds.Kernel, FoundationCapabilityIds.Validation, FoundationCapabilityIds.WebApi, FoundationCapabilityIds.Observability, FoundationCapabilityIds.Security, FoundationCapabilityIds.Identity, FoundationCapabilityIds.Authorization, FoundationCapabilityIds.Auditing, FoundationCapabilityIds.Settings, FoundationCapabilityIds.Notifications, FoundationCapabilityIds.Files, FoundationCapabilityIds.Localization, FoundationCapabilityIds.MultiTenancy, FoundationCapabilityIds.FeatureManagement, FoundationCapabilityIds.Jobs, FoundationCapabilityIds.Webhooks, FoundationCapabilityIds.Caching, FoundationCapabilityIds.Search]),
        new(InternalBusiness, "Internal Business", "Internal systems with organization, workflow, approvals, tasks, reporting, and numbering vocabulary.",
            [FoundationCapabilityIds.Kernel, FoundationCapabilityIds.Validation, FoundationCapabilityIds.WebApi, FoundationCapabilityIds.Observability, FoundationCapabilityIds.Security, FoundationCapabilityIds.Identity, FoundationCapabilityIds.Authorization, FoundationCapabilityIds.Auditing, FoundationCapabilityIds.Settings, FoundationCapabilityIds.Notifications, FoundationCapabilityIds.Files, FoundationCapabilityIds.Localization, FoundationCapabilityIds.Organization, FoundationCapabilityIds.Workflow, FoundationCapabilityIds.Approvals, FoundationCapabilityIds.Tasks, FoundationCapabilityIds.Reporting, FoundationCapabilityIds.Numbering]),
        new(PublicPortal, "Public Portal", "External portal baseline with identity, files, notifications, search, and localization vocabulary.",
            [FoundationCapabilityIds.Kernel, FoundationCapabilityIds.Validation, FoundationCapabilityIds.WebApi, FoundationCapabilityIds.Observability, FoundationCapabilityIds.Security, FoundationCapabilityIds.Identity, FoundationCapabilityIds.Authorization, FoundationCapabilityIds.Files, FoundationCapabilityIds.Notifications, FoundationCapabilityIds.Search, FoundationCapabilityIds.Localization])
    ];

    public static IReadOnlyList<CapabilityProfile> All => Profiles;

    public static CapabilityProfile Get(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return Profiles.FirstOrDefault(profile => string.Equals(profile.Id, id, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"Unknown FoundationKit capability profile '{id}'.");
    }
}

public sealed class CapabilityResolver
{
    private readonly Dictionary<string, CapabilityDescriptor> _descriptors;

    public CapabilityResolver(IEnumerable<CapabilityDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);
        _descriptors = descriptors.ToDictionary(descriptor => descriptor.Id, StringComparer.OrdinalIgnoreCase);
    }

    public static CapabilityResolver CreateDefault() => new(FoundationCapabilityCatalog.All);

    public IReadOnlyList<CapabilityDescriptor> Resolve(IEnumerable<string> requestedCapabilityIds)
    {
        ArgumentNullException.ThrowIfNull(requestedCapabilityIds);
        var requested = requestedCapabilityIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var visitState = new Dictionary<string, VisitState>(StringComparer.OrdinalIgnoreCase);
        var resolved = new List<CapabilityDescriptor>();
        foreach (var id in requested)
            Visit(id, visitState, resolved);
        return resolved;
    }

    public IReadOnlyList<CapabilityDescriptor> ResolveSelection(
        string profileId,
        IEnumerable<string>? include = null,
        IEnumerable<string>? exclude = null)
    {
        var profile = FoundationCapabilityProfiles.Get(profileId);
        var excluded = new HashSet<string>(exclude ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        var requested = new HashSet<string>(profile.CapabilityIds, StringComparer.OrdinalIgnoreCase);
        requested.ExceptWith(excluded);
        requested.UnionWith(include ?? Array.Empty<string>());
        var resolved = Resolve(requested);
        var excludedDependency = resolved.FirstOrDefault(descriptor => excluded.Contains(descriptor.Id));
        if (excludedDependency is not null)
            throw new InvalidOperationException($"Capability '{excludedDependency.Id}' cannot be excluded because another selected capability requires it.");
        return resolved;
    }

    private void Visit(string id, IDictionary<string, VisitState> state, ICollection<CapabilityDescriptor> resolved)
    {
        if (!_descriptors.TryGetValue(id, out var descriptor))
            throw new KeyNotFoundException($"Unknown FoundationKit capability '{id}'.");
        if (state.TryGetValue(id, out var existing))
        {
            if (existing == VisitState.Visited) return;
            throw new InvalidOperationException($"Capability dependency cycle detected at '{id}'.");
        }
        state[id] = VisitState.Visiting;
        foreach (var dependency in descriptor.Dependencies)
            Visit(dependency, state, resolved);
        state[id] = VisitState.Visited;
        resolved.Add(descriptor);
    }

    private enum VisitState
    {
        Visiting,
        Visited
    }
}
