using System.Diagnostics;
using System.Xml.Linq;
using FoundationKit.Workbench.Endpoints;

namespace FoundationKit.Workbench.Tests;

public sealed class ComposerStudioGeneratorTests
{
    [Fact]
    public async Task Generate_writes_linked_schema_v2_project_inside_bounded_workspace_and_solution_includes_core_closure()
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
                force: false,
                foundationMode: "linked");

            Assert.True(result.Generated, result.Error);
            Assert.Equal("StudioProof", result.ProjectName);
            Assert.Equal("generated/StudioProof", result.RelativeOutputPath);
            Assert.Equal("StudioProof.sln", result.SolutionFileName);
            Assert.Equal("project", result.ReferenceMode);
            Assert.True(result.GeneratedFileCount > 0);

            var projectRoot = Path.Combine(generationRoot, "StudioProof");
            var solutionPath = Path.Combine(projectRoot, "StudioProof.sln");
            Assert.True(File.Exists(solutionPath));
            Assert.True(File.Exists(Path.Combine(projectRoot, ".foundationkit-generated.json")));
            Assert.True(File.Exists(Path.Combine(projectRoot, "FOUNDATION-BINDING.md")));

            var solution = await File.ReadAllTextAsync(solutionPath);
            Assert.Contains("FoundationKit.Blazor", solution, StringComparison.Ordinal);
            Assert.Contains("FoundationKit.Authorization", solution, StringComparison.Ordinal);
            Assert.Contains("FoundationKit.Identity", solution, StringComparison.Ordinal);
            Assert.Contains("FoundationKit local source", await File.ReadAllTextAsync(Path.Combine(projectRoot, "README.md")), StringComparison.Ordinal);

            await AssertBuildSucceedsWhenRequested(projectRoot, solutionPath);
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Fact]
    public async Task Generate_source_copy_vendors_required_core_dependency_closure_and_keeps_all_project_references_inside_workspace()
    {
        var foundationRoot = ComposerStudioGenerator.ResolveFoundationRoot(AppContext.BaseDirectory, null);
        var workspace = Path.Combine(Path.GetTempPath(), $"foundationkit-studio-copy-{Guid.NewGuid():N}");
        var generationRoot = Path.Combine(workspace, "generated");

        try
        {
            var result = await ComposerStudioGenerator.GenerateAsync(
                ValidManifest(),
                generationRoot,
                foundationRoot,
                force: false,
                foundationMode: "source-copy");

            Assert.True(result.Generated, result.Error);
            Assert.Equal("source-copy", result.ReferenceMode);

            var projectRoot = Path.Combine(generationRoot, "StudioProof");
            var vendoredRoot = Path.Combine(projectRoot, "foundation");
            var solutionPath = Path.Combine(projectRoot, "StudioProof.sln");
            Assert.True(File.Exists(Path.Combine(vendoredRoot, "Directory.Build.props")));
            Assert.True(File.Exists(Path.Combine(vendoredRoot, "Directory.Packages.props")));
            Assert.True(File.Exists(Path.Combine(vendoredRoot, "src", "FoundationKit.Blazor", "FoundationKit.Blazor.csproj")));
            Assert.True(File.Exists(Path.Combine(vendoredRoot, "src", "FoundationKit.Authorization", "FoundationKit.Authorization.csproj")));
            Assert.True(File.Exists(Path.Combine(vendoredRoot, "src", "FoundationKit.Identity", "FoundationKit.Identity.csproj")));

            var solution = await File.ReadAllTextAsync(solutionPath);
            Assert.Contains("foundation\\src\\FoundationKit.Blazor\\FoundationKit.Blazor.csproj", solution, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("source-copy", await File.ReadAllTextAsync(Path.Combine(projectRoot, ".foundationkit-generated.json")), StringComparison.Ordinal);

            AssertProjectReferencesStayInside(projectRoot);
            await AssertBuildSucceedsWhenRequested(projectRoot, solutionPath);
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Fact]
    public async Task Generate_rejects_unknown_foundation_mode()
    {
        var foundationRoot = ComposerStudioGenerator.ResolveFoundationRoot(AppContext.BaseDirectory, null);
        var workspace = Path.Combine(Path.GetTempPath(), $"foundationkit-studio-mode-{Guid.NewGuid():N}");
        var generationRoot = Path.Combine(workspace, "generated");

        try
        {
            var result = await ComposerStudioGenerator.GenerateAsync(
                ValidManifest(),
                generationRoot,
                foundationRoot,
                force: false,
                foundationMode: "mystery");

            Assert.False(result.Generated);
            Assert.Contains("linked", result.Error, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("source-copy", result.Error, StringComparison.OrdinalIgnoreCase);
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

    private static async Task AssertBuildSucceedsWhenRequested(string projectRoot, string solutionPath)
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("FOUNDATIONKIT_COMPOSER_BINDING_BUILD_PROOF"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = projectRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("build");
        startInfo.ArgumentList.Add(solutionPath);
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add("Release");

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start dotnet build for Composer binding proof.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await process.WaitForExitAsync(timeout.Token);
        var output = await stdout;
        var error = await stderr;

        Assert.True(
            process.ExitCode == 0,
            $"Generated solution build failed.{Environment.NewLine}STDOUT:{Environment.NewLine}{output}{Environment.NewLine}STDERR:{Environment.NewLine}{error}");
    }

    private static void AssertProjectReferencesStayInside(string projectRoot)
    {
        foreach (var projectPath in Directory.EnumerateFiles(projectRoot, "*.csproj", SearchOption.AllDirectories))
        {
            var document = XDocument.Load(projectPath);
            foreach (var reference in document.Descendants().Where(element => element.Name.LocalName == "ProjectReference"))
            {
                var include = reference.Attributes().First(attribute => attribute.Name.LocalName == "Include").Value;
                var target = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(projectPath)!, include));
                Assert.True(
                    IsUnderDirectory(target, projectRoot),
                    $"ProjectReference escaped standalone workspace: {projectPath} -> {target}");
            }
        }
    }

    private static bool IsUnderDirectory(string candidate, string root)
    {
        var normalizedCandidate = Path.GetFullPath(candidate);
        var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return normalizedCandidate.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, comparison) ||
               string.Equals(Path.TrimEndingDirectorySeparator(normalizedCandidate), normalizedRoot, comparison);
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
