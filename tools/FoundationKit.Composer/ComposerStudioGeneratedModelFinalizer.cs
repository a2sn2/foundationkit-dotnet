using System.Text;

namespace FoundationKit.Composer;

public static class ComposerStudioGeneratedModelFinalizer
{
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
                var decimalFields = resource.Fields
                    .Where(field => field.Type == StudioFieldType.Decimal)
                    .Select(field => field.Name)
                    .ToArray();
                if (decimalFields.Length == 0)
                    continue;

                var path = Path.Combine(
                    generated.OutputDirectory,
                    "src",
                    $"{prefix}.Infrastructure",
                    "GeneratedModules",
                    module.Name,
                    $"{resource.Name}EntityConfiguration.cs");
                if (!File.Exists(path))
                    throw new ComposerGenerationException($"Studio generated EF configuration was not found for '{module.Name}.{resource.Name}'.");

                var source = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
                foreach (var field in decimalFields)
                {
                    var simple = $"builder.Property(entity => entity.{field});";
                    var precise = $"builder.Property(entity => entity.{field}).HasPrecision(18, 2);";
                    if (source.Contains(precise, StringComparison.Ordinal))
                        continue;
                    if (!source.Contains(simple, StringComparison.Ordinal))
                        throw new ComposerGenerationException($"Studio could not locate generated decimal property '{resource.Name}.{field}' for precision configuration.");
                    source = source.Replace(simple, precise, StringComparison.Ordinal);
                }

                await File.WriteAllTextAsync(
                    path,
                    Normalize(source),
                    new UTF8Encoding(false),
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static string Normalize(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .TrimEnd() + "\n";
}
