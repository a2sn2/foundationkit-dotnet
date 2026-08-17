using System.Text;

namespace FoundationKit.Composer;

public sealed record StudioWorkspaceBackup(
    string BackupDirectory,
    IReadOnlyList<string> ConsumerOwnedFiles,
    bool HadStudioBlueprint) : IAsyncDisposable, IDisposable
{
    public void Dispose()
    {
        if (Directory.Exists(BackupDirectory))
            Directory.Delete(BackupDirectory, recursive: true);
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}

public static class ComposerStudioWorkspace
{
    public const string StudioBlueprintFile = "foundationkit.studio.json";
    private const string MarkerFile = ".foundationkit-generated.json";

    public static StudioWorkspaceBackup CaptureConsumerOwnedFiles(string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        var fullOutput = Path.GetFullPath(outputDirectory);
        var backupDirectory = Path.Combine(Path.GetTempPath(), $"foundationkit-studio-{Guid.NewGuid():N}");
        Directory.CreateDirectory(backupDirectory);

        if (!Directory.Exists(fullOutput))
            return new StudioWorkspaceBackup(backupDirectory, [], false);

        var files = Directory
            .EnumerateFiles(fullOutput, "*", SearchOption.AllDirectories)
            .Select(path => new
            {
                FullPath = path,
                RelativePath = NormalizePath(Path.GetRelativePath(fullOutput, path))
            })
            .Where(item => IsConsumerOwned(item.RelativePath) ||
                           item.RelativePath.Equals(StudioBlueprintFile, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.RelativePath, StringComparer.Ordinal)
            .ToArray();

        var consumerFiles = new List<string>();
        var hadStudioBlueprint = false;
        foreach (var file in files)
        {
            if (file.RelativePath.Equals(StudioBlueprintFile, StringComparison.OrdinalIgnoreCase))
                hadStudioBlueprint = true;
            else
                consumerFiles.Add(file.RelativePath);

            var backupPath = Path.Combine(backupDirectory, ToPlatformPath(file.RelativePath));
            var parent = Path.GetDirectoryName(backupPath);
            if (!string.IsNullOrWhiteSpace(parent))
                Directory.CreateDirectory(parent);
            File.Move(file.FullPath, backupPath, overwrite: true);
        }

        DeleteEmptyDirectories(fullOutput);
        return new StudioWorkspaceBackup(backupDirectory, consumerFiles, hadStudioBlueprint);
    }

    public static void RestoreConsumerOwnedFiles(
        string outputDirectory,
        StudioWorkspaceBackup backup,
        bool restorePreviousStudioBlueprint = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentNullException.ThrowIfNull(backup);
        Directory.CreateDirectory(outputDirectory);

        foreach (var relativePath in backup.ConsumerOwnedFiles)
        {
            var source = Path.Combine(backup.BackupDirectory, ToPlatformPath(relativePath));
            if (!File.Exists(source))
                continue;
            var destination = Path.Combine(outputDirectory, ToPlatformPath(relativePath));
            if (File.Exists(destination))
                throw new ComposerGenerationException(
                    $"Studio regeneration would overwrite consumer-owned file '{relativePath}'.");
            var parent = Path.GetDirectoryName(destination);
            if (!string.IsNullOrWhiteSpace(parent))
                Directory.CreateDirectory(parent);
            File.Move(source, destination);
        }

        if (restorePreviousStudioBlueprint && backup.HadStudioBlueprint)
        {
            var source = Path.Combine(backup.BackupDirectory, StudioBlueprintFile);
            var destination = Path.Combine(outputDirectory, StudioBlueprintFile);
            if (File.Exists(source) && !File.Exists(destination))
                File.Move(source, destination);
        }
    }

    public static async Task WriteStudioBlueprintAsync(
        string outputDirectory,
        StudioProjectBlueprint blueprint,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentNullException.ThrowIfNull(blueprint);
        Directory.CreateDirectory(outputDirectory);
        var path = Path.Combine(outputDirectory, StudioBlueprintFile);
        await File.WriteAllTextAsync(
            path,
            NormalizeText(ComposerStudioBlueprintCompiler.Serialize(blueprint)),
            new UTF8Encoding(false),
            cancellationToken).ConfigureAwait(false);
    }

    public static async Task RefreshGeneratedOwnershipAsync(
        string outputDirectory,
        string productName,
        string projectPrefix,
        string referenceMode,
        string generatorContractVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceMode);

        var files = Directory
            .EnumerateFiles(outputDirectory, "*", SearchOption.AllDirectories)
            .Select(path => new
            {
                Path = path,
                RelativePath = NormalizePath(Path.GetRelativePath(outputDirectory, path))
            })
            .Where(item => !item.RelativePath.Equals(MarkerFile, StringComparison.Ordinal) &&
                           !item.RelativePath.Equals(StudioBlueprintFile, StringComparison.OrdinalIgnoreCase) &&
                           !IsConsumerOwned(item.RelativePath) &&
                           !IsTransient(item.RelativePath))
            .OrderBy(item => item.RelativePath, StringComparer.Ordinal)
            .ToDictionary(
                item => item.RelativePath,
                item => File.ReadAllText(item.Path),
                StringComparer.Ordinal);

        var marker = ComposerGeneratedOwnership.BuildMarker(
            productName,
            projectPrefix,
            referenceMode,
            files,
            generatorContractVersion);
        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, MarkerFile),
            NormalizeText(marker),
            new UTF8Encoding(false),
            cancellationToken).ConfigureAwait(false);
    }

    public static IReadOnlyList<string> ListConsumerOwnedFiles(string outputDirectory)
    {
        if (!Directory.Exists(outputDirectory))
            return [];
        return Directory
            .EnumerateFiles(outputDirectory, "*", SearchOption.AllDirectories)
            .Select(path => NormalizePath(Path.GetRelativePath(outputDirectory, path)))
            .Where(IsConsumerOwned)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    public static bool IsConsumerOwned(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return false;
        var segments = NormalizePath(relativePath)
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return segments.Any(segment => segment.Equals("Custom", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsTransient(string relativePath)
    {
        var transient = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", ".vs", "TestResults"
        };
        return NormalizePath(relativePath)
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Any(transient.Contains);
    }

    private static void DeleteEmptyDirectories(string root)
    {
        if (!Directory.Exists(root))
            return;
        foreach (var directory in Directory
                     .EnumerateDirectories(root, "*", SearchOption.AllDirectories)
                     .OrderByDescending(path => path.Length))
        {
            if (!Directory.EnumerateFileSystemEntries(directory).Any())
                Directory.Delete(directory);
        }
    }

    private static string NormalizeText(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .TrimEnd() + "\n";

    private static string NormalizePath(string value) => value.Replace('\\', '/');
    private static string ToPlatformPath(string value) => value.Replace('/', Path.DirectorySeparatorChar);
}
