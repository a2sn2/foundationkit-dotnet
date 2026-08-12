using FoundationKit.Workbench.Endpoints;

namespace FoundationKit.Workbench.Tests;

public sealed class ComposerRegenerationTransientArtifactTests
{
    [Fact]
    public async Task Safe_force_ignores_only_transient_visual_studio_and_build_artifacts()
    {
        var foundationRoot = ComposerStudioGenerator.ResolveFoundationRoot(AppContext.BaseDirectory, null);
        var workspace = Path.Combine(Path.GetTempPath(), $"foundationkit-studio-transient-{Guid.NewGuid():N}");
        var generationRoot = Path.Combine(workspace, "generated");
        var projectRoot = Path.Combine(generationRoot, "StudioTransient");

        try
        {
            var first = await ComposerStudioGenerator.GenerateAsync(
                ValidManifest(), generationRoot, foundationRoot, force: false);
            Assert.True(first.Generated, first.Error);

            WriteTransient(Path.Combine(projectRoot, ".vs", "state.bin"));
            WriteTransient(Path.Combine(projectRoot, "src", "StudioTransient.Api", "bin", "Debug", "net10.0", "host.tmp"));
            WriteTransient(Path.Combine(projectRoot, "src", "StudioTransient.Api", "obj", "project.assets.json"));
            WriteTransient(Path.Combine(projectRoot, "tests", "StudioTransient.Tests", "TestResults", "result.trx"));

            var regenerated = await ComposerStudioGenerator.GenerateAsync(
                ValidManifest(), generationRoot, foundationRoot, force: true);

            Assert.True(regenerated.Generated, regenerated.Error);
            Assert.False(Directory.Exists(Path.Combine(projectRoot, ".vs")));
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    private static void WriteTransient(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "transient");
    }

    private static string ValidManifest() =>
        """
        {
          "schemaVersion": 2,
          "name": "StudioTransient",
          "profile": "minimal",
          "includeCapabilities": ["blazor"],
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
                  "behaviors": ["crud"],
                  "fields": [
                    { "name": "Name", "type": "text", "required": true, "maximumLength": 120 }
                  ],
                  "api": {
                    "routePrefix": "api",
                    "idempotency": "disabled",
                    "concurrency": "application-policy",
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
