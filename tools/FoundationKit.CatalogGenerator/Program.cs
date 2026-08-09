using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FoundationKit.Application.Capabilities;

namespace FoundationKit.CatalogGenerator;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly JsonSerializerOptions ExportJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static async Task<int> Main(string[] args)
    {
        var arguments = args.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var checkOnly = arguments.Contains("--check");
        var rootArgumentIndex = Array.FindIndex(
            args,
            value => value.Equals("--root", StringComparison.OrdinalIgnoreCase));
        var repositoryRoot = rootArgumentIndex >= 0 && rootArgumentIndex + 1 < args.Length
            ? Path.GetFullPath(args[rootArgumentIndex + 1])
            : FindRepositoryRoot(AppContext.BaseDirectory);

        var catalogPath = Path.Combine(repositoryRoot, "catalog", "foundationkit.catalog.json");
        var capabilityCatalogPath = Path.Combine(
            repositoryRoot,
            "catalog",
            "foundationkit.capabilities.json");
        var maturityEvidencePath = Path.Combine(
            repositoryRoot,
            "catalog",
            "foundationkit.maturity-evidence.json");
        var outputPath = Path.Combine(repositoryRoot, "docs", "FEATURES.md");

        var json = await File.ReadAllTextAsync(catalogPath);
        var catalog = JsonSerializer.Deserialize<CatalogDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("The FoundationKit catalog is empty.");

        Validate(catalog);
        ValidateCapabilityModel();

        var generated = GenerateMarkdown(catalog);
        var generatedCapabilityCatalog = GenerateCapabilityCatalog();
        var generatedMaturityEvidence = GenerateMaturityEvidenceCatalog();

        if (checkOnly)
        {
            var generatedDocumentationMatches = CheckGeneratedFile(
                outputPath,
                generated,
                "docs/FEATURES.md",
                "dotnet run --project tools/FoundationKit.CatalogGenerator");
            var capabilityCatalogMatches = CheckGeneratedFile(
                capabilityCatalogPath,
                generatedCapabilityCatalog,
                "catalog/foundationkit.capabilities.json",
                "dotnet run --project tools/FoundationKit.CatalogGenerator");
            var maturityEvidenceMatches = CheckGeneratedFile(
                maturityEvidencePath,
                generatedMaturityEvidence,
                "catalog/foundationkit.maturity-evidence.json",
                "dotnet run --project tools/FoundationKit.CatalogGenerator");

            if (!generatedDocumentationMatches || !capabilityCatalogMatches || !maturityEvidenceMatches)
            {
                return 1;
            }

            Console.WriteLine(
                "Catalog validation, capability graph validation, maturity evidence validation, and generated-file checks passed.");
            return 0;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllTextAsync(outputPath, generated, new UTF8Encoding(false));
        await File.WriteAllTextAsync(
            capabilityCatalogPath,
            generatedCapabilityCatalog,
            new UTF8Encoding(false));
        await File.WriteAllTextAsync(
            maturityEvidencePath,
            generatedMaturityEvidence,
            new UTF8Encoding(false));

        Console.WriteLine(
            $"Generated {Path.GetRelativePath(repositoryRoot, outputPath)} from the canonical catalog.");
        Console.WriteLine(
            $"Generated {Path.GetRelativePath(repositoryRoot, capabilityCatalogPath)} from the compiled capability model.");
        Console.WriteLine(
            $"Generated {Path.GetRelativePath(repositoryRoot, maturityEvidencePath)} from the compiled maturity evidence model.");
        return 0;
    }

    private static string FindRepositoryRoot(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FoundationKit.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the FoundationKit repository root.");
    }

    private static bool CheckGeneratedFile(
        string path,
        string expected,
        string displayPath,
        string regenerationCommand)
    {
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"Generated file is missing: {displayPath}");
            Console.Error.WriteLine("Run:");
            Console.Error.WriteLine($"  {regenerationCommand}");
            return false;
        }

        var current = File.ReadAllText(path);
        if (string.Equals(Normalize(current), Normalize(expected), StringComparison.Ordinal))
        {
            return true;
        }

        Console.Error.WriteLine($"{displayPath} is out of date. Run:");
        Console.Error.WriteLine($"  {regenerationCommand}");
        return false;
    }

    private static void Validate(CatalogDocument catalog)
    {
        if (catalog.SchemaVersion != 1)
        {
            throw new InvalidOperationException(
                $"Unsupported catalog schema version: {catalog.SchemaVersion}.");
        }

        if (string.IsNullOrWhiteSpace(catalog.CoreVersion))
        {
            throw new InvalidOperationException("coreVersion is required.");
        }

        if (catalog.Packages.Count == 0)
        {
            throw new InvalidOperationException("At least one package is required.");
        }

        var packageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var capabilityIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var package in catalog.Packages)
        {
            if (!packageIds.Add(package.PackageId))
            {
                throw new InvalidOperationException($"Duplicate packageId: {package.PackageId}.");
            }

            if (package.Capabilities.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Package {package.PackageId} has no capabilities.");
            }

            foreach (var capability in package.Capabilities)
            {
                if (!capabilityIds.Add(capability.Id))
                {
                    throw new InvalidOperationException(
                        $"Duplicate capability id: {capability.Id}.");
                }

                if (!capability.Status.Equals("implemented", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Catalog capability {capability.Id} is not implemented. " +
                        "Design intent and future work must not be listed as implemented behavior.");
                }
            }
        }

        var ideaIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var idea in catalog.Ideas)
        {
            if (!ideaIds.Add(idea.Id))
            {
                throw new InvalidOperationException($"Duplicate idea id: {idea.Id}.");
            }

            foreach (var capabilityId in idea.RecommendedCapabilityIds)
            {
                if (!capabilityIds.Contains(capabilityId))
                {
                    throw new InvalidOperationException(
                        $"Idea {idea.Id} references unknown capability {capabilityId}.");
                }
            }
        }
    }

    private static void ValidateCapabilityModel()
    {
        var capabilities = FoundationCapabilityCatalog.All;
        var profiles = FoundationCapabilityProfiles.All;
        var capabilityIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var capability in capabilities)
        {
            if (string.IsNullOrWhiteSpace(capability.Id))
            {
                throw new InvalidOperationException("Capability IDs cannot be empty.");
            }

            if (!capabilityIds.Add(capability.Id))
            {
                throw new InvalidOperationException(
                    $"Duplicate capability-model ID: {capability.Id}.");
            }
        }

        var contracts = FoundationCapabilityContracts.All;
        if (contracts.Count != capabilities.Count)
        {
            throw new InvalidOperationException(
                "Capability contract metadata must cover every capability exactly once.");
        }

        var contractIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var contract in contracts)
        {
            if (!capabilityIds.Contains(contract.CapabilityId))
            {
                throw new InvalidOperationException(
                    $"Capability contract references unknown capability '{contract.CapabilityId}'.");
            }

            if (!contractIds.Add(contract.CapabilityId))
            {
                throw new InvalidOperationException(
                    $"Duplicate capability contract metadata: {contract.CapabilityId}.");
            }

            if (contract.ContractVersion <= 0)
            {
                throw new InvalidOperationException(
                    $"Capability contract '{contract.CapabilityId}' must have a positive version.");
            }
        }

        CapabilityMaturityEvidencePolicy.EnsureCatalogValid(
            capabilities,
            FoundationCapabilityMaturityEvidence.All);

        var resolver = CapabilityResolver.CreateDefault();
        foreach (var capability in capabilities)
        {
            _ = resolver.Resolve([capability.Id]);
        }

        var profileIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var profile in profiles)
        {
            if (!profileIds.Add(profile.Id))
            {
                throw new InvalidOperationException(
                    $"Duplicate capability profile ID: {profile.Id}.");
            }

            _ = resolver.Resolve(profile.CapabilityIds);
        }
    }

    private static string GenerateCapabilityCatalog()
    {
        var document = new CapabilityCatalogExport(
            1,
            FoundationCapabilityCatalog.All,
            FoundationCapabilityContracts.All,
            FoundationCapabilityProfiles.All);
        return JsonSerializer.Serialize(document, ExportJsonOptions) + Environment.NewLine;
    }

    private static string GenerateMaturityEvidenceCatalog()
    {
        var document = new CapabilityMaturityEvidenceExport(
            1,
            FoundationCapabilityMaturityEvidence.All);
        return JsonSerializer.Serialize(document, ExportJsonOptions) + Environment.NewLine;
    }

    private static string GenerateMarkdown(CatalogDocument catalog)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# FoundationKit Capabilities");
        builder.AppendLine();
        builder.AppendLine(
            "> Generated from `catalog/foundationkit.catalog.json`. Do not edit this file manually.");
        builder.AppendLine();
        builder.AppendLine(FormattableString.Invariant($"Core version: `{catalog.CoreVersion}`"));
        builder.AppendLine();
        builder.AppendLine(
            "Only implemented behavior is listed below. Product-specific concerns and future " +
            "recommendations remain outside the reusable core.");
        builder.AppendLine();

        foreach (var package in catalog.Packages)
        {
            builder.AppendLine(FormattableString.Invariant($"## {package.PackageId}"));
            builder.AppendLine();
            builder.AppendLine(package.SummaryEn);
            builder.AppendLine();

            foreach (var capability in package.Capabilities)
            {
                builder.AppendLine(FormattableString.Invariant($"### {capability.TitleEn}"));
                builder.AppendLine();
                builder.AppendLine(capability.DescriptionEn);
                builder.AppendLine();
                builder.AppendLine(FormattableString.Invariant(
                    $"Public surface: {string.Join(", ", capability.PublicTypes.Select(type => $"`{type}`"))}"));
                builder.AppendLine();
            }
        }

        builder.AppendLine("## Project ideas");
        builder.AppendLine();
        foreach (var idea in catalog.Ideas)
        {
            builder.AppendLine(FormattableString.Invariant(
                $"- **{idea.TitleEn}** — {idea.DescriptionEn}"));
        }

        builder.AppendLine();
        builder.AppendLine("## Keeping the catalog current");
        builder.AppendLine();
        builder.AppendLine("When an implemented public capability changes:");
        builder.AppendLine();
        builder.AppendLine("1. update the code and tests;");
        builder.AppendLine("2. update `catalog/foundationkit.catalog.json`;");
        builder.AppendLine(
            "3. run `dotnet run --project tools/FoundationKit.CatalogGenerator`;");
        builder.AppendLine("4. update `CHANGELOG.md`.");
        builder.AppendLine();

        return builder.ToString();
    }

    private static string Normalize(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd() + "\n";
}

internal sealed record CapabilityCatalogExport(
    int SchemaVersion,
    IReadOnlyList<CapabilityDescriptor> Capabilities,
    IReadOnlyList<CapabilityContractDescriptor> Contracts,
    IReadOnlyList<CapabilityProfile> Profiles);

internal sealed record CapabilityMaturityEvidenceExport(
    int SchemaVersion,
    IReadOnlyList<CapabilityMaturityEvidenceDescriptor> Evidence);

internal sealed record CatalogDocument(
    int SchemaVersion,
    string CoreVersion,
    DateTimeOffset UpdatedUtc,
    Contact Contact,
    IReadOnlyList<CatalogPackage> Packages,
    IReadOnlyList<ProjectIdea> Ideas,
    IReadOnlyList<AdoptionStep> AdoptionSteps);

internal sealed record Contact(
    string Name,
    string GithubProfile,
    string Repository,
    string NewIssue);

internal sealed record CatalogPackage(
    string Id,
    string PackageId,
    string TitleAr,
    string TitleEn,
    string SummaryAr,
    string SummaryEn,
    IReadOnlyList<Capability> Capabilities);

internal sealed record Capability(
    string Id,
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    string Status,
    IReadOnlyList<string> PublicTypes);

internal sealed record ProjectIdea(
    string Id,
    string Icon,
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    IReadOnlyList<string> RecommendedCapabilityIds,
    IReadOnlyList<string> ProductDecisions);

internal sealed record AdoptionStep(
    int Number,
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    string? Command);
