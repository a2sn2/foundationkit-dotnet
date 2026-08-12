namespace FoundationKit.Workbench.Contracts;

public sealed record ComposerGenerationRequest(
    string ManifestJson,
    bool Force = false,
    string FoundationMode = "linked");

public sealed record ComposerGenerationResponse(
    bool Generated,
    string? ProjectName,
    string? RelativeOutputPath,
    string? SolutionFileName,
    string? ReferenceMode,
    int GeneratedFileCount,
    string? Error);
