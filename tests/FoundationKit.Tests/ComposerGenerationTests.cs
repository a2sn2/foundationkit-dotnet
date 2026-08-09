using FoundationKit.Composer;
using Xunit;

namespace FoundationKit.Tests;

public sealed class ComposerGenerationTests
{
    [Fact]
    public async Task Generator_produces_identical_package_mode_output_for_the_same_manifest()
    {
        var manifest = ComposerManifestParser.Parse(
            """
            {
              "schemaVersion": 1,
              "name": "Golden.Product",
              "profile": "minimal",
              "includeCapabilities": ["blazor", "auditing"],
              "excludeCapabilities": [],
              "providers": ["provider-smtp"],
              "capabilityContracts": {
                "auditing": 1,
                "provider-smtp": 1
              }
            }
            """);
        var first = NewTempDirectory();
        var second = NewTempDirectory();

        try
        {
            var firstResult = await ComposerProjectGenerator.GenerateAsync(
                manifest,
                new ProjectGenerationOptions(first));
            var secondResult = await ComposerProjectGenerator.GenerateAsync(
                manifest,
                new ProjectGenerationOptions(second));

            Assert.Equal("package", firstResult.ReferenceMode);
            Assert.Equal("package", secondResult.ReferenceMode);
            AssertSnapshotsEqual(ReadSnapshot(first), ReadSnapshot(second));
            Assert.Contains("src/Golden.Product.Api/Golden.Product.Api.csproj", firstResult.GeneratedFiles);
            Assert.Contains("src/Golden.Product.Client/Golden.Product.Client.csproj", firstResult.GeneratedFiles);
            Assert.Contains("tests/Golden.Product.Tests/GeneratedScaffoldTests.cs", firstResult.GeneratedFiles);

            var architecture = await File.ReadAllTextAsync(Path.Combine(first, "ARCHITECTURE.md"));
            Assert.Contains("`FoundationKit.Auditing`", architecture, StringComparison.Ordinal);
            Assert.Contains("observability", architecture, StringComparison.Ordinal);
            Assert.Contains("no reusable package binding", architecture, StringComparison.Ordinal);

            var applicationProject = await File.ReadAllTextAsync(
                Path.Combine(first, "src", "Golden.Product.Application", "Golden.Product.Application.csproj"));
            Assert.Contains("PackageReference Include=\"FoundationKit.Auditing\"", applicationProject, StringComparison.Ordinal);

            var infrastructureProject = await File.ReadAllTextAsync(
                Path.Combine(first, "src", "Golden.Product.Infrastructure", "Golden.Product.Infrastructure.csproj"));
            Assert.Contains("PackageReference Include=\"FoundationKit.Notifications.Smtp\"", infrastructureProject, StringComparison.Ordinal);
        }
        finally
        {
            DeleteDirectory(first);
            DeleteDirectory(second);
        }
    }

    [Fact]
    public async Task Generator_project_mode_uses_repository_project_references()
    {
        var manifest = ComposerManifestParser.Parse(
            """
            {
              "schemaVersion": 1,
              "name": "Repository.Local",
              "profile": "minimal",
              "includeCapabilities": ["blazor"],
              "excludeCapabilities": [],
              "providers": []
            }
            """);
        var destination = NewTempDirectory();
        var repositoryRoot = FindRepositoryRoot();

        try
        {
            var result = await ComposerProjectGenerator.GenerateAsync(
                manifest,
                new ProjectGenerationOptions(destination, repositoryRoot));

            Assert.Equal("project", result.ReferenceMode);
            var domainProject = await File.ReadAllTextAsync(
                Path.Combine(destination, "src", "Repository.Local.Domain", "Repository.Local.Domain.csproj"));
            Assert.Contains("ProjectReference Include=", domainProject, StringComparison.Ordinal);
            Assert.Contains("FoundationKit.Domain.csproj", domainProject, StringComparison.Ordinal);
            Assert.DoesNotContain("PackageReference Include=\"FoundationKit.Domain\"", domainProject, StringComparison.Ordinal);
        }
        finally
        {
            DeleteDirectory(destination);
        }
    }

    [Fact]
    public async Task Generator_force_refuses_unknown_nonempty_destination()
    {
        var manifest = MinimalManifest("Safe.Product");
        var destination = NewTempDirectory();
        await File.WriteAllTextAsync(Path.Combine(destination, "keep.txt"), "owner data");

        try
        {
            var exception = await Assert.ThrowsAsync<ComposerGenerationException>(() =>
                ComposerProjectGenerator.GenerateAsync(
                    manifest,
                    new ProjectGenerationOptions(destination, Force: true)));

            Assert.Contains("generated marker", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(Path.Combine(destination, "keep.txt")));
        }
        finally
        {
            DeleteDirectory(destination);
        }
    }

    [Fact]
    public async Task Generator_force_refuses_extra_files_added_after_generation()
    {
        var manifest = MinimalManifest("Protected.Product");
        var destination = NewTempDirectory();

        try
        {
            await ComposerProjectGenerator.GenerateAsync(
                manifest,
                new ProjectGenerationOptions(destination));
            var userFile = Path.Combine(destination, "keep-user-file.txt");
            await File.WriteAllTextAsync(userFile, "user data");

            var exception = await Assert.ThrowsAsync<ComposerGenerationException>(() =>
                ComposerProjectGenerator.GenerateAsync(
                    manifest,
                    new ProjectGenerationOptions(destination, Force: true)));

            Assert.Contains("not part of the previous FoundationKit generation set", exception.Message, StringComparison.Ordinal);
            Assert.Equal("user data", await File.ReadAllTextAsync(userFile));
        }
        finally
        {
            DeleteDirectory(destination);
        }
    }

    [Fact]
    public async Task Generator_force_can_replace_its_own_unchanged_previous_output()
    {
        var manifest = MinimalManifest("Repeatable.Product");
        var destination = NewTempDirectory();

        try
        {
            await ComposerProjectGenerator.GenerateAsync(
                manifest,
                new ProjectGenerationOptions(destination));
            var before = ReadSnapshot(destination);

            await ComposerProjectGenerator.GenerateAsync(
                manifest,
                new ProjectGenerationOptions(destination, Force: true));

            AssertSnapshotsEqual(before, ReadSnapshot(destination));
            Assert.True(File.Exists(Path.Combine(destination, ".foundationkit-generated.json")));
        }
        finally
        {
            DeleteDirectory(destination);
        }
    }

    [Fact]
    public async Task Cli_new_generates_a_scaffold()
    {
        var manifestPath = await WriteManifestAsync(
            """
            {
              "schemaVersion": 1,
              "name": "Cli.Product",
              "profile": "minimal",
              "includeCapabilities": ["blazor"],
              "excludeCapabilities": [],
              "providers": []
            }
            """);
        var destination = NewTempDirectory();

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            var exitCode = await ComposerCli.RunAsync(
                ["new", manifestPath, "--output", destination],
                output,
                error);

            Assert.Equal(0, exitCode);
            Assert.Contains("Generated project: Cli.Product", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("Foundation references: package", output.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, error.ToString());
            Assert.True(File.Exists(Path.Combine(destination, "Cli.Product.sln")));
            Assert.True(File.Exists(Path.Combine(destination, "ARCHITECTURE.md")));
        }
        finally
        {
            File.Delete(manifestPath);
            DeleteDirectory(destination);
        }
    }

    [Fact]
    public async Task Cli_new_require_stable_fails_before_writing_files()
    {
        var manifestPath = await WriteManifestAsync(
            """
            {
              "schemaVersion": 1,
              "name": "Stable.Only",
              "profile": "minimal",
              "includeCapabilities": [],
              "excludeCapabilities": [],
              "providers": []
            }
            """);
        var destination = NewTempDirectory();

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            var exitCode = await ComposerCli.RunAsync(
                ["new", manifestPath, "--output", destination, "--require-stable"],
                output,
                error);

            Assert.Equal(3, exitCode);
            Assert.Contains("NOT GENERATED", output.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, error.ToString());
            Assert.Empty(Directory.EnumerateFileSystemEntries(destination));
        }
        finally
        {
            File.Delete(manifestPath);
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

    private static Dictionary<string, string> ReadSnapshot(string root)
    {
        return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .ToDictionary(
                path => Path.GetRelativePath(root, path).Replace('\\', '/'),
                File.ReadAllText,
                StringComparer.Ordinal);
    }

    private static void AssertSnapshotsEqual(
        IReadOnlyDictionary<string, string> expected,
        IReadOnlyDictionary<string, string> actual)
    {
        Assert.Equal(expected.Keys.Order(StringComparer.Ordinal), actual.Keys.Order(StringComparer.Ordinal));
        foreach (var key in expected.Keys)
        {
            Assert.Equal(expected[key], actual[key]);
        }
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "FoundationKit.sln")) &&
                File.Exists(Path.Combine(
                    current.FullName,
                    "src",
                    "FoundationKit.Domain",
                    "FoundationKit.Domain.csproj")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate the FoundationKit repository root for project-reference generation tests.");
    }

    private static string NewTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundationkit-generation-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static async Task<string> WriteManifestAsync(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundationkit-generation-manifest-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, json);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
