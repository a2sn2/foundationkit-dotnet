using System.Globalization;
using System.Text.Json;
using FoundationKit.Application.Capabilities;

namespace FoundationKit.Composer;

internal static class ComposerInteractiveSession
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<ComposerManifest?> CollectManifestAsync(
        TextReader input,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        await output.WriteLineAsync("FoundationKit Composer interactive project setup");
        await output.WriteLineAsync("Type 'cancel' at any prompt to stop before generation.");
        await output.WriteLineAsync();

        var projectName = await ReadProjectNameAsync(input, output, cancellationToken);
        if (projectName is null)
        {
            return await CancelAsync(output);
        }

        var profile = await ReadProfileAsync(input, output, cancellationToken);
        if (profile is null)
        {
            return await CancelAsync(output);
        }

        var includeCapabilities = await ReadOptionalCapabilitiesAsync(
            input,
            output,
            profile,
            cancellationToken);
        if (includeCapabilities is null)
        {
            return await CancelAsync(output);
        }

        var providers = await ReadProvidersAsync(input, output, cancellationToken);
        if (providers is null)
        {
            return await CancelAsync(output);
        }

        return CreateValidatedManifest(projectName, profile.Id, includeCapabilities, providers);
    }

    public static async Task WritePreviewAsync(
        CompositionAnalysis analysis,
        TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(output);

        await output.WriteLineAsync();
        await output.WriteLineAsync("Composition preview");
        await output.WriteLineAsync($"  Project: {analysis.Manifest.Name}");
        await output.WriteLineAsync($"  Profile: {analysis.Manifest.Profile}");
        await output.WriteLineAsync(
            $"  Extra capabilities: {FormatSelection(analysis.Manifest.IncludeCapabilities)}");
        await output.WriteLineAsync($"  Providers: {FormatSelection(analysis.Manifest.Providers)}");
        await output.WriteLineAsync("  Resolved capabilities (dependency-first):");

        foreach (var entry in analysis.Entries)
        {
            var contractVersion = FoundationCapabilityContracts.Get(entry.Capability.Id).ContractVersion;
            await output.WriteLineAsync(
                $"    - {entry.Capability.Id} [{entry.Capability.Kind}/{entry.Capability.Maturity}/contract:v{contractVersion}]");
        }

        if (analysis.Warnings.Count > 0)
        {
            await output.WriteLineAsync("  Maturity warnings:");
            foreach (var warning in analysis.Warnings)
            {
                await output.WriteLineAsync($"    - {warning}");
            }
        }
    }

    public static async Task<bool> ConfirmGenerationAsync(
        TextReader input,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        while (true)
        {
            var answer = await PromptAsync(
                input,
                output,
                "Generate this project now? [y/N]: ",
                cancellationToken);

            if (answer is null || answer.Length == 0 ||
                answer.Equals("n", StringComparison.OrdinalIgnoreCase) ||
                answer.Equals("no", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (answer.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                answer.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            await output.WriteLineAsync("Please answer 'y' or 'n'.");
        }
    }

    private static async Task<string?> ReadProjectNameAsync(
        TextReader input,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var value = await PromptAsync(
                input,
                output,
                "Project name: ",
                cancellationToken);
            if (value is null)
            {
                return null;
            }

            if (value.Length == 0)
            {
                await output.WriteLineAsync("Project name is required.");
                continue;
            }

            try
            {
                return CreateValidatedManifest(
                    value,
                    FoundationCapabilityProfiles.Minimal,
                    Array.Empty<string>(),
                    Array.Empty<string>()).Name;
            }
            catch (ComposerManifestException exception)
            {
                await output.WriteLineAsync($"Invalid project name: {exception.Message}");
            }
        }
    }

    private static async Task<CapabilityProfile?> ReadProfileAsync(
        TextReader input,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        var profiles = FoundationCapabilityProfiles.All.ToArray();
        await WriteProfilesAsync(output, profiles);

        while (true)
        {
            var value = await PromptAsync(
                input,
                output,
                $"Profile [1-{profiles.Length}, default 1={profiles[0].Id}, ?=list]: ",
                cancellationToken);
            if (value is null)
            {
                return null;
            }

            if (value.Length == 0)
            {
                return profiles[0];
            }

            if (value.Equals("?", StringComparison.Ordinal))
            {
                await WriteProfilesAsync(output, profiles);
                continue;
            }

            if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) &&
                number >= 1 && number <= profiles.Length)
            {
                return profiles[number - 1];
            }

            var profile = profiles.FirstOrDefault(item =>
                item.Id.Equals(value, StringComparison.OrdinalIgnoreCase));
            if (profile is not null)
            {
                return profile;
            }

            await output.WriteLineAsync(
                $"Unknown profile '{value}'. Enter a profile number, canonical ID, or '?'.");
        }
    }

    private static async Task<IReadOnlyList<string>?> ReadOptionalCapabilitiesAsync(
        TextReader input,
        TextWriter output,
        CapabilityProfile profile,
        CancellationToken cancellationToken)
    {
        var allOptional = FoundationCapabilityCatalog.All
            .Where(capability => capability.Kind == CapabilityKind.Optional)
            .OrderBy(capability => capability.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(capability => capability.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var byId = allOptional.ToDictionary(capability => capability.Id, StringComparer.OrdinalIgnoreCase);
        var profileCapabilities = profile.CapabilityIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var available = allOptional
            .Where(capability => !profileCapabilities.Contains(capability.Id))
            .ToArray();

        await WriteCapabilitiesAsync(output, "Optional capabilities not already in the profile", available);

        while (true)
        {
            var value = await PromptAsync(
                input,
                output,
                "Extra capabilities [comma-separated IDs, Enter=none, ?=list]: ",
                cancellationToken);
            if (value is null)
            {
                return null;
            }

            if (value.Length == 0 || value.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return Array.Empty<string>();
            }

            if (value.Equals("?", StringComparison.Ordinal))
            {
                await WriteCapabilitiesAsync(
                    output,
                    "Optional capabilities not already in the profile",
                    available);
                continue;
            }

            var parsed = ParseIds(value);
            if (parsed.Count == 0)
            {
                await output.WriteLineAsync("Enter one or more canonical capability IDs, or press Enter for none.");
                continue;
            }

            string? problem = null;
            foreach (var id in parsed)
            {
                if (!byId.ContainsKey(id))
                {
                    problem = $"Unknown optional capability '{id}'.";
                    break;
                }

                if (profileCapabilities.Contains(id))
                {
                    problem = $"Capability '{id}' is already included by profile '{profile.Id}'.";
                    break;
                }
            }

            if (problem is not null)
            {
                await output.WriteLineAsync(problem);
                continue;
            }

            return parsed;
        }
    }

    private static async Task<IReadOnlyList<string>?> ReadProvidersAsync(
        TextReader input,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        var providers = FoundationCapabilityCatalog.All
            .Where(capability => capability.Kind == CapabilityKind.Provider)
            .OrderBy(capability => capability.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var byId = providers.ToDictionary(capability => capability.Id, StringComparer.OrdinalIgnoreCase);

        await WriteCapabilitiesAsync(output, "Available providers", providers);

        while (true)
        {
            var value = await PromptAsync(
                input,
                output,
                "Providers [comma-separated IDs, Enter=none, ?=list]: ",
                cancellationToken);
            if (value is null)
            {
                return null;
            }

            if (value.Length == 0 || value.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return Array.Empty<string>();
            }

            if (value.Equals("?", StringComparison.Ordinal))
            {
                await WriteCapabilitiesAsync(output, "Available providers", providers);
                continue;
            }

            var parsed = ParseIds(value);
            if (parsed.Count == 0)
            {
                await output.WriteLineAsync("Enter one or more canonical provider IDs, or press Enter for none.");
                continue;
            }

            var unknown = parsed.FirstOrDefault(id => !byId.ContainsKey(id));
            if (unknown is not null)
            {
                await output.WriteLineAsync($"Unknown provider '{unknown}'.");
                continue;
            }

            return parsed;
        }
    }

    private static List<string> ParseIds(string value)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.Length > 0 && seen.Add(part))
            {
                result.Add(part);
            }
        }

        return result;
    }

    private static ComposerManifest CreateValidatedManifest(
        string name,
        string profile,
        IReadOnlyList<string> includeCapabilities,
        IReadOnlyList<string> providers)
    {
        var document = new InteractiveManifestDocument(
            1,
            name,
            profile,
            includeCapabilities,
            Array.Empty<string>(),
            providers);
        var json = JsonSerializer.Serialize(document, JsonOptions);
        return ComposerManifestParser.Parse(json);
    }

    private static async Task WriteProfilesAsync(
        TextWriter output,
        CapabilityProfile[] profiles)
    {
        await output.WriteLineAsync("Profiles:");
        for (var index = 0; index < profiles.Length; index++)
        {
            var profile = profiles[index];
            await output.WriteLineAsync(
                $"  {index + 1}. {profile.Id} - {profile.DisplayName}: {profile.Description}");
        }
    }

    private static async Task WriteCapabilitiesAsync(
        TextWriter output,
        string heading,
        CapabilityDescriptor[] capabilities)
    {
        await output.WriteLineAsync($"{heading}:");
        if (capabilities.Length == 0)
        {
            await output.WriteLineAsync("  - none");
            return;
        }

        foreach (var capability in capabilities)
        {
            await output.WriteLineAsync(
                $"  - {capability.Id} [{capability.Maturity}] - {capability.DisplayName}");
        }
    }

    private static async Task<string?> PromptAsync(
        TextReader input,
        TextWriter output,
        string prompt,
        CancellationToken cancellationToken)
    {
        await output.WriteAsync(prompt);
        var line = await input.ReadLineAsync(cancellationToken);
        if (line is null)
        {
            return null;
        }

        var value = line.Trim();
        if (IsCancel(value))
        {
            return null;
        }

        return value;
    }

    private static bool IsCancel(string value) =>
        value.Equals("cancel", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("q", StringComparison.OrdinalIgnoreCase);

    private static string FormatSelection(IReadOnlyList<string> values) =>
        values.Count == 0 ? "none" : string.Join(", ", values);

    private static async Task<ComposerManifest?> CancelAsync(TextWriter output)
    {
        await output.WriteLineAsync("Interactive composition cancelled before generation.");
        return null;
    }

    private sealed record InteractiveManifestDocument(
        int SchemaVersion,
        string Name,
        string Profile,
        IReadOnlyList<string> IncludeCapabilities,
        IReadOnlyList<string> ExcludeCapabilities,
        IReadOnlyList<string> Providers);
}
