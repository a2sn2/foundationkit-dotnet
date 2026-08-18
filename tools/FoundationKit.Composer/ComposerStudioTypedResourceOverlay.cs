using System.Text;
using System.Text.Json;

namespace FoundationKit.Composer;

public static class ComposerStudioTypedResourceOverlay
{
    public static async Task ApplyAsync(
        StudioBlueprintCompilation compilation,
        GeneratedProjectResult generated,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(compilation);
        ArgumentNullException.ThrowIfNull(generated);
        var prefix = Path.GetFileNameWithoutExtension(generated.SolutionPath);
        var resourceMap = compilation.Blueprint.Modules
            .SelectMany(module => module.Resources.Select(resource => (Module: module, Resource: resource)))
            .ToDictionary(item => item.Resource.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var module in compilation.Blueprint.Modules)
        {
            foreach (var resource in module.Resources)
            {
                var folder = Path.Combine("GeneratedModules", module.Name);
                await WriteAsync(
                    Path.Combine(generated.OutputDirectory, "src", $"{prefix}.Domain", folder, $"{resource.Name}.cs"),
                    BuildDomain(prefix, module, resource),
                    cancellationToken).ConfigureAwait(false);
                await WriteAsync(
                    Path.Combine(generated.OutputDirectory, "src", $"{prefix}.Application", folder, $"{resource.Name}Contracts.cs"),
                    BuildContracts(prefix, module, resource),
                    cancellationToken).ConfigureAwait(false);
                await WriteAsync(
                    Path.Combine(generated.OutputDirectory, "src", $"{prefix}.Application", folder, $"{resource.Name}Application.cs"),
                    BuildApplication(prefix, module, resource),
                    cancellationToken).ConfigureAwait(false);
                await WriteAsync(
                    Path.Combine(generated.OutputDirectory, "src", $"{prefix}.Infrastructure", folder, $"{resource.Name}EntityConfiguration.cs"),
                    BuildEntityConfiguration(prefix, module, resource, resourceMap),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        await PatchMigrationAsync(compilation, generated, cancellationToken).ConfigureAwait(false);
        await WriteTypeReportAsync(compilation, generated.OutputDirectory, cancellationToken).ConfigureAwait(false);
    }

    private static string BuildDomain(
        string prefix,
        StudioModuleBlueprint module,
        StudioResourceBlueprint resource)
    {
        var initializers = resource.Fields
            .Where(field => field.Required && field.Type == StudioFieldType.Text)
            .Select(field => $"        {field.Name} = string.Empty;")
            .ToArray();
        var parameters = string.Join(", ", resource.Fields.Select(field => $"{ClrType(field)} {Camel(field.Name)}"));
        var assignments = string.Join("\n", resource.Fields.Select(field => $"        {field.Name} = {Camel(field.Name)};"));
        var properties = string.Join("\n", resource.Fields.Select(field => $"    public {ClrType(field)} {field.Name} {{ get; private set; }}"));
        var concurrency = resource.Concurrency ? "\n    public int Version { get; private set; } = 1;\n" : string.Empty;
        var increment = resource.Concurrency ? "\n        Version = checked(Version + 1);" : string.Empty;

        return $$"""
            #nullable enable

            using FoundationKit.Domain.Primitives;

            namespace {{prefix}}.Domain.GeneratedModules.{{module.Name}};

            public sealed class {{resource.Name}} : Entity<Guid>
            {
                private {{resource.Name}}()
                {
            {{string.Join("\n", initializers)}}
                }

                private {{resource.Name}}(Guid id, {{parameters}}) : base(id)
                {
            {{assignments}}
                }

            {{properties}}
            {{concurrency}}
                public static {{resource.Name}} Create({{parameters}}) =>
                    new(Guid.NewGuid(), {{string.Join(", ", resource.Fields.Select(field => Camel(field.Name)))}});

                public void ApplyUpdate({{parameters}})
                {
            {{assignments}}{{increment}}
                }
            }
            """;
    }

    private static string BuildContracts(
        string prefix,
        StudioModuleBlueprint module,
        StudioResourceBlueprint resource)
    {
        var parameters = string.Join(",\n    ", resource.Fields.Select(BuildContractParameter));
        var response = new List<string> { "Guid Id" };
        response.AddRange(resource.Fields.Select(field => $"{ClrType(field)} {field.Name}"));
        if (resource.Concurrency)
            response.Add("int Version");

        return $$"""
            #nullable enable

            using System.ComponentModel.DataAnnotations;

            namespace {{prefix}}.Application.GeneratedModules.{{module.Name}};

            public sealed record {{resource.Name}}CreateRequest(
                {{parameters}});

            public sealed record {{resource.Name}}UpdateRequest(
                {{parameters}});

            public sealed record {{resource.Name}}Response(
                {{string.Join(",\n    ", response)}});
            """;
    }

    private static string BuildApplication(
        string prefix,
        StudioModuleBlueprint module,
        StudioResourceBlueprint resource)
    {
        var entity = $"{prefix}.Domain.GeneratedModules.{module.Name}.{resource.Name}";
        var requestArgs = string.Join(", ", resource.Fields.Select(field => $"request.{field.Name}"));
        var responseArgs = new List<string> { "entity.Id" };
        responseArgs.AddRange(resource.Fields.Select(field => $"entity.{field.Name}"));
        if (resource.Concurrency)
            responseArgs.Add("entity.Version");
        var authorization = resource.Authorization
            ? $$"""
                public sealed class {{resource.Name}}AuthorizationPolicy(ICurrentUser currentUser)
                    : ICrudAuthorizationPolicy<{{entity}}, Guid>
                {
                    private readonly ICurrentUser _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

                    public ValueTask<Result> AuthorizeAsync(
                        CrudAuthorizationContext<{{entity}}, Guid> context,
                        CancellationToken cancellationToken = default)
                    {
                        ArgumentNullException.ThrowIfNull(context);
                        var allowed = _currentUser.IsAuthenticated && _currentUser.IsInRole("admin");
                        return ValueTask.FromResult(allowed
                            ? Result.Success()
                            : Result.Failure(Error.Forbidden(
                                "Generated.Authorization.AdminRequired",
                                "The generated reference resource requires an authenticated admin role.")));
                    }
                }
                """
            : string.Empty;
        var concurrency = resource.Concurrency
            ? $$"""
                public sealed class {{resource.Name}}ConcurrencyPolicy
                    : ICrudConcurrencyPolicy<{{entity}}, {{resource.Name}}UpdateRequest>
                {
                    public Result Validate({{entity}} entity, {{resource.Name}}UpdateRequest request) =>
                        Result.Failure(Error.PreconditionRequired(
                            "Generated.Version.Required",
                            "An If-Match concurrency token is required."));

                    public Result Validate(
                        {{entity}} entity,
                        {{resource.Name}}UpdateRequest request,
                        CrudConcurrencyPrecondition? precondition)
                    {
                        if (precondition is null)
                            return Validate(entity, request);
                        var expected = $"\"{entity.Version}\"";
                        return string.Equals(precondition.Token, expected, StringComparison.Ordinal)
                            ? Result.Success()
                            : Result.Failure(Error.PreconditionFailed(
                                "Generated.Version.PreconditionFailed",
                                "The resource changed since it was read."));
                    }
                }
                """
            : string.Empty;

        return $$"""
            #nullable enable

            using FoundationKit.Application.Abstractions;
            using FoundationKit.Application.Crud;
            using FoundationKit.Application.Results;

            namespace {{prefix}}.Application.GeneratedModules.{{module.Name}};

            public sealed class {{resource.Name}}Mapper
                : ICrudMapper<{{entity}}, Guid, {{resource.Name}}CreateRequest, {{resource.Name}}UpdateRequest, {{resource.Name}}Response>
            {
                public {{entity}} Create({{resource.Name}}CreateRequest request) =>
                    {{entity}}.Create({{requestArgs}});

                public void ApplyUpdate({{entity}} entity, {{resource.Name}}UpdateRequest request) =>
                    entity.ApplyUpdate({{requestArgs}});

                public {{resource.Name}}Response ToReadModel({{entity}} entity) =>
                    new({{string.Join(", ", responseArgs)}});
            }

            {{authorization}}

            {{concurrency}}
            """;
    }

    private static string BuildEntityConfiguration(
        string prefix,
        StudioModuleBlueprint module,
        StudioResourceBlueprint resource,
        IReadOnlyDictionary<string, (StudioModuleBlueprint Module, StudioResourceBlueprint Resource)> resourceMap)
    {
        var entity = $"{prefix}.Domain.GeneratedModules.{module.Name}.{resource.Name}";
        var body = new StringBuilder();
        foreach (var field in resource.Fields)
        {
            if (field.Type == StudioFieldType.Text)
            {
                body.Append("        builder.Property(entity => entity.").Append(field.Name)
                    .Append(").HasMaxLength(").Append(field.MaximumLength).Append(')');
                if (field.Required)
                    body.Append(".IsRequired()");
                body.AppendLine(";");
            }
            else
            {
                body.Append("        builder.Property(entity => entity.").Append(field.Name).AppendLine(");");
            }

            if (field.Indexed)
            {
                body.Append("        builder.HasIndex(entity => entity.").Append(field.Name).Append(")");
                if (field.Unique)
                    body.Append(".IsUnique()");
                body.AppendLine(";");
            }

            if (field.Type == StudioFieldType.Reference &&
                resourceMap.TryGetValue(field.ReferenceResource!, out var target))
            {
                var targetEntity = $"{prefix}.Domain.GeneratedModules.{target.Module.Name}.{target.Resource.Name}";
                body.Append("        builder.HasOne<").Append(targetEntity).Append(">()")
                    .Append(".WithMany().HasForeignKey(entity => entity.").Append(field.Name)
                    .AppendLine(").OnDelete(DeleteBehavior.Restrict);");
            }
        }
        if (resource.Concurrency)
            body.AppendLine("        builder.Property(entity => entity.Version).IsConcurrencyToken();");

        return $$"""
            #nullable enable

            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Metadata.Builders;

            namespace {{prefix}}.Infrastructure.GeneratedModules.{{module.Name}};

            public sealed class {{resource.Name}}EntityConfiguration
                : IEntityTypeConfiguration<{{entity}}>
            {
                public void Configure(EntityTypeBuilder<{{entity}}> builder)
                {
                    builder.ToTable({{JsonSerializer.Serialize(TableName(prefix, resource))}});
                    builder.HasKey(entity => entity.Id);
            {{body.ToString().TrimEnd()}}
                }
            }
            """;
    }

    private static async Task PatchMigrationAsync(
        StudioBlueprintCompilation compilation,
        GeneratedProjectResult generated,
        CancellationToken cancellationToken)
    {
        var prefix = Path.GetFileNameWithoutExtension(generated.SolutionPath);
        var path = Path.Combine(
            generated.OutputDirectory,
            "src",
            $"{prefix}.Infrastructure",
            "GeneratedPlatform",
            "Migrations",
            "20260811000000_InitialGenerated.cs");
        if (!File.Exists(path))
            return;

        var migration = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        foreach (var resource in compilation.Blueprint.Modules.SelectMany(module => module.Resources))
        {
            foreach (var field in resource.Fields.Where(field => field.Type != StudioFieldType.Text))
            {
                var old = $"{field.Name} = table.Column<string>(type: \"nvarchar(128)\", maxLength: 128, nullable: {(!field.Required).ToString().ToLowerInvariant()})";
                var replacement = BuildSqlColumn(field);
                if (!migration.Contains(old, StringComparison.Ordinal))
                {
                    throw new ComposerGenerationException(
                        $"Studio could not specialize generated SQL column '{resource.Name}.{field.Name}'.");
                }
                migration = migration.Replace(old, replacement, StringComparison.Ordinal);
            }
        }

        await File.WriteAllTextAsync(path, Normalize(migration), new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
    }

    private static string BuildSqlColumn(StudioFieldBlueprint field)
    {
        var nullable = (!field.Required).ToString().ToLowerInvariant();
        return field.Type switch
        {
            StudioFieldType.Integer => $"{field.Name} = table.Column<int>(type: \"int\", nullable: {nullable})",
            StudioFieldType.Decimal => $"{field.Name} = table.Column<decimal>(type: \"decimal(18,2)\", nullable: {nullable})",
            StudioFieldType.Boolean => $"{field.Name} = table.Column<bool>(type: \"bit\", nullable: {nullable})",
            StudioFieldType.Date => $"{field.Name} = table.Column<DateOnly>(type: \"date\", nullable: {nullable})",
            StudioFieldType.DateTime => $"{field.Name} = table.Column<DateTimeOffset>(type: \"datetimeoffset\", nullable: {nullable})",
            StudioFieldType.Guid or StudioFieldType.Reference => $"{field.Name} = table.Column<Guid>(type: \"uniqueidentifier\", nullable: {nullable})",
            _ => throw new ComposerGenerationException($"Unsupported Studio SQL field type '{field.Type}'.")
        };
    }

    private static string BuildContractParameter(StudioFieldBlueprint field)
    {
        if (field.Type != StudioFieldType.Text)
            return $"{ClrType(field)} {field.Name}";
        var attributes = new List<string>();
        if (field.Required)
            attributes.Add("Required");
        attributes.Add($"StringLength({field.MaximumLength})");
        return $"[property: {string.Join(", ", attributes)}] {ClrType(field)} {field.Name}";
    }

    private static string ClrType(StudioFieldBlueprint field)
    {
        var core = field.Type switch
        {
            StudioFieldType.Text => "string",
            StudioFieldType.Integer => "int",
            StudioFieldType.Decimal => "decimal",
            StudioFieldType.Boolean => "bool",
            StudioFieldType.Date => "DateOnly",
            StudioFieldType.DateTime => "DateTimeOffset",
            StudioFieldType.Guid or StudioFieldType.Reference => "Guid",
            _ => throw new ComposerGenerationException($"Unsupported Studio field type '{field.Type}'.")
        };
        return field.Required || field.Type == StudioFieldType.Text && field.Required
            ? core
            : core + "?";
    }

    private static async Task WriteTypeReportAsync(
        StudioBlueprintCompilation compilation,
        string output,
        CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Studio typed data model");
        builder.AppendLine();
        foreach (var module in compilation.Blueprint.Modules)
        {
            builder.Append("## ").AppendLine(module.Name);
            builder.AppendLine();
            foreach (var resource in module.Resources)
            {
                builder.Append("### ").AppendLine(resource.Name);
                builder.AppendLine();
                foreach (var field in resource.Fields)
                {
                    builder.Append("- `").Append(field.Name).Append("`: ").Append(field.Type);
                    if (field.Required) builder.Append(" required");
                    if (field.Indexed) builder.Append(" indexed");
                    if (field.Unique) builder.Append(" unique");
                    if (field.Type == StudioFieldType.Reference) builder.Append(" → `").Append(field.ReferenceResource).Append('`');
                    builder.AppendLine();
                }
                builder.AppendLine();
            }
        }
        await WriteAsync(Path.Combine(output, "STUDIO-DATA-MODEL.md"), builder.ToString(), cancellationToken).ConfigureAwait(false);
    }

    private static string TableName(string prefix, StudioResourceBlueprint resource)
    {
        // Base Composer owns deterministic table naming. This fallback is only used by EF configuration;
        // the generated migration remains the source of truth for the exact physical table name.
        // Project prefix is already deterministic from the same product name.
        var normalizedPrefix = new string(prefix.ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) ? c : '_').ToArray()).Trim('_');
        var normalizedRoute = new string(resource.Route.ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) ? c : '_').ToArray()).Trim('_');
        return $"{normalizedPrefix}_{normalizedRoute}";
    }

    private static string Camel(string value) => char.ToLowerInvariant(value[0]) + value[1..];

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
}
