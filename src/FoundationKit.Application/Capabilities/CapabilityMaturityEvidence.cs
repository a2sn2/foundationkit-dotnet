namespace FoundationKit.Application.Capabilities;

public sealed record CapabilityMaturityEvidenceDescriptor(
    string CapabilityId,
    CapabilityMaturity AssessedMaturity,
    bool HasImplementationEvidence,
    bool HasQualityEvidence,
    bool HasAdoptionEvidence,
    bool HasCompatibilityEvidence,
    string Rationale);

public sealed record CapabilityMaturityEvidenceResult(
    string CapabilityId,
    CapabilityMaturity DeclaredMaturity,
    bool IsValid,
    IReadOnlyList<string> MissingEvidence);

public static class FoundationCapabilityMaturityEvidence
{
    public const int MaxRationaleLength = 500;

    private static readonly CapabilityMaturityEvidenceDescriptor[] Evidence =
    [
        E(FoundationCapabilityIds.Kernel, CapabilityMaturity.Stable, true, true, true, true, "Base domain/application primitives are packaged, tested, exercised by Workbench/generated projects, and supported as the composition baseline."),
        E(FoundationCapabilityIds.Validation, CapabilityMaturity.Stable, true, true, true, true, "Validation contracts are implemented, tested, exercised by the active application stack, and supported as base public surface."),
        E(FoundationCapabilityIds.WebApi, CapabilityMaturity.Stable, true, true, true, true, "HTTP result, Problem Details, correlation, pipeline, and endpoint conventions are implemented, tested, used by Workbench, and compatibility-supported as base surface."),
        E(FoundationCapabilityIds.Blazor, CapabilityMaturity.Stable, true, true, true, true, "Typed API/result/state primitives are implemented, tested, used by Workbench client code, and supported as base public surface."),
        E(FoundationCapabilityIds.Observability, CapabilityMaturity.Preview, true, true, false, false, "Operational correlation, health, and diagnostic conventions have implementation and quality evidence; provider and compatibility commitments remain incomplete."),
        E(FoundationCapabilityIds.Security, CapabilityMaturity.Preview, true, true, false, false, "Reusable security conventions are implemented and quality-gated while broader deployment/provider compatibility remains environment-specific."),
        E(FoundationCapabilityIds.Identity, CapabilityMaturity.ReferenceOnly, true, true, false, false, "Identity policy, notification, security-event, and step-up contracts are implemented and tested; broad active adoption and provider compatibility are not asserted."),
        E(FoundationCapabilityIds.Authorization, CapabilityMaturity.ReferenceOnly, true, true, false, false, "Authorization primitives are implemented and tested; organization/tenant policy compatibility and broader active adoption remain future evidence."),
        E(FoundationCapabilityIds.Auditing, CapabilityMaturity.ReferenceOnly, true, true, true, false, "Audit contracts are implemented and tested, and Workbench composes the CRUD audit observer; broad sink/retention compatibility remains unclaimed."),
        E(FoundationCapabilityIds.Settings, CapabilityMaturity.ReferenceOnly, true, true, true, false, "Settings v1 is implemented, tested, and exercised by Workbench; provider breadth and compatibility support remain limited."),
        E(FoundationCapabilityIds.FeatureManagement, CapabilityMaturity.ReferenceOnly, true, true, true, false, "Feature Management v1 is implemented, tested, and exercised by Workbench; targeting/rollout compatibility remains future work."),
        E(FoundationCapabilityIds.Localization, CapabilityMaturity.ReferenceOnly, true, true, true, false, "Localization v1 is implemented, tested, and exercised by Workbench; translation/provider breadth and compatibility remain limited."),
        P(FoundationCapabilityIds.Organization, "Reusable organization hierarchy semantics are defined as vocabulary only; no active reusable implementation is claimed."),
        P(FoundationCapabilityIds.MultiTenancy, "Tenant identity, resolution, authorization scope, and storage-isolation topology remain explicit future contracts."),
        E(FoundationCapabilityIds.Workflow, CapabilityMaturity.ReferenceOnly, true, true, false, false, "Deterministic workflow transition primitives are implemented and tested; broad active adoption and migration compatibility are not asserted."),
        E(FoundationCapabilityIds.Approvals, CapabilityMaturity.ReferenceOnly, true, true, false, false, "Approvals v1 is implemented and tested; advanced approval patterns and broad active compatibility evidence are not established."),
        P(FoundationCapabilityIds.Tasks, "Reusable work-item assignment, priority, due-date, and lifecycle semantics are not yet implemented."),
        E(FoundationCapabilityIds.Notifications, CapabilityMaturity.ReferenceOnly, true, true, false, false, "Channel-neutral notification contracts are implemented and tested; active channel/provider diversity and compatibility support remain limited."),
        P(FoundationCapabilityIds.Files, "Provider-neutral file storage and lifecycle contracts are not yet implemented."),
        P(FoundationCapabilityIds.Documents, "Reusable document metadata, classification, versioning, and lifecycle contracts are not yet implemented."),
        P(FoundationCapabilityIds.Jobs, "Delayed, scheduled, recurring-work, retry, and worker lifecycle contracts are not yet implemented."),
        P(FoundationCapabilityIds.Messaging, "In-process domain-event dispatch is deliberately not treated as durable integration messaging or outbox/inbox delivery."),
        P(FoundationCapabilityIds.Webhooks, "Reusable signing, replay, retry, and delivery-history webhook contracts are not yet implemented."),
        P(FoundationCapabilityIds.Realtime, "No provider-neutral realtime delivery boundary is implemented yet."),
        E(FoundationCapabilityIds.Caching, CapabilityMaturity.ReferenceOnly, true, true, true, false, "Caching v1 is implemented, tested, and exercised by Workbench; distributed-provider consistency/compatibility remain unclaimed."),
        P(FoundationCapabilityIds.Search, "Provider-neutral cross-resource search and indexing contracts are not yet implemented."),
        P(FoundationCapabilityIds.Reporting, "Reusable report-definition, grouping, export, and provider contracts are not yet implemented."),
        P(FoundationCapabilityIds.Idempotency, "A reusable reservation/completion/replay idempotency contract is not implemented in the active Core surface."),
        E(FoundationCapabilityIds.Concurrency, CapabilityMaturity.ReferenceOnly, true, true, true, false, "Core vNext adds provider-neutral CRUD concurrency policy plus EF conflict translation and Workbench SQL proof; broader precondition/provider compatibility remains limited."),
        P(FoundationCapabilityIds.Money, "Currency-aware reusable money semantics and conversion boundaries are not yet implemented."),
        P(FoundationCapabilityIds.Numbering, "Reusable business sequence semantics and explicit scoping are not yet implemented."),
        P(FoundationCapabilityIds.Privacy, "PII classification, masking, consent, and anonymization remain future reusable/policy boundaries."),
        P(FoundationCapabilityIds.Retention, "Retention, archive, deletion, and anonymization scheduling contracts remain future work."),
        P(FoundationCapabilityIds.ArtificialIntelligence, "AI abstractions remain deferred until provider-neutral requirements and control/observability boundaries are established."),
        E(FoundationCapabilityIds.SqlServerProvider, CapabilityMaturity.ReferenceOnly, true, true, true, false, "SQL Server is exercised by the host-owned Workbench EF integration; no reusable provider-family compatibility commitment is claimed."),
        P(FoundationCapabilityIds.RedisProvider, "No Redis adapter is implemented; distributed cache consistency remains an explicit future provider decision."),
        E(FoundationCapabilityIds.SmtpProvider, CapabilityMaturity.ReferenceOnly, true, true, false, false, "The SMTP adapter is implemented, packaged, and tested; broad active adoption/provider-family support is not asserted."),
        E(FoundationCapabilityIds.CliTooling, CapabilityMaturity.ReferenceOnly, true, true, false, false, "Composer discovery, validation, compatibility, deterministic generation, and interactive generation are implemented and tested; external adoption/support remains limited."),
        E(FoundationCapabilityIds.WorkbenchTooling, CapabilityMaturity.ReferenceOnly, true, true, false, false, "Workbench is implemented and tested as the executable Core/SQL reference; it is not yet a supported visual composer product.")
    ];

    private static readonly Dictionary<string, CapabilityMaturityEvidenceDescriptor> ById =
        Evidence.ToDictionary(item => item.CapabilityId, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<CapabilityMaturityEvidenceDescriptor> All => Evidence;

    public static CapabilityMaturityEvidenceDescriptor Get(string capabilityId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(capabilityId);
        return ById.TryGetValue(capabilityId, out var evidence)
            ? evidence
            : throw new KeyNotFoundException($"Unknown FoundationKit maturity evidence '{capabilityId}'.");
    }

    private static CapabilityMaturityEvidenceDescriptor E(
        string id,
        CapabilityMaturity maturity,
        bool implementation,
        bool quality,
        bool adoption,
        bool compatibility,
        string rationale) => new(id, maturity, implementation, quality, adoption, compatibility, rationale);

    private static CapabilityMaturityEvidenceDescriptor P(string id, string rationale) =>
        new(id, CapabilityMaturity.Planned, false, false, false, false, rationale);
}

public static class CapabilityMaturityEvidencePolicy
{
    public static CapabilityMaturityEvidenceResult Evaluate(
        CapabilityDescriptor capability,
        CapabilityMaturityEvidenceDescriptor evidence)
    {
        ArgumentNullException.ThrowIfNull(capability);
        ArgumentNullException.ThrowIfNull(evidence);
        if (!string.Equals(capability.Id, evidence.CapabilityId, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Maturity evidence '{evidence.CapabilityId}' does not describe capability '{capability.Id}'.", nameof(evidence));

        var missing = new List<string>();
        if (evidence.AssessedMaturity != capability.Maturity) missing.Add("declared-maturity-match");
        var rationale = evidence.Rationale?.Trim() ?? string.Empty;
        if (rationale.Length == 0 || rationale.Length > FoundationCapabilityMaturityEvidence.MaxRationaleLength) missing.Add("bounded-rationale");

        switch (capability.Maturity)
        {
            case CapabilityMaturity.Planned:
                break;
            case CapabilityMaturity.ReferenceOnly:
                Require(evidence.HasImplementationEvidence, "implementation-or-proof", missing);
                break;
            case CapabilityMaturity.Preview:
                Require(evidence.HasImplementationEvidence, "implementation-or-proof", missing);
                Require(evidence.HasQualityEvidence, "quality-gates", missing);
                break;
            case CapabilityMaturity.Stable:
                Require(evidence.HasImplementationEvidence, "implementation-or-proof", missing);
                Require(evidence.HasQualityEvidence, "quality-gates", missing);
                Require(evidence.HasAdoptionEvidence, "adoption", missing);
                Require(evidence.HasCompatibilityEvidence, "compatibility-support", missing);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(capability), capability.Maturity, "Unknown capability maturity.");
        }

        return new CapabilityMaturityEvidenceResult(capability.Id, capability.Maturity, missing.Count == 0, missing);
    }

    public static IReadOnlyList<CapabilityMaturityEvidenceResult> EvaluateCatalog(
        IEnumerable<CapabilityDescriptor> capabilities,
        IEnumerable<CapabilityMaturityEvidenceDescriptor> evidence)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(evidence);
        var capabilityList = capabilities.ToArray();
        var evidenceList = evidence.ToArray();
        var capabilityById = ToUniqueDictionary(capabilityList, item => item.Id, "capability");
        var evidenceById = ToUniqueDictionary(evidenceList, item => item.CapabilityId, "maturity evidence");

        foreach (var item in evidenceList)
            if (!capabilityById.ContainsKey(item.CapabilityId))
                throw new InvalidOperationException($"Maturity evidence references unknown capability '{item.CapabilityId}'.");

        var results = new List<CapabilityMaturityEvidenceResult>(capabilityList.Length);
        foreach (var capability in capabilityList)
        {
            if (!evidenceById.TryGetValue(capability.Id, out var item))
                throw new InvalidOperationException($"Capability '{capability.Id}' has no maturity evidence assessment.");
            results.Add(Evaluate(capability, item));
        }
        return results;
    }

    public static void EnsureCatalogValid(
        IEnumerable<CapabilityDescriptor> capabilities,
        IEnumerable<CapabilityMaturityEvidenceDescriptor> evidence)
    {
        var invalid = EvaluateCatalog(capabilities, evidence).Where(result => !result.IsValid).ToArray();
        if (invalid.Length == 0) return;
        var details = string.Join("; ", invalid.Select(result => $"{result.CapabilityId}: {string.Join(",", result.MissingEvidence)}"));
        throw new InvalidOperationException($"Capability maturity evidence policy failed: {details}.");
    }

    private static Dictionary<string, T> ToUniqueDictionary<T>(IEnumerable<T> items, Func<T, string> idSelector, string description)
    {
        var result = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var id = idSelector(item);
            if (string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException($"A {description} ID cannot be empty.");
            if (!result.TryAdd(id, item)) throw new InvalidOperationException($"Duplicate {description} ID '{id}'.");
        }
        return result;
    }

    private static void Require(bool condition, string evidenceName, List<string> missing)
    {
        if (!condition) missing.Add(evidenceName);
    }
}
