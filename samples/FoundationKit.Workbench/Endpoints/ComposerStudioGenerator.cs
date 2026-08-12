using FoundationKit.Composer;
using FoundationKit.Workbench.Contracts;

namespace FoundationKit.Workbench.Endpoints;

public static class ComposerStudioGenerator
{
    public const string GenerationRootConfigurationKey = "ComposerStudio:GenerationRoot";
    public const string FoundationRootConfigurationKey = "ComposerStudio:FoundationRoot";

    public static async Task<ComposerGenerationResponse> GenerateAsync(
        string? manifestJson,
        string generationRoot,
        string foundationRoot,
        bool force,
        string foundationMode = "linked",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(manifestJson))
            return Failed("Manifest JSON is required.");
        if (manifestJson.Length > ComposerStudioValidator.MaximumManifestCharacters)
        {
            return Failed(
                $"Manifest JSON exceeds the {ComposerStudioValidator.MaximumManifestCharacters} character Studio generation limit.");
        }
        if (string.IsNullOrWhiteSpace(generationRoot))
            return Failed("Composer Studio generation root is not configured.");
        if (string.IsNullOrWhiteSpace(foundationRoot))
            return Failed("Composer Studio Foundation root is not configured.");

        try
        {
            var manifest = ComposerManifestParser.Parse(manifestJson);
            if (manifest.SchemaVersion != 2 || manifest.ProjectModel is null)
                return Failed("Core Studio project generation requires a schemaVersion 2 manifest with modules/resources.");

            var bindingMode = ParseFoundationMode(foundationMode);
            var projectDirectoryName = ValidateProjectDirectoryName(manifest.Name);
            var normalizedGenerationRoot = Path.GetFullPath(generationRoot);
            var normalizedFoundationRoot = Path.GetFullPath(foundationRoot);
            Directory.CreateDirectory(normalizedGenerationRoot);

            var outputDirectory = Path.GetFullPath(Path.Combine(normalizedGenerationRoot, projectDirectoryName));
            EnsureChildPath(normalizedGenerationRoot, outputDirectory);

            var analysis = CompositionAnalyzer.Analyze(manifest);
            var generated = await ComposerProjectModelGenerator.GenerateAsync(
                analysis,
                new ProjectGenerationOptions(
                    outputDirectory,
                    normalizedFoundationRoot,
                    force),
                cancellationToken).ConfigureAwait(false);

            var result = ComposerFoundationBinding.FinalizeLocalSourceBinding(
                generated,
                manifest.Name,
                normalizedFoundationRoot,
                bindingMode);

            return new ComposerGenerationResponse(
                Generated: true,
                ProjectName: manifest.Name,
                RelativeOutputPath: $"generated/{projectDirectoryName}",
                SolutionFileName: Path.GetFileName(result.SolutionPath),
                ReferenceMode: result.ReferenceMode,
                GeneratedFileCount: result.GeneratedFiles.Count,
                Error: null);
        }
        catch (ComposerManifestException exception)
        {
            return Failed(exception.Message);
        }
        catch (ComposerGenerationException exception)
        {
            return Failed(exception.Message);
        }
        catch (KeyNotFoundException exception)
        {
            return Failed(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Failed(exception.Message);
        }
        catch (IOException exception)
        {
            return Failed($"Could not write the generated project: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return Failed($"Could not write the generated project: {exception.Message}");
        }
    }

    public static string ResolveFoundationRoot(string contentRootPath, string? configuredFoundationRoot)
    {
        if (!string.IsNullOrWhiteSpace(configuredFoundationRoot))
            return Path.GetFullPath(configuredFoundationRoot);

        var current = new DirectoryInfo(Path.GetFullPath(contentRootPath));
        while (current is not null)
        {
            var marker = Path.Combine(
                current.FullName,
                "src",
                "FoundationKit.Domain",
                "FoundationKit.Domain.csproj");
            if (File.Exists(marker))
                return current.FullName;

            current = current.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate the FoundationKit repository root. Configure ComposerStudio:FoundationRoot explicitly.");
    }

    public static string ResolveGenerationRoot(string foundationRoot, string? configuredGenerationRoot) =>
        string.IsNullOrWhiteSpace(configuredGenerationRoot)
            ? Path.Combine(Path.GetFullPath(foundationRoot), "generated")
            : Path.GetFullPath(configuredGenerationRoot);

    private static ComposerFoundationBindingMode ParseFoundationMode(string? foundationMode)
    {
        var normalized = foundationMode?.Trim().ToLowerInvariant();
        return normalized switch
        {
            null or "" or "linked" or "reference" or "project" => ComposerFoundationBindingMode.Linked,
            "copy" or "source-copy" or "standalone" => ComposerFoundationBindingMode.SourceCopy,
            _ => throw new ComposerGenerationException(
                "Foundation mode must be 'linked' or 'source-copy'.")
        };
    }

    private static string ValidateProjectDirectoryName(string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName) ||
            projectName is "." or ".." ||
            !string.Equals(Path.GetFileName(projectName), projectName, StringComparison.Ordinal) ||
            projectName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ComposerGenerationException(
                "The validated project name cannot be used as a local generation directory.");
        }

        return projectName;
    }

    private static void EnsureChildPath(string root, string candidate)
    {
        var relative = Path.GetRelativePath(root, candidate);
        if (relative.Equals("..", StringComparison.Ordinal) ||
            relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            Path.IsPathRooted(relative))
        {
            throw new ComposerGenerationException(
                "The generated project must remain inside the configured Studio generation root.");
        }
    }

    private static ComposerGenerationResponse Failed(string message) => new(
        Generated: false,
        ProjectName: null,
        RelativeOutputPath: null,
        SolutionFileName: null,
        ReferenceMode: null,
        GeneratedFileCount: 0,
        Error: message);
}
