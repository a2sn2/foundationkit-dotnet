using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace FoundationKit.Composer;

public static class ComposerStudioPlatformOverlay
{
    private const string AbpVersion = "10.6.0";

    private static readonly IReadOnlyDictionary<string, (string Namespace, string Module)> AbpModules =
        new Dictionary<string, (string Namespace, string Module)>(StringComparer.Ordinal)
        {
            ["Volo.Abp.AspNetCore"] = ("Volo.Abp.AspNetCore", "AbpAspNetCoreModule"),
            ["Volo.Abp.Security"] = ("Volo.Abp.Security", "AbpSecurityModule"),
            ["Volo.Abp.Authorization.Abstractions"] = ("Volo.Abp.Authorization", "AbpAuthorizationAbstractionsModule"),
            ["Volo.Abp.Auditing"] = ("Volo.Abp.Auditing", "AbpAuditingModule"),
            ["Volo.Abp.Settings"] = ("Volo.Abp.Settings", "AbpSettingsModule"),
            ["Volo.Abp.Features"] = ("Volo.Abp.Features", "AbpFeaturesModule"),
            ["Volo.Abp.MultiTenancy"] = ("Volo.Abp.MultiTenancy", "AbpMultiTenancyModule"),
            ["Volo.Abp.BackgroundJobs"] = ("Volo.Abp.BackgroundJobs", "AbpBackgroundJobsModule"),
            ["Volo.Abp.EventBus"] = ("Volo.Abp.EventBus", "AbpEventBusModule"),
            ["Volo.Abp.BlobStoring"] = ("Volo.Abp.BlobStoring", "AbpBlobStoringModule"),
            ["Volo.Abp.DistributedLocking"] = ("Volo.Abp.DistributedLocking", "AbpDistributedLockingModule")
        };

    public static async Task ApplyAsync(
        StudioBlueprintCompilation compilation,
        GeneratedProjectResult generated,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(compilation);
        ArgumentNullException.ThrowIfNull(generated);
        var output = generated.OutputDirectory;
        var projectPrefix = Path.GetFileNameWithoutExtension(generated.SolutionPath);
        var apiProject = Path.Combine(output, "src", $"{projectPrefix}.Api", $"{projectPrefix}.Api.csproj");
        var programPath = Path.Combine(output, "src", $"{projectPrefix}.Api", "Program.cs");

        if (File.Exists(apiProject) && compilation.AbpPackages.Count > 0)
        {
            await UpdateCentralPackagesAsync(output, compilation.AbpPackages, cancellationToken).ConfigureAwait(false);
            await UpdateApiProjectPackagesAsync(apiProject, compilation.AbpPackages, cancellationToken).ConfigureAwait(false);
            await WriteAbpModuleAsync(output, projectPrefix, compilation, cancellationToken).ConfigureAwait(false);
            if (File.Exists(programPath))
                await WireAbpHostAsync(programPath, projectPrefix, cancellationToken).ConfigureAwait(false);
        }

        if (File.Exists(programPath))
            await WireCustomizationHooksAsync(programPath, projectPrefix, cancellationToken).ConfigureAwait(false);

        await WriteCustomizationHookAsync(output, projectPrefix, cancellationToken).ConfigureAwait(false);
        await WritePlatformManifestAsync(output, projectPrefix, compilation, cancellationToken).ConfigureAwait(false);
        await WriteCompositionReportAsync(output, compilation, cancellationToken).ConfigureAwait(false);
    }

    private static async Task UpdateCentralPackagesAsync(
        string output,
        IReadOnlyList<string> packages,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(output, "Directory.Packages.props");
        var document = XDocument.Parse(await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false));
        var root = document.Root ?? throw new ComposerGenerationException("Generated Directory.Packages.props is invalid.");
        var itemGroup = root.Elements("ItemGroup").FirstOrDefault(group => group.Elements("PackageVersion").Any())
            ?? new XElement("ItemGroup");
        if (itemGroup.Parent is null)
            root.Add(itemGroup);

        foreach (var package in packages.Order(StringComparer.Ordinal))
        {
            var existing = itemGroup.Elements("PackageVersion")
                .FirstOrDefault(element => string.Equals((string?)element.Attribute("Include"), package, StringComparison.Ordinal));
            if (existing is null)
                itemGroup.Add(new XElement("PackageVersion", new XAttribute("Include", package), new XAttribute("Version", AbpVersion)));
            else
                existing.SetAttributeValue("Version", AbpVersion);
        }

        await File.WriteAllTextAsync(path, NormalizeXml(document), new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
    }

    private static async Task UpdateApiProjectPackagesAsync(
        string apiProject,
        IReadOnlyList<string> packages,
        CancellationToken cancellationToken)
    {
        var document = XDocument.Parse(await File.ReadAllTextAsync(apiProject, cancellationToken).ConfigureAwait(false));
        var root = document.Root ?? throw new ComposerGenerationException("Generated API project file is invalid.");
        var itemGroup = root.Elements("ItemGroup").FirstOrDefault(group => group.Elements("PackageReference").Any())
            ?? new XElement("ItemGroup");
        if (itemGroup.Parent is null)
            root.Add(itemGroup);

        foreach (var package in packages.Order(StringComparer.Ordinal))
        {
            if (itemGroup.Elements("PackageReference")
                .Any(element => string.Equals((string?)element.Attribute("Include"), package, StringComparison.Ordinal)))
                continue;
            itemGroup.Add(new XElement("PackageReference", new XAttribute("Include", package)));
        }

        await File.WriteAllTextAsync(apiProject, NormalizeXml(document), new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteAbpModuleAsync(
        string output,
        string projectPrefix,
        StudioBlueprintCompilation compilation,
        CancellationToken cancellationToken)
    {
        var moduleEntries = compilation.AbpPackages
            .Where(AbpModules.ContainsKey)
            .Select(package => AbpModules[package])
            .Distinct()
            .OrderBy(entry => entry.Namespace, StringComparer.Ordinal)
            .ThenBy(entry => entry.Module, StringComparer.Ordinal)
            .ToArray();
        if (moduleEntries.Length == 0)
            return;

        var usings = string.Join("\n", moduleEntries.Select(entry => $"using {entry.Namespace};"));
        var dependencies = string.Join(",\n    ", moduleEntries.Select(entry => $"typeof({entry.Module})"));
        var featureIds = compilation.Features.Select(feature => feature.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var jobConfiguration = featureIds.Contains(StudioFeatureIds.Jobs)
            ? """
                Configure<AbpBackgroundJobOptions>(options =>
                {
                    // The Studio enables the ABP job infrastructure but does not invent a persistence topology.
                    // A consumer can enable execution after selecting/configuring a durable IBackgroundJobStore.
                    options.IsJobExecutionEnabled = false;
                });
                """
            : string.Empty;
        var jobUsing = featureIds.Contains(StudioFeatureIds.Jobs)
            ? "using Volo.Abp.BackgroundJobs;"
            : string.Empty;

        var content = $$"""
            #nullable enable

            using Volo.Abp.Modularity;
            {{jobUsing}}
            {{usings}}

            namespace {{projectPrefix}}.Api.GeneratedPlatform;

            [DependsOn(
                {{dependencies}})]
            public sealed class GeneratedAbpPlatformModule : AbpModule
            {
                public override void ConfigureServices(ServiceConfigurationContext context)
                {
            {{Indent(jobConfiguration, 8)}}
                }
            }
            """;

        await WriteAsync(
            Path.Combine(output, "src", $"{projectPrefix}.Api", "GeneratedPlatform", "GeneratedAbpPlatformModule.cs"),
            content,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task WireAbpHostAsync(
        string programPath,
        string projectPrefix,
        CancellationToken cancellationToken)
    {
        var program = await File.ReadAllTextAsync(programPath, cancellationToken).ConfigureAwait(false);
        if (!program.Contains("using Volo.Abp;", StringComparison.Ordinal))
            program = "using Volo.Abp;\n" + program;

        if (!program.Contains("AddApplicationAsync<GeneratedAbpPlatformModule>", StringComparison.Ordinal))
        {
            const string marker = "var app = builder.Build();";
            if (!program.Contains(marker, StringComparison.Ordinal))
                throw new ComposerGenerationException("Studio could not locate the generated application-build boundary for ABP integration.");
            program = program.Replace(
                marker,
                $"await builder.AddApplicationAsync<{projectPrefix}.Api.GeneratedPlatform.GeneratedAbpPlatformModule>();\n\n{marker}",
                StringComparison.Ordinal);
        }

        if (!program.Contains("InitializeApplicationAsync", StringComparison.Ordinal))
        {
            const string marker = "var app = builder.Build();";
            program = program.Replace(
                marker,
                marker + "\nawait app.InitializeApplicationAsync();",
                StringComparison.Ordinal);
        }

        await File.WriteAllTextAsync(programPath, Normalize(program), new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
    }

    private static async Task WireCustomizationHooksAsync(
        string programPath,
        string projectPrefix,
        CancellationToken cancellationToken)
    {
        var program = await File.ReadAllTextAsync(programPath, cancellationToken).ConfigureAwait(false);
        var serviceCall = $"{projectPrefix}.Api.GeneratedPlatform.GeneratedCustomization.ConfigureServices(builder);";
        if (!program.Contains(serviceCall, StringComparison.Ordinal))
        {
            const string buildMarker = "var app = builder.Build();";
            if (!program.Contains(buildMarker, StringComparison.Ordinal))
                throw new ComposerGenerationException("Studio could not locate the generated application-build boundary for customization hooks.");
            program = program.Replace(buildMarker, serviceCall + "\n\n" + buildMarker, StringComparison.Ordinal);
        }

        var pipelineCall = $"{projectPrefix}.Api.GeneratedPlatform.GeneratedCustomization.ConfigurePipeline(app);";
        if (!program.Contains(pipelineCall, StringComparison.Ordinal))
        {
            var candidates = new[] { "app.UseSwagger();", "app.UseFoundationRequestDiagnostics();" };
            var marker = candidates.FirstOrDefault(candidate => program.Contains(candidate, StringComparison.Ordinal));
            if (marker is null)
                throw new ComposerGenerationException("Studio could not locate the generated HTTP pipeline boundary for customization hooks.");
            program = program.Replace(marker, pipelineCall + "\n" + marker, StringComparison.Ordinal);
        }

        await File.WriteAllTextAsync(programPath, Normalize(program), new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteCustomizationHookAsync(
        string output,
        string projectPrefix,
        CancellationToken cancellationToken)
    {
        var content = $$"""
            #nullable enable

            namespace {{projectPrefix}}.Api.GeneratedPlatform;

            public static partial class GeneratedCustomization
            {
                public static void ConfigureServices(WebApplicationBuilder builder)
                {
                    ArgumentNullException.ThrowIfNull(builder);
                    ConfigureServicesCore(builder);
                }

                public static void ConfigurePipeline(WebApplication app)
                {
                    ArgumentNullException.ThrowIfNull(app);
                    ConfigurePipelineCore(app);
                }

                static partial void ConfigureServicesCore(WebApplicationBuilder builder);
                static partial void ConfigurePipelineCore(WebApplication app);
            }
            """;
        await WriteAsync(
            Path.Combine(output, "src", $"{projectPrefix}.Api", "GeneratedPlatform", "GeneratedCustomization.cs"),
            content,
            cancellationToken).ConfigureAwait(false);

        var guide = $$"""
            # Safe customization

            FoundationKit Studio owns generated files and may replace them during regeneration.

            Consumer code belongs under a directory segment named `Custom`, for example:

            `src/{{projectPrefix}}.Api/Custom/GeneratedCustomization.Custom.cs`

            A consumer may implement the generated partial hooks without modifying generated files:

            ```csharp
            namespace {{projectPrefix}}.Api.GeneratedPlatform;

            public static partial class GeneratedCustomization
            {
                static partial void ConfigureServicesCore(WebApplicationBuilder builder)
                {
                    // Register product-specific services here.
                }

                static partial void ConfigurePipelineCore(WebApplication app)
                {
                    // Add product-specific middleware/endpoints here.
                }
            }
            ```

            Studio preview/regeneration preserves every file located under a `Custom` path and refuses to overwrite a custom file with generated output.
            `foundationkit.studio.json` is the editable Studio blueprint and is intentionally outside the generated ownership marker.
            """;
        await WriteAsync(Path.Combine(output, "CUSTOMIZATION.md"), guide, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WritePlatformManifestAsync(
        string output,
        string projectPrefix,
        StudioBlueprintCompilation compilation,
        CancellationToken cancellationToken)
    {
        var entries = compilation.Features.Select(feature => new
        {
            feature.Id,
            feature.DisplayName,
            feature.Category,
            readiness = feature.Readiness.ToString(),
            provider = ComposerStudioFeatureCatalog.ResolveProvider(feature, compilation.Blueprint.ProviderChoices),
            feature.CapabilityId
        }).ToArray();
        var payload = JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            generatedBy = "FoundationKit Studio",
            features = entries,
            abpPackages = compilation.AbpPackages
        }, new JsonSerializerOptions { WriteIndented = true });

        var jsonLiteral = JsonSerializer.Serialize(payload);
        var content = $$"""
            #nullable enable

            using System.Text.Json;

            namespace {{projectPrefix}}.Api.GeneratedPlatform;

            public static class GeneratedStudioPlatformManifest
            {
                public const string Json = {{jsonLiteral}};
                public static JsonDocument Parse() => JsonDocument.Parse(Json);
            }
            """;
        await WriteAsync(
            Path.Combine(output, "src", $"{projectPrefix}.Api", "GeneratedPlatform", "GeneratedStudioPlatformManifest.cs"),
            content,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteCompositionReportAsync(
        string output,
        StudioBlueprintCompilation compilation,
        CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# FoundationKit Studio composition");
        builder.AppendLine();
        builder.Append("Project: `").Append(compilation.Blueprint.Name).AppendLine("`");
        builder.Append("Profile: `").Append(compilation.Blueprint.Profile).AppendLine("`");
        builder.Append("Foundation mode: `").Append(compilation.Blueprint.FoundationMode).AppendLine("`");
        builder.AppendLine();
        builder.AppendLine("## Selected / dependency-resolved features");
        builder.AppendLine();
        foreach (var feature in compilation.Features)
        {
            var provider = ComposerStudioFeatureCatalog.ResolveProvider(feature, compilation.Blueprint.ProviderChoices);
            builder.Append("- **").Append(feature.DisplayName).Append("** (`").Append(feature.Id).Append("`) — ")
                .Append(feature.Readiness).Append(" — provider `").Append(provider).AppendLine("`");
        }
        builder.AppendLine();
        builder.AppendLine("## Ownership");
        builder.AppendLine();
        builder.AppendLine("- Generated files are owned by FoundationKit Studio and may be regenerated.");
        builder.AppendLine("- Any file under a `Custom` directory is consumer-owned and preserved by Studio regeneration.");
        builder.AppendLine("- `foundationkit.studio.json` is the editable project blueprint and is not hash-owned by the generator.");
        builder.AppendLine("- Environment secrets, deployment topology, external provider credentials and Production governance remain consumer/deployment responsibilities.");
        builder.AppendLine();
        if (compilation.AbpPackages.Count > 0)
        {
            builder.AppendLine("## ABP OSS provider packages");
            builder.AppendLine();
            foreach (var package in compilation.AbpPackages)
                builder.Append("- `").Append(package).AppendLine("` 10.6.0");
            builder.AppendLine();
            builder.AppendLine("ABP infrastructure is initialized through `GeneratedAbpPlatformModule`. Provider-specific durable stores/transports are not invented by Studio; select/configure them explicitly for the target environment.");
        }
        await WriteAsync(Path.Combine(output, "STUDIO-COMPOSITION.md"), builder.ToString(), cancellationToken).ConfigureAwait(false);
    }

    private static string NormalizeXml(XDocument document)
    {
        using var writer = new Utf8StringWriter();
        document.Save(writer, SaveOptions.None);
        return Normalize(writer.ToString());
    }

    private static async Task WriteAsync(string path, string content, CancellationToken cancellationToken)
    {
        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);
        await File.WriteAllTextAsync(path, Normalize(content), new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
    }

    private static string Normalize(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .TrimEnd() + "\n";

    private static string Indent(string value, int count)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        var prefix = new string(' ', count);
        return string.Join("\n", Normalize(value).TrimEnd().Split('\n').Select(line => prefix + line));
    }

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => new UTF8Encoding(false);
    }
}
