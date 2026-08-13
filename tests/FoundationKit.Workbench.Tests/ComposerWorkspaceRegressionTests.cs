namespace FoundationKit.Workbench.Tests;

public sealed class ComposerWorkspaceRegressionTests
{
    [Fact]
    public void Compose_invalidates_previous_validation_when_manifest_changes()
    {
        var repositoryRoot = FindRepositoryRoot();
        var composePath = Path.Combine(
            repositoryRoot,
            "samples",
            "FoundationKit.Workbench.Client",
            "Pages",
            "Compose.razor");

        var source = File.ReadAllText(composePath);

        Assert.Contains("private string ManifestJson", source, StringComparison.Ordinal);
        Assert.Contains("InvalidateValidationState();", source, StringComparison.Ordinal);
        Assert.Contains("private string? _validatedManifestJson;", source, StringComparison.Ordinal);
        Assert.Contains("string.Equals(_validatedManifestJson, _manifestJson, StringComparison.Ordinal)", source, StringComparison.Ordinal);
        Assert.Contains("var manifest = _manifestJson;", source, StringComparison.Ordinal);
        Assert.Contains("_validatedManifestJson = manifest;", source, StringComparison.Ordinal);
        Assert.Contains("IsCurrentManifestValidated", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_uses_shared_language_cascade_dedicated_workspace_styles_and_both_foundation_modes()
    {
        var repositoryRoot = FindRepositoryRoot();
        var composePath = Path.Combine(
            repositoryRoot,
            "samples",
            "FoundationKit.Workbench.Client",
            "Pages",
            "Compose.razor");
        var indexPath = Path.Combine(
            repositoryRoot,
            "samples",
            "FoundationKit.Workbench.Client",
            "wwwroot",
            "index.html");
        var stylePath = Path.Combine(
            repositoryRoot,
            "samples",
            "FoundationKit.Workbench.Client",
            "wwwroot",
            "css",
            "composer.css");

        var compose = File.ReadAllText(composePath);
        var index = File.ReadAllText(indexPath);
        var styles = File.ReadAllText(stylePath);

        Assert.Contains("[CascadingParameter(Name = \"FoundationLanguage\")]", compose, StringComparison.Ordinal);
        Assert.Contains("private string T(string ar, string en)", compose, StringComparison.Ordinal);
        Assert.Contains("Project choices", compose, StringComparison.Ordinal);
        Assert.Contains("اختيارات المشروع", compose, StringComparison.Ordinal);
        Assert.Contains("Generate Project", compose, StringComparison.Ordinal);
        Assert.Contains("ولّد المشروع", compose, StringComparison.Ordinal);
        Assert.Contains("Foundation binding mode", compose, StringComparison.Ordinal);
        Assert.Contains("طريقة استخدام الكور", compose, StringComparison.Ordinal);
        Assert.Contains("Value=\"@(\"linked\")\"", compose, StringComparison.Ordinal);
        Assert.Contains("Value=\"@(\"source-copy\")\"", compose, StringComparison.Ordinal);
        Assert.Contains("_foundationMode = \"linked\"", compose, StringComparison.Ordinal);
        Assert.Contains("_foundationMode);", compose, StringComparison.Ordinal);
        Assert.Contains("CurrentGenerationTarget", compose, StringComparison.Ordinal);
        Assert.Contains("GeneratedSolutionTarget", compose, StringComparison.Ordinal);
        Assert.Contains("No other generated folder with a different name", compose, StringComparison.Ordinal);
        Assert.Contains("Open this exact solution after generation", compose, StringComparison.Ordinal);
        Assert.Contains("css/composer.css", index, StringComparison.Ordinal);
        Assert.Contains(".composer-actionbar", styles, StringComparison.Ordinal);
        Assert.Contains(".composer-binding-choice", styles, StringComparison.Ordinal);
        Assert.Contains(".composer-page .mud-input-label-outlined", styles, StringComparison.Ordinal);
        Assert.Contains("background-color: var(--fk-surface-default) !important;", styles, StringComparison.Ordinal);
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
