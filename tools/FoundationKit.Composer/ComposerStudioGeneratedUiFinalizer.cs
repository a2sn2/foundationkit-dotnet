using System.Globalization;
using System.Text;

namespace FoundationKit.Composer;

public static class ComposerStudioGeneratedUiFinalizer
{
    public static async Task ApplyAsync(
        GeneratedProjectResult generated,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(generated);
        var prefix = Path.GetFileNameWithoutExtension(generated.SolutionPath);
        var pagesRoot = Path.Combine(
            generated.OutputDirectory,
            "src",
            $"{prefix}.Client",
            "Pages",
            "Generated");
        if (!Directory.Exists(pagesRoot))
            return;

        foreach (var pagePath in Directory.EnumerateFiles(pagesRoot, "*.razor", SearchOption.TopDirectoryOnly))
        {
            var source = await File.ReadAllTextAsync(pagePath, cancellationToken).ConfigureAwait(false);
            const string marker = "    private void Prepare(HttpRequestMessage request)";
            if (!source.Contains(marker, StringComparison.Ordinal))
                continue;
            if (source.Contains("private static string RequireText", StringComparison.Ordinal))
                continue;

            source = source.Replace(marker, BuildHelpers() + "\n\n" + marker, StringComparison.Ordinal);
            await File.WriteAllTextAsync(
                pagePath,
                Normalize(source),
                new UTF8Encoding(false),
                cancellationToken).ConfigureAwait(false);
        }
    }

    private static string BuildHelpers() => """
        private static string RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"{name} is required.");
            return value.Trim();
        }

        private static int? ParseInt(string value, bool required, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (required) throw new InvalidOperationException($"{name} is required.");
                return null;
            }
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                throw new InvalidOperationException($"{name} must be a valid integer.");
            return parsed;
        }

        private static decimal? ParseDecimal(string value, bool required, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (required) throw new InvalidOperationException($"{name} is required.");
                return null;
            }
            if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                throw new InvalidOperationException($"{name} must be a valid decimal number.");
            return parsed;
        }

        private static DateOnly? ParseDate(string value, bool required, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (required) throw new InvalidOperationException($"{name} is required.");
                return null;
            }
            if (!DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                throw new InvalidOperationException($"{name} must be a valid date.");
            return parsed;
        }

        private static DateTimeOffset? ParseDateTime(string value, bool required, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (required) throw new InvalidOperationException($"{name} is required.");
                return null;
            }
            if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
                throw new InvalidOperationException($"{name} must be a valid date/time.");
            return parsed;
        }

        private static Guid? ParseGuid(string value, bool required, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (required) throw new InvalidOperationException($"{name} is required.");
                return null;
            }
            if (!Guid.TryParse(value, out var parsed) || parsed == Guid.Empty)
                throw new InvalidOperationException($"{name} must be a non-empty GUID.");
            return parsed;
        }
        """;

    private static string Normalize(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .TrimEnd() + "\n";
}
