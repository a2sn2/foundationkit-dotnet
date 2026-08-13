using System.Text.Json;
using FoundationKit.Composer;
using Xunit;

namespace FoundationKit.Tests;

public sealed class ComposerBlazorRuntimeGenerationTests
{
    [Fact]
    public async Task Schema_v2_blazor_generates_runnable_wasm_client_and_local_runtime_wiring()
    {
        var manifest = ComposerManifestParser.Parse(
            """
            {
              "schemaVersion": 2,
              "name": "Runtime.Ui.Proof",
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
            """);
        var destination = NewTempDirectory();

        try
        {
            var result = await ComposerProjectModelGenerator.GenerateAsync(
                CompositionAnalyzer.Analyze(manifest),
                new ProjectGenerationOptions(destination, FindRepositoryRoot()));

            Assert.Equal("project", result.ReferenceMode);

            var clientRoot = Path.Combine(destination, "src", "Runtime.Ui.Proof.Client");
            var clientProject = await File.ReadAllTextAsync(
                Path.Combine(clientRoot, "Runtime.Ui.Proof.Client.csproj"));
            Assert.Contains("Microsoft.NET.Sdk.BlazorWebAssembly", clientProject, StringComparison.Ordinal);
            Assert.Contains("Microsoft.AspNetCore.Components.WebAssembly", clientProject, StringComparison.Ordinal);
            Assert.Contains("Microsoft.AspNetCore.Components.WebAssembly.DevServer", clientProject, StringComparison.Ordinal);
            Assert.DoesNotContain("FrameworkReference Include=\"Microsoft.AspNetCore.App\"", clientProject, StringComparison.Ordinal);
            Assert.DoesNotContain("MudBlazor", clientProject, StringComparison.Ordinal);

            Assert.True(File.Exists(Path.Combine(clientRoot, "Program.cs")));
            Assert.True(File.Exists(Path.Combine(clientRoot, "App.razor")));
            Assert.True(File.Exists(Path.Combine(clientRoot, "Layout", "MainLayout.razor")));
            Assert.True(File.Exists(Path.Combine(clientRoot, "Pages", "Home.razor")));
            Assert.True(File.Exists(Path.Combine(clientRoot, "Pages", "Runtime.razor")));
            Assert.True(File.Exists(Path.Combine(clientRoot, "Api", "GeneratedApiProbe.cs")));
            Assert.True(File.Exists(Path.Combine(clientRoot, "wwwroot", "index.html")));
            Assert.True(File.Exists(Path.Combine(clientRoot, "wwwroot", "css", "app.css")));
            Assert.True(File.Exists(Path.Combine(clientRoot, "wwwroot", "appsettings.json")));
            Assert.True(File.Exists(Path.Combine(clientRoot, "Properties", "launchSettings.json")));
            Assert.False(File.Exists(Path.Combine(clientRoot, "GeneratedProduct.razor")));
            Assert.DoesNotContain(
                "src/Runtime.Ui.Proof.Client/GeneratedProduct.razor",
                result.GeneratedFiles,
                StringComparer.Ordinal);
            Assert.True(File.Exists(Path.Combine(destination, "GENERATED-FRONTEND.md")));

            var solutionLaunchPath = Path.Combine(destination, "Runtime.Ui.Proof.slnLaunch");
            Assert.True(File.Exists(solutionLaunchPath));
            Assert.Contains("Runtime.Ui.Proof.slnLaunch", result.GeneratedFiles, StringComparer.Ordinal);
            using (var solutionLaunch = JsonDocument.Parse(await File.ReadAllTextAsync(solutionLaunchPath)))
            {
                var profile = solutionLaunch.RootElement.EnumerateArray().Single();
                Assert.Equal("FoundationKit Local", profile.GetProperty("Name").GetString());
                var projects = profile.GetProperty("Projects").EnumerateArray().ToArray();
                Assert.Equal(2, projects.Length);
                Assert.Equal("src\\Runtime.Ui.Proof.Api\\Runtime.Ui.Proof.Api.csproj", projects[0].GetProperty("Path").GetString());
                Assert.Equal("Start", projects[0].GetProperty("Action").GetString());
                Assert.Equal("src\\Runtime.Ui.Proof.Client\\Runtime.Ui.Proof.Client.csproj", projects[1].GetProperty("Path").GetString());
                Assert.Equal("Start", projects[1].GetProperty("Action").GetString());
            }

            var packages = await File.ReadAllTextAsync(Path.Combine(destination, "Directory.Packages.props"));
            Assert.Contains("Microsoft.AspNetCore.Components.WebAssembly", packages, StringComparison.Ordinal);
            Assert.Contains("Microsoft.AspNetCore.Components.WebAssembly.DevServer", packages, StringComparison.Ordinal);
            Assert.Contains("Swashbuckle.AspNetCore", packages, StringComparison.Ordinal);

            var apiProgram = await File.ReadAllTextAsync(
                Path.Combine(destination, "src", "Runtime.Ui.Proof.Api", "Program.cs"));
            Assert.Contains("GeneratedLocalClient", apiProgram, StringComparison.Ordinal);
            Assert.Contains("uri.IsLoopback", apiProgram, StringComparison.Ordinal);
            Assert.Contains("app.UseCors", apiProgram, StringComparison.Ordinal);

            var probe = await File.ReadAllTextAsync(Path.Combine(clientRoot, "Api", "GeneratedApiProbe.cs"));
            Assert.Contains("namespace Runtime.Ui.Proof.Client.Api;", probe, StringComparison.Ordinal);
            Assert.Contains("api/foundationkit/health", probe, StringComparison.Ordinal);
            Assert.Contains("swagger/v1/swagger.json", probe, StringComparison.Ordinal);

            using var appSettings = JsonDocument.Parse(
                await File.ReadAllTextAsync(Path.Combine(clientRoot, "wwwroot", "appsettings.json")));
            var apiBaseUrl = appSettings.RootElement.GetProperty("FoundationApiBaseUrl").GetString();
            Assert.NotNull(apiBaseUrl);
            Assert.StartsWith("http://localhost:", apiBaseUrl, StringComparison.Ordinal);

            using var apiLaunch = JsonDocument.Parse(
                await File.ReadAllTextAsync(Path.Combine(
                    destination,
                    "src",
                    "Runtime.Ui.Proof.Api",
                    "Properties",
                    "launchSettings.json")));
            var apiApplicationUrl = apiLaunch.RootElement
                .GetProperty("profiles")
                .GetProperty("http")
                .GetProperty("applicationUrl")
                .GetString();
            Assert.Equal(apiApplicationUrl + "/", apiBaseUrl);

            var index = await File.ReadAllTextAsync(Path.Combine(clientRoot, "wwwroot", "index.html"));
            Assert.Contains("_content/FoundationKit.Blazor/foundationkit.css", index, StringComparison.Ordinal);
            Assert.Contains("_content/FoundationKit.Blazor/foundationkit.js", index, StringComparison.Ordinal);
            Assert.Contains("_framework/blazor.webassembly.js", index, StringComparison.Ordinal);
        }
        finally
        {
            DeleteDirectory(destination);
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

        throw new InvalidOperationException("Could not locate the FoundationKit repository root.");
    }

    private static string NewTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foundationkit-blazor-runtime-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
