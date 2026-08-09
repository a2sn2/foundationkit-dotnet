using FoundationKit.Application.Capabilities;
using FoundationKit.Composer;

namespace FoundationKit.Tests;

public sealed class ComposerTests
{
    [Fact]
    public void Parser_accepts_strict_v1_manifest()
    {
        var manifest = ComposerManifestParser.Parse(
            """
            {
              "schemaVersion": 1,
              "name": "CustomerPortal",
              "profile": "minimal",
              "includeCapabilities": ["auditing"],
              "excludeCapabilities": [],
              "providers": ["provider-sqlserver"]
            }
            """);

        Assert.Equal(1, manifest.SchemaVersion);
        Assert.Equal("CustomerPortal", manifest.Name);
        Assert.Equal(FoundationCapabilityProfiles.Minimal, manifest.Profile);
        Assert.Equal([FoundationCapabilityIds.Auditing], manifest.IncludeCapabilities);
        Assert.Equal([FoundationCapabilityIds.SqlServerProvider], manifest.Providers);
        Assert.Empty(manifest.ContractRequirements);
    }

    [Fact]
    public void Parser_accepts_bounded_capability_contract_requirements()
    {
        var manifest = ComposerManifestParser.Parse(
            """
            {
              "schemaVersion": 1,
              "name": "ApprovalSystem",
              "profile": "minimal",
              "includeCapabilities": ["approvals"],
              "excludeCapabilities": [],
              "providers": [],
              "capabilityContracts": {
                "approvals": 1,
                "authorization": 1
              }
            }
            """);

        Assert.Collection(
            manifest.ContractRequirements,
            requirement =>
            {
                Assert.Equal(FoundationCapabilityIds.Approvals, requirement.CapabilityId);
                Assert.Equal(1, requirement.ContractVersion);
            },
            requirement =>
            {
                Assert.Equal(FoundationCapabilityIds.Authorization, requirement.CapabilityId);
                Assert.Equal(1, requirement.ContractVersion);
            });
    }

    [Fact]
    public void Parser_rejects_invalid_contract_version()
    {
        var exception = Assert.Throws<ComposerManifestException>(() =>
            ComposerManifestParser.Parse(
                """
                {
                  "schemaVersion": 1,
                  "name": "CustomerPortal",
                  "profile": "minimal",
                  "includeCapabilities": [],
                  "excludeCapabilities": [],
                  "providers": [],
                  "capabilityContracts": {
                    "kernel": 0
                  }
                }
                """));

        Assert.Contains("must be an integer from 1", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parser_rejects_unknown_json_fields()
    {
        var exception = Assert.Throws<ComposerManifestException>(() =>
            ComposerManifestParser.Parse(
                """
                {
                  "schemaVersion": 1,
                  "name": "CustomerPortal",
                  "profile": "minimal",
                  "includeCapabilities": [],
                  "excludeCapabilities": [],
                  "providers": [],
                  "surprise": true
                }
                """));

        Assert.Contains("not valid FoundationKit JSON", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("9Invalid")]
    [InlineData("Invalid Name")]
    [InlineData("Invalid/Name")]
    public void Parser_rejects_unsafe_project_names(string projectName)
    {
        var json = $$"""
            {
              "schemaVersion": 1,
              "name": "{{projectName}}",
              "profile": "minimal",
              "includeCapabilities": [],
              "excludeCapabilities": [],
              "providers": []
            }
            """;

        Assert.Throws<ComposerManifestException>(() => ComposerManifestParser.Parse(json));
    }

    [Fact]
    public void Analyzer_rejects_provider_in_capability_list()
    {
        var manifest = ComposerManifestParser.Parse(
            """
            {
              "schemaVersion": 1,
              "name": "CustomerPortal",
              "profile": "minimal",
              "includeCapabilities": ["provider-sqlserver"],
              "excludeCapabilities": [],
              "providers": []
            }
            """);

        var exception = Assert.Throws<ComposerManifestException>(() =>
            CompositionAnalyzer.Analyze(manifest));

        Assert.Contains("must be listed under 'providers'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Analyzer_explains_transitive_approval_dependencies()
    {
        var manifest = new ComposerManifest(
            1,
            "ApprovalSystem",
            FoundationCapabilityProfiles.Minimal,
            [FoundationCapabilityIds.Approvals],
            Array.Empty<string>(),
            Array.Empty<string>());

        var analysis = CompositionAnalyzer.Analyze(manifest);
        var authorization = analysis.Entries.Single(
            entry => entry.Capability.Id == FoundationCapabilityIds.Authorization);
        var auditing = analysis.Entries.Single(
            entry => entry.Capability.Id == FoundationCapabilityIds.Auditing);

        Assert.Contains("required-by:approvals", authorization.Reasons);
        Assert.Contains("required-by:workflow", auditing.Reasons);
        Assert.False(analysis.IsStableOnly);
    }

    [Fact]
    public void Analyzer_accepts_compatible_transitive_contract_requirement()
    {
        var manifest = ComposerManifestParser.Parse(
            """
            {
              "schemaVersion": 1,
              "name": "ApprovalSystem",
              "profile": "minimal",
              "includeCapabilities": ["approvals"],
              "excludeCapabilities": [],
              "providers": [],
              "capabilityContracts": {
                "authorization": 1
              }
            }
            """);

        var analysis = CompositionAnalyzer.Analyze(manifest);
        var compatibility = Assert.Single(analysis.CompatibilityResults);

        Assert.Equal(FoundationCapabilityIds.Authorization, compatibility.CapabilityId);
        Assert.Equal(1, compatibility.RequiredContractVersion);
        Assert.Equal(1, compatibility.AvailableContractVersion);
        Assert.True(compatibility.IsCompatible);
    }

    [Fact]
    public void Analyzer_rejects_incompatible_contract_requirement()
    {
        var manifest = ComposerManifestParser.Parse(
            """
            {
              "schemaVersion": 1,
              "name": "ApprovalSystem",
              "profile": "minimal",
              "includeCapabilities": ["approvals"],
              "excludeCapabilities": [],
              "providers": [],
              "capabilityContracts": {
                "approvals": 2
              }
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            CompositionAnalyzer.Analyze(manifest));

        Assert.Contains("requires contract v2", exception.Message, StringComparison.Ordinal);
        Assert.Contains("provides v1", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Analyzer_rejects_contract_requirement_for_unselected_capability()
    {
        var manifest = ComposerManifestParser.Parse(
            """
            {
              "schemaVersion": 1,
              "name": "MinimalApi",
              "profile": "minimal",
              "includeCapabilities": [],
              "excludeCapabilities": [],
              "providers": [],
              "capabilityContracts": {
                "approvals": 1
              }
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            CompositionAnalyzer.Analyze(manifest));

        Assert.Contains("does not resolve in this composition", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cli_validate_returns_warning_but_success_for_nonstable_selection()
    {
        var path = await WriteManifestAsync(
            """
            {
              "schemaVersion": 1,
              "name": "MinimalApi",
              "profile": "minimal",
              "includeCapabilities": [],
              "excludeCapabilities": [],
              "providers": []
            }
            """);

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();

            var exitCode = await ComposerCli.RunAsync(
                ["validate", path],
                output,
                error);

            Assert.Equal(0, exitCode);
            Assert.Contains("Manifest valid", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("WARNING", output.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Cli_validate_reports_satisfied_contract_requirements()
    {
        var path = await WriteManifestAsync(
            """
            {
              "schemaVersion": 1,
              "name": "ApprovalSystem",
              "profile": "minimal",
              "includeCapabilities": ["approvals"],
              "excludeCapabilities": [],
              "providers": [],
              "capabilityContracts": {
                "approvals": 1
              }
            }
            """);

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();

            var exitCode = await ComposerCli.RunAsync(
                ["validate", path],
                output,
                error);

            Assert.Equal(0, exitCode);
            Assert.Contains("Contract requirements satisfied: 1", output.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Cli_incompatible_contract_fails_closed()
    {
        var path = await WriteManifestAsync(
            """
            {
              "schemaVersion": 1,
              "name": "ApprovalSystem",
              "profile": "minimal",
              "includeCapabilities": ["approvals"],
              "excludeCapabilities": [],
              "providers": [],
              "capabilityContracts": {
                "approvals": 2
              }
            }
            """);

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();

            var exitCode = await ComposerCli.RunAsync(
                ["validate", path],
                output,
                error);

            Assert.Equal(2, exitCode);
            Assert.Contains("requires contract v2", error.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain("Manifest valid", output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Cli_require_stable_fails_closed_when_profile_contains_preview_capability()
    {
        var path = await WriteManifestAsync(
            """
            {
              "schemaVersion": 1,
              "name": "MinimalApi",
              "profile": "minimal",
              "includeCapabilities": [],
              "excludeCapabilities": [],
              "providers": []
            }
            """);

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();

            var exitCode = await ComposerCli.RunAsync(
                ["validate", path, "--require-stable"],
                output,
                error);

            Assert.Equal(3, exitCode);
            Assert.Contains("NOT READY", output.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Cli_explain_outputs_dependency_reason_and_contract_without_echoing_manifest_json()
    {
        var path = await WriteManifestAsync(
            """
            {
              "schemaVersion": 1,
              "name": "ApprovalSystem",
              "profile": "minimal",
              "includeCapabilities": ["approvals"],
              "excludeCapabilities": [],
              "providers": [],
              "capabilityContracts": {
                "authorization": 1
              }
            }
            """);

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();

            var exitCode = await ComposerCli.RunAsync(
                ["explain", path],
                output,
                error);
            var text = output.ToString();

            Assert.Equal(0, exitCode);
            Assert.Contains("authorization", text, StringComparison.Ordinal);
            Assert.Contains("required-by:approvals", text, StringComparison.Ordinal);
            Assert.Contains("contract:v1", text, StringComparison.Ordinal);
            Assert.Contains("requires:v1=compatible", text, StringComparison.Ordinal);
            Assert.DoesNotContain("schemaVersion", text, StringComparison.Ordinal);
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Cli_capabilities_lists_contract_versions()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await ComposerCli.RunAsync(
            ["capabilities"],
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Contains("ID | Contract | Kind", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("kernel | v1 | Kernel", output.ToString(), StringComparison.Ordinal);
        Assert.Equal(string.Empty, error.ToString());
    }

    private static async Task<string> WriteManifestAsync(string json)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"foundationkit-composer-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, json);
        return path;
    }
}
