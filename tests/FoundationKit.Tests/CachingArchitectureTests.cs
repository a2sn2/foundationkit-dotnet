using FoundationKit.Application.Results;
using FoundationKit.Caching;
using FoundationKit.Domain.Primitives;
using FoundationKit.Infrastructure.Persistence;
using FoundationKit.Settings;
using FoundationKit.FeatureManagement;
using FoundationKit.Localization;

namespace FoundationKit.Tests;

public sealed class CachingArchitectureTests
{
    [Fact]
    public void Lower_and_peer_packages_do_not_depend_back_on_caching()
    {
        foreach (var assembly in new[] { typeof(Entity<>).Assembly, typeof(Result).Assembly, typeof(EfRepository<,,>).Assembly,
                     typeof(SettingReader).Assembly, typeof(FeatureDefinition).Assembly, typeof(SupportedCultureSet).Assembly })
            AssertNoReferences(assembly, "FoundationKit.Caching");
    }

    [Fact]
    public void Caching_is_provider_neutral() => AssertNoReferences(
        typeof(InMemoryCacheStore).Assembly, "FoundationKit.Infrastructure", "FoundationKit.WebApi",
        "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "StackExchange.Redis");

    private static void AssertNoReferences(System.Reflection.Assembly assembly, params string[] forbidden)
    {
        var refs = assembly.GetReferencedAssemblies().Select(x => x.Name ?? string.Empty).ToArray();
        foreach (var name in forbidden) Assert.DoesNotContain(refs, x => x.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
