using FoundationKit.Composer;
using FoundationKit.Workbench.Contracts;

namespace FoundationKit.Workbench.Endpoints;

public static class ComposerStudioValidator
{
    public const int MaximumManifestCharacters = 262_144;

    public static ComposerValidationResponse Validate(string? manifestJson)
    {
        if (string.IsNullOrWhiteSpace(manifestJson))
            return Invalid("Manifest JSON is required.");
        if (manifestJson.Length > MaximumManifestCharacters)
        {
            return Invalid(
                $"Manifest JSON exceeds the {MaximumManifestCharacters} character Studio validation limit.");
        }

        try
        {
            var manifest = ComposerManifestParser.Parse(manifestJson);
            var analysis = CompositionAnalyzer.Analyze(manifest);
            var projectModel = manifest.ProjectModel;
            var capabilities = analysis.Entries
                .Select(entry => new ComposerCapabilityEvidence(
                    entry.Capability.Id,
                    entry.Capability.Maturity.ToString(),
                    entry.Reasons))
                .ToArray();

            return new ComposerValidationResponse(
                Valid: true,
                SchemaVersion: manifest.SchemaVersion,
                ProjectName: manifest.Name,
                Profile: manifest.Profile,
                ModuleCount: projectModel?.Modules.Count ?? 0,
                ResourceCount: projectModel?.Resources.Count ?? 0,
                ReadModelCount: projectModel?.ReadModels.Count ?? 0,
                StableOnly: analysis.IsStableOnly,
                Capabilities: capabilities,
                Warnings: analysis.Warnings,
                Error: null);
        }
        catch (ComposerManifestException exception)
        {
            return Invalid(exception.Message);
        }
    }

    private static ComposerValidationResponse Invalid(string message) => new(
        Valid: false,
        SchemaVersion: null,
        ProjectName: null,
        Profile: null,
        ModuleCount: 0,
        ResourceCount: 0,
        ReadModelCount: 0,
        StableOnly: false,
        Capabilities: Array.Empty<ComposerCapabilityEvidence>(),
        Warnings: Array.Empty<string>(),
        Error: message);
}
