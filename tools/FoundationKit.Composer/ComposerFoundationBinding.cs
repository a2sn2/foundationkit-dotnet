using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace FoundationKit.Composer;

public enum ComposerFoundationBindingMode
{
    Linked,
    SourceCopy
}

/// <summary>
/// Finalizes a generated Composer workspace for local FoundationKit source consumption.
/// Linked mode keeps references to the canonical local FoundationKit tree and makes the
/// complete dependency closure visible to the generated solution. SourceCopy mode vendors
/// that same required project closure into the generated workspace and rewrites product
/// references so the result is self-contained.
/// </summary>
public static class ComposerFoundationBinding
{
    private const string MarkerFile = ".foundationkit-generated.json";
    private const string CSharpProjectTypeGuid = "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}";

    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly string[] FoundationRootSupportFiles =
    [
        "Directory.Build.props",
        "Directory.Build.targets",
        "Directory.Packages.props",
        "global.json",
        "NuGet.Config",
        "nuget.config"
    ];

    public static GeneratedProjectResult FinalizeLocalSourceBinding(
        GeneratedProjectResult result,
        string productName,
        string foundationRoot,
        ComposerFoundationBindingMode mode)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);
        ArgumentException.ThrowIfNullOrWhiteSpace(foundationRoot);

        var outputDirectory = Path.GetFullPath(result.OutputDirectory);
        var normalizedFoundationRoot = Path.GetFullPath(foundationRoot);
        var solutionPath = Path.GetFullPath(result.SolutionPath);

        if (!Directory.Exists(outputDirectory))
            throw new ComposerGenerationException($"Generated output directory was not found: {outputDirectory}");
        if (!File.Exists(solutionPath))
            throw new ComposerGenerationException($"Generated solution was not found: {solutionPath}");

        var generatedProjects = Directory
            .EnumerateFiles(outputDirectory, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !IsUnderDirectory(path, Path.Combine(outputDirectory, "foundation")))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var directFoundationProjects = DiscoverExternalFoundationReferences(
            generatedProjects,
            normalizedFoundationRoot);
        var foundationProjects = DiscoverFoundationProjectClosure(
            directFoundationProjects,
            normalizedFoundationRoot);

        IReadOnlyList<FoundationProject> solutionFoundationProjects;
        string referenceMode;

        if (mode == ComposerFoundationBindingMode.SourceCopy)
        {
            var vendoredRoot = Path.Combine(outputDirectory, "foundation");
            CopyFoundationRootSupportFiles(normalizedFoundationRoot, vendoredRoot);
            CopyFoundationProjects(foundationProjects, normalizedFoundationRoot, vendoredRoot);
            RewriteGeneratedProjectReferencesToVendoredCopy(
                generatedProjects,
                normalizedFoundationRoot,
                vendoredRoot);

            solutionFoundationProjects = foundationProjects
                .Select(project => project with
                {
                    ProjectPath = Path.Combine(
                        vendoredRoot,
                        Path.GetRelativePath(normalizedFoundationRoot, project.ProjectPath))
                })
                .ToArray();

            VerifyStandaloneProjectReferencesStayInsideOutput(outputDirectory);
            referenceMode = "source-copy";
        }
        else
        {
            solutionFoundationProjects = foundationProjects;
            referenceMode = "project";
        }

        File.WriteAllText(
            solutionPath,
            BuildSolutionWithFoundationProjects(
                File.ReadAllText(solutionPath),
                outputDirectory,
                solutionFoundationProjects),
            new UTF8Encoding(false));

        WriteBindingEvidence(
            outputDirectory,
            productName,
            referenceMode,
            foundationProjects);
        RefreshOwnershipMarker(
            outputDirectory,
            productName,
            Path.GetFileNameWithoutExtension(solutionPath),
            referenceMode,
            ComposerProjectModelGenerator.GeneratorContractVersion);

        var generatedFiles = Directory
            .EnumerateFiles(outputDirectory, "*", SearchOption.AllDirectories)
            .Select(path => NormalizeRelativePath(Path.GetRelativePath(outputDirectory, path)))
            .Order(StringComparer.Ordinal)
            .ToArray();

        return new GeneratedProjectResult(
            outputDirectory,
            solutionPath,
            referenceMode,
            generatedFiles);
    }

    private static FoundationProject[] DiscoverExternalFoundationReferences(
        IEnumerable<string> generatedProjects,
        string foundationRoot)
    {
        var projects = new SortedDictionary<string, FoundationProject>(StringComparer.OrdinalIgnoreCase);
        foreach (var generatedProject in generatedProjects)
        {
            foreach (var reference in ReadProjectReferences(generatedProject))
            {
                var target = ResolveProjectReference(generatedProject, reference);
                if (!IsUnderDirectory(target, foundationRoot))
                    continue;

                ValidateFoundationProjectPath(target, foundationRoot);
                projects[target] = CreateFoundationProject(target, foundationRoot);
            }
        }

        return projects.Values.ToArray();
    }

    private static FoundationProject[] DiscoverFoundationProjectClosure(
        IEnumerable<FoundationProject> roots,
        string foundationRoot)
    {
        var discovered = new SortedDictionary<string, FoundationProject>(StringComparer.OrdinalIgnoreCase);
        var pending = new Queue<FoundationProject>(roots.OrderBy(item => item.RelativePath, StringComparer.OrdinalIgnoreCase));

        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            if (discovered.ContainsKey(current.ProjectPath))
                continue;

            ValidateFoundationProjectPath(current.ProjectPath, foundationRoot);
            discovered[current.ProjectPath] = current;

            foreach (var reference in ReadProjectReferences(current.ProjectPath))
            {
                var target = ResolveProjectReference(current.ProjectPath, reference);
                if (!IsUnderDirectory(target, foundationRoot))
                {
                    throw new ComposerGenerationException(
                        $"Foundation project '{current.Name}' references a project outside the FoundationKit root: {target}");
                }

                ValidateFoundationProjectPath(target, foundationRoot);
                if (!discovered.ContainsKey(target))
                    pending.Enqueue(CreateFoundationProject(target, foundationRoot));
            }
        }

        return discovered.Values
            .OrderBy(item => item.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string[] ReadProjectReferences(string projectPath)
    {
        XDocument document;
        try
        {
            document = XDocument.Load(projectPath, LoadOptions.PreserveWhitespace);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            throw new ComposerGenerationException($"Could not read project references from '{projectPath}'.", exception);
        }

        return document
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => element.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();
    }

    private static string ResolveProjectReference(string sourceProjectPath, string include)
    {
        if (include.Contains("$(", StringComparison.Ordinal))
        {
            throw new ComposerGenerationException(
                $"Composer cannot safely resolve an MSBuild-variable ProjectReference '{include}' in '{sourceProjectPath}'.");
        }

        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceProjectPath)!, ToPlatformPath(include)));
    }

    private static FoundationProject CreateFoundationProject(string projectPath, string foundationRoot)
    {
        var name = Path.GetFileNameWithoutExtension(projectPath);
        var relativePath = NormalizeRelativePath(Path.GetRelativePath(foundationRoot, projectPath));
        return new FoundationProject(name, Path.GetFullPath(projectPath), relativePath);
    }

    private static void ValidateFoundationProjectPath(string projectPath, string foundationRoot)
    {
        if (!File.Exists(projectPath))
            throw new ComposerGenerationException($"Referenced FoundationKit project was not found: {projectPath}");
        if (!projectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            throw new ComposerGenerationException($"FoundationKit ProjectReference is not a C# project: {projectPath}");
        if (!IsUnderDirectory(projectPath, foundationRoot))
            throw new ComposerGenerationException($"FoundationKit ProjectReference escapes the configured root: {projectPath}");
    }

    private static void CopyFoundationRootSupportFiles(string foundationRoot, string vendoredRoot)
    {
        Directory.CreateDirectory(vendoredRoot);
        foreach (var fileName in FoundationRootSupportFiles)
        {
            var source = Path.Combine(foundationRoot, fileName);
            if (!File.Exists(source))
                continue;

            File.Copy(source, Path.Combine(vendoredRoot, fileName), overwrite: true);
        }
    }

    private static void CopyFoundationProjects(
        IEnumerable<FoundationProject> projects,
        string foundationRoot,
        string vendoredRoot)
    {
        foreach (var project in projects)
        {
            var sourceDirectory = Path.GetDirectoryName(project.ProjectPath)!;
            var relativeDirectory = Path.GetRelativePath(foundationRoot, sourceDirectory);
            var destinationDirectory = Path.Combine(vendoredRoot, relativeDirectory);
            CopyDirectory(sourceDirectory, destinationDirectory);
        }
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        var pending = new Stack<(DirectoryInfo Source, string Destination)>();
        pending.Push((new DirectoryInfo(sourceDirectory), destinationDirectory));

        while (pending.Count > 0)
        {
            var (source, destination) = pending.Pop();
            Directory.CreateDirectory(destination);

            foreach (var file in source.EnumerateFiles().OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
            {
                if ((file.Attributes & FileAttributes.ReparsePoint) != 0)
                    continue;
                File.Copy(file.FullName, Path.Combine(destination, file.Name), overwrite: true);
            }

            foreach (var directory in source.EnumerateDirectories().OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
            {
                if ((directory.Attributes & FileAttributes.ReparsePoint) != 0 || IsIgnoredBuildDirectory(directory.Name))
                    continue;
                pending.Push((directory, Path.Combine(destination, directory.Name)));
            }
        }
    }

    private static bool IsIgnoredBuildDirectory(string name) =>
        name.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
        name.Equals(".vs", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("TestResults", StringComparison.OrdinalIgnoreCase);

    private static void RewriteGeneratedProjectReferencesToVendoredCopy(
        IEnumerable<string> generatedProjects,
        string foundationRoot,
        string vendoredRoot)
    {
        foreach (var generatedProject in generatedProjects)
        {
            var document = XDocument.Load(generatedProject, LoadOptions.PreserveWhitespace);
            var changed = false;

            foreach (var reference in document.Descendants().Where(element => element.Name.LocalName == "ProjectReference"))
            {
                var include = reference.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "Include");
                if (include is null || string.IsNullOrWhiteSpace(include.Value))
                    continue;

                var target = ResolveProjectReference(generatedProject, include.Value);
                if (!IsUnderDirectory(target, foundationRoot))
                    continue;

                var vendoredTarget = Path.Combine(vendoredRoot, Path.GetRelativePath(foundationRoot, target));
                var relative = Path.GetRelativePath(Path.GetDirectoryName(generatedProject)!, vendoredTarget);
                include.Value = NormalizeRelativePath(relative);
                changed = true;
            }

            if (changed)
            {
                File.WriteAllText(
                    generatedProject,
                    NormalizeLineEndings(document.ToString(SaveOptions.DisableFormatting)),
                    new UTF8Encoding(false));
            }
        }
    }

    private static void VerifyStandaloneProjectReferencesStayInsideOutput(string outputDirectory)
    {
        foreach (var project in Directory.EnumerateFiles(outputDirectory, "*.csproj", SearchOption.AllDirectories))
        {
            foreach (var reference in ReadProjectReferences(project))
            {
                var target = ResolveProjectReference(project, reference);
                if (!IsUnderDirectory(target, outputDirectory))
                {
                    throw new ComposerGenerationException(
                        $"Standalone source-copy generation left a ProjectReference outside the generated workspace: {target}");
                }
            }
        }
    }

    private static string BuildSolutionWithFoundationProjects(
        string solution,
        string outputDirectory,
        IReadOnlyList<FoundationProject> foundationProjects)
    {
        if (foundationProjects.Count == 0)
            return NormalizeLineEndings(solution);

        var normalized = NormalizeLineEndings(solution);
        var globalIndex = normalized.IndexOf("Global\n", StringComparison.Ordinal);
        if (globalIndex < 0)
            throw new ComposerGenerationException("Generated solution has an unexpected shape: Global section was not found.");

        var projectEntries = new StringBuilder();
        foreach (var project in foundationProjects.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            var relative = NormalizeRelativePath(Path.GetRelativePath(outputDirectory, project.ProjectPath)).Replace('/', '\\');
            var guid = DeterministicGuid($"foundation:{project.Name}").ToString("B").ToUpperInvariant();
            projectEntries
                .Append("Project(\"").Append(CSharpProjectTypeGuid).Append("\") = \"")
                .Append(project.Name).Append("\", \"").Append(relative)
                .Append("\", \"").Append(guid).AppendLine("\"");
            projectEntries.AppendLine("EndProject");
        }
        normalized = normalized.Insert(globalIndex, projectEntries.ToString());

        const string configurationHeader = "\tGlobalSection(ProjectConfigurationPlatforms) = postSolution\n";
        var configurationIndex = normalized.IndexOf(configurationHeader, StringComparison.Ordinal);
        if (configurationIndex < 0)
        {
            throw new ComposerGenerationException(
                "Generated solution has an unexpected shape: project configuration section was not found.");
        }

        var configurationEnd = normalized.IndexOf(
            "\tEndGlobalSection\n",
            configurationIndex + configurationHeader.Length,
            StringComparison.Ordinal);
        if (configurationEnd < 0)
        {
            throw new ComposerGenerationException(
                "Generated solution has an unexpected shape: project configuration terminator was not found.");
        }

        var configurations = new StringBuilder();
        foreach (var project in foundationProjects.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            var guid = DeterministicGuid($"foundation:{project.Name}").ToString("B").ToUpperInvariant();
            configurations.Append("\t\t").Append(guid).AppendLine(".Debug|Any CPU.ActiveCfg = Debug|Any CPU");
            configurations.Append("\t\t").Append(guid).AppendLine(".Debug|Any CPU.Build.0 = Debug|Any CPU");
            configurations.Append("\t\t").Append(guid).AppendLine(".Release|Any CPU.ActiveCfg = Release|Any CPU");
            configurations.Append("\t\t").Append(guid).AppendLine(".Release|Any CPU.Build.0 = Release|Any CPU");
        }
        normalized = normalized.Insert(configurationEnd, configurations.ToString());
        return normalized;
    }

    private static void WriteBindingEvidence(
        string outputDirectory,
        string productName,
        string referenceMode,
        IReadOnlyList<FoundationProject> projects)
    {
        var description = referenceMode == "source-copy"
            ? "Required FoundationKit source projects were copied into `foundation/` inside this generated workspace. Product and FoundationKit ProjectReferences remain inside this directory, so the generated solution is portable without the parent FoundationKit repository."
            : "Product projects reference the canonical local FoundationKit source tree. The generated solution also includes the complete referenced FoundationKit project dependency closure so Visual Studio can restore and build the solution directly.";

        var builder = new StringBuilder();
        builder.Append("# Foundation binding — ").AppendLine(productName);
        builder.AppendLine();
        builder.Append("- Mode: `").Append(referenceMode).AppendLine("`");
        builder.Append("- Foundation projects in dependency closure: ").AppendLine(projects.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.AppendLine();
        builder.AppendLine(description);
        builder.AppendLine();
        builder.AppendLine("## Included FoundationKit projects");
        builder.AppendLine();
        foreach (var project in projects.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
            builder.Append("- `").Append(project.Name).Append("` — `").Append(project.RelativePath).AppendLine("`");

        File.WriteAllText(
            Path.Combine(outputDirectory, "FOUNDATION-BINDING.md"),
            NormalizeLineEndings(builder.ToString()),
            new UTF8Encoding(false));

        var readmePath = Path.Combine(outputDirectory, "README.md");
        var readme = File.Exists(readmePath) ? File.ReadAllText(readmePath) : $"# {productName}\n";
        var bindingSection = $$"""

        ## Foundation binding

        - Mode: `{{referenceMode}}`
        - Dependency closure: `{{projects.Count}}` FoundationKit project(s)
        - Details: [`FOUNDATION-BINDING.md`](FOUNDATION-BINDING.md)
        """;
        File.WriteAllText(
            readmePath,
            NormalizeLineEndings(readme.TrimEnd() + bindingSection),
            new UTF8Encoding(false));
    }

    private static void RefreshOwnershipMarker(
        string outputDirectory,
        string productName,
        string projectPrefix,
        string referenceMode,
        string generatorContractVersion)
    {
        var files = Directory
            .EnumerateFiles(outputDirectory, "*", SearchOption.AllDirectories)
            .Where(path => !Path.GetFileName(path).Equals(MarkerFile, StringComparison.Ordinal))
            .Select(path => new
            {
                RelativePath = NormalizeRelativePath(Path.GetRelativePath(outputDirectory, path)),
                FullPath = path
            })
            .OrderBy(item => item.RelativePath, StringComparer.Ordinal)
            .ToArray();

        var generatedFiles = files
            .Select(item => item.RelativePath)
            .Append(MarkerFile)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var contentSha256 = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            contentSha256[file.RelativePath] = Convert
                .ToHexString(SHA256.HashData(File.ReadAllBytes(file.FullPath)))
                .ToLowerInvariant();
        }

        var marker = new
        {
            schemaVersion = 1,
            generator = "FoundationKit.Composer",
            generatorContractVersion,
            productName,
            projectPrefix,
            referenceMode,
            generatedFiles,
            contentSha256
        };

        File.WriteAllText(
            Path.Combine(outputDirectory, MarkerFile),
            NormalizeLineEndings(JsonSerializer.Serialize(marker, IndentedJsonOptions)),
            new UTF8Encoding(false));
    }

    private static Guid DeterministicGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        var bytes = hash.AsSpan(0, 16).ToArray();
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }

    private static bool IsUnderDirectory(string candidate, string root)
    {
        var normalizedCandidate = Path.GetFullPath(candidate);
        var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (string.Equals(
                Path.TrimEndingDirectorySeparator(normalizedCandidate),
                normalizedRoot,
                comparison))
        {
            return true;
        }

        return normalizedCandidate.StartsWith(
            normalizedRoot + Path.DirectorySeparatorChar,
            comparison);
    }

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .TrimEnd() + "\n";

    private static string NormalizeRelativePath(string value) => value.Replace('\\', '/');

    private static string ToPlatformPath(string value) =>
        value.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

    private sealed record FoundationProject(
        string Name,
        string ProjectPath,
        string RelativePath);
}
