using System.Text.Json;
using System.Text.Json.Serialization;
using FoundationKit.Application.Capabilities;

namespace FoundationKit.Composer;

public sealed record ComposerManifest(
    int SchemaVersion,
    string Name,
    string Profile,
    IReadOnlyList<string> IncludeCapabilities,
    IReadOnlyList<string> ExcludeCapabilities,
    IReadOnlyList<string> Providers,
    IReadOnlyList<CapabilityContractRequirement>? CapabilityContracts = null)
{
    public IReadOnlyList<CapabilityContractRequirement> ContractRequirements =>
        CapabilityContracts ?? Array.Empty<CapabilityContractRequirement>();

    public FoundationKitProjectManifest ToProjectManifest() =>
        new(Name, Profile, IncludeCapabilities, ExcludeCapabilities, Providers, ContractRequirements);
}

public static class ComposerManifestParser
{
    private const int MaxContractVersion = 9999;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static ComposerManifest Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        ManifestDocument document;
        try
        {
            document = JsonSerializer.Deserialize<ManifestDocument>(json, JsonOptions)
                ?? throw new ComposerManifestException("The manifest is empty.");
        }
        catch (JsonException exception)
        {
            throw new ComposerManifestException(
                $"The manifest is not valid FoundationKit JSON: {exception.Message}",
                exception);
        }

        if (document.SchemaVersion != 1)
        {
            throw new ComposerManifestException(
                $"Unsupported manifest schemaVersion '{document.SchemaVersion}'. Expected 1.");
        }

        var name = RequireProjectName(document.Name);
        var profile = RequireValue(document.Profile, "profile");
        var include = NormalizeIds(document.IncludeCapabilities, "includeCapabilities");
        var exclude = NormalizeIds(document.ExcludeCapabilities, "excludeCapabilities");
        var providers = NormalizeIds(document.Providers, "providers");
        var contracts = NormalizeContracts(document.CapabilityContracts);

        if (include.Intersect(exclude, StringComparer.OrdinalIgnoreCase).FirstOrDefault() is { } conflict)
        {
            throw new ComposerManifestException(
                $"Capability '{conflict}' cannot appear in both includeCapabilities and excludeCapabilities.");
        }

        return new ComposerManifest(1, name, profile, include, exclude, providers, contracts);
    }

    public static async Task<ComposerManifest> ParseFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            throw new ComposerManifestException($"Manifest file was not found: {path}");
        }

        var json = await File.ReadAllTextAsync(path, cancellationToken);
        return Parse(json);
    }

    private static string RequireProjectName(string? value)
    {
        var name = RequireValue(value, "name");
        if (name.Length > 100)
        {
            throw new ComposerManifestException("Project name cannot exceed 100 characters.");
        }

        if (!char.IsLetter(name[0]) || name.Any(character =>
                !(char.IsLetterOrDigit(character) || character is '.' or '-' or '_')))
        {
            throw new ComposerManifestException(
                "Project name must start with a letter and contain only letters, digits, '.', '-', or '_'.");
        }

        return name;
    }

    private static string RequireValue(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ComposerManifestException($"Manifest field '{fieldName}' is required.");
        }

        return value.Trim();
    }

    private static IReadOnlyList<string> NormalizeIds(
        IReadOnlyList<string>? values,
        string fieldName)
    {
        if (values is null)
        {
            return Array.Empty<string>();
        }

        var result = new List<string>(values.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ComposerManifestException(
                    $"Manifest field '{fieldName}' cannot contain an empty capability ID.");
            }

            var normalized = value.Trim();
            if (!seen.Add(normalized))
            {
                throw new ComposerManifestException(
                    $"Manifest field '{fieldName}' contains duplicate capability '{normalized}'.");
            }

            result.Add(normalized);
        }

        return result;
    }

    private static IReadOnlyList<CapabilityContractRequirement> NormalizeContracts(
        IReadOnlyDictionary<string, int>? values)
    {
        if (values is null)
        {
            return Array.Empty<CapabilityContractRequirement>();
        }

        var result = new List<CapabilityContractRequirement>(values.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in values)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                throw new ComposerManifestException(
                    "Manifest field 'capabilityContracts' cannot contain an empty capability ID.");
            }

            var capabilityId = pair.Key.Trim();
            if (!seen.Add(capabilityId))
            {
                throw new ComposerManifestException(
                    $"Manifest field 'capabilityContracts' contains duplicate capability '{capabilityId}'.");
            }

            if (pair.Value is <= 0 or > MaxContractVersion)
            {
                throw new ComposerManifestException(
                    $"Capability contract '{capabilityId}' must be an integer from 1 to {MaxContractVersion}.");
            }

            result.Add(new CapabilityContractRequirement(capabilityId, pair.Value));
        }

        return result
            .OrderBy(requirement => requirement.CapabilityId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private sealed record ManifestDocument(
        int SchemaVersion,
        string? Name,
        string? Profile,
        IReadOnlyList<string>? IncludeCapabilities,
        IReadOnlyList<string>? ExcludeCapabilities,
        IReadOnlyList<string>? Providers,
        IReadOnlyDictionary<string, int>? CapabilityContracts);
}

public sealed class ComposerManifestException : Exception
{
    public ComposerManifestException(string message)
        : base(message)
    {
    }

    public ComposerManifestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
