using FoundationKit.Application.Capabilities;

namespace FoundationKit.Composer;

public static class ComposerCli
{
    public static async Task<int> RunAsync(
        IReadOnlyList<string> args,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (args.Count == 0 || IsHelp(args[0]))
        {
            await WriteHelpAsync(output);
            return 0;
        }

        try
        {
            return args[0].ToLowerInvariant() switch
            {
                "capabilities" => await ListCapabilitiesAsync(args, output),
                "profiles" => await ListProfilesAsync(args, output),
                "validate" => await ValidateAsync(args, output, cancellationToken),
                "explain" => await ExplainAsync(args, output, cancellationToken),
                "new" => await NewAsync(args, output, cancellationToken),
                _ => await UnknownCommandAsync(args[0], error)
            };
        }
        catch (ComposerManifestException exception)
        {
            await error.WriteLineAsync($"Manifest error: {exception.Message}");
            return 2;
        }
        catch (ComposerGenerationException exception)
        {
            await error.WriteLineAsync($"Generation error: {exception.Message}");
            return 2;
        }
        catch (KeyNotFoundException exception)
        {
            await error.WriteLineAsync($"Composition error: {exception.Message}");
            return 2;
        }
        catch (InvalidOperationException exception)
        {
            await error.WriteLineAsync($"Composition error: {exception.Message}");
            return 2;
        }
    }

    private static async Task<int> ListCapabilitiesAsync(
        IReadOnlyList<string> args,
        TextWriter output)
    {
        if (args.Count != 1)
        {
            throw new ComposerManifestException("Usage: capabilities");
        }

        await output.WriteLineAsync("ID | Contract | Kind | Maturity | Category | Dependencies");
        foreach (var capability in FoundationCapabilityCatalog.All
                     .OrderBy(item => item.Category, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(item => item.Id, StringComparer.OrdinalIgnoreCase))
        {
            var dependencies = capability.Dependencies.Count == 0
                ? "-"
                : string.Join(",", capability.Dependencies);
            var contractVersion = FoundationCapabilityContracts.Get(capability.Id).ContractVersion;
            await output.WriteLineAsync(
                $"{capability.Id} | v{contractVersion} | {capability.Kind} | {capability.Maturity} | " +
                $"{capability.Category} | {dependencies}");
        }

        return 0;
    }

    private static async Task<int> ListProfilesAsync(
        IReadOnlyList<string> args,
        TextWriter output)
    {
        if (args.Count != 1)
        {
            throw new ComposerManifestException("Usage: profiles");
        }

        await output.WriteLineAsync("ID | Name | Capabilities");
        foreach (var profile in FoundationCapabilityProfiles.All
                     .OrderBy(item => item.Id, StringComparer.OrdinalIgnoreCase))
        {
            await output.WriteLineAsync(
                $"{profile.Id} | {profile.DisplayName} | {string.Join(",", profile.CapabilityIds)}");
        }

        return 0;
    }

    private static async Task<int> ValidateAsync(
        IReadOnlyList<string> args,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        if (args.Count is < 2 or > 3)
        {
            throw new ComposerManifestException(
                "Usage: validate <manifest.json> [--require-stable]");
        }

        var requireStable = args.Count == 3;
        if (requireStable && !args[2].Equals("--require-stable", StringComparison.OrdinalIgnoreCase))
        {
            throw new ComposerManifestException(
                "The only supported validate option is '--require-stable'.");
        }

        var manifest = await ComposerManifestParser.ParseFileAsync(args[1], cancellationToken);
        var analysis = CompositionAnalyzer.Analyze(manifest);

        await output.WriteLineAsync(
            $"Manifest valid: {manifest.Name} ({manifest.Profile}), {analysis.Entries.Count} resolved capabilities.");

        if (analysis.CompatibilityResults.Count > 0)
        {
            await output.WriteLineAsync(
                $"Contract requirements satisfied: {analysis.CompatibilityResults.Count}.");
        }

        foreach (var warning in analysis.Warnings)
        {
            await output.WriteLineAsync($"WARNING: {warning}");
        }

        if (requireStable && !analysis.IsStableOnly)
        {
            await output.WriteLineAsync(
                "NOT READY: this composition includes capabilities that are not Stable.");
            return 3;
        }

        return 0;
    }

    private static async Task<int> ExplainAsync(
        IReadOnlyList<string> args,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        if (args.Count != 2)
        {
            throw new ComposerManifestException("Usage: explain <manifest.json>");
        }

        var manifest = await ComposerManifestParser.ParseFileAsync(args[1], cancellationToken);
        var analysis = CompositionAnalyzer.Analyze(manifest);
        var compatibilityById = analysis.CompatibilityResults.ToDictionary(
            result => result.CapabilityId,
            StringComparer.OrdinalIgnoreCase);

        await output.WriteLineAsync($"Project: {manifest.Name}");
        await output.WriteLineAsync($"Profile: {manifest.Profile}");
        await output.WriteLineAsync("Resolved capabilities (dependency-first):");

        foreach (var entry in analysis.Entries)
        {
            var contractVersion = FoundationCapabilityContracts.Get(entry.Capability.Id).ContractVersion;
            var compatibility = compatibilityById.TryGetValue(entry.Capability.Id, out var requirement)
                ? $" | requires:v{requirement.RequiredContractVersion}=compatible"
                : string.Empty;

            await output.WriteLineAsync(
                $"- {entry.Capability.Id} [{entry.Capability.Kind}/{entry.Capability.Maturity}/contract:v{contractVersion}] " +
                $"<- {string.Join(", ", entry.Reasons)}{compatibility}");
        }

        if (analysis.Warnings.Count > 0)
        {
            await output.WriteLineAsync("Maturity warnings:");
            foreach (var warning in analysis.Warnings)
            {
                await output.WriteLineAsync($"- {warning}");
            }
        }

        return 0;
    }

    private static async Task<int> NewAsync(
        IReadOnlyList<string> args,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        if (args.Count < 4)
        {
            throw new ComposerManifestException(
                "Usage: new <manifest.json> --output <directory> [--foundation-root <directory>] [--force] [--require-stable]");
        }

        var manifestPath = args[1];
        string? outputDirectory = null;
        string? foundationRoot = null;
        var force = false;
        var requireStable = false;

        for (var index = 2; index < args.Count; index++)
        {
            var option = args[index];
            if (option.Equals("--output", StringComparison.OrdinalIgnoreCase))
            {
                if (outputDirectory is not null)
                {
                    throw new ComposerManifestException("Option '--output' can be specified only once.");
                }

                outputDirectory = ReadOptionValue(args, ref index, "--output");
            }
            else if (option.Equals("--foundation-root", StringComparison.OrdinalIgnoreCase))
            {
                if (foundationRoot is not null)
                {
                    throw new ComposerManifestException("Option '--foundation-root' can be specified only once.");
                }

                foundationRoot = ReadOptionValue(args, ref index, "--foundation-root");
            }
            else if (option.Equals("--force", StringComparison.OrdinalIgnoreCase))
            {
                if (force)
                {
                    throw new ComposerManifestException("Option '--force' can be specified only once.");
                }

                force = true;
            }
            else if (option.Equals("--require-stable", StringComparison.OrdinalIgnoreCase))
            {
                if (requireStable)
                {
                    throw new ComposerManifestException("Option '--require-stable' can be specified only once.");
                }

                requireStable = true;
            }
            else
            {
                throw new ComposerManifestException($"Unknown new option '{option}'.");
            }
        }

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ComposerManifestException("The new command requires '--output <directory>'.");
        }

        var manifest = await ComposerManifestParser.ParseFileAsync(manifestPath, cancellationToken);
        var analysis = CompositionAnalyzer.Analyze(manifest);

        foreach (var warning in analysis.Warnings)
        {
            await output.WriteLineAsync($"WARNING: {warning}");
        }

        if (requireStable && !analysis.IsStableOnly)
        {
            await output.WriteLineAsync(
                "NOT GENERATED: this composition includes capabilities that are not Stable.");
            return 3;
        }

        var result = await ComposerProjectGenerator.GenerateAsync(
            analysis,
            new ProjectGenerationOptions(outputDirectory, foundationRoot, force),
            cancellationToken);

        await output.WriteLineAsync($"Generated project: {manifest.Name}");
        await output.WriteLineAsync($"Output: {result.OutputDirectory}");
        await output.WriteLineAsync($"Solution: {result.SolutionPath}");
        await output.WriteLineAsync($"Foundation references: {result.ReferenceMode}");
        await output.WriteLineAsync($"Generated files: {result.GeneratedFiles.Count}");
        return 0;
    }

    private static string ReadOptionValue(
        IReadOnlyList<string> args,
        ref int index,
        string optionName)
    {
        if (index + 1 >= args.Count)
        {
            throw new ComposerManifestException($"Option '{optionName}' requires a value.");
        }

        var value = args[++index];
        if (string.IsNullOrWhiteSpace(value) || value.StartsWith("--", StringComparison.Ordinal))
        {
            throw new ComposerManifestException($"Option '{optionName}' requires a value.");
        }

        return value;
    }

    private static async Task<int> UnknownCommandAsync(string command, TextWriter error)
    {
        await error.WriteLineAsync($"Unknown command '{command}'. Use --help.");
        return 2;
    }

    private static bool IsHelp(string value) =>
        value.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("help", StringComparison.OrdinalIgnoreCase);

    private static async Task WriteHelpAsync(TextWriter output)
    {
        await output.WriteLineAsync("FoundationKit Composer CLI v1");
        await output.WriteLineAsync();
        await output.WriteLineAsync("Commands:");
        await output.WriteLineAsync("  capabilities");
        await output.WriteLineAsync("  profiles");
        await output.WriteLineAsync("  validate <manifest.json> [--require-stable]");
        await output.WriteLineAsync("  explain <manifest.json>");
        await output.WriteLineAsync(
            "  new <manifest.json> --output <directory> [--foundation-root <directory>] [--force] [--require-stable]");
        await output.WriteLineAsync();
        await output.WriteLineAsync(
            "The v1 composer validates the canonical capability graph and can generate a deterministic product skeleton. Interactive composition remains future work.");
    }
}
