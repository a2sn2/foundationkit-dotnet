using FoundationKit.Workbench.Endpoints;

namespace FoundationKit.Workbench.Tests;

public sealed class ComposerStudioValidatorTests
{
    [Fact]
    public void Validate_accepts_schema_v2_through_real_composer_engine()
    {
        var result = ComposerStudioValidator.Validate(ValidManifest());

        Assert.True(result.Valid, result.Error);
        Assert.Equal(2, result.SchemaVersion);
        Assert.Equal("StudioProof", result.ProjectName);
        Assert.Equal(1, result.ModuleCount);
        Assert.Equal(1, result.ResourceCount);
        Assert.Equal(0, result.ReadModelCount);
        Assert.Contains(result.Capabilities, item => item.Id == "provider-sqlserver");
    }

    [Fact]
    public void Validate_returns_composer_error_for_invalid_manifest()
    {
        var invalid = ValidManifest().Replace(
            "\"provider-sqlserver\"",
            "\"provider-not-real\"",
            StringComparison.Ordinal);

        var result = ComposerStudioValidator.Validate(invalid);

        Assert.False(result.Valid);
        Assert.NotNull(result.Error);
        Assert.Contains("provider-not-real", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_rejects_oversized_studio_payload_before_parsing()
    {
        var result = ComposerStudioValidator.Validate(
            new string('x', ComposerStudioValidator.MaximumManifestCharacters + 1));

        Assert.False(result.Valid);
        Assert.Contains("exceeds", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    private static string ValidManifest() =>
        """
        {
          "schemaVersion": 2,
          "name": "StudioProof",
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
}
