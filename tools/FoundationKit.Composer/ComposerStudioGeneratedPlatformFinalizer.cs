using System.Text;
using System.Xml.Linq;

namespace FoundationKit.Composer;

public static class ComposerStudioGeneratedPlatformFinalizer
{
    private const string AbpMvcPackage = "Volo.Abp.AspNetCore.Mvc";
    private const string AbpVersion = "10.6.0";

    public static async Task ApplyAsync(
        GeneratedProjectResult generated,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(generated);
        var prefix = Path.GetFileNameWithoutExtension(generated.SolutionPath);
        var modulePath = Path.Combine(
            generated.OutputDirectory,
            "src",
            $"{prefix}.Api",
            "GeneratedPlatform",
            "GeneratedAbpPlatformModule.cs");
        if (!File.Exists(modulePath))
            return;

        // ABP's supported ASP.NET Core application host uses AbpAspNetCoreMvcModule.
        // The lower-level AbpAspNetCoreModule does not register the complete API-description
        // infrastructure required during application initialization.
        await EnsureAbpMvcPackageAsync(generated.OutputDirectory, prefix, cancellationToken).ConfigureAwait(false);

        var source = await File.ReadAllTextAsync(modulePath, cancellationToken).ConfigureAwait(false);
        source = source
            .Replace("using Volo.Abp.AspNetCore;", "using Volo.Abp.AspNetCore.Mvc;", StringComparison.Ordinal)
            .Replace("typeof(AbpAspNetCoreModule)", "typeof(AbpAspNetCoreMvcModule)", StringComparison.Ordinal);

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
            modulePath,
            normalized.ToString().TrimEnd() + "\n",
            new UTF8Encoding(false),
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureAbpMvcPackageAsync(
        string outputDirectory,
        string prefix,
        CancellationToken cancellationToken)
    {
        var centralPath = Path.Combine(outputDirectory, "Directory.Packages.props");
        var central = XDocument.Parse(await File.ReadAllTextAsync(centralPath, cancellationToken).ConfigureAwait(false));
        var centralRoot = central.Root ?? throw new ComposerGenerationException("Generated Directory.Packages.props is invalid.");
        var versions = centralRoot.Elements("ItemGroup").FirstOrDefault(group => group.Elements("PackageVersion").Any())
            ?? new XElement("ItemGroup");
        if (versions.Parent is null)
            centralRoot.Add(versions);
        if (!versions.Elements("PackageVersion").Any(element =>
                string.Equals((string?)element.Attribute("Include"), AbpMvcPackage, StringComparison.Ordinal)))
        {
            versions.Add(new XElement(
                "PackageVersion",
                new XAttribute("Include", AbpMvcPackage),
                new XAttribute("Version", AbpVersion)));
        }
        await File.WriteAllTextAsync(
            centralPath,
            NormalizeXml(central),
            new UTF8Encoding(false),
            cancellationToken).ConfigureAwait(false);

        var apiPath = Path.Combine(outputDirectory, "src", $"{prefix}.Api", $"{prefix}.Api.csproj");
        var api = XDocument.Parse(await File.ReadAllTextAsync(apiPath, cancellationToken).ConfigureAwait(false));
        var apiRoot = api.Root ?? throw new ComposerGenerationException("Generated API project file is invalid.");
        var references = apiRoot.Elements("ItemGroup").FirstOrDefault(group => group.Elements("PackageReference").Any())
            ?? new XElement("ItemGroup");
        if (references.Parent is null)
            apiRoot.Add(references);
        if (!references.Elements("PackageReference").Any(element =>
                string.Equals((string?)element.Attribute("Include"), AbpMvcPackage, StringComparison.Ordinal)))
        {
            references.Add(new XElement("PackageReference", new XAttribute("Include", AbpMvcPackage)));
        }
        await File.WriteAllTextAsync(
            apiPath,
            NormalizeXml(api),
            new UTF8Encoding(false),
            cancellationToken).ConfigureAwait(false);
    }

    private static string NormalizeXml(XDocument document)
    {
        using var writer = new Utf8StringWriter();
        document.Save(writer, SaveOptions.None);
        return writer.ToString()
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .TrimEnd() + "\n";
    }

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => new UTF8Encoding(false);
    }
}