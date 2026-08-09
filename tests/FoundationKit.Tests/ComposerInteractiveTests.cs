using FoundationKit.Composer;

namespace FoundationKit.Tests;

public sealed class ComposerInteractiveTests
{
    [Fact]
    public async Task Cli_interactive_new_generates_confirmed_project()
    {
        var destination = NewTempPath();
        using var input = new StringReader(
            """
            Interactive.Product
            minimal
            blazor,auditing
            provider-smtp
            yes
            """);
        using var output = new StringWriter();
        using var error = new StringWriter();

        try
        {
            var exitCode = await ComposerCli.RunAsync(
                ["new", "--interactive", "--output", destination],
                input,
                output,
                error);

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            Assert.Contains("Composition preview", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("provider-smtp", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("Generated project: Interactive.Product", output.ToString(), StringComparison.Ordinal);
            Assert.True(File.Exists(Path.Combine(destination, "Interactive.Product.sln")));
            Assert.True(File.Exists(Path.Combine(destination, "foundationkit.project.json")));

            var manifest = await File.ReadAllTextAsync(Path.Combine(destination, "foundationkit.project.json"));
            Assert.Contains("\"blazor\"", manifest, StringComparison.Ordinal);
            Assert.Contains("\"auditing\"", manifest, StringComparison.Ordinal);
            Assert.Contains("\"provider-smtp\"", manifest, StringComparison.Ordinal);
        }
        finally
        {
            DeleteDirectory(destination);
        }
    }

    [Fact]
    public async Task Cli_interactive_retries_invalid_capability_and_provider_ids()
    {
        var destination = NewTempPath();
        using var input = new StringReader(
            """
            Retry.Product
            1
            not-real
            auditing
            provider-nope
            provider-smtp
            y
            """);
        using var output = new StringWriter();
        using var error = new StringWriter();

        try
        {
            var exitCode = await ComposerCli.RunAsync(
                ["new", "--interactive", "--output", destination],
                input,
                output,
                error);

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            Assert.Contains("Unknown optional capability 'not-real'", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("Unknown provider 'provider-nope'", output.ToString(), StringComparison.Ordinal);
            Assert.True(File.Exists(Path.Combine(destination, "Retry.Product.sln")));
        }
        finally
        {
            DeleteDirectory(destination);
        }
    }

    [Fact]
    public async Task Cli_interactive_no_confirmation_writes_nothing()
    {
        var destination = NewTempPath();
        using var input = new StringReader(
            """
            Cancel.Product
            minimal


            no
            """);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await ComposerCli.RunAsync(
            ["new", "--interactive", "--output", destination],
            input,
            output,
            error);

        Assert.Equal(4, exitCode);
        Assert.Equal(string.Empty, error.ToString());
        Assert.Contains("CANCELLED: no files were generated", output.ToString(), StringComparison.Ordinal);
        Assert.False(Directory.Exists(destination));
    }

    [Fact]
    public async Task Cli_interactive_require_stable_fails_before_confirmation_or_writes()
    {
        var destination = NewTempPath();
        using var input = new StringReader(
            """
            Stable.Product
            minimal


            """);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await ComposerCli.RunAsync(
            ["new", "--interactive", "--output", destination, "--require-stable"],
            input,
            output,
            error);

        Assert.Equal(3, exitCode);
        Assert.Equal(string.Empty, error.ToString());
        Assert.Contains("Composition preview", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("NOT GENERATED", output.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("Generate this project now?", output.ToString(), StringComparison.Ordinal);
        Assert.False(Directory.Exists(destination));
    }

    [Fact]
    public async Task Cli_interactive_rejects_manifest_path_combination()
    {
        var manifestPath = Path.Combine(Path.GetTempPath(), $"composer-interactive-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(manifestPath, "{}");
        var destination = NewTempPath();
        using var input = new StringReader(string.Empty);
        using var output = new StringWriter();
        using var error = new StringWriter();

        try
        {
            var exitCode = await ComposerCli.RunAsync(
                ["new", manifestPath, "--interactive", "--output", destination],
                input,
                output,
                error);

            Assert.Equal(2, exitCode);
            Assert.Contains("cannot be combined with a manifest path", error.ToString(), StringComparison.Ordinal);
            Assert.False(Directory.Exists(destination));
        }
        finally
        {
            File.Delete(manifestPath);
        }
    }

    private static string NewTempPath() =>
        Path.Combine(Path.GetTempPath(), $"foundationkit-interactive-{Guid.NewGuid():N}");

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
