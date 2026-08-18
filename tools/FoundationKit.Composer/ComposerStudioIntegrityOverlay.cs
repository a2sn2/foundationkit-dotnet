using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FoundationKit.Composer;

public static class ComposerStudioIntegrityOverlay
{
    private const string RelationMigrationId = "20260811001000_StudioRelations";

    public static async Task ApplyAsync(
        StudioBlueprintCompilation compilation,
        GeneratedProjectResult generated,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(compilation);
        ArgumentNullException.ThrowIfNull(generated);
        var prefix = Path.GetFileNameWithoutExtension(generated.SolutionPath);

        foreach (var module in compilation.Blueprint.Modules)
        {
            foreach (var resource in module.Resources)
            {
                var path = Path.Combine(
                    generated.OutputDirectory,
                    "src",
                    $"{prefix}.Infrastructure",
                    "GeneratedModules",
                    module.Name,
                    $"{resource.Name}EntityConfiguration.cs");
                if (!File.Exists(path))
                    continue;

                var source = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
                var marker = "builder.ToTable(";
                var lineStart = source.IndexOf(marker, StringComparison.Ordinal);
                if (lineStart < 0)
                    throw new ComposerGenerationException($"Studio could not locate EF table mapping for '{resource.Name}'.");
                var lineEnd = source.IndexOf(';', lineStart);
                if (lineEnd < 0)
                    throw new ComposerGenerationException($"Studio found an invalid EF table mapping for '{resource.Name}'.");
                var replacement = $"builder.ToTable({JsonSerializer.Serialize(TableName(compilation.Blueprint.Name, resource))})";
                source = source[..lineStart] + replacement + source[(lineEnd)..];
                await File.WriteAllTextAsync(path, Normalize(source), new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
            }
        }

        await WriteRelationsMigrationAsync(compilation, generated, prefix, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteRelationsMigrationAsync(
        StudioBlueprintCompilation compilation,
        GeneratedProjectResult generated,
        string prefix,
        CancellationToken cancellationToken)
    {
        var resourceMap = compilation.Blueprint.Modules
            .SelectMany(module => module.Resources)
            .ToDictionary(resource => resource.Name, StringComparer.OrdinalIgnoreCase);
        var relations = compilation.Blueprint.Modules
            .SelectMany(module => module.Resources)
            .SelectMany(resource => resource.Fields
                .Where(field => field.Type == StudioFieldType.Reference)
                .Select(field => (Resource: resource, Field: field)))
            .ToArray();
        if (relations.Length == 0)
            return;

        var up = new StringBuilder();
        var down = new StringBuilder();
        foreach (var relation in relations)
        {
            var target = resourceMap[relation.Field.ReferenceResource!];
            var table = TableName(compilation.Blueprint.Name, relation.Resource);
            var principal = TableName(compilation.Blueprint.Name, target);
            var index = RelationIndexName(compilation.Blueprint.Name, relation.Resource, relation.Field);
            var foreignKey = ForeignKeyName(compilation.Blueprint.Name, relation.Resource, relation.Field, target);

            if (!relation.Field.Indexed)
            {
                up.AppendLine($"        migrationBuilder.CreateIndex(name: {JsonSerializer.Serialize(index)}, table: {JsonSerializer.Serialize(table)}, column: {JsonSerializer.Serialize(relation.Field.Name)});");
                down.Insert(0, $"        migrationBuilder.DropIndex(name: {JsonSerializer.Serialize(index)}, table: {JsonSerializer.Serialize(table)});\n");
            }

            up.AppendLine($"        migrationBuilder.AddForeignKey(name: {JsonSerializer.Serialize(foreignKey)}, table: {JsonSerializer.Serialize(table)}, column: {JsonSerializer.Serialize(relation.Field.Name)}, principalTable: {JsonSerializer.Serialize(principal)}, principalColumn: \"Id\", onDelete: ReferentialAction.Restrict);");
            down.Insert(0, $"        migrationBuilder.DropForeignKey(name: {JsonSerializer.Serialize(foreignKey)}, table: {JsonSerializer.Serialize(table)});\n");
        }

        var content = $$"""
            #nullable enable

            using Microsoft.EntityFrameworkCore.Infrastructure;
            using Microsoft.EntityFrameworkCore.Migrations;

            namespace {{prefix}}.Infrastructure.GeneratedPlatform.Migrations;

            [DbContext(typeof({{prefix}}.Infrastructure.GeneratedPlatform.GeneratedDbContext))]
            [Migration({{JsonSerializer.Serialize(RelationMigrationId)}})]
            public sealed class StudioRelations : Migration
            {
                protected override void Up(MigrationBuilder migrationBuilder)
                {
            {{up.ToString().TrimEnd()}}
                }

                protected override void Down(MigrationBuilder migrationBuilder)
                {
            {{down.ToString().TrimEnd()}}
                }
            }
            """;

        var path = Path.Combine(
            generated.OutputDirectory,
            "src",
            $"{prefix}.Infrastructure",
            "GeneratedPlatform",
            "Migrations",
            $"{RelationMigrationId}.cs");
        await WriteAsync(path, content, cancellationToken).ConfigureAwait(false);
    }

    public static string TableName(string projectName, StudioResourceBlueprint resource)
    {
        var suffix = SqlIdentifier(resource.Route);
        if (suffix.Length > 56)
            suffix = $"{suffix[..47].TrimEnd('_')}_{ShortHash(resource.Route)}";
        return $"{SqlNamespace(projectName)}_{suffix}";
    }

    private static string SqlNamespace(string projectName)
    {
        var normalized = new string(projectName.ToLowerInvariant()
            .Select(character => char.IsAsciiLetterOrDigit(character) ? character : '_')
            .ToArray()).Trim('_');
        if (normalized.Length > 32)
            normalized = normalized[..32].TrimEnd('_');
        return $"{normalized}_{ShortHash(projectName)}";
    }

    private static string RelationIndexName(string projectName, StudioResourceBlueprint resource, StudioFieldBlueprint field) =>
        LimitIdentifier($"IX_{TableName(projectName, resource)}_{SqlIdentifier(field.Name)}");

    private static string ForeignKeyName(
        string projectName,
        StudioResourceBlueprint resource,
        StudioFieldBlueprint field,
        StudioResourceBlueprint target) =>
        LimitIdentifier($"FK_{TableName(projectName, resource)}_{TableName(projectName, target)}_{SqlIdentifier(field.Name)}");

    private static string LimitIdentifier(string raw)
    {
        if (raw.Length <= 120)
            return raw;
        return $"{raw[..111].TrimEnd('_')}_{ShortHash(raw)}";
    }

    private static string SqlIdentifier(string value) =>
        new(value.ToLowerInvariant().Select(character => char.IsAsciiLetterOrDigit(character) ? character : '_').ToArray());

    private static string ShortHash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..8].ToLowerInvariant();

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
