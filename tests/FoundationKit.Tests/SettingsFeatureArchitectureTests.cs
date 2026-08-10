using FoundationKit.Application.Results;
using FoundationKit.Domain.Primitives;
using FoundationKit.FeatureManagement;
using FoundationKit.Infrastructure.Persistence;
using FoundationKit.Settings;

namespace FoundationKit.Tests;

public sealed class SettingsFeatureArchitectureTests
{
    [Fact]
    public void Lower_packages_do_not_depend_back_on_settings_or_features()
    {
        foreach (var assembly in new[] { typeof(Entity<>).Assembly, typeof(Result).Assembly, typeof(EfRepository<,,>).Assembly })
            AssertNoReferences(assembly, "FoundationKit.Settings", "FoundationKit.FeatureManagement");
    }

    [Fact]
    public void Settings_is_provider_neutral() => AssertNoReferences(
        typeof(SettingReader).Assembly, "FoundationKit.Infrastructure", "FoundationKit.WebApi", "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore");

    [Fact]
    public void Feature_management_depends_on_settings_without_server_dependencies()
    {
        var refs = typeof(FeatureDefinition).Assembly.GetReferencedAssemblies().Select(x => x.Name ?? string.Empty).ToArray();
        Assert.Contains(refs, x => x.Equals("FoundationKit.Settings", StringComparison.OrdinalIgnoreCase));
        AssertNoReferences(typeof(FeatureDefinition).Assembly, "FoundationKit.Infrastructure", "FoundationKit.WebApi", "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore");
    }

    private static void AssertNoReferences(System.Reflection.Assembly assembly, params string[] forbidden)
    {
        var refs = assembly.GetReferencedAssemblies().Select(x => x.Name ?? string.Empty).ToArray();
        foreach (var name in forbidden) Assert.DoesNotContain(refs, x => x.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
