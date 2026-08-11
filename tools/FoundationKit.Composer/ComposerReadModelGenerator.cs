using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FoundationKit.Composer;

internal static class ComposerReadModelGenerator
{
    public static void Apply(
        ComposerManifest manifest,
        string projectPrefix,
        SortedDictionary<string, string> files)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPrefix);
        ArgumentNullException.ThrowIfNull(files);

        var model = manifest.ProjectModel;
        if (model is null || model.ReadModels.Count == 0)
            return;

        foreach (var module in model.Modules)
        {
            foreach (var readModel in module.ReadModels)
            {
                var applicationFolder = $"GeneratedReadModels/{module.Name}";
                files[$"src/{projectPrefix}.Application/{applicationFolder}/{readModel.Name}.cs"] =
                    BuildApplication(projectPrefix, module, readModel);
                files[$"src/{projectPrefix}.Infrastructure/{applicationFolder}/{readModel.Name}Configuration.cs"] =
                    BuildConfiguration(manifest, projectPrefix, module, readModel);
            }
        }

        var dbContextPath = $"src/{projectPrefix}.Infrastructure/GeneratedPlatform/GeneratedDbContext.cs";
        var migrationPath = $"src/{projectPrefix}.Infrastructure/GeneratedPlatform/Migrations/20260811000000_InitialGenerated.cs";
        var programPath = $"src/{projectPrefix}.Api/Program.cs";
        files[dbContextPath] = ExtendDbContext(files[dbContextPath], projectPrefix, model);
        files[migrationPath] = ExtendMigration(files[migrationPath], manifest, model);
        files[programPath] = ExtendProgram(files[programPath], projectPrefix, model);
        files["GENERATED-READ-MODELS.md"] = BuildReport(manifest, model);
    }

    private static string BuildApplication(
        string projectPrefix,
        ComposerModuleDefinition module,
        ComposerReadModelDefinition readModel)
    {
        var rowProperties = string.Join("\n", readModel.Fields.Select(field =>
            $"    public {FieldClrType(field)} {field.Name} {{ get; init; }}{RequiredInitializer(field)}"));
        var responseParameters = string.Join(",\n    ", readModel.Fields.Select(field =>
            $"{FieldClrType(field)} {field.Name}"));
        var responseArguments = string.Join(", ", readModel.Fields.Select(field => $"model.{field.Name}"));

        return $$"""
            #nullable enable

            using FoundationKit.Application.ReadModels;

            namespace {{projectPrefix}}.Application.GeneratedReadModels.{{module.Name}};

            public sealed class {{readModel.Name}}Row
            {
            {{rowProperties}}
            }

            public sealed record {{readModel.Name}}Response(
                {{responseParameters}});

            public sealed class {{readModel.Name}}Mapper
                : IReadModelMapper<{{readModel.Name}}Row, {{readModel.Name}}Response>
            {
                public {{readModel.Name}}Response Map({{readModel.Name}}Row model)
                {
                    ArgumentNullException.ThrowIfNull(model);
                    return new({{responseArguments}});
                }
            }
            """;
    }

    private static string BuildConfiguration(
        ComposerManifest manifest,
        string projectPrefix,
        ComposerModuleDefinition module,
        ComposerReadModelDefinition readModel)
    {
        var properties = new StringBuilder();
        foreach (var field in readModel.Fields)
        {
            properties.Append("        builder.Property(model => model.").Append(field.Name)
                .Append(").HasColumnName(").Append(JsonSerializer.Serialize(field.Name)).Append(')');
            if (field.Type == ComposerReadModelFieldType.Text)
                properties.Append(".HasMaxLength(").Append(field.MaximumLength).Append(')');
            if (field.Required)
                properties.Append(".IsRequired()");
            properties.AppendLine(";");
        }

        return $$"""
            #nullable enable

            using {{projectPrefix}}.Application.GeneratedReadModels.{{module.Name}};
            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Metadata.Builders;

            namespace {{projectPrefix}}.Infrastructure.GeneratedReadModels.{{module.Name}};

            internal sealed class {{readModel.Name}}Configuration
                : IEntityTypeConfiguration<{{readModel.Name}}Row>
            {
                public void Configure(EntityTypeBuilder<{{readModel.Name}}Row> builder)
                {
                    builder.HasNoKey();
                    builder.ToView({{JsonSerializer.Serialize(ViewName(manifest, readModel))}});
            {{properties.ToString().TrimEnd()}}
                }
            }
            """;
    }

    private static string ExtendDbContext(
        string source,
        string projectPrefix,
        ComposerProjectModel model)
    {
        var dbSets = string.Join("\n", model.Modules.SelectMany(module => module.ReadModels.Select(readModel =>
            $"    public DbSet<{projectPrefix}.Application.GeneratedReadModels.{module.Name}.{readModel.Name}Row> {module.Name}{readModel.Name} => Set<{projectPrefix}.Application.GeneratedReadModels.{module.Name}.{readModel.Name}Row>();")));
        const string marker = "    protected override void OnModelCreating(ModelBuilder modelBuilder)";
        if (!source.Contains(marker, StringComparison.Ordinal))
            throw new ComposerGenerationException("Generated DbContext read-model insertion marker was not found.");
        return source.Replace(marker, dbSets + "\n\n" + marker, StringComparison.Ordinal);
    }

    private static string ExtendMigration(
        string source,
        ComposerManifest manifest,
        ComposerProjectModel model)
    {
        var createStatements = new StringBuilder();
        var dropStatements = new StringBuilder();
        foreach (var module in model.Modules)
        {
            foreach (var readModel in module.ReadModels)
            {
                createStatements.AppendLine(BuildCreateViewMigration(manifest, module, readModel));
                dropStatements.Append("        migrationBuilder.Sql(")
                    .Append(JsonSerializer.Serialize($"DROP VIEW IF EXISTS [{ViewName(manifest, readModel)}];"))
                    .AppendLine(");");
            }
        }

        const string upMarker = "    }\n\n    protected override void Down(MigrationBuilder migrationBuilder)";
        if (!source.Contains(upMarker, StringComparison.Ordinal))
            throw new ComposerGenerationException("Generated migration Up insertion marker was not found.");
        source = source.Replace(
            upMarker,
            createStatements.ToString().TrimEnd() + "\n    }\n\n    protected override void Down(MigrationBuilder migrationBuilder)",
            StringComparison.Ordinal);

        const string downMarker = "    protected override void Down(MigrationBuilder migrationBuilder)\n    {";
        if (!source.Contains(downMarker, StringComparison.Ordinal))
            throw new ComposerGenerationException("Generated migration Down insertion marker was not found.");
        return source.Replace(
            downMarker,
            downMarker + "\n" + dropStatements.ToString().TrimEnd(),
            StringComparison.Ordinal);
    }

    private static string BuildCreateViewMigration(
        ComposerManifest manifest,
        ComposerModuleDefinition module,
        ComposerReadModelDefinition readModel)
    {
        var source = module.Resources.Single(resource =>
            string.Equals(resource.Name, readModel.SourceResource, StringComparison.OrdinalIgnoreCase));
        var joined = module.Resources.Single(resource =>
            string.Equals(resource.Name, readModel.Join.Resource, StringComparison.OrdinalIgnoreCase));
        var columns = string.Join(",\n    ", readModel.Fields.Select(field =>
        {
            var alias = string.Equals(field.SourceResource, source.Name, StringComparison.OrdinalIgnoreCase)
                ? "src"
                : "jn";
            return $"{alias}.[{field.SourceField}] AS [{field.Name}]";
        }));
        var sql = $$"""
            CREATE VIEW [{{ViewName(manifest, readModel)}}] AS
            SELECT
                {{columns}}
            FROM [{{TableName(manifest, source)}}] AS src
            LEFT JOIN [{{TableName(manifest, joined)}}] AS jn
                ON src.[{{readModel.Join.LeftField}}] = jn.[{{readModel.Join.RightField}}];
            """;

        return "        migrationBuilder.Sql(" +
            JsonSerializer.Serialize(sql.TrimEnd()) +
            ");";
    }

    private static string ExtendProgram(
        string source,
        string projectPrefix,
        ComposerProjectModel model)
    {
        var usingMarker = "using FoundationKit.Application.Modules;";
        if (!source.Contains(usingMarker, StringComparison.Ordinal))
            throw new ComposerGenerationException("Generated Program using insertion marker was not found.");
        source = source.Replace(
            usingMarker,
            usingMarker + "\nusing FoundationKit.Application.ReadModels;",
            StringComparison.Ordinal);
        var webUsingMarker = "using FoundationKit.WebApi.Crud;";
        source = source.Replace(
            webUsingMarker,
            webUsingMarker + "\nusing FoundationKit.WebApi.ReadModels;",
            StringComparison.Ordinal);

        var registrations = new StringBuilder();
        var mappings = new StringBuilder();
        foreach (var module in model.Modules)
        {
            foreach (var readModel in module.ReadModels)
            {
                var application = $"{projectPrefix}.Application.GeneratedReadModels.{module.Name}";
                var row = $"{application}.{readModel.Name}Row";
                var response = $"{application}.{readModel.Name}Response";
                var mapper = $"{application}.{readModel.Name}Mapper";
                registrations.AppendLine(CultureInfo.InvariantCulture,
                    $"builder.Services.AddFoundationEfReadModel<{row}, GeneratedDbContext>();");
                registrations.AppendLine(CultureInfo.InvariantCulture,
                    $"builder.Services.AddScoped<IReadModelMapper<{row}, {response}>, {mapper}>();");
                registrations.AppendLine(CultureInfo.InvariantCulture,
                    $"builder.Services.AddScoped<IReadModelQueryPolicy<{row}>>(_ =>");
                registrations.AppendLine(CultureInfo.InvariantCulture,
                    $"    new ConfiguredReadModelQueryPolicy<{row}>(new CrudStringQueryField<{row}>[]");
                registrations.AppendLine("    {");
                foreach (var field in readModel.Fields.Where(field =>
                             field.FilterMode != ComposerResourceFieldFilterMode.None || field.Sortable))
                {
                    registrations.Append("        new(")
                        .Append(JsonSerializer.Serialize(field.Name))
                        .Append(", model => model.").Append(field.Name)
                        .Append(", CrudStringFilterMode.").Append(FilterMode(field.FilterMode))
                        .Append(", Sortable: ").Append(field.Sortable ? "true" : "false")
                        .AppendLine("),");
                }
                registrations.AppendLine("    }));");
                registrations.AppendLine(CultureInfo.InvariantCulture,
                    $"builder.Services.AddScoped<ReadModelQueryService<{row}, {response}>>();");

                mappings.AppendLine(CultureInfo.InvariantCulture,
                    $"app.MapFoundationReadModel<{row}, {response}>(");
                mappings.AppendLine(CultureInfo.InvariantCulture, $"    {JsonSerializer.Serialize(readModel.Name)},");
                mappings.AppendLine(CultureInfo.InvariantCulture, $"    {JsonSerializer.Serialize(readModel.Route)},");
                mappings.AppendLine("    options =>");
                mappings.AppendLine("    {");
                mappings.AppendLine(CultureInfo.InvariantCulture, $"        options.RoutePrefix = {JsonSerializer.Serialize(readModel.Api.RoutePrefix)};");
                mappings.AppendLine("        options.MaximumPageSize = 100;");
                mappings.AppendLine(CultureInfo.InvariantCulture, $"        options.MaximumFilters = {readModel.Api.MaximumFilters};");
                mappings.AppendLine(CultureInfo.InvariantCulture, $"        options.MaximumSorts = {readModel.Api.MaximumSorts};");
                mappings.AppendLine("        options.AuthorizationPolicy = GeneratedAuthentication.AdminPolicy;");
                if (readModel.Api.RateLimitPolicyName is not null)
                {
                    mappings.AppendLine(CultureInfo.InvariantCulture,
                        $"        options.RateLimitPolicyName = {JsonSerializer.Serialize(readModel.Api.RateLimitPolicyName)};");
                }
                mappings.AppendLine("    });");
            }
        }

        const string appMarker = "var app = builder.Build();";
        if (!source.Contains(appMarker, StringComparison.Ordinal))
            throw new ComposerGenerationException("Generated Program service insertion marker was not found.");
        source = source.Replace(
            appMarker,
            registrations.ToString().TrimEnd() + "\n\n" + appMarker,
            StringComparison.Ordinal);

        const string runMarker = "app.Run();";
        if (!source.Contains(runMarker, StringComparison.Ordinal))
            throw new ComposerGenerationException("Generated Program endpoint insertion marker was not found.");
        return source.Replace(
            runMarker,
            mappings.ToString().TrimEnd() + "\n" + runMarker,
            StringComparison.Ordinal);
    }

    private static string BuildReport(ComposerManifest manifest, ComposerProjectModel model)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Generated SQL-view read models");
        builder.AppendLine();
        builder.AppendLine("Read models are read-only projections. Commands continue through generated entities/tables.");
        builder.AppendLine();
        foreach (var module in model.Modules)
        {
            foreach (var readModel in module.ReadModels)
            {
                builder.Append("- `").Append(module.Name).Append('.').Append(readModel.Name)
                    .Append("` (`").Append(readModel.Kind.ToString().ToLowerInvariant()).Append("`) → view `")
                    .Append(ViewName(manifest, readModel)).Append("` → `/")
                    .Append(readModel.Api.RoutePrefix).Append('/').Append(readModel.Route).AppendLine("`");
            }
        }
        builder.AppendLine();
        builder.AppendLine("View definitions use explicit projected columns and one bounded LEFT JOIN; no arbitrary SQL is accepted from the manifest.");
        return builder.ToString();
    }

    private static string RequiredInitializer(ComposerReadModelField field) =>
        field.Type == ComposerReadModelFieldType.Text && field.Required ? " = string.Empty;" : string.Empty;

    private static string FieldClrType(ComposerReadModelField field) => field.Type switch
    {
        ComposerReadModelFieldType.Uuid => field.Required ? "Guid" : "Guid?",
        ComposerReadModelFieldType.Text => field.Required ? "string" : "string?",
        _ => throw new InvalidOperationException($"Unsupported read-model field type '{field.Type}'.")
    };

    private static string FilterMode(ComposerResourceFieldFilterMode value) => value switch
    {
        ComposerResourceFieldFilterMode.None => "None",
        ComposerResourceFieldFilterMode.Exact => "Exact",
        ComposerResourceFieldFilterMode.Prefix => "Prefix",
        _ => throw new InvalidOperationException($"Unsupported read-model filter mode '{value}'.")
    };

    private static string ViewName(ComposerManifest manifest, ComposerReadModelDefinition readModel)
    {
        var raw = $"vw_{SqlNamespace(manifest)}_{SqlIdentifier(readModel.Route)}";
        if (raw.Length <= 120)
            return raw;
        return $"{raw[..111].TrimEnd('_')}_{ShortHash(raw)}";
    }

    private static string TableName(ComposerManifest manifest, ComposerResourceDefinition resource)
    {
        var suffix = SqlIdentifier(resource.Route);
        if (suffix.Length > 56)
            suffix = $"{suffix[..47].TrimEnd('_')}_{ShortHash(resource.Route)}";
        return $"{SqlNamespace(manifest)}_{suffix}";
    }

    private static string SqlNamespace(ComposerManifest manifest)
    {
        var normalized = new string(manifest.Name.ToLowerInvariant()
            .Select(character => char.IsAsciiLetterOrDigit(character) ? character : '_')
            .ToArray()).Trim('_');
        if (normalized.Length > 32)
            normalized = normalized[..32].TrimEnd('_');
        return $"{normalized}_{ShortHash(manifest.Name)}";
    }

    private static string SqlIdentifier(string value) =>
        new(value.ToLowerInvariant()
            .Select(character => char.IsAsciiLetterOrDigit(character) ? character : '_')
            .ToArray());

    private static string ShortHash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..8].ToLowerInvariant();
}
