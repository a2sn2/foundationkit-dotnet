using System.Text.Json;
using FoundationKit.Application.Capabilities;
using FoundationKit.Composer;

namespace FoundationKit.Tests;

public sealed class ComposerProjectModelTests
{
    private const string ValidV2 = """
        {
          "schemaVersion": 2,
          "name": "ProjectModel.Product",
          "profile": "minimal",
          "includeCapabilities": [],
          "excludeCapabilities": [],
          "providers": [],
          "capabilityContracts": {
            "authorization": 1
          },
          "modules": [
            {
              "name": "Sales",
              "resources": [
                {
                  "name": "Customer",
                  "route": "customers",
                  "idType": "guid",
                  "behaviors": ["crud", "auditing", "authorization", "concurrency", "caching"],
                  "overrides": {
                    "manager": "CustomerManager"
                  },
                  "api": {
                    "routePrefix": "api/v1",
                    "idempotency": "required",
                    "concurrency": "require-if-match",
                    "maximumFilters": 4,
                    "maximumSorts": 2,
                    "rateLimitPolicyName": "customer-write"
                  }
                }
              ]
            }
          ]
        }
        """;

    [Fact]
    public void Parser_accepts_strict_v2_project_model_and_normalizes_resource_intent()
    {
        var manifest = ComposerManifestParser.Parse(ValidV2);

        Assert.Equal(2, manifest.SchemaVersion);
        var model = Assert.IsType<ComposerProjectModel>(manifest.ProjectModel);
        var module = Assert.Single(model.Modules);
        var resource = Assert.Single(module.Resources);
        Assert.Equal("Sales", module.Name);
        Assert.Equal("Customer", resource.Name);
        Assert.Equal("customers", resource.Route);
        Assert.Equal(ComposerResourceIdType.Guid, resource.IdType);
        Assert.Contains(ComposerResourceBehavior.Authorization, resource.Behaviors);
        Assert.Equal("CustomerManager", resource.Overrides.Manager);
        Assert.Equal("api/v1", resource.Api.RoutePrefix);
        Assert.Equal(ComposerApiIdempotencyMode.Required, resource.Api.Idempotency);
        Assert.Equal(ComposerApiConcurrencyMode.RequireIfMatch, resource.Api.Concurrency);
        Assert.Contains(FoundationCapabilityIds.Authorization, manifest.ResourceCapabilityIds);
        Assert.Contains(FoundationCapabilityIds.Auditing, manifest.ResourceCapabilityIds);
        Assert.Contains(FoundationCapabilityIds.Caching, manifest.ResourceCapabilityIds);
    }

    [Fact]
    public void Parser_keeps_schema_v1_strict_and_rejects_v2_fields()
    {
        var exception = Assert.Throws<ComposerManifestException>(() => ComposerManifestParser.Parse(
            """
            {
              "schemaVersion": 1,
              "name": "Legacy.Product",
              "profile": "minimal",
              "includeCapabilities": [],
              "excludeCapabilities": [],
              "providers": [],
              "modules": []
            }
            """));

        Assert.Contains("requires schemaVersion 2", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("Unsafe.Manager")]
    [InlineData("9Manager")]
    public void Parser_rejects_unsafe_manager_override(string manager)
    {
        var json = ValidV2.Replace("CustomerManager", manager, StringComparison.Ordinal);

        var exception = Assert.Throws<ComposerManifestException>(() => ComposerManifestParser.Parse(json));

        Assert.Contains("safe C# identifier", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parser_rejects_duplicate_effective_api_routes()
    {
        var json = """
            {
              "schemaVersion": 2,
              "name": "Duplicate.Routes",
              "profile": "minimal",
              "includeCapabilities": [],
              "excludeCapabilities": [],
              "providers": [],
              "modules": [
                {
                  "name": "One",
                  "resources": [
                    { "name": "Customer", "route": "customers", "idType": "guid", "behaviors": ["crud"] }
                  ]
                },
                {
                  "name": "Two",
                  "resources": [
                    { "name": "CustomerArchive", "route": "customers", "idType": "guid", "behaviors": ["crud"] }
                  ]
                }
              ]
            }
            """;

        var exception = Assert.Throws<ComposerManifestException>(() => ComposerManifestParser.Parse(json));

        Assert.Contains("Duplicate resource API route", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parser_rejects_non_crud_resource_until_an_executable_non_crud_engine_exists()
    {
        var json = ValidV2.Replace(
            "[\"crud\", \"auditing\", \"authorization\", \"concurrency\", \"caching\"]",
            "[\"auditing\", \"authorization\"]",
            StringComparison.Ordinal);

        var exception = Assert.Throws<ComposerManifestException>(() => ComposerManifestParser.Parse(json));

        Assert.Contains("must include 'crud'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parser_rejects_global_exclusion_required_by_resource_behavior()
    {
        var json = ValidV2.Replace(
            "\"excludeCapabilities\": []",
            "\"excludeCapabilities\": [\"authorization\"]",
            StringComparison.Ordinal);

        var exception = Assert.Throws<ComposerManifestException>(() => ComposerManifestParser.Parse(json));

        Assert.Contains("required by a resource behavior", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Analyzer_uses_canonical_capability_graph_for_resource_behaviors()
    {
        var manifest = ComposerManifestParser.Parse(ValidV2);

        var analysis = CompositionAnalyzer.Analyze(manifest);
        var authorization = analysis.Entries.Single(entry =>
            entry.Capability.Id == FoundationCapabilityIds.Authorization);
        var identity = analysis.Entries.Single(entry =>
            entry.Capability.Id == FoundationCapabilityIds.Identity);

        Assert.Contains("resource:Sales.Customer:authorization", authorization.Reasons);
        Assert.Contains("required-by:authorization", identity.Reasons);
    }

    [Fact]
    public void Normalized_v2_manifest_is_deterministic_and_retains_project_model()
    {
        var manifest = ComposerManifestParser.Parse(ValidV2);

        var first = ComposerProjectModelGenerator.BuildNormalizedManifest(manifest);
        var second = ComposerProjectModelGenerator.BuildNormalizedManifest(manifest);
        using var document = JsonDocument.Parse(first);

        Assert.Equal(first, second);
        Assert.Equal(2, document.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("Sales", document.RootElement.GetProperty("modules")[0].GetProperty("name").GetString());
        Assert.Equal("Customer", document.RootElement.GetProperty("modules")[0].GetProperty("resources")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task V2_generator_is_deterministic_and_ownership_safe()
    {
        var manifest = ComposerManifestParser.Parse(ValidV2);
        var analysis = CompositionAnalyzer.Analyze(manifest);
        var first = NewTempDirectory();
        var second = NewTempDirectory();

        try
        {
            var firstResult = await ComposerProjectModelGenerator.GenerateAsync(
                analysis,
                new ProjectGenerationOptions(first));
            await ComposerProjectModelGenerator.GenerateAsync(
                analysis,
                new ProjectGenerationOptions(second));

            AssertSnapshotsEqual(ReadSnapshot(first), ReadSnapshot(second));
            Assert.Contains("PROJECT-MODEL.md", firstResult.GeneratedFiles);
            Assert.Contains(
                "src/ProjectModel.Product.Application/GeneratedModules/Sales/CustomerDefinition.g.cs",
                firstResult.GeneratedFiles);

            var normalized = await File.ReadAllTextAsync(Path.Combine(first, "foundationkit.project.json"));
            Assert.Contains("\"schemaVersion\": 2", normalized, StringComparison.Ordinal);
            var marker = await File.ReadAllTextAsync(Path.Combine(first, ".foundationkit-generated.json"));
            Assert.Contains("\"generatorContractVersion\": \"2\"", marker, StringComparison.Ordinal);
            var descriptor = await File.ReadAllTextAsync(Path.Combine(
                first,
                "src",
                "ProjectModel.Product.Application",
                "GeneratedModules",
                "Sales",
                "CustomerDefinition.g.cs"));
            Assert.Contains("CustomerManager", descriptor, StringComparison.Ordinal);
            Assert.Contains("require-if-match", descriptor, StringComparison.Ordinal);

            var before = ReadSnapshot(first);
            await ComposerProjectModelGenerator.GenerateAsync(
                analysis,
                new ProjectGenerationOptions(first, Force: true));
            AssertSnapshotsEqual(before, ReadSnapshot(first));
        }
        finally
        {
            DeleteDirectory(first);
            DeleteDirectory(second);
        }
    }

    private static Dictionary<string, string> ReadSnapshot(string root) =>
        Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .ToDictionary(
                path => Path.GetRelativePath(root, path).Replace('\\', '/'),
                File.ReadAllText,
                StringComparer.Ordinal);

    private static void AssertSnapshotsEqual(
        IReadOnlyDictionary<string, string> expected,
        IReadOnlyDictionary<string, string> actual)
    {
        Assert.Equal(expected.Keys.Order(StringComparer.Ordinal), actual.Keys.Order(StringComparer.Ordinal));
        foreach (var key in expected.Keys)
            Assert.Equal(expected[key], actual[key]);
    }

    private static string NewTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundationkit-composer-v2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
