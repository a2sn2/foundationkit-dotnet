namespace FoundationKit.Workbench.Tests;

public sealed class ComposerStarterChoiceRegressionTests
{
    [Fact]
    public void Compose_invalidates_validation_when_starter_choices_drift_from_manifest()
    {
        var repositoryRoot = FindRepositoryRoot();
        var behaviorPath = Path.Combine(
            repositoryRoot,
            "samples",
            "FoundationKit.Workbench.Client",
            "Pages",
            "Compose.razor.cs");

        var source = File.ReadAllText(behaviorPath);

        Assert.Contains("ManifestMatchesStarterChoices", source, StringComparison.Ordinal);
        Assert.Contains("_starterChoicesDirty = true;", source, StringComparison.Ordinal);
        Assert.Contains("InvalidateValidationState();", source, StringComparison.Ordinal);
        Assert.Contains("IsCurrentManifestValidated", source, StringComparison.Ordinal);
        Assert.Contains("Project choices changed but are not applied to the Manifest yet", source, StringComparison.Ordinal);
        Assert.Contains("Apply the choices to the Manifest, then validate again", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_compares_all_bounded_starter_choices_without_rebuilding_advanced_manifest_content()
    {
        var repositoryRoot = FindRepositoryRoot();
        var behaviorPath = Path.Combine(
            repositoryRoot,
            "samples",
            "FoundationKit.Workbench.Client",
            "Pages",
            "Compose.razor.cs");

        var source = File.ReadAllText(behaviorPath);

        Assert.Contains("_projectName", source, StringComparison.Ordinal);
        Assert.Contains("_profile", source, StringComparison.Ordinal);
        Assert.Contains("_moduleName", source, StringComparison.Ordinal);
        Assert.Contains("_resourceName", source, StringComparison.Ordinal);
        Assert.Contains("_route", source, StringComparison.Ordinal);
        Assert.Contains("_idType", source, StringComparison.Ordinal);
        Assert.Contains("_authorization", source, StringComparison.Ordinal);
        Assert.Contains("_auditing", source, StringComparison.Ordinal);
        Assert.Contains("_concurrency", source, StringComparison.Ordinal);
        Assert.Contains("_idempotency", source, StringComparison.Ordinal);
        Assert.Contains("_blazor", source, StringComparison.Ordinal);
        Assert.Contains("JsonDocument.Parse(_manifestJson)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ManifestJson =", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FoundationKit.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the FoundationKit repository root from the test output directory.");
    }
}
