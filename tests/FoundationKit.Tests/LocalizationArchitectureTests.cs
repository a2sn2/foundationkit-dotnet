using FoundationKit.Application.Results;
using FoundationKit.Domain.Primitives;
using FoundationKit.Infrastructure.Persistence;
using FoundationKit.Localization;
using FoundationKit.Settings;
using FoundationKit.FeatureManagement;

namespace FoundationKit.Tests;

public sealed class LocalizationArchitectureTests
{
    [Fact]
    public void Lower_and_peer_packages_do_not_depend_back_on_localization()
    {
        foreach (var assembly in new[] { typeof(Entity<>).Assembly, typeof(Result).Assembly, typeof(EfRepository<,,>).Assembly,
                     typeof(SettingReader).Assembly, typeof(FeatureDefinition).Assembly })
            AssertNoReferences(assembly, "FoundationKit.Localization");
    }

    [Fact]
    public void Localization_is_provider_neutral() => AssertNoReferences(
        typeof(SupportedCultureSet).Assembly, "FoundationKit.Infrastructure", "FoundationKit.WebApi",
        "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore");

    private static void AssertNoReferences(System.Reflection.Assembly assembly, params string[] forbidden)
    {
        var refs = assembly.GetReferencedAssemblies().Select(x => x.Name ?? string.Empty).ToArray();
        foreach (var name in forbidden) Assert.DoesNotContain(refs, x => x.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
