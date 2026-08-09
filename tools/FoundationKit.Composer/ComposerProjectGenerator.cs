using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FoundationKit.Application.Capabilities;

namespace FoundationKit.Composer;

public sealed record ProjectGenerationOptions(
    string OutputDirectory,
    string? FoundationRoot = null,
    bool Force = false);

public sealed record GeneratedProjectResult(
    string OutputDirectory,
    string SolutionPath,
    string ReferenceMode,
    IReadOnlyList<string> GeneratedFiles);

public static class ComposerProjectGenerator
{
    public const string GeneratorContractVersion = "1";
    public const string FoundationKitPackageVersion = "0.1.0";

    private const string TargetFramework = "net8.0";
    private const string TestSdkVersion = "17.8.0";
    private const string XunitVersion = "2.6.1";
    private const string XunitRunnerVersion = "2.5.3";
    private const string AspNetCoreVersion = "8.0.29";

    private const string GeneratedMarkerFile = ".foundationkit-generated.json";
    private const string CSharpProjectTypeGuid = "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}";

    private static readonly PackageBinding[] PackageBindings =
    [
        new(FoundationCapabilityIds.Kernel, "FoundationKit.Domain", "src/FoundationKit.Domain/FoundationKit.Domain.csproj", GeneratedLayer.Domain),
        new(FoundationCapabilityIds.Kernel, "FoundationKit.Application", "src/FoundationKit.Application/FoundationKit.Application.csproj", GeneratedLayer.Application),
        new(FoundationCapabilityIds.Kernel, "FoundationKit.Infrastructure", "src/FoundationKit.Infrastructure/FoundationKit.Infrastructure.csproj", GeneratedLayer.Infrastructure),
        new(FoundationCapabilityIds.Validation, "FoundationKit.Application", "src/FoundationKit.Application/FoundationKit.Application.csproj", GeneratedLayer.Application),
        new(FoundationCapabilityIds.WebApi, "FoundationKit.WebApi", "src/FoundationKit.WebApi/FoundationKit.WebApi.csproj", GeneratedLayer.Api),
        new(FoundationCapabilityIds.Blazor, "FoundationKit.Blazor", "src/FoundationKit.Blazor/FoundationKit.Blazor.csproj", GeneratedLayer.Client),
        new(FoundationCapabilityIds.Auditing, "FoundationKit.Auditing", "src/FoundationKit.Auditing/FoundationKit.Auditing.csproj", GeneratedLayer.Application),
        new(FoundationCapabilityIds.Security, "FoundationKit.Security", "src/FoundationKit.Security/FoundationKit.Security.csproj", GeneratedLayer.Api),
        new(FoundationCapabilityIds.Identity, "FoundationKit.Identity", "src/FoundationKit.Identity/FoundationKit.Identity.csproj", GeneratedLayer.Infrastructure),
        new(FoundationCapabilityIds.Authorization, "FoundationKit.Authorization", "src/FoundationKit.Authorization/FoundationKit.Authorization.csproj", GeneratedLayer.Application),
        new(FoundationCapabilityIds.Workflow, "FoundationKit.Workflow", "src/FoundationKit.Workflow/FoundationKit.Workflow.csproj", GeneratedLayer.Application),
        new(FoundationCapabilityIds.Approvals, "FoundationKit.Approvals", "src/FoundationKit.Approvals/FoundationKit.Approvals.csproj", GeneratedLayer.Application),
        new(FoundationCapabilityIds.Notifications, "FoundationKit.Notifications", "src/FoundationKit.Notifications/FoundationKit.Notifications.csproj", GeneratedLayer.Application),
        new(FoundationCapabilityIds.Settings, "FoundationKit.Settings", "src/FoundationKit.Settings/FoundationKit.Settings.csproj", GeneratedLayer.Application),
        new(FoundationCapabilityIds.FeatureManagement, "FoundationKit.FeatureManagement", "src/FoundationKit.FeatureManagement/FoundationKit.FeatureManagement.csproj", GeneratedLayer.Application),
        new(FoundationCapabilityIds.Localization, "FoundationKit.Localization", "src/FoundationKit.Localization/FoundationKit.Localization.csproj", GeneratedLayer.Application),
        new(FoundationCapabilityIds.Caching, "FoundationKit.Caching", "src/FoundationKit.Caching/FoundationKit.Caching.csproj", GeneratedLayer.Application),
        new(FoundationCapabilityIds.SmtpProvider, "FoundationKit.Notifications.Smtp", "src/FoundationKit.Notifications.Smtp/FoundationKit.Notifications.Smtp.csproj", GeneratedLayer.Infrastructure)
    ];

    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class",
        "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event",
        "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if",
        "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null",
        "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref",
        "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
        "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
        "virtual", "void", "volatile", "while"
    };

    public static Task<GeneratedProjectResult> GenerateAsync(
        ComposerManifest manifest,
        ProjectGenerationOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return GenerateAsync(CompositionAnalyzer.Analyze(manifest), options, cancellationToken);
    }

    public static async Task<GeneratedProjectResult> GenerateAsync(
        CompositionAnalysis analysis,
        ProjectGenerationOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(options);

        var outputDirectory = NormalizeOutputDirectory(options.OutputDirectory);
        var foundationRoot = NormalizeFoundationRoot(options.FoundationRoot);
        ValidatePathRelationship(outputDirectory, foundationRoot);
        PrepareDestination(outputDirectory, options.Force);

        var projectPrefix = ToProjectPrefix(analysis.Manifest.Name);
        var resolvedIds = analysis.Entries
            .Select(entry => entry.Capability.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hasApi = resolvedIds.Contains(FoundationCapabilityIds.WebApi);
        var hasClient = resolvedIds.Contains(FoundationCapabilityIds.Blazor);
        var referenceMode = foundationRoot is null ? "package" : "project";

        var projects = BuildProjectPlan(projectPrefix, hasApi, hasClient);
        var bindings = ResolveBindings(resolvedIds, projects);
        ValidateProjectReferences(bindings, foundationRoot);

        var files = BuildGeneratedFiles(
            analysis,
            outputDirectory,
            foundationRoot,
            referenceMode,
            projectPrefix,
            projects,
            bindings,
            resolvedIds);

        Directory.CreateDirectory(outputDirectory);
        foreach (var file in files.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = Path.Combine(outputDirectory, ToPlatformPath(file.Key));
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(
                path,
                NormalizeLineEndings(file.Value),
                new UTF8Encoding(false),
                cancellationToken);
        }

        var generatedFiles = files.Keys.Order(StringComparer.Ordinal).ToArray();
        return new GeneratedProjectResult(
            outputDirectory,
            Path.Combine(outputDirectory, $"{projectPrefix}.sln"),
            referenceMode,
            generatedFiles);
    }

    private static string NormalizeOutputDirectory(string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ComposerGenerationException("An output directory is required.");
        }

        var fullPath = Path.GetFullPath(outputDirectory);
        var root = Path.GetPathRoot(fullPath);
        if (!string.IsNullOrEmpty(root) && PathsEqual(fullPath, root))
        {
            throw new ComposerGenerationException("The filesystem root cannot be used as a generation destination.");
        }

        if (File.Exists(fullPath))
        {
            throw new ComposerGenerationException($"The generation destination is a file: {fullPath}");
        }

        return fullPath;
    }

    private static string? NormalizeFoundationRoot(string? foundationRoot)
    {
        if (string.IsNullOrWhiteSpace(foundationRoot))
        {
            return null;
        }

        var fullPath = Path.GetFullPath(foundationRoot);
        if (!Directory.Exists(fullPath))
        {
            throw new ComposerGenerationException($"Foundation root was not found: {fullPath}");
        }

        var marker = Path.Combine(fullPath, "src", "FoundationKit.Domain", "FoundationKit.Domain.csproj");
        if (!File.Exists(marker))
        {
            throw new ComposerGenerationException(
                "The supplied foundation root does not contain the expected FoundationKit source tree.");
        }

        return fullPath;
    }

    private static void ValidatePathRelationship(string outputDirectory, string? foundationRoot)
    {
        if (foundationRoot is null)
        {
            return;
        }

        if (IsSameOrAncestor(outputDirectory, foundationRoot))
        {
            throw new ComposerGenerationException(
                "The generation destination cannot be the FoundationKit repository root or one of its ancestors.");
        }
    }

    private static void PrepareDestination(string outputDirectory, bool force)
    {
        if (!Directory.Exists(outputDirectory))
        {
            return;
        }

        if (!Directory.EnumerateFileSystemEntries(outputDirectory).Any())
        {
            return;
        }

        if (!force)
        {
            throw new ComposerGenerationException(
                "The generation destination is not empty. Re-run with --force only for a directory previously generated by FoundationKit Composer.");
        }

        var marker = Path.Combine(outputDirectory, GeneratedMarkerFile);
        if (!File.Exists(marker))
        {
            throw new ComposerGenerationException(
                "Refusing to force-regenerate a non-empty directory without the FoundationKit generated marker.");
        }

        try
        {
            Directory.Delete(outputDirectory, recursive: true);
        }
        catch (IOException exception)
        {
            throw new ComposerGenerationException("Could not replace the existing generated destination.", exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new ComposerGenerationException("Could not replace the existing generated destination.", exception);
        }
    }

    private static GeneratedProject[] BuildProjectPlan(string projectPrefix, bool hasApi, bool hasClient)
    {
        var projects = new List<GeneratedProject>
        {
            CreateProject(projectPrefix, "Domain", GeneratedLayer.Domain, "src"),
            CreateProject(projectPrefix, "Application", GeneratedLayer.Application, "src"),
            CreateProject(projectPrefix, "Infrastructure", GeneratedLayer.Infrastructure, "src")
        };

        if (hasApi)
        {
            projects.Add(CreateProject(projectPrefix, "Api", GeneratedLayer.Api, "src"));
        }

        if (hasClient)
        {
            projects.Add(CreateProject(projectPrefix, "Client", GeneratedLayer.Client, "src"));
        }

        projects.Add(CreateProject(projectPrefix, "Tests", GeneratedLayer.Tests, "tests"));
        return [.. projects];
    }

    private static GeneratedProject CreateProject(
        string projectPrefix,
        string suffix,
        GeneratedLayer layer,
        string root)
    {
        var name = $"{projectPrefix}.{suffix}";
        var directory = $"{root}/{name}";
        return new GeneratedProject(name, directory, $"{directory}/{name}.csproj", layer);
    }

    private static PackageBinding[] ResolveBindings(
        HashSet<string> resolvedIds,
        GeneratedProject[] projects)
    {
        var availableLayers = projects.Select(project => project.Layer).ToHashSet();
        return PackageBindings
            .Where(binding =>
                resolvedIds.Contains(binding.CapabilityId) &&
                availableLayers.Contains(binding.Layer))
            .GroupBy(binding => new { binding.Layer, binding.PackageId })
            .Select(group => group.First())
            .OrderBy(binding => binding.Layer)
            .ThenBy(binding => binding.PackageId, StringComparer.Ordinal)
            .ToArray();
    }

    private static void ValidateProjectReferences(PackageBinding[] bindings, string? foundationRoot)
    {
        if (foundationRoot is null)
        {
            return;
        }

        foreach (var binding in bindings)
        {
            var path = Path.Combine(foundationRoot, ToPlatformPath(binding.ProjectPath));
            if (!File.Exists(path))
            {
                throw new ComposerGenerationException(
                    $"Foundation project for '{binding.PackageId}' was not found: {path}");
            }
        }
    }

    private static SortedDictionary<string, string> BuildGeneratedFiles(
        CompositionAnalysis analysis,
        string outputDirectory,
        string? foundationRoot,
        string referenceMode,
        string projectPrefix,
        GeneratedProject[] projects,
        PackageBinding[] bindings,
        HashSet<string> resolvedIds)
    {
        var files = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            [".gitignore"] = "bin/\nobj/\n.vs/\nTestResults/\n",
            ["Directory.Build.props"] = BuildDirectoryBuildProps(projectPrefix),
            ["Directory.Packages.props"] = BuildDirectoryPackagesProps(bindings, foundationRoot, resolvedIds),
            [$"{projectPrefix}.sln"] = BuildSolution(projects),
            ["README.md"] = BuildReadme(analysis, projectPrefix, projects, referenceMode),
            ["ARCHITECTURE.md"] = BuildArchitectureReport(analysis, bindings, referenceMode),
            ["foundationkit.project.json"] = BuildNormalizedManifest(analysis.Manifest)
        };

        foreach (var project in projects)
        {
            files[project.ProjectPath] = BuildProjectFile(
                project,
                projects,
                bindings,
                outputDirectory,
                foundationRoot,
                resolvedIds);

            foreach (var source in BuildSourceFiles(project, analysis.Manifest.Name, projectPrefix))
            {
                files[$"{project.RelativeDirectory}/{source.Key}"] = source.Value;
            }
        }

        var generatedFileNames = files.Keys
            .Append(GeneratedMarkerFile)
            .Order(StringComparer.Ordinal)
            .ToArray();
        files[GeneratedMarkerFile] = BuildGeneratedMarker(
            analysis.Manifest.Name,
            projectPrefix,
            referenceMode,
            generatedFileNames);

        return files;
    }

    private static string BuildDirectoryBuildProps(string projectPrefix) =>
        $$"""
        <Project>
          <PropertyGroup>
            <TargetFramework>{{TargetFramework}}</TargetFramework>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
            <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
            <Deterministic>true</Deterministic>
            <EnableNETAnalyzers>true</EnableNETAnalyzers>
            <AnalysisLevel>8.0-recommended</AnalysisLevel>
            <RootNamespace>{{XmlEscape(projectPrefix)}}</RootNamespace>
          </PropertyGroup>
        </Project>
        """;

    private static string BuildDirectoryPackagesProps(
        PackageBinding[] bindings,
        string? foundationRoot,
        HashSet<string> resolvedIds)
    {
        var versions = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["Microsoft.NET.Test.Sdk"] = TestSdkVersion,
            ["xunit"] = XunitVersion,
            ["xunit.runner.visualstudio"] = XunitRunnerVersion
        };

        if (foundationRoot is null)
        {
            foreach (var packageId in bindings.Select(binding => binding.PackageId).Distinct(StringComparer.Ordinal))
            {
                versions[packageId] = FoundationKitPackageVersion;
            }
        }

        if (resolvedIds.Contains(FoundationCapabilityIds.SqlServerProvider))
        {
            versions["Microsoft.EntityFrameworkCore.SqlServer"] = AspNetCoreVersion;
            if (resolvedIds.Contains(FoundationCapabilityIds.Identity))
            {
                versions["Microsoft.AspNetCore.Identity.EntityFrameworkCore"] = AspNetCoreVersion;
            }
        }

        var builder = new StringBuilder();
        builder.AppendLine("<Project>");
        builder.AppendLine("  <PropertyGroup>");
        builder.AppendLine("    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>");
        builder.AppendLine("  </PropertyGroup>");
        builder.AppendLine("  <ItemGroup>");
        foreach (var version in versions)
        {
            builder.Append("    <PackageVersion Include=\"")
                .Append(XmlEscape(version.Key))
                .Append("\" Version=\"")
                .Append(XmlEscape(version.Value))
                .AppendLine("\" />");
        }

        builder.AppendLine("  </ItemGroup>");
        builder.AppendLine("</Project>");
        return builder.ToString();
    }

    private static string BuildProjectFile(
        GeneratedProject project,
        GeneratedProject[] projects,
        PackageBinding[] bindings,
        string outputDirectory,
        string? foundationRoot,
        HashSet<string> resolvedIds)
    {
        var sdk = project.Layer switch
        {
            GeneratedLayer.Api => "Microsoft.NET.Sdk.Web",
            GeneratedLayer.Client => "Microsoft.NET.Sdk.Razor",
            _ => "Microsoft.NET.Sdk"
        };

        var builder = new StringBuilder();
        builder.Append("<Project Sdk=\"").Append(sdk).AppendLine("\">");
        builder.AppendLine("  <PropertyGroup>");
        builder.Append("    <RootNamespace>").Append(XmlEscape(project.Name)).AppendLine("</RootNamespace>");
        builder.Append("    <AssemblyName>").Append(XmlEscape(project.Name)).AppendLine("</AssemblyName>");
        builder.AppendLine("    <IsPackable>false</IsPackable>");
        if (project.Layer == GeneratedLayer.Tests)
        {
            builder.AppendLine("    <IsTestProject>true</IsTestProject>");
        }

        builder.AppendLine("  </PropertyGroup>");

        var productReferences = GetProductReferences(project, projects);
        var foundationReferences = bindings.Where(binding => binding.Layer == project.Layer).ToArray();
        if (productReferences.Length > 0 || foundationReferences.Length > 0)
        {
            builder.AppendLine("  <ItemGroup>");
            foreach (var reference in productReferences)
            {
                var currentDirectory = Path.Combine(outputDirectory, ToPlatformPath(project.RelativeDirectory));
                var target = Path.Combine(outputDirectory, ToPlatformPath(reference.ProjectPath));
                var relative = NormalizeProjectPath(Path.GetRelativePath(currentDirectory, target));
                builder.Append("    <ProjectReference Include=\"")
                    .Append(XmlEscape(relative))
                    .AppendLine("\" />");
            }

            foreach (var binding in foundationReferences)
            {
                builder.AppendLine(BuildFoundationReference(
                    binding,
                    project,
                    outputDirectory,
                    foundationRoot));
            }

            builder.AppendLine("  </ItemGroup>");
        }

        var packageReferences = GetProductPackageReferences(project.Layer, resolvedIds);
        if (project.Layer == GeneratedLayer.Tests)
        {
            packageReferences.Add("Microsoft.NET.Test.Sdk");
            packageReferences.Add("xunit");
            packageReferences.Add("xunit.runner.visualstudio");
        }

        if (packageReferences.Count > 0)
        {
            builder.AppendLine("  <ItemGroup>");
            foreach (var packageId in packageReferences.Order(StringComparer.Ordinal))
            {
                if (packageId == "xunit.runner.visualstudio")
                {
                    builder.AppendLine("    <PackageReference Include=\"xunit.runner.visualstudio\">");
                    builder.AppendLine("      <PrivateAssets>all</PrivateAssets>");
                    builder.AppendLine("      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>");
                    builder.AppendLine("    </PackageReference>");
                }
                else
                {
                    builder.Append("    <PackageReference Include=\"")
                        .Append(XmlEscape(packageId))
                        .AppendLine("\" />");
                }
            }

            builder.AppendLine("  </ItemGroup>");
        }

        if (project.Layer == GeneratedLayer.Client)
        {
            builder.AppendLine("  <ItemGroup>");
            builder.AppendLine("    <FrameworkReference Include=\"Microsoft.AspNetCore.App\" />");
            builder.AppendLine("  </ItemGroup>");
        }

        builder.AppendLine("</Project>");
        return builder.ToString();
    }

    private static GeneratedProject[] GetProductReferences(
        GeneratedProject project,
        GeneratedProject[] projects)
    {
        GeneratedLayer[] layers = project.Layer switch
        {
            GeneratedLayer.Application => [GeneratedLayer.Domain],
            GeneratedLayer.Infrastructure => [GeneratedLayer.Domain, GeneratedLayer.Application],
            GeneratedLayer.Api => [GeneratedLayer.Application, GeneratedLayer.Infrastructure],
            GeneratedLayer.Client => Array.Empty<GeneratedLayer>(),
            GeneratedLayer.Tests => [GeneratedLayer.Domain, GeneratedLayer.Application],
            _ => Array.Empty<GeneratedLayer>()
        };

        return layers
            .Select(layer => projects.Single(candidate => candidate.Layer == layer))
            .ToArray();
    }

    private static HashSet<string> GetProductPackageReferences(
        GeneratedLayer layer,
        HashSet<string> resolvedIds)
    {
        var references = new HashSet<string>(StringComparer.Ordinal);
        if (layer != GeneratedLayer.Infrastructure || !resolvedIds.Contains(FoundationCapabilityIds.SqlServerProvider))
        {
            return references;
        }

        references.Add("Microsoft.EntityFrameworkCore.SqlServer");
        if (resolvedIds.Contains(FoundationCapabilityIds.Identity))
        {
            references.Add("Microsoft.AspNetCore.Identity.EntityFrameworkCore");
        }

        return references;
    }

    private static string BuildFoundationReference(
        PackageBinding binding,
        GeneratedProject project,
        string outputDirectory,
        string? foundationRoot)
    {
        if (foundationRoot is null)
        {
            return $"    <PackageReference Include=\"{XmlEscape(binding.PackageId)}\" />";
        }

        var currentDirectory = Path.Combine(outputDirectory, ToPlatformPath(project.RelativeDirectory));
        var target = Path.Combine(foundationRoot, ToPlatformPath(binding.ProjectPath));
        var relative = NormalizeProjectPath(Path.GetRelativePath(currentDirectory, target));
        return $"    <ProjectReference Include=\"{XmlEscape(relative)}\" />";
    }

    private static SortedDictionary<string, string> BuildSourceFiles(
        GeneratedProject project,
        string productName,
        string projectPrefix)
    {
        var files = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var productLiteral = JsonSerializer.Serialize(productName);

        switch (project.Layer)
        {
            case GeneratedLayer.Domain:
                files["ProductDomainMarker.cs"] = $$"""
                    namespace {{projectPrefix}}.Domain;

                    public static class ProductDomainMarker
                    {
                        public const string ProductName = {{productLiteral}};
                    }
                    """;
                break;

            case GeneratedLayer.Application:
                files["ProductApplicationMarker.cs"] = $$"""
                    using {{projectPrefix}}.Domain;

                    namespace {{projectPrefix}}.Application;

                    public static class ProductApplicationMarker
                    {
                        public static string ProductName => ProductDomainMarker.ProductName;
                    }
                    """;
                break;

            case GeneratedLayer.Infrastructure:
                files["ProductInfrastructureMarker.cs"] = $$"""
                    using {{projectPrefix}}.Application;

                    namespace {{projectPrefix}}.Infrastructure;

                    public static class ProductInfrastructureMarker
                    {
                        public static string ProductName => ProductApplicationMarker.ProductName;
                    }
                    """;
                break;

            case GeneratedLayer.Api:
                files["Program.cs"] = $$"""
                    using {{projectPrefix}}.Application;

                    var builder = WebApplication.CreateBuilder(args);
                    var app = builder.Build();

                    app.MapGet("/health", () => Results.Ok(new
                    {
                        status = "ready",
                        product = ProductApplicationMarker.ProductName
                    }));

                    app.Run();
                    """;
                break;

            case GeneratedLayer.Client:
                files["GeneratedProduct.razor"] = $$"""
                    @namespace {{projectPrefix}}.Client

                    <section>
                        <h1>{{SecurityElement.Escape(productName)}}</h1>
                        <p>FoundationKit generated client boundary.</p>
                    </section>
                    """;
                break;

            case GeneratedLayer.Tests:
                files["GeneratedScaffoldTests.cs"] = $$"""
                    using {{projectPrefix}}.Application;
                    using {{projectPrefix}}.Domain;
                    using Xunit;

                    namespace {{projectPrefix}}.Tests;

                    public sealed class GeneratedScaffoldTests
                    {
                        [Fact]
                        public void Product_markers_share_the_manifest_name()
                        {
                            Assert.Equal(ProductDomainMarker.ProductName, ProductApplicationMarker.ProductName);
                            Assert.Equal({{productLiteral}}, ProductDomainMarker.ProductName);
                        }
                    }
                    """;
                break;

            default:
                throw new InvalidOperationException($"Unsupported generated layer '{project.Layer}'.");
        }

        return files;
    }

    private static string BuildSolution(GeneratedProject[] projects)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
        builder.AppendLine("# Visual Studio Version 17");
        builder.AppendLine("VisualStudioVersion = 17.0.31903.59");
        builder.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");

        foreach (var project in projects)
        {
            var guid = DeterministicGuid(project.Name).ToString("B").ToUpperInvariant();
            var path = project.ProjectPath.Replace('/', '\\');
            builder.Append("Project(\"").Append(CSharpProjectTypeGuid).Append("\") = \"")
                .Append(project.Name).Append("\", \"").Append(path).Append("\", \"")
                .Append(guid).AppendLine("\"");
            builder.AppendLine("EndProject");
        }

        builder.AppendLine("Global");
        builder.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
        builder.AppendLine("\t\tDebug|Any CPU = Debug|Any CPU");
        builder.AppendLine("\t\tRelease|Any CPU = Release|Any CPU");
        builder.AppendLine("\tEndGlobalSection");
        builder.AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
        foreach (var project in projects)
        {
            var guid = DeterministicGuid(project.Name).ToString("B").ToUpperInvariant();
            builder.Append("\t\t").Append(guid).AppendLine(".Debug|Any CPU.ActiveCfg = Debug|Any CPU");
            builder.Append("\t\t").Append(guid).AppendLine(".Debug|Any CPU.Build.0 = Debug|Any CPU");
            builder.Append("\t\t").Append(guid).AppendLine(".Release|Any CPU.ActiveCfg = Release|Any CPU");
            builder.Append("\t\t").Append(guid).AppendLine(".Release|Any CPU.Build.0 = Release|Any CPU");
        }

        builder.AppendLine("\tEndGlobalSection");
        builder.AppendLine("EndGlobal");
        return builder.ToString();
    }

    private static string BuildReadme(
        CompositionAnalysis analysis,
        string projectPrefix,
        GeneratedProject[] projects,
        string referenceMode)
    {
        var projectList = string.Join(
            Environment.NewLine,
            projects.Select(project => $"- `{project.ProjectPath}`"));

        return $$"""
            # {{analysis.Manifest.Name}}

            Generated deterministically by FoundationKit Composer contract v{{GeneratorContractVersion}}.

            ## Composition

            - Profile: `{{analysis.Manifest.Profile}}`
            - Resolved capabilities: {{analysis.Entries.Count}}
            - Foundation reference mode: `{{referenceMode}}`
            - Generated project prefix: `{{projectPrefix}}`

            ## Projects

            {{projectList}}

            ## Verify

            ```bash
            dotnet restore {{projectPrefix}}.sln
            dotnet build {{projectPrefix}}.sln --configuration Release --no-restore
            dotnet test {{projectPrefix}}.sln --configuration Release --no-build
            ```

            `ARCHITECTURE.md` records why every capability is present, its contract version/maturity, and whether a reusable package binding exists.

            The scaffold intentionally contains no product entities, migrations, authorization roles, tenant model, secrets, or deployment policy. Those remain product-owned decisions.
            """;
    }

    private static string BuildArchitectureReport(
        CompositionAnalysis analysis,
        PackageBinding[] bindings,
        string referenceMode)
    {
        var builder = new StringBuilder();
        builder.Append("# ").AppendLine(analysis.Manifest.Name);
        builder.AppendLine();
        builder.AppendLine("## FoundationKit composition decision report");
        builder.AppendLine();
        builder.Append("- Profile: `").Append(analysis.Manifest.Profile).AppendLine("`");
        builder.Append("- Reference mode: `").Append(referenceMode).AppendLine("`");
        builder.Append("- Resolved capabilities: ").Append(analysis.Entries.Count).AppendLine();
        builder.AppendLine();
        builder.AppendLine("| Capability | Contract | Kind | Maturity | Why present | Generated binding |");
        builder.AppendLine("|---|---:|---|---|---|---|");

        foreach (var entry in analysis.Entries)
        {
            var capabilityBindings = PackageBindings
                .Where(binding => binding.CapabilityId.Equals(entry.Capability.Id, StringComparison.OrdinalIgnoreCase))
                .Select(binding => binding.PackageId)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray();
            var generatedBindings = bindings
                .Where(binding => binding.CapabilityId.Equals(entry.Capability.Id, StringComparison.OrdinalIgnoreCase))
                .Select(binding => binding.PackageId)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray();

            string bindingText;
            if (entry.Capability.Id.Equals(FoundationCapabilityIds.SqlServerProvider, StringComparison.OrdinalIgnoreCase))
            {
                bindingText = "product-owned EF Core SQL Server reference";
            }
            else if (capabilityBindings.Length == 0)
            {
                bindingText = "no reusable package binding; composition/product implementation remains explicit";
            }
            else if (generatedBindings.Length == 0)
            {
                bindingText = "package exists but no matching generated layer is active";
            }
            else
            {
                bindingText = string.Join(", ", generatedBindings.Select(packageId => $"`{packageId}`"));
            }

            var reasons = string.Join(", ", entry.Reasons.Select(EscapeMarkdownCell));
            var contractVersion = FoundationCapabilityContracts.Get(entry.Capability.Id).ContractVersion;
            builder.Append("| `").Append(entry.Capability.Id).Append("` | v")
                .Append(contractVersion).Append(" | ")
                .Append(entry.Capability.Kind).Append(" | ")
                .Append(entry.Capability.Maturity).Append(" | ")
                .Append(reasons).Append(" | ")
                .Append(EscapeMarkdownCell(bindingText)).AppendLine(" |");
        }

        if (analysis.Warnings.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Maturity warnings");
            builder.AppendLine();
            foreach (var warning in analysis.Warnings)
            {
                builder.Append("- ").AppendLine(warning);
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Ownership boundary");
        builder.AppendLine();
        builder.AppendLine("Generated references only represent FoundationKit surfaces that exist today. Planned or product-specific capability semantics are not synthesized into fake packages or domain code.");
        builder.AppendLine("Database schema, migrations, identity persistence, organization model, authorization roles, secrets, deployment topology, retention, and product workflows remain owned by the generated product.");
        return builder.ToString();
    }

    private static string BuildNormalizedManifest(ComposerManifest manifest)
    {
        var contracts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var requirement in manifest.ContractRequirements)
        {
            contracts[requirement.CapabilityId] = requirement.ContractVersion;
        }

        var normalized = new
        {
            schemaVersion = 1,
            name = manifest.Name,
            profile = manifest.Profile,
            includeCapabilities = manifest.IncludeCapabilities.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            excludeCapabilities = manifest.ExcludeCapabilities.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            providers = manifest.Providers.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            capabilityContracts = contracts
        };

        return JsonSerializer.Serialize(normalized, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string BuildGeneratedMarker(
        string productName,
        string projectPrefix,
        string referenceMode,
        string[] generatedFiles)
    {
        var marker = new
        {
            schemaVersion = 1,
            generator = "FoundationKit.Composer",
            generatorContractVersion = GeneratorContractVersion,
            productName,
            projectPrefix,
            referenceMode,
            generatedFiles
        };

        return JsonSerializer.Serialize(marker, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string ToProjectPrefix(string name)
    {
        var segments = name.Split('.', StringSplitOptions.None);
        for (var index = 0; index < segments.Length; index++)
        {
            var segment = segments[index].Replace('-', '_');
            if (segment.Length == 0)
            {
                segment = "_";
            }

            if (!(char.IsLetter(segment[0]) || segment[0] == '_'))
            {
                segment = "_" + segment;
            }

            if (CSharpKeywords.Contains(segment))
            {
                segment = "_" + segment;
            }

            segments[index] = segment;
        }

        return string.Join('.', segments);
    }

    private static Guid DeterministicGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        var bytes = hash.AsSpan(0, 16).ToArray();
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }

    private static string XmlEscape(string value) => SecurityElement.Escape(value) ?? value;

    private static string EscapeMarkdownCell(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .TrimEnd() + "\n";

    private static string NormalizeProjectPath(string value) => value.Replace('\\', '/');

    private static string ToPlatformPath(string value) =>
        value.Replace('/', Path.DirectorySeparatorChar);

    private static bool PathsEqual(string left, string right)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return string.Equals(
            Path.TrimEndingDirectorySeparator(left),
            Path.TrimEndingDirectorySeparator(right),
            comparison);
    }

    private static bool IsSameOrAncestor(string candidateAncestor, string target)
    {
        if (PathsEqual(candidateAncestor, target))
        {
            return true;
        }

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var ancestor = Path.TrimEndingDirectorySeparator(candidateAncestor) + Path.DirectorySeparatorChar;
        return target.StartsWith(ancestor, comparison);
    }

    private sealed record PackageBinding(
        string CapabilityId,
        string PackageId,
        string ProjectPath,
        GeneratedLayer Layer);

    private sealed record GeneratedProject(
        string Name,
        string RelativeDirectory,
        string ProjectPath,
        GeneratedLayer Layer);

    private enum GeneratedLayer
    {
        Domain,
        Application,
        Infrastructure,
        Api,
        Client,
        Tests
    }
}

public sealed class ComposerGenerationException : Exception
{
    public ComposerGenerationException(string message)
        : base(message)
    {
    }

    public ComposerGenerationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
