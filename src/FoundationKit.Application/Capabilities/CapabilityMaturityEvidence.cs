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
        new(FoundationCapabilityIds.Kernel, CapabilityMaturity.Stable, true, true, true, true,
            "Base domain/application primitives are packaged, tested, consumed by repository products, and treated as the supported composition baseline."),
        new(FoundationCapabilityIds.Validation, CapabilityMaturity.Stable, true, true, true, true,
            "Validation contracts are implemented in the supported base surface, covered by tests, and consumed through the current application stack."),
        new(FoundationCapabilityIds.WebApi, CapabilityMaturity.Stable, true, true, true, true,
            "Web API result, Problem Details, correlation, and response conventions are implemented, tested, adopted by repository hosts, and supported as base public surface."),
        new(FoundationCapabilityIds.Blazor, CapabilityMaturity.Stable, true, true, true, true,
            "Blazor API-result/client/state primitives are implemented, tested, used by repository clients, and supported as base public surface."),

        new(FoundationCapabilityIds.Observability, CapabilityMaturity.Preview, true, true, true, false,
            "Operational logging, correlation, health, and diagnostic behavior has repository proof and quality coverage, while a broader compatibility commitment remains incomplete."),
        new(FoundationCapabilityIds.Security, CapabilityMaturity.Preview, true, true, true, false,
            "Reusable security conventions are packaged, tested, and consumed by real hosts, while compatibility/provider breadth is still evolving."),

        new(FoundationCapabilityIds.Identity, CapabilityMaturity.ReferenceOnly, true, true, true, false,
            "Identity policy and notification/step-up contracts are implemented and exercised by product consumers, but broader compatibility evidence remains limited."),
        new(FoundationCapabilityIds.Authorization, CapabilityMaturity.ReferenceOnly, true, true, true, false,
            "Authorization primitives are implemented, tested, and reused by products; the supported compatibility surface remains intentionally conservative."),
        new(FoundationCapabilityIds.Auditing, CapabilityMaturity.ReferenceOnly, true, true, true, false,
            "Auditing contracts are implemented, packaged, tested, and composed by products, without a broad compatibility/support commitment yet."),
        new(FoundationCapabilityIds.Settings, CapabilityMaturity.ReferenceOnly, true, true, true, false,
            "Settings v1 is implemented, tested, and consumed by Workbench, while provider/adoption breadth and compatibility evidence remain limited."),
        new(FoundationCapabilityIds.FeatureManagement, CapabilityMaturity.ReferenceOnly, true, true, true, false,
            "Feature Management v1 is implemented, tested, and consumed through Workbench, while advanced rollout and compatibility evidence remain future work."),
        new(FoundationCapabilityIds.Localization, CapabilityMaturity.ReferenceOnly, true, true, true, false,
            "Localization v1 is implemented, tested, and consumed by Workbench; translation/provider breadth and compatibility evidence remain limited."),
        new(FoundationCapabilityIds.Workflow, CapabilityMaturity.ReferenceOnly, true, true, true, false,
            "Workflow transition primitives are implemented, tested, and consumed by Athar and Madar, with broader compatibility evidence still intentionally limited."),
        new(FoundationCapabilityIds.Approvals, CapabilityMaturity.ReferenceOnly, true, true, true, false,
            "Approvals v1 is implemented and reused by two independent product shapes, but advanced approval patterns and compatibility breadth are not established."),
        new(FoundationCapabilityIds.Notifications, CapabilityMaturity.ReferenceOnly, true, true, true, false,
            "Notification message/delivery contracts are implemented and reused by Athar and Madar, while channel/provider diversity and compatibility breadth remain limited."),
        new(FoundationCapabilityIds.Caching, CapabilityMaturity.ReferenceOnly, true, true, true, false,
            "Caching v1 is implemented, tested, and consumed by Workbench; distributed-provider and compatibility semantics remain deliberately unclaimed."),
        new(FoundationCapabilityIds.Idempotency, CapabilityMaturity.ReferenceOnly, true, true, true, false,
            "Real duplicate-write prevention behavior is proven in Athar, but a separate reusable reservation/replay contract has not yet been extracted."),
        new(FoundationCapabilityIds.Concurrency, CapabilityMaturity.ReferenceOnly, true, true, true, false,
            "Real optimistic-concurrency behavior is proven in products, but a provider-neutral client precondition/token contract has not yet been extracted."),
        new(FoundationCapabilityIds.SqlServerProvider, CapabilityMaturity.ReferenceOnly, true, true, true, false,
            "SQL Server is exercised across repository products and integration workflows, while no broad reusable provider-family compatibility commitment is claimed."),
        new(FoundationCapabilityIds.SmtpProvider, CapabilityMaturity.ReferenceOnly, true, true, true, false,
            "The SMTP provider adapter is implemented, packaged, tested, and consumed, while provider-family breadth and compatibility support remain limited."),
        new(FoundationCapabilityIds.CliTooling, CapabilityMaturity.ReferenceOnly, true, true, false, false,
            "Composer CLI discovery, validation, compatibility, and explanation are implemented and tested; external adoption/support evidence remains limited."),
        new(FoundationCapabilityIds.WorkbenchTooling, CapabilityMaturity.ReferenceOnly, true, true, false, false,
            "Workbench is implemented and tested as an interactive repository reference consumer, while it is not yet a supported visual composer product."),

        new(FoundationCapabilityIds.Organization, CapabilityMaturity.Planned, false, false, false, false,
            "Madar departments provide product evidence only; a reusable organization hierarchy contract is intentionally not extracted."),
        new(FoundationCapabilityIds.MultiTenancy, CapabilityMaturity.Planned, false, false, false, false,
            "Tenant identity, resolution, and isolation topology remain product/owner decisions without a reusable implementation."),
        new(FoundationCapabilityIds.Tasks, CapabilityMaturity.Planned, false, false, false, false,
            "Assignable reusable work-item semantics have not yet been established by an independent capability implementation."),
        new(FoundationCapabilityIds.Files, CapabilityMaturity.Planned, false, false, false, false,
            "Madar attachments remain product-owned evidence; a provider-neutral reusable file-storage contract is not yet extracted."),
        new(FoundationCapabilityIds.Documents, CapabilityMaturity.Planned, false, false, false, false,
            "Reusable document metadata, versioning, classification, and lifecycle semantics are not yet established."),
        new(FoundationCapabilityIds.Jobs, CapabilityMaturity.Planned, false, false, false, false,
            "Madar exposes an SLA evaluator seam, but reusable delayed/scheduled/recurring job semantics and providers are not established."),
        new(FoundationCapabilityIds.Messaging, CapabilityMaturity.Planned, false, false, false, false,
            "The existing in-process domain-event dispatcher is deliberately not treated as integration messaging or outbox/inbox delivery."),
        new(FoundationCapabilityIds.Webhooks, CapabilityMaturity.Planned, false, false, false, false,
            "Reusable webhook signing, replay, retry, and delivery-history contracts are not implemented."),
        new(FoundationCapabilityIds.Realtime, CapabilityMaturity.Planned, false, false, false, false,
            "No provider-neutral realtime delivery boundary has sufficient consumer evidence for extraction."),
        new(FoundationCapabilityIds.Search, CapabilityMaturity.Planned, false, false, false, false,
            "Madar v0.10 search is product-owned relational behavior and does not yet establish a reusable search-provider contract."),
        new(FoundationCapabilityIds.Reporting, CapabilityMaturity.Planned, false, false, false, false,
            "Madar operational counts are product-owned and do not yet establish reusable report-definition/export semantics."),
        new(FoundationCapabilityIds.Money, CapabilityMaturity.Planned, false, false, false, false,
            "Currency-aware reusable money semantics and conversion boundaries have not yet been established."),
        new(FoundationCapabilityIds.Numbering, CapabilityMaturity.Planned, false, false, false, false,
            "Business sequence semantics and organizational scoping require concrete product evidence before extraction."),
        new(FoundationCapabilityIds.Privacy, CapabilityMaturity.Planned, false, false, false, false,
            "PII classification, masking, consent, and anonymization policy remain product/legal decisions without a reusable implementation."),
        new(FoundationCapabilityIds.Retention, CapabilityMaturity.Planned, false, false, false, false,
            "Retention, archive, deletion, and anonymization scheduling semantics remain product/legal/provider decisions."),
        new(FoundationCapabilityIds.ArtificialIntelligence, CapabilityMaturity.Planned, false, false, false, false,
            "AI abstractions remain intentionally deferred until provider-neutral consumer requirements and observability controls are established."),
        new(FoundationCapabilityIds.RedisProvider, CapabilityMaturity.Planned, false, false, false, false,
            "No Redis provider adapter is selected or implemented; distributed cache semantics remain an explicit future provider decision.")
    ];

    private static readonly Dictionary<string, CapabilityMaturityEvidenceDescriptor> ById =
        Evidence.ToDictionary(item => item.CapabilityId, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<CapabilityMaturityEvidenceDescriptor> All => Evidence;

    public static CapabilityMaturityEvidenceDescriptor Get(string capabilityId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(capabilityId);

        return ById.TryGetValue(capabilityId, out var evidence)
            ? evidence
            : throw new KeyNotFoundException(
                $"Unknown FoundationKit maturity evidence '{capabilityId}'.");
    }
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
        {
            throw new ArgumentException(
                $"Maturity evidence '{evidence.CapabilityId}' does not describe capability '{capability.Id}'.",
                nameof(evidence));
        }

        var missing = new List<string>();

        if (evidence.AssessedMaturity != capability.Maturity)
        {
            missing.Add("declared-maturity-match");
        }

        var rationale = evidence.Rationale?.Trim() ?? string.Empty;
        if (rationale.Length == 0 || rationale.Length > FoundationCapabilityMaturityEvidence.MaxRationaleLength)
        {
            missing.Add("bounded-rationale");
        }

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
                throw new ArgumentOutOfRangeException(
                    nameof(capability),
                    capability.Maturity,
                    "Unknown capability maturity.");
        }

        return new CapabilityMaturityEvidenceResult(
            capability.Id,
            capability.Maturity,
            missing.Count == 0,
            missing);
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

        foreach (var evidenceItem in evidenceList)
        {
            if (!capabilityById.ContainsKey(evidenceItem.CapabilityId))
            {
                throw new InvalidOperationException(
                    $"Maturity evidence references unknown capability '{evidenceItem.CapabilityId}'.");
            }
        }

        var results = new List<CapabilityMaturityEvidenceResult>(capabilityList.Length);
        foreach (var capability in capabilityList)
        {
            if (!evidenceById.TryGetValue(capability.Id, out var evidenceItem))
            {
                throw new InvalidOperationException(
                    $"Capability '{capability.Id}' has no maturity evidence assessment.");
            }

            results.Add(Evaluate(capability, evidenceItem));
        }

        return results;
    }

    public static void EnsureCatalogValid(
        IEnumerable<CapabilityDescriptor> capabilities,
        IEnumerable<CapabilityMaturityEvidenceDescriptor> evidence)
    {
        var invalid = EvaluateCatalog(capabilities, evidence)
            .Where(result => !result.IsValid)
            .ToArray();

        if (invalid.Length == 0)
        {
            return;
        }

        var details = string.Join(
            "; ",
            invalid.Select(result =>
                $"{result.CapabilityId}: {string.Join(",", result.MissingEvidence)}"));
        throw new InvalidOperationException(
            $"Capability maturity evidence policy failed: {details}.");
    }

    private static Dictionary<string, T> ToUniqueDictionary<T>(
        IEnumerable<T> items,
        Func<T, string> idSelector,
        string description)
    {
        var result = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var id = idSelector(item);
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new InvalidOperationException($"A {description} ID cannot be empty.");
            }

            if (!result.TryAdd(id, item))
            {
                throw new InvalidOperationException($"Duplicate {description} ID '{id}'.");
            }
        }

        return result;
    }

    private static void Require(bool condition, string evidenceName, ICollection<string> missing)
    {
        if (!condition)
        {
            missing.Add(evidenceName);
        }
    }
}
