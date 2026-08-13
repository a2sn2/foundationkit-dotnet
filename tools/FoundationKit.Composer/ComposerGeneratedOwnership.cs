using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FoundationKit.Composer;

internal static class ComposerGeneratedOwnership
{
    private const string MarkerFile = ".foundationkit-generated.json";

    private static readonly HashSet<string> TransientDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin",
        "obj",
        ".vs",
        "TestResults"
    };

    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true
    };

    public static string BuildMarker(
        string productName,
        string projectPrefix,
        string referenceMode,
        IReadOnlyDictionary<string, string> files,
        string? generatorContractVersion = null)
    {
        ArgumentNullException.ThrowIfNull(files);

        var ownedFiles = files
            .Where(file => !file.Key.Equals(MarkerFile, StringComparison.Ordinal))
            .OrderBy(file => file.Key, StringComparer.Ordinal)
            .ToArray();
        var generatedFiles = ownedFiles
            .Select(file => file.Key)
            .Append(MarkerFile)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var contentSha256 = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (var file in ownedFiles)
            contentSha256[file.Key] = HashText(file.Value);

        var marker = new
        {
            schemaVersion = 1,
            generator = "FoundationKit.Composer",
            generatorContractVersion = generatorContractVersion ?? ComposerProjectGenerator.GeneratorContractVersion,
            productName,
            projectPrefix,
            referenceMode,
            generatedFiles,
            contentSha256
        };

        return JsonSerializer.Serialize(marker, IndentedJsonOptions);
    }

    public static void ValidateUnchangedGeneratedDirectory(string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        var ownership = ReadMarker(outputDirectory);
        var generatedFiles = ownership.GeneratedFiles.ToHashSet(StringComparer.Ordinal);
        var actualFiles = Directory
            .EnumerateFiles(outputDirectory, "*", SearchOption.AllDirectories)
            .Select(path => NormalizeRelativePath(Path.GetRelativePath(outputDirectory, path)))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var comparableFiles = actualFiles
            .Where(path => generatedFiles.Contains(path) || !IsTransientGeneratedArtifact(path))
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (!ownership.GeneratedFiles.SequenceEqual(comparableFiles, StringComparer.Ordinal))
        {
            throw new ComposerGenerationException(
                "Refusing to force-regenerate because the destination contains files that are not part of the previous FoundationKit generation set.");
        }

        foreach (var expectedHash in ownership.ContentSha256)
        {
            var path = Path.Combine(
                outputDirectory,
                expectedHash.Key.Replace('/', Path.DirectorySeparatorChar));
            var actualHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(expectedHash.Value),
                    Encoding.ASCII.GetBytes(actualHash)))
            {
                throw new ComposerGenerationException(
                    $"Refusing to force-regenerate because generated file '{expectedHash.Key}' was modified after generation.");
            }
        }

        DeleteTransientGeneratedArtifacts(outputDirectory, actualFiles, generatedFiles);
    }

    private static GeneratedOwnership ReadMarker(string outputDirectory)
    {
        var markerPath = Path.Combine(outputDirectory, MarkerFile);
        if (!File.Exists(markerPath))
        {
            throw new ComposerGenerationException(
                "Refusing to force-regenerate a non-empty directory without the FoundationKit generated marker.");
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(markerPath));
            var root = document.RootElement;
            if (!root.TryGetProperty("schemaVersion", out var schemaVersion) || schemaVersion.GetInt32() != 1 ||
                !root.TryGetProperty("generator", out var generator) ||
                !string.Equals(generator.GetString(), "FoundationKit.Composer", StringComparison.Ordinal) ||
                !root.TryGetProperty("generatedFiles", out var generatedFilesElement) ||
                generatedFilesElement.ValueKind != JsonValueKind.Array ||
                !root.TryGetProperty("contentSha256", out var hashesElement) ||
                hashesElement.ValueKind != JsonValueKind.Object)
            {
                throw new ComposerGenerationException("The FoundationKit generated marker is invalid.");
            }

            var generatedFiles = generatedFilesElement
                .EnumerateArray()
                .Select(item => item.GetString())
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path!)
                .Order(StringComparer.Ordinal)
                .ToArray();

            if (generatedFiles.Length == 0 ||
                !generatedFiles.Contains(MarkerFile, StringComparer.Ordinal) ||
                generatedFiles.Any(IsUnsafeRelativePath))
            {
                throw new ComposerGenerationException("The FoundationKit generated marker contains an unsafe file set.");
            }

            var hashes = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var property in hashesElement.EnumerateObject())
            {
                if (IsUnsafeRelativePath(property.Name) ||
                    property.Name.Equals(MarkerFile, StringComparison.Ordinal) ||
                    property.Value.ValueKind != JsonValueKind.String)
                {
                    throw new ComposerGenerationException("The FoundationKit generated marker contains unsafe hash metadata.");
                }

                var hash = property.Value.GetString();
                if (hash is null || hash.Length != 64 || !hash.All(IsLowerHexDigit))
                    throw new ComposerGenerationException("The FoundationKit generated marker contains an invalid content hash.");

                hashes[property.Name] = hash;
            }

            var expectedHashPaths = generatedFiles
                .Where(path => !path.Equals(MarkerFile, StringComparison.Ordinal))
                .Order(StringComparer.Ordinal)
                .ToArray();
            if (!expectedHashPaths.SequenceEqual(hashes.Keys, StringComparer.Ordinal))
            {
                throw new ComposerGenerationException(
                    "The FoundationKit generated marker does not contain a complete hash set for generated files.");
            }

            return new GeneratedOwnership(generatedFiles, hashes);
        }
        catch (JsonException exception)
        {
            throw new ComposerGenerationException("The FoundationKit generated marker is not valid JSON.", exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new ComposerGenerationException("The FoundationKit generated marker is invalid.", exception);
        }
    }

    private static string HashText(string value)
    {
        var normalized = NormalizeLineEndings(value);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .TrimEnd() + "\n";

    private static string NormalizeRelativePath(string value) => value.Replace('\\', '/');

    private static bool IsTransientGeneratedArtifact(string path)
    {
        var normalized = NormalizeRelativePath(path);
        return normalized.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Any(TransientDirectoryNames.Contains);
    }

    private static void DeleteTransientGeneratedArtifacts(
        string outputDirectory,
        IEnumerable<string> actualFiles,
        IReadOnlySet<string> generatedFiles)
    {
        foreach (var relativePath in actualFiles.Where(path =>
                     !generatedFiles.Contains(path) && IsTransientGeneratedArtifact(path)))
        {
            var path = Path.Combine(
                outputDirectory,
                relativePath.Replace('/', Path.DirectorySeparatorChar));
            File.Delete(path);
        }
    }

    private static bool IsUnsafeRelativePath(string path)
    {
        if (Path.IsPathRooted(path))
            return true;

        var normalized = NormalizeRelativePath(path);
        return normalized.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Any(segment => segment == "..");
    }

    private static bool IsLowerHexDigit(char value) =>
        value is >= '0' and <= '9' or >= 'a' and <= 'f';

    private sealed record GeneratedOwnership(
        string[] GeneratedFiles,
        SortedDictionary<string, string> ContentSha256);
}
