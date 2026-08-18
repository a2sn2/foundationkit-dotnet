using System.Security.Cryptography;
using FoundationKit.Application.Capabilities;
using FoundationKit.Composer;
using FoundationKit.Workbench.Contracts;

namespace FoundationKit.Workbench.Endpoints;

public static class ProjectStudioGenerator
{
    public static StudioCatalogResponse GetCatalog() => new(
        ComposerStudioFeatureCatalog.All.Select(feature => new StudioFeatureContract(
            feature.Id,
            feature.DisplayName,
            feature.Category,
            feature.Description,
            feature.Readiness.ToString(),
            feature.CapabilityId,
            feature.Dependencies,
            feature.Providers.Select(provider => new StudioProviderContract(
                provider.Id,
                provider.DisplayName,
                provider.Kind,
                provider.NuGetPackages,
                provider.Notes)).ToArray(),
            feature.DefaultProvider)).ToArray(),
        FoundationCapabilityProfiles.All.Select(profile => profile.Id).ToArray(),
        Enum.GetNames<StudioFieldType>(),
        "standalone");

    public static async Task<StudioPreviewResponse> PreviewAsync(
        StudioProjectRequest request,
        string generationRoot,
        string foundationRoot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var previewRoot = Path.Combine(Path.GetTempPath(), $"foundationkit-studio-preview-{Guid.NewGuid():N}");
        try
        {
            var blueprint = ToBlueprint(request);
            var compilation = ComposerStudioProjectCompiler.Compile(blueprint);
            var target = ResolveOutputDirectory(generationRoot, blueprint.Name);
            var previewOutput = Path.Combine(previewRoot, ValidateProjectDirectoryName(blueprint.Name));
            Directory.CreateDirectory(previewRoot);

            var generated = await GenerateCoreAsync(
                compilation,
                previewOutput,
                foundationRoot,
                force: false,
                cancellationToken).ConfigureAwait(false);
            await PersistStudioMetadataAsync(compilation, generated, cancellationToken).ConfigureAwait(false);

            var consumerFiles = ComposerStudioWorkspace.ListConsumerOwnedFiles(target);
            var consumerSet = consumerFiles.ToHashSet(StringComparer.Ordinal);
            var candidateFiles = EnumerateComparableFiles(previewOutput, exclude: null);
            var currentFiles = EnumerateComparableFiles(target, consumerSet);

            var created = candidateFiles.Keys.Except(currentFiles.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            var deleted = currentFiles.Keys.Except(candidateFiles.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            var updated = candidateFiles.Keys.Intersect(currentFiles.Keys, StringComparer.Ordinal)
                .Where(path => !string.Equals(candidateFiles[path], currentFiles[path], StringComparison.Ordinal))
                .Order(StringComparer.Ordinal)
                .ToArray();

            var samples = created.Select(path => $"+ {path}")
                .Concat(updated.Select(path => $"~ {path}"))
                .Concat(deleted.Select(path => $"- {path}"))
                .Take(30)
                .ToArray();
            var selected = request.SelectedFeatures.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var resolved = BuildResolvedFeatures(compilation, selected);
            var warnings = BuildWarnings(compilation);

            return new StudioPreviewResponse(
                true,
                blueprint.Name,
                $"generated/{ValidateProjectDirectoryName(blueprint.Name)}",
                resolved,
                compilation.AbpPackages,
                created.Length,
                updated.Length,
                deleted.Length,
                consumerFiles.Count,
                samples,
                warnings,
                null);
        }
        catch (Exception exception) when (IsExpected(exception))
        {
            return new StudioPreviewResponse(
                false,
                request.Name,
                null,
                [],
                [],
                0,
                0,
                0,
                0,
                [],
                [],
                exception.Message);
        }
        finally
        {
            TryDeleteDirectory(previewRoot);
        }
    }

    public static async Task<StudioProjectGenerationResponse> GenerateAsync(
        StudioProjectRequest request,
        string generationRoot,
        string foundationRoot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        StudioWorkspaceBackup? backup = null;
        string? output = null;
        try
        {
            var blueprint = ToBlueprint(request);
            var compilation = ComposerStudioProjectCompiler.Compile(blueprint);
            output = ResolveOutputDirectory(generationRoot, blueprint.Name);
            backup = ComposerStudioWorkspace.CaptureConsumerOwnedFiles(output);
            var hadExistingGeneratedProject = Directory.Exists(output) &&
                                              Directory.EnumerateFileSystemEntries(output).Any();

            var generated = await GenerateCoreAsync(
                compilation,
                output,
                foundationRoot,
                force: hadExistingGeneratedProject,
                cancellationToken).ConfigureAwait(false);
            await PersistStudioMetadataAsync(compilation, generated, cancellationToken).ConfigureAwait(false);

            ComposerStudioWorkspace.RestoreConsumerOwnedFiles(output, backup);
            var preserved = backup.ConsumerOwnedFiles.Count;
            backup.Dispose();
            backup = null;

            var totalFiles = Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories).Count();
            return new StudioProjectGenerationResponse(
                true,
                blueprint.Name,
                $"generated/{ValidateProjectDirectoryName(blueprint.Name)}",
                Path.GetFileName(generated.SolutionPath),
                generated.ReferenceMode,
                totalFiles,
                preserved,
                compilation.Features.Count,
                compilation.AbpPackages,
                null);
        }
        catch (Exception exception) when (IsExpected(exception))
        {
            if (backup is not null && output is not null)
            {
                try
                {
                    ComposerStudioWorkspace.RestoreConsumerOwnedFiles(output, backup, restorePreviousStudioBlueprint: true);
                }
                catch
                {
                    // Preserve the original generation failure; never replace it with a cleanup exception.
                }
            }

            return new StudioProjectGenerationResponse(
                false,
                request.Name,
                null,
                null,
                null,
                0,
                backup?.ConsumerOwnedFiles.Count ?? 0,
                0,
                [],
                exception.Message);
        }
        finally
        {
            backup?.Dispose();
        }
    }

    private static async Task<GeneratedProjectResult> GenerateCoreAsync(
        StudioBlueprintCompilation compilation,
        string outputDirectory,
        string foundationRoot,
        bool force,
        CancellationToken cancellationToken)
    {
        var generated = await ComposerProjectModelGenerator.GenerateAsync(
            compilation.Analysis,
            new ProjectGenerationOptions(outputDirectory, foundationRoot, force),
            cancellationToken).ConfigureAwait(false);

        generated = ComposerFoundationBinding.FinalizeLocalSourceBinding(
            generated,
            compilation.Blueprint.Name,
            foundationRoot,
            ParseFoundationMode(compilation.Blueprint.FoundationMode));

        await ComposerStudioTypedResourceOverlay.ApplyAsync(compilation, generated, cancellationToken).ConfigureAwait(false);
        await ComposerStudioIntegrityOverlay.ApplyAsync(compilation, generated, cancellationToken).ConfigureAwait(false);
        await ComposerStudioPlatformOverlay.ApplyAsync(compilation, generated, cancellationToken).ConfigureAwait(false);
        await ComposerStudioBusinessUiOverlay.ApplyAsync(compilation, generated, cancellationToken).ConfigureAwait(false);
        await ComposerStudioGeneratedUiFinalizer.ApplyAsync(generated, cancellationToken).ConfigureAwait(false);
        return generated;
    }

    private static async Task PersistStudioMetadataAsync(
        StudioBlueprintCompilation compilation,
        GeneratedProjectResult generated,
        CancellationToken cancellationToken)
    {
        await ComposerStudioWorkspace.WriteStudioBlueprintAsync(
            generated.OutputDirectory,
            compilation.Blueprint,
            cancellationToken).ConfigureAwait(false);
        await ComposerStudioWorkspace.RefreshGeneratedOwnershipAsync(
            generated.OutputDirectory,
            compilation.Blueprint.Name,
            Path.GetFileNameWithoutExtension(generated.SolutionPath),
            generated.ReferenceMode,
            $"studio-{ComposerProjectModelGenerator.GeneratorContractVersion}",
            cancellationToken).ConfigureAwait(false);
    }

    private static StudioProjectBlueprint ToBlueprint(StudioProjectRequest request)
    {
        if (request.SchemaVersion != 1)
            throw new ComposerGenerationException("Project Studio request schemaVersion must be 1.");
        if (request.Modules is null)
            throw new ComposerGenerationException("Project Studio modules are required.");

        var modules = request.Modules.Select(module => new StudioModuleBlueprint(
            module.Name,
            module.Resources.Select(resource => new StudioResourceBlueprint(
                resource.Name,
                resource.Route,
                resource.Fields.Select(field => new StudioFieldBlueprint(
                    field.Name,
                    ParseFieldType(field.Type),
                    field.Required,
                    field.MaximumLength,
                    field.Indexed,
                    field.Unique,
                    field.Filterable,
                    field.Sortable,
                    field.ReferenceResource)).ToArray(),
                resource.Auditing,
                resource.Authorization,
                resource.Idempotency,
                resource.Concurrency)).ToArray())).ToArray();

        return new StudioProjectBlueprint(
            request.SchemaVersion,
            request.Name,
            request.Profile,
            request.FoundationMode,
            request.SelectedFeatures ?? [],
            modules,
            request.ProviderChoices);
    }

    private static StudioFieldType ParseFieldType(string value)
    {
        if (Enum.TryParse<StudioFieldType>(value, ignoreCase: true, out var parsed))
            return parsed;
        throw new ComposerGenerationException($"Unknown Studio field type '{value}'.");
    }

    private static ComposerFoundationBindingMode ParseFoundationMode(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "linked" or "reference" or "project" => ComposerFoundationBindingMode.Linked,
            "standalone" or "source-copy" or "copy" => ComposerFoundationBindingMode.SourceCopy,
            _ => throw new ComposerGenerationException("Studio foundation mode must be 'linked' or 'standalone'.")
        };

    private static StudioResolvedFeatureContract[] BuildResolvedFeatures(
        StudioBlueprintCompilation compilation,
        HashSet<string> selected) =>
        compilation.Features.Select(feature => new StudioResolvedFeatureContract(
            feature.Id,
            feature.DisplayName,
            feature.Category,
            feature.Readiness.ToString(),
            ComposerStudioFeatureCatalog.ResolveProvider(feature, compilation.Blueprint.ProviderChoices),
            selected.Contains(feature.Id),
            feature.Dependencies)).ToArray();

    private static string[] BuildWarnings(StudioBlueprintCompilation compilation)
    {
        var warnings = compilation.Analysis.Warnings.ToList();
        warnings.AddRange(compilation.Features
            .Where(feature => feature.Readiness is StudioFeatureReadiness.Reference or StudioFeatureReadiness.Planned)
            .Select(feature =>
                $"Studio feature '{feature.Id}' is {feature.Readiness}; selection exposes its contract/provider boundary but does not claim a complete production implementation."));
        if (compilation.AbpPackages.Count > 0)
            warnings.Add("ABP OSS packages are generated at version 10.6.0. External stores/transports, secrets and production topology remain explicit consumer configuration.");
        return warnings.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static Dictionary<string, string> EnumerateComparableFiles(
        string root,
        HashSet<string>? exclude)
    {
        if (!Directory.Exists(root))
            return new Dictionary<string, string>(StringComparer.Ordinal);
        return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => new
            {
                Path = path,
                Relative = NormalizePath(Path.GetRelativePath(root, path))
            })
            .Where(item => !IsTransient(item.Relative))
            .Where(item => exclude is null || !exclude.Contains(item.Relative))
            .OrderBy(item => item.Relative, StringComparer.Ordinal)
            .ToDictionary(
                item => item.Relative,
                item => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(item.Path))).ToLowerInvariant(),
                StringComparer.Ordinal);
    }

    private static string ResolveOutputDirectory(string generationRoot, string projectName)
    {
        if (string.IsNullOrWhiteSpace(generationRoot))
            throw new ComposerGenerationException("Project Studio generation root is not configured.");
        var normalizedRoot = Path.GetFullPath(generationRoot);
        Directory.CreateDirectory(normalizedRoot);
        var output = Path.GetFullPath(Path.Combine(normalizedRoot, ValidateProjectDirectoryName(projectName)));
        var relative = Path.GetRelativePath(normalizedRoot, output);
        if (relative.Equals("..", StringComparison.Ordinal) ||
            relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            Path.IsPathRooted(relative))
            throw new ComposerGenerationException("Studio output must remain inside the configured generation root.");
        return output;
    }

    private static string ValidateProjectDirectoryName(string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName) ||
            projectName is "." or ".." ||
            !string.Equals(Path.GetFileName(projectName), projectName, StringComparison.Ordinal) ||
            projectName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ComposerGenerationException("Studio project name cannot be used as a local generation directory.");
        return projectName;
    }

    private static bool IsTransient(string relativePath)
    {
        var segments = NormalizePath(relativePath).Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                                       segment.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                                       segment.Equals(".vs", StringComparison.OrdinalIgnoreCase) ||
                                       segment.Equals("TestResults", StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizePath(string value) => value.Replace('\\', '/');

    private static bool IsExpected(Exception exception) => exception is
        ComposerManifestException or
        ComposerGenerationException or
        KeyNotFoundException or
        InvalidOperationException or
        IOException or
        UnauthorizedAccessException;

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
        }
    }
}