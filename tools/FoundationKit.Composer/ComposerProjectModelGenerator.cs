using System.Text;
using System.Text.Json;

namespace FoundationKit.Composer;

public static class ComposerProjectModelGenerator
{
    public const string GeneratorContractVersion = "2";

    private const string MarkerFile = ".foundationkit-generated.json";
    private const string SwashbuckleVersion = "10.2.3";
    private static readonly JsonSerializerOptions IndentedJsonOptions = new() { WriteIndented = true };

    public static async Task<GeneratedProjectResult> GenerateAsync(
        CompositionAnalysis analysis,
        ProjectGenerationOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(options);

        if (analysis.Manifest.SchemaVersion != 2 || analysis.Manifest.ProjectModel is null)
        {
            throw new ComposerGenerationException(
                "Composer project-model generation requires a schemaVersion 2 manifest with modules/resources.");
        }

        var baseResult = await ComposerProjectGenerator.GenerateAsync(
            analysis,
            options,
            cancellationToken).ConfigureAwait(false);

        var projectPrefix = Path.GetFileNameWithoutExtension(baseResult.SolutionPath);
        var overlay = BuildOverlayFiles(analysis.Manifest, projectPrefix);
        if (analysis.Manifest.ProjectModel.Resources.Any(resource => resource.IsExecutable))
        {
            AddExecutableProjectBuildFiles(
                overlay,
                baseResult.OutputDirectory,
                projectPrefix);
            foreach (var file in ComposerExecutableResourceGenerator.BuildFiles(analysis.Manifest, projectPrefix))
                overlay[file.Key] = file.Value;
        }

        ComposerBlazorRuntimeGenerator.Apply(
            analysis,
            baseResult.OutputDirectory,
            projectPrefix,
            overlay);

        foreach (var file in overlay)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = Path.Combine(baseResult.OutputDirectory, ToPlatformPath(file.Key));
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(
                path,
                NormalizeLineEndings(file.Value),
                new UTF8Encoding(false),
                cancellationToken).ConfigureAwait(false);
        }

        var allFiles = Directory
            .EnumerateFiles(baseResult.OutputDirectory, "*", SearchOption.AllDirectories)
            .Select(path => NormalizeProjectPath(Path.GetRelativePath(baseResult.OutputDirectory, path)))
            .Where(path => !path.Equals(MarkerFile, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToDictionary(
                path => path,
                path => File.ReadAllText(Path.Combine(baseResult.OutputDirectory, ToPlatformPath(path))),
                StringComparer.Ordinal);

        allFiles[MarkerFile] = ComposerGeneratedOwnership.BuildMarker(
            analysis.Manifest.Name,
            projectPrefix,
            baseResult.ReferenceMode,
            allFiles,
            GeneratorContractVersion);
        await File.WriteAllTextAsync(
            Path.Combine(baseResult.OutputDirectory, MarkerFile),
            NormalizeLineEndings(allFiles[MarkerFile]),
            new UTF8Encoding(false),
            cancellationToken).ConfigureAwait(false);

        var generatedFiles = allFiles.Keys.Order(StringComparer.Ordinal).ToArray();
        return new GeneratedProjectResult(
            baseResult.OutputDirectory,
            baseResult.SolutionPath,
            baseResult.ReferenceMode,
            generatedFiles);
    }

    public static string BuildNormalizedManifest(ComposerManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var projectModel = manifest.ProjectModel
            ?? throw new ComposerGenerationException("Schema v2 manifest is missing its project model.");

        var contracts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var requirement in manifest.ContractRequirements)
            contracts[requirement.CapabilityId] = requirement.ContractVersion;

        var modules = projectModel.Modules
            .OrderBy(module => module.Name, StringComparer.OrdinalIgnoreCase)
            .Select(module => new
            {
                name = module.Name,
                resources = module.Resources
                    .OrderBy(resource => resource.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(BuildNormalizedResource)
                    .ToArray()
            })
            .ToArray();

        var normalized = new
        {
            schemaVersion = 2,
            name = manifest.Name,
            profile = manifest.Profile,
            includeCapabilities = manifest.IncludeCapabilities.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            excludeCapabilities = manifest.ExcludeCapabilities.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            providers = manifest.Providers.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            capabilityContracts = contracts,
            modules
        };

        return JsonSerializer.Serialize(normalized, IndentedJsonOptions);
    }

    private static object BuildNormalizedResource(ComposerResourceDefinition resource)
    {
        var overrides = new { manager = resource.Overrides.Manager };
        var api = new
        {
            routePrefix = resource.Api.RoutePrefix,
            idempotency = IdempotencyName(resource.Api.Idempotency),
            concurrency = ConcurrencyName(resource.Api.Concurrency),
            maximumFilters = resource.Api.MaximumFilters,
            maximumSorts = resource.Api.MaximumSorts,
            rateLimitPolicyName = resource.Api.RateLimitPolicyName
        };

        if (!resource.IsExecutable)
        {
            return new
            {
                name = resource.Name,
                route = resource.Route,
                idType = IdTypeName(resource.IdType),
                behaviors = resource.Behaviors.Select(BehaviorName).ToArray(),
                overrides,
                api
            };
        }

        return new
        {
            name = resource.Name,
            route = resource.Route,
            idType = IdTypeName(resource.IdType),
            behaviors = resource.Behaviors.Select(BehaviorName).ToArray(),
            fields = resource.Fields.Select(field => new
            {
                name = field.Name,
                type = FieldTypeName(field.Type),
                required = field.Required,
                maximumLength = field.MaximumLength
            }).ToArray(),
            overrides,
            api
        };
    }

    private static SortedDictionary<string, string> BuildOverlayFiles(
        ComposerManifest manifest,
        string projectPrefix)
    {
        var projectModel = manifest.ProjectModel
            ?? throw new ComposerGenerationException("Schema v2 manifest is missing its project model.");
        var files = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["foundationkit.project.json"] = BuildNormalizedManifest(manifest),
            ["PROJECT-MODEL.md"] = BuildProjectModelReport(manifest)
        };

        foreach (var module in projectModel.Modules)
        {
            foreach (var resource in module.Resources)
            {
                files[$"src/{projectPrefix}.Application/GeneratedModules/{module.Name}/{resource.Name}Definition.g.cs"] =
                    BuildResourceDescriptor(projectPrefix, module, resource);
            }
        }

        return files;
    }

    private static void AddExecutableProjectBuildFiles(
        SortedDictionary<string, string> overlay,
        string outputDirectory,
        string projectPrefix)
    {
        var packagesPath = Path.Combine(outputDirectory, "Directory.Packages.props");
        var apiProjectRelativePath = $"src/{projectPrefix}.Api/{projectPrefix}.Api.csproj";
        var apiProjectPath = Path.Combine(outputDirectory, ToPlatformPath(apiProjectRelativePath));
        if (!File.Exists(packagesPath) || !File.Exists(apiProjectPath))
        {
            throw new ComposerGenerationException(
                "Executable resources require the generated Web API project and central package file.");
        }

        var packages = File.ReadAllText(packagesPath);
        if (!packages.Contains("Swashbuckle.AspNetCore", StringComparison.Ordinal))
        {
            const string itemGroupEnd = "  </ItemGroup>";
            var index = packages.IndexOf(itemGroupEnd, StringComparison.Ordinal);
            if (index < 0)
                throw new ComposerGenerationException("Generated central package file has an unexpected shape.");
            var insertion = $"    <PackageVersion Include=\"Swashbuckle.AspNetCore\" Version=\"{SwashbuckleVersion}\" />\n";
            packages = packages.Insert(index, insertion);
        }
        overlay["Directory.Packages.props"] = packages;

        var apiProject = File.ReadAllText(apiProjectPath);
        if (!apiProject.Contains("Swashbuckle.AspNetCore", StringComparison.Ordinal))
        {
            const string projectEnd = "</Project>";
            var index = apiProject.LastIndexOf(projectEnd, StringComparison.Ordinal);
            if (index < 0)
                throw new ComposerGenerationException("Generated API project has an unexpected shape.");
            var insertion = "  <ItemGroup>\n    <PackageReference Include=\"Swashbuckle.AspNetCore\" />\n  </ItemGroup>\n";
            apiProject = apiProject.Insert(index, insertion);
        }
        overlay[apiProjectRelativePath] = apiProject;
    }

    private static string BuildResourceDescriptor(
        string projectPrefix,
        ComposerModuleDefinition module,
        ComposerResourceDefinition resource)
    {
        var behaviors = string.Join(", ", resource.Behaviors.Select(behavior => JsonSerializer.Serialize(BehaviorName(behavior))));
        var managerLiteral = resource.Overrides.Manager is null
            ? "null"
            : JsonSerializer.Serialize(resource.Overrides.Manager);
        var rateLimitLiteral = resource.Api.RateLimitPolicyName is null
            ? "null"
            : JsonSerializer.Serialize(resource.Api.RateLimitPolicyName);
        var moduleLiteral = JsonSerializer.Serialize(module.Name);
        var resourceLiteral = JsonSerializer.Serialize(resource.Name);
        var routeLiteral = JsonSerializer.Serialize(resource.Route);
        var idTypeLiteral = JsonSerializer.Serialize(IdTypeName(resource.IdType));
        var routePrefixLiteral = JsonSerializer.Serialize(resource.Api.RoutePrefix);
        var idempotencyLiteral = JsonSerializer.Serialize(IdempotencyName(resource.Api.Idempotency));
        var concurrencyLiteral = JsonSerializer.Serialize(ConcurrencyName(resource.Api.Concurrency));

        return $$"""
            #nullable enable

            namespace {{projectPrefix}}.Application.GeneratedModules.{{module.Name}};

            /// <summary>
            /// Deterministic Composer v2 resource intent. This descriptor contains configuration only;
            /// project business logic belongs in consumer-owned code.
            /// </summary>
            public static class {{resource.Name}}Definition
            {
                public const string ModuleName = {{moduleLiteral}};
                public const string ResourceName = {{resourceLiteral}};
                public const string Route = {{routeLiteral}};
                public const string IdType = {{idTypeLiteral}};
                public const string ApiRoutePrefix = {{routePrefixLiteral}};
                public const string Idempotency = {{idempotencyLiteral}};
                public const string Concurrency = {{concurrencyLiteral}};
                public const int MaximumFilters = {{resource.Api.MaximumFilters}};
                public const int MaximumSorts = {{resource.Api.MaximumSorts}};
                public const string? ManagerOverride = {{managerLiteral}};
                public const string? RateLimitPolicyName = {{rateLimitLiteral}};

                public static IReadOnlyList<string> Behaviors { get; } = new[] { {{behaviors}} };
            }
            """;
    }

    private static string BuildProjectModelReport(ComposerManifest manifest)
    {
        var model = manifest.ProjectModel
            ?? throw new ComposerGenerationException("Schema v2 manifest is missing its project model.");
        var hasExecutable = model.Resources.Any(resource => resource.IsExecutable);
        var builder = new StringBuilder();
        builder.Append("# ").AppendLine(manifest.Name);
        builder.AppendLine();
        builder.AppendLine("## Composer v2 project model");
        builder.AppendLine();
        builder.AppendLine(hasExecutable
            ? "This file is generated from `foundationkit.project.json`. Resources with explicit `fields` have the bounded Phase 12 executable full-stack overlay; resources without fields remain configuration descriptors."
            : "This file is generated from `foundationkit.project.json`. It records project/module/resource intent; it does not synthesize product business rules.");
        builder.AppendLine();
        builder.AppendLine("| Module | Resource | Route | ID | Behaviors | Manager | API | Idempotency | Concurrency |");
        builder.AppendLine("|---|---|---|---|---|---|---|---|---|");

        foreach (var module in model.Modules.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var resource in module.Resources.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
            {
                builder.Append("| `").Append(module.Name).Append("` | `")
                    .Append(resource.Name).Append("` | `/")
                    .Append(resource.Api.RoutePrefix).Append('/').Append(resource.Route).Append("` | `")
                    .Append(IdTypeName(resource.IdType)).Append("` | ")
                    .Append(string.Join(", ", resource.Behaviors.Select(behavior => $"`{BehaviorName(behavior)}`"))).Append(" | ")
                    .Append(resource.Overrides.Manager is null ? "-" : $"`{resource.Overrides.Manager}`").Append(" | ")
                    .Append($"filters:{resource.Api.MaximumFilters}, sorts:{resource.Api.MaximumSorts}").Append(" | `")
                    .Append(IdempotencyName(resource.Api.Idempotency)).Append("` | `")
                    .Append(ConcurrencyName(resource.Api.Concurrency)).AppendLine("` |");
            }
        }

        if (hasExecutable)
        {
            builder.AppendLine();
            builder.AppendLine("## Executable fields");
            builder.AppendLine();
            foreach (var module in model.Modules)
            {
                foreach (var resource in module.Resources.Where(resource => resource.IsExecutable))
                {
                    builder.Append("- `").Append(module.Name).Append('.').Append(resource.Name).Append("`: ")
                        .AppendLine(string.Join(", ", resource.Fields.Select(field =>
                            $"`{field.Name}:{FieldTypeName(field.Type)}:{(field.Required ? "required" : "optional")}:max={field.MaximumLength}`")));
                }
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Boundary");
        builder.AppendLine();
        builder.AppendLine("- Global FoundationKit capability/provider resolution still uses the canonical Core capability graph.");
        builder.AppendLine("- Resource `behaviors` are module intent and map onto existing runtime capabilities where one exists.");
        builder.AppendLine("- `manager` is a safe identifier only; Composer does not accept or inject arbitrary C# source from the manifest.");
        builder.AppendLine(hasExecutable
            ? "- Phase 12 executable resources are deliberately bounded: explicit text fields, Guid IDs, SQL Server, CRUD/validation/authorization/audit/concurrency/idempotency surfaces only when declared and supported."
            : "- Concrete database fields, business validation, authorization semantics, external integrations, and secrets remain explicit project code/configuration.");
        return builder.ToString();
    }

    private static string IdTypeName(ComposerResourceIdType value) => value switch
    {
        ComposerResourceIdType.Guid => "guid",
        ComposerResourceIdType.String => "string",
        ComposerResourceIdType.Long => "long",
        ComposerResourceIdType.Int => "int",
        _ => throw new InvalidOperationException($"Unsupported resource ID type '{value}'.")
    };

    private static string FieldTypeName(ComposerResourceFieldType value) => value switch
    {
        ComposerResourceFieldType.Text => "text",
        _ => throw new InvalidOperationException($"Unsupported resource field type '{value}'.")
    };

    private static string BehaviorName(ComposerResourceBehavior value) => value switch
    {
        ComposerResourceBehavior.FeatureManagement => "feature-management",
        _ => value.ToString().ToLowerInvariant()
    };

    private static string IdempotencyName(ComposerApiIdempotencyMode value) => value switch
    {
        ComposerApiIdempotencyMode.Disabled => "disabled",
        ComposerApiIdempotencyMode.Optional => "optional",
        ComposerApiIdempotencyMode.Required => "required",
        _ => throw new InvalidOperationException($"Unsupported idempotency mode '{value}'.")
    };

    private static string ConcurrencyName(ComposerApiConcurrencyMode value) => value switch
    {
        ComposerApiConcurrencyMode.ApplicationPolicy => "application-policy",
        ComposerApiConcurrencyMode.RequireIfMatch => "require-if-match",
        _ => throw new InvalidOperationException($"Unsupported concurrency mode '{value}'.")
    };

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .TrimEnd() + "\n";

    private static string NormalizeProjectPath(string value) => value.Replace('\\', '/');

    private static string ToPlatformPath(string value) =>
        value.Replace('/', Path.DirectorySeparatorChar);
}
