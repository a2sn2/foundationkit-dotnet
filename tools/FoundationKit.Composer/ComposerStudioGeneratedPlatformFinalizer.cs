using System.Text;

namespace FoundationKit.Composer;

public static class ComposerStudioGeneratedPlatformFinalizer
{
    public static async Task ApplyAsync(
        GeneratedProjectResult generated,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(generated);
        var prefix = Path.GetFileNameWithoutExtension(generated.SolutionPath);
        var path = Path.Combine(
            generated.OutputDirectory,
            "src",
            $"{prefix}.Api",
            "GeneratedPlatform",
            "GeneratedAbpPlatformModule.cs");
        if (!File.Exists(path))
            return;

        var source = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        var seenUsings = new HashSet<string>(StringComparer.Ordinal);
        var lines = source.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Split('\n');
        var normalized = new StringBuilder();
        foreach (var line in lines)
        {
            if (line.StartsWith("using ", StringComparison.Ordinal) &&
                !seenUsings.Add(line.Trim()))
            {
                continue;
            }

            normalized.AppendLine(line);
        }

        await File.WriteAllTextAsync(
            path,
            normalized.ToString().TrimEnd() + "\n",
            new UTF8Encoding(false),
            cancellationToken).ConfigureAwait(false);
    }
}
