using Xunit;

namespace FoundationKit.Workbench.Tests;

public sealed class AppShellRtlRegressionTests
{
    [Fact]
    public void Reusable_app_shell_keeps_rtl_sidebar_on_the_physical_right()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "FoundationKit.Blazor",
            "Components",
            "FkAppShell.razor"));

        Assert.Contains(".fk-shell[dir=\"rtl\"]", source, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: var(--fk-sidebar-width) minmax(0, 1fr);", source, StringComparison.Ordinal);
        Assert.Contains("\"sidebar topbar\"", source, StringComparison.Ordinal);
        Assert.Contains("\"sidebar content\"", source, StringComparison.Ordinal);
        Assert.Contains("border-inline-end: 1px solid var(--fk-border-default);", source, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: var(--fk-sidebar-compact) minmax(0, 1fr);", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "FoundationKit.sln")))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the FoundationKit repository root from the test output directory.");
    }
}
