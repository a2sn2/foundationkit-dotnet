using FoundationKit.Workbench.Endpoints;

namespace FoundationKit.Workbench.Tests;

public sealed class ComposerStudioGeneratorTests
{
    [Fact]
    public async Task Generate_writes_schema_v2_project_inside_bounded_workspace()
    {
        var foundationRoot = ComposerStudioGenerator.ResolveFoundationRoot(AppContext.BaseDirectory, null);
        var workspace = Path.Combine(Path.GetTempPath(), $"foundationkit-studio-{Guid.NewGuid():N}");
        var generationRoot = Path.Combine(workspace, "generated");

        try
        {
            var result = await ComposerStudioGenerator.GenerateAsync(
                ValidManifest(),
                generationRoot,
                foundationRoot,
                force: false);

            Assert.True(result.Generated, result.Error);
            Assert.Equal("StudioProof", result.ProjectName);
            Assert.Equal("generated/StudioProof", result.RelativeOutputPath);
            Assert.Equal("StudioProof.sln", result.SolutionFileName);
            Assert.Equal("project", result.ReferenceMode);
            Assert.True(result.GeneratedFileCount > 0);
            Assert.True(File.Exists(Path.Combine(generationRoot, "StudioProof", "StudioProof.sln")));
            Assert.True(File.Exists(Path.Combine(generationRoot, "StudioProof", ".foundationkit-generated.json")));
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Fact]
    public async Task Generate_refuses_nonempty_destination_without_force_and_allows_safe_force()
    {
        var foundationRoot = ComposerStudioGenerator.ResolveFoundationRoot(AppContext.BaseDirectory, null);
        var workspace = Path.Combine(Path.GetTempPath(), $"foundationkit-studio-{Guid.NewGuid():N}");
        var generationRoot = Path.Combine(workspace, "generated");

        try
        {
            var first = await ComposerStudioGenerator.GenerateAsync(
                ValidManifest(), generationRoot, foundationRoot, force: false);
            Assert.True(first.Generated, first.Error);

            var blocked = await ComposerStudioGenerator.GenerateAsync(
                ValidManifest(), generationRoot, foundationRoot, force: false);
            Assert.False(blocked.Generated);
            Assert.Contains("not empty", blocked.Error, StringComparison.OrdinalIgnoreCase);

            var regenerated = await ComposerStudioGenerator.GenerateAsync(
                ValidManifest(), generationRoot, foundationRoot, force: true);
            Assert.True(regenerated.Generated, regenerated.Error);
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Fact]
    public async Task Generate_safe_force_refuses_user_added_file()
    {
        var foundationRoot = ComposerStudioGenerator.ResolveFoundationRoot(AppContext.BaseDirectory, null);
        var workspace = Path.Combine(Path.GetTempPath(), $"foundationkit-studio-{Guid.NewGuid():N}");
        var generationRoot = Path.Combine(workspace, "generated");
        var projectRoot = Path.Combine(generationRoot, "StudioProof");

        try
        {
            var first = await ComposerStudioGenerator.GenerateAsync(
                ValidManifest(), generationRoot, foundationRoot, force: false);
            Assert.True(first.Generated, first.Error);

            await File.WriteAllTextAsync(Path.Combine(projectRoot, "user-note.txt"), "keep me");

            var blocked = await ComposerStudioGenerator.GenerateAsync(
                ValidManifest(), generationRoot, foundationRoot, force: true);

            Assert.False(blocked.Generated);
            Assert.Contains("files that are not part", blocked.Error, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(Path.Combine(projectRoot, "user-note.txt")));
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    private static string ValidManifest() =>
        """
        {
          "schemaVersion": 2,
          "name": "StudioProof",
          "profile": "minimal",
          "includeCapabilities": ["concurrency", "idempotency", "blazor"],
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
