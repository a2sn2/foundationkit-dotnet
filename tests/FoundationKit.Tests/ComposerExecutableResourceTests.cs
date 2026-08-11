using FoundationKit.Application.Capabilities;
using FoundationKit.Composer;

namespace FoundationKit.Tests;

public sealed class ComposerExecutableResourceTests
{
    [Fact]
    public void Descriptor_only_v2_resource_remains_compatible_without_sql_provider()
    {
        var manifest = ComposerManifestParser.Parse(
            """
            {
              "schemaVersion": 2,
              "name": "DescriptorOnly",
              "profile": "minimal",
              "includeCapabilities": [],
              "excludeCapabilities": [],
              "providers": [],
              "modules": [
                {
                  "name": "Customers",
                  "resources": [
                    {
                      "name": "Customer",
                      "route": "customers",
                      "idType": "guid",
                      "behaviors": ["crud"],
                      "api": {
                        "maximumFilters": 10,
                        "maximumSorts": 5
                      }
                    }
                  ]
                }
              ]
            }
            """);

        var resource = Assert.Single(Assert.Single(manifest.ProjectModel!.Modules).Resources);
        Assert.False(resource.IsExecutable);
        Assert.Empty(resource.Fields);
        Assert.NotNull(CompositionAnalyzer.Analyze(manifest));
    }

    [Fact]
    public void Parser_accepts_bounded_executable_text_fields()
    {
        var manifest = ComposerManifestParser.Parse(ValidExecutableManifest());
        var resource = Assert.Single(Assert.Single(manifest.ProjectModel!.Modules).Resources);

        Assert.True(resource.IsExecutable);
        Assert.Collection(
            resource.Fields,
            field =>
            {
                Assert.Equal("Name", field.Name);
                Assert.Equal(ComposerResourceFieldType.Text, field.Type);
                Assert.True(field.Required);
                Assert.Equal(120, field.MaximumLength);
                Assert.Equal(ComposerResourceFieldFilterMode.None, field.FilterMode);
                Assert.False(field.Sortable);
                Assert.False(field.Indexed);
                Assert.False(field.Unique);
            },
            field =>
            {
                Assert.Equal("Note", field.Name);
                Assert.Equal(ComposerResourceFieldType.Text, field.Type);
                Assert.False(field.Required);
                Assert.Equal(400, field.MaximumLength);
            });
    }

    [Fact]
    public void Parser_accepts_indexed_prefix_filter_and_sort_intent()
    {
        var manifest = ComposerManifestParser.Parse(QueryableExecutableManifest());
        var resource = Assert.Single(Assert.Single(manifest.ProjectModel!.Modules).Resources);
        var field = resource.Fields.Single(item => item.Name == "Name");

        Assert.Equal(ComposerResourceFieldFilterMode.Prefix, field.FilterMode);
        Assert.True(field.Sortable);
        Assert.True(field.Indexed);
        Assert.False(field.Unique);
        Assert.Equal(3, resource.Api.MaximumFilters);
        Assert.Equal(1, resource.Api.MaximumSorts);
    }

    [Fact]
    public void Parser_rejects_filterable_field_without_index()
    {
        var json = QueryableExecutableManifest().Replace(
            "\"enabled\": true",
            "\"enabled\": false",
            StringComparison.Ordinal);

        var exception = Assert.Throws<ComposerManifestException>(() => ComposerManifestParser.Parse(json));
        Assert.Contains("must enable an index", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parser_rejects_unique_field_without_enabled_index()
    {
        var json = QueryableExecutableManifest()
            .Replace("\"filter\": \"prefix\"", "\"filter\": \"none\"", StringComparison.Ordinal)
            .Replace("\"sortable\": true", "\"sortable\": false", StringComparison.Ordinal)
            .Replace("\"enabled\": true", "\"enabled\": false", StringComparison.Ordinal)
            .Replace("\"unique\": false", "\"unique\": true", StringComparison.Ordinal);

        var exception = Assert.Throws<ComposerManifestException>(() => ComposerManifestParser.Parse(json));
        Assert.Contains("cannot be unique unless index.enabled is true", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parser_rejects_overlong_sql_server_indexed_text_field()
    {
        var json = QueryableExecutableManifest().Replace(
            "\"maximumLength\": 120",
            "\"maximumLength\": 451",
            StringComparison.Ordinal);

        var exception = Assert.Throws<ComposerManifestException>(() => ComposerManifestParser.Parse(json));
        Assert.Contains("cannot exceed 450", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parser_rejects_filter_intent_when_api_disallows_filters()
    {
        var json = QueryableExecutableManifest().Replace(
            "\"maximumFilters\": 3",
            "\"maximumFilters\": 0",
            StringComparison.Ordinal);

        var exception = Assert.Throws<ComposerManifestException>(() => ComposerManifestParser.Parse(json));
        Assert.Contains("declares filterable fields", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parser_rejects_sort_intent_when_api_disallows_sorts()
    {
        var json = QueryableExecutableManifest().Replace(
            "\"maximumSorts\": 1",
            "\"maximumSorts\": 0",
            StringComparison.Ordinal);

        var exception = Assert.Throws<ComposerManifestException>(() => ComposerManifestParser.Parse(json));
        Assert.Contains("declares sortable fields", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parser_rejects_multiple_generated_sorts_until_query_plan_supports_then_by()
    {
        var json = QueryableExecutableManifest().Replace(
            "\"maximumSorts\": 1",
            "\"maximumSorts\": 2",
            StringComparison.Ordinal);

        var exception = Assert.Throws<ComposerManifestException>(() => ComposerManifestParser.Parse(json));
        Assert.Contains("supports at most one SQL sort", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Id")]
    [InlineData("Version")]
    public void Parser_rejects_reserved_generated_field_names(string fieldName)
    {
        var json = ValidExecutableManifest().Replace(
            "\"Name\", \"type\": \"text\", \"required\": true, \"maximumLength\": 120",
            $"\"{fieldName}\", \"type\": \"text\", \"required\": true, \"maximumLength\": 120",
            StringComparison.Ordinal);

        var exception = Assert.Throws<ComposerManifestException>(() => ComposerManifestParser.Parse(json));
        Assert.Contains("reserved by generated infrastructure", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Analyzer_rejects_executable_resource_without_sql_server_provider()
    {
        var manifest = ComposerManifestParser.Parse(
            ValidExecutableManifest().Replace(
                "\"providers\": [\"provider-sqlserver\"]",
                "\"providers\": []",
                StringComparison.Ordinal));

        var exception = Assert.Throws<ComposerManifestException>(() => CompositionAnalyzer.Analyze(manifest));
        Assert.Contains("provider-sqlserver", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Analyzer_rejects_behavior_not_emitted_by_executable_contract()
    {
        var manifest = ComposerManifestParser.Parse(
            ValidExecutableManifest().Replace(
                "\"crud\", \"auditing\", \"authorization\", \"concurrency\"",
                "\"crud\", \"auditing\", \"authorization\", \"concurrency\", \"caching\"",
                StringComparison.Ordinal));

        var exception = Assert.Throws<ComposerManifestException>(() => CompositionAnalyzer.Analyze(manifest));
        Assert.Contains("not generated by the Phase 12 executable contract", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Analyzer_requires_canonical_idempotency_capability_when_http_replay_is_enabled()
    {
        var manifest = ComposerManifestParser.Parse(
            ValidExecutableManifest().Replace(
                "\"includeCapabilities\": [\"concurrency\", \"idempotency\"]",
                "\"includeCapabilities\": [\"concurrency\"]",
                StringComparison.Ordinal));

        var exception = Assert.Throws<ComposerManifestException>(() => CompositionAnalyzer.Analyze(manifest));
        Assert.Contains(FoundationCapabilityIds.Idempotency, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Analyzer_accepts_bounded_executable_resource_and_records_canonical_reasons()
    {
        var manifest = ComposerManifestParser.Parse(ValidExecutableManifest());
        var analysis = CompositionAnalyzer.Analyze(manifest);

        var idempotency = analysis.Entries.Single(entry => entry.Capability.Id == FoundationCapabilityIds.Idempotency);
        var concurrency = analysis.Entries.Single(entry => entry.Capability.Id == FoundationCapabilityIds.Concurrency);

        Assert.Contains("resource:Customers.Customer:api-idempotency", idempotency.Reasons);
        Assert.Contains("resource:Customers.Customer:concurrency", concurrency.Reasons);
    }

    private static string ValidExecutableManifest() =>
        """
        {
          "schemaVersion": 2,
          "name": "GeneratedAlpha",
          "profile": "minimal",
          "includeCapabilities": ["concurrency", "idempotency"],
          "excludeCapabilities": [],
          "providers": ["provider-sqlserver"],
          "modules": [
            {
              "name": "Customers",
              "resources": [
                {
                  "name": "Customer",
                  "route": "customers",
                  "idType": "guid",
                  "behaviors": ["crud", "auditing", "authorization", "concurrency"],
                  "fields": [
                    { "name": "Name", "type": "text", "required": true, "maximumLength": 120 },
                    { "name": "Note", "type": "text", "required": false, "maximumLength": 400 }
                  ],
                  "api": {
                    "routePrefix": "api",
                    "idempotency": "required",
                    "concurrency": "require-if-match",
                    "maximumFilters": 0,
                    "maximumSorts": 0
                  }
                }
              ]
            }
          ]
        }
        """;

    private static string QueryableExecutableManifest() =>
        """
        {
          "schemaVersion": 2,
          "name": "GeneratedQueryable",
          "profile": "minimal",
          "includeCapabilities": ["concurrency", "idempotency"],
          "excludeCapabilities": [],
          "providers": ["provider-sqlserver"],
          "modules": [
            {
              "name": "Customers",
              "resources": [
                {
                  "name": "Customer",
                  "route": "customers",
                  "idType": "guid",
                  "behaviors": ["crud", "auditing", "authorization", "concurrency"],
                  "fields": [
                    {
                      "name": "Name",
                      "type": "text",
                      "required": true,
                      "maximumLength": 120,
                      "query": { "filter": "prefix", "sortable": true },
                      "index": { "enabled": true, "unique": false }
                    },
                    { "name": "Note", "type": "text", "required": false, "maximumLength": 400 }
                  ],
                  "api": {
                    "routePrefix": "api",
                    "idempotency": "required",
                    "concurrency": "require-if-match",
                    "maximumFilters": 3,
                    "maximumSorts": 1
                  }
                }
              ]
            }
          ]
        }
        """;
}
