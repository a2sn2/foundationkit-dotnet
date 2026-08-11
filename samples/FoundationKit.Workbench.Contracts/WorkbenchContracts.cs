namespace FoundationKit.Workbench.Contracts;

public sealed record RuntimeResponse(
    string Mode,
    string Persistence,
    string Database,
    string ContactName);

public sealed record PlatformReferenceResponse(
    string DefaultCulture,
    string TextDirection,
    string CultureResolutionSource,
    string CultureSettingScope,
    string DefaultTimeZone,
    string TimeZoneSettingScope,
    bool CatalogPreviewEnabled,
    string FeatureDecisionSource,
    string? FeatureSettingScope);

public sealed record HealthResponse(string Status, string Database);

public sealed record ModuleCompositionResponse(
    string Name,
    string Route,
    string ApiRoute,
    IReadOnlyList<string> DeclaredCapabilities,
    IReadOnlyList<string> EffectiveCapabilities,
    ModuleApiContract Api);

public sealed record ModuleApiContract(
    string RoutePrefix,
    string Idempotency,
    string Concurrency,
    int MaximumFilters,
    int MaximumSorts,
    string? RateLimitPolicyName);

public sealed record ComposerValidationRequest(string ManifestJson);

public sealed record ComposerCapabilityEvidence(
    string Id,
    string Maturity,
    IReadOnlyList<string> Reasons);

public sealed record ComposerValidationResponse(
    bool Valid,
    int? SchemaVersion,
    string? ProjectName,
    string? Profile,
    int ModuleCount,
    int ResourceCount,
    int ReadModelCount,
    bool StableOnly,
    IReadOnlyList<ComposerCapabilityEvidence> Capabilities,
    IReadOnlyList<string> Warnings,
    string? Error);

public sealed class CatalogResponse
{
    public int SchemaVersion { get; set; }
    public string CoreVersion { get; set; } = string.Empty;
    public DateTimeOffset UpdatedUtc { get; set; }
    public ContactContract Contact { get; set; } = new();
    public List<PackageContract> Packages { get; set; } = [];
    public List<IdeaContract> Ideas { get; set; } = [];
    public List<AdoptionStepContract> AdoptionSteps { get; set; } = [];
}

public sealed class ContactContract
{
    public string Name { get; set; } = string.Empty;
    public string GithubProfile { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string NewIssue { get; set; } = string.Empty;
}

public sealed class PackageContract
{
    public string Id { get; set; } = string.Empty;
    public string PackageId { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string SummaryAr { get; set; } = string.Empty;
    public string SummaryEn { get; set; } = string.Empty;
    public List<CapabilityContract> Capabilities { get; set; } = [];
}

public sealed class CapabilityContract
{
    public string Id { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<string> PublicTypes { get; set; } = [];
}

public sealed class IdeaContract
{
    public string Id { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public List<string> RecommendedCapabilityIds { get; set; } = [];
    public List<string> ProductDecisions { get; set; } = [];
}

public sealed class AdoptionStepContract
{
    public int Number { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
}
