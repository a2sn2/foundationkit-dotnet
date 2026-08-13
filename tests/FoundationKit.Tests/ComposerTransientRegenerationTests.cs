using FoundationKit.Composer;
using Xunit;

namespace FoundationKit.Tests;

public sealed class ComposerTransientRegenerationTests
{
    [Fact]
    public async Task Force_regeneration_allows_transient_build_and_ide_artifacts()
    {
        var manifest = MinimalManifest("Built.Product");
        var destination = NewTempDirectory();

        try
        {
            await ComposerProjectGenerator.GenerateAsync(
                manifest,
                new ProjectGenerationOptions(destination));

            var transientFiles = new[]
            {
                Path.Combine(destination, "src", "Built.Product.Domain", "bin", "Debug", "net10.0", "Built.Product.Domain.dll"),
                Path.Combine(destination, "src", "Built.Product.Domain", "obj", "project.assets.json"),
                Path.Combine(destination, ".vs", "Built.Product", "v17", ".suo"),
                Path.Combine(destination, "tests", "Built.Product.Tests", "TestResults", "result.trx")
            };

            foreach (var artifact in transientFiles)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(artifact)!);
                await File.WriteAllTextAsync(artifact, "transient tool output");
            }

            await ComposerProjectGenerator.GenerateAsync(
                manifest,
                new ProjectGenerationOptions(destination, Force: true));

            Assert.True(File.Exists(Path.Combine(destination, "README.md")));
            Assert.All(transientFiles, artifact => Assert.False(File.Exists(artifact)));
        }
        finally
        {
            DeleteDirectory(destination);
        }
    }

    [Fact]
    public async Task Force_regeneration_still_refuses_real_user_files_when_transient_artifacts_exist()
    {
        var manifest = MinimalManifest("Protected.Built.Product");
        var destination = NewTempDirectory();

        try
        {
            await ComposerProjectGenerator.GenerateAsync(
                manifest,
                new ProjectGenerationOptions(destination));

            var binArtifact = Path.Combine(
                destination,
                "src",
                "Protected.Built.Product.Domain",
                "bin",
                "Debug",
                "net10.0",
                "Protected.Built.Product.Domain.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(binArtifact)!);
            await File.WriteAllTextAsync(binArtifact, "transient tool output");

            var userFile = Path.Combine(destination, "keep-user-file.txt");
            await File.WriteAllTextAsync(userFile, "user data");

            var exception = await Assert.ThrowsAsync<ComposerGenerationException>(() =>
                ComposerProjectGenerator.GenerateAsync(
                    manifest,
                    new ProjectGenerationOptions(destination, Force: true)));

            Assert.Contains(
                "not part of the previous FoundationKit generation set",
                exception.Message,
                StringComparison.Ordinal);
            Assert.Equal("user data", await File.ReadAllTextAsync(userFile));
            Assert.True(File.Exists(binArtifact));
        }
        finally
        {
            DeleteDirectory(destination);
        }
    }

    private static ComposerManifest MinimalManifest(string name) =>
        ComposerManifestParser.Parse(
            $$"""
            {
              "schemaVersion": 1,
              "name": "{{name}}",
              "profile": "minimal",
              "includeCapabilities": [],
              "excludeCapabilities": [],
              "providers": []
            }
            """);

    private static string NewTempDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "foundationkit-composer-transient-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
            return;

        Directory.Delete(path, recursive: true);
    }
}
