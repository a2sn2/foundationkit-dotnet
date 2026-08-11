namespace FoundationKit.Workbench.Tests;

public sealed class StudioLocaleRegressionTests
{
    [Fact]
    public void Locale_observer_never_rewrites_an_unchanged_text_or_attribute_value()
    {
        var repositoryRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(
            repositoryRoot,
            "samples",
            "FoundationKit.Workbench.Client",
            "wwwroot",
            "studio-locale.js");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("const targetValue = translatedText(original, language);", script, StringComparison.Ordinal);
        Assert.Contains("if (current !== targetValue)", script, StringComparison.Ordinal);
        Assert.DoesNotContain("node.nodeValue = original;", script, StringComparison.Ordinal);
        Assert.DoesNotContain("element.setAttribute(attribute, language ===", script, StringComparison.Ordinal);
        Assert.Contains("characterData: true", script, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FoundationKit.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the FoundationKit repository root from the test output directory.");
    }
}
