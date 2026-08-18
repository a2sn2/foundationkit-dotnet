namespace FoundationKit.Workbench.Contracts;

public sealed record StudioProviderContract(
    string Id,
    string DisplayName,
    string Kind,
    IReadOnlyList<string> NuGetPackages,
    string? Notes);

public sealed record StudioFeatureContract(
    string Id,
    string DisplayName,
    string Category,
    string Description,
    string Readiness,
    string? CapabilityId,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<StudioProviderContract> Providers,
    string DefaultProvider);

public sealed record StudioCatalogResponse(
    IReadOnlyList<StudioFeatureContract> Features,
    IReadOnlyList<string> Profiles,
    IReadOnlyList<string> FieldTypes,
    string DefaultFoundationMode);

public sealed record StudioFieldContract(
    string Name,
    string Type,
    bool Required = false,
    int MaximumLength = 200,
    bool Indexed = false,
    bool Unique = false,
    bool Filterable = false,
    bool Sortable = false,
    string? ReferenceResource = null);

public sealed record StudioResourceContract(
    string Name,
    string Route,
    IReadOnlyList<StudioFieldContract> Fields,
    bool Auditing = true,
    bool Authorization = true,
    bool Idempotency = true,
    bool Concurrency = true);

public sealed record StudioModuleContract(
    string Name,
    IReadOnlyList<StudioResourceContract> Resources);

public sealed record StudioProjectRequest(
    int SchemaVersion,
    string Name,
    string Profile,
    string FoundationMode,
    IReadOnlyList<string> SelectedFeatures,
    IReadOnlyList<StudioModuleContract> Modules,
    IReadOnlyDictionary<string, string>? ProviderChoices = null);

public sealed record StudioResolvedFeatureContract(
    string Id,
    string DisplayName,
    string Category,
    string Readiness,
    string Provider,
    bool WasSelected,
    IReadOnlyList<string> Dependencies);

public sealed record StudioPreviewResponse(
    bool Valid,
    string? ProjectName,
    string? RelativeOutputPath,
    IReadOnlyList<StudioResolvedFeatureContract> Features,
    IReadOnlyList<string> AbpPackages,
    int FilesToCreate,
    int FilesToUpdate,
    int FilesToDelete,
    int ConsumerFilesPreserved,
    IReadOnlyList<string> SampleChanges,
    IReadOnlyList<string> Warnings,
    string? Error);

public sealed record StudioProjectGenerationResponse(
    bool Generated,
    string? ProjectName,
    string? RelativeOutputPath,
    string? SolutionFileName,
    string? ReferenceMode,
    int GeneratedFileCount,
    int ConsumerFilesPreserved,
    int ResolvedFeatureCount,
    IReadOnlyList<string> AbpPackages,
    string? Error);
