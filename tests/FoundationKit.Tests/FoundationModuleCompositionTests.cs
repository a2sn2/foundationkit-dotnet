using FoundationKit.Application.Modules;
using FoundationKit.Domain.Primitives;

namespace FoundationKit.Tests;

public sealed class FoundationModuleCompositionTests
{
    [Fact]
    public void Cross_cutting_capabilities_expand_through_one_canonical_dependency_closure()
    {
        var module = new FoundationModuleBuilder<TestEntity, Guid>()
            .Named("Orders", "orders")
            .Crud()
            .Approvals()
            .FeatureManagement()
            .Localization()
            .Caching()
            .Notifications()
            .Build();

        Assert.True(module.DeclaredCapabilities.HasFlag(FoundationModuleCapability.Approvals));
        Assert.True(module.DeclaredCapabilities.HasFlag(FoundationModuleCapability.FeatureManagement));
        Assert.False(module.DeclaredCapabilities.HasFlag(FoundationModuleCapability.Workflow));
        Assert.False(module.DeclaredCapabilities.HasFlag(FoundationModuleCapability.Settings));

        Assert.True(module.Capabilities.HasFlag(FoundationModuleCapability.Approvals));
        Assert.True(module.Capabilities.HasFlag(FoundationModuleCapability.Workflow));
        Assert.True(module.Capabilities.HasFlag(FoundationModuleCapability.Auditing));
        Assert.True(module.Capabilities.HasFlag(FoundationModuleCapability.Authorization));
        Assert.True(module.Capabilities.HasFlag(FoundationModuleCapability.Identity));
        Assert.True(module.Capabilities.HasFlag(FoundationModuleCapability.Security));
        Assert.True(module.Capabilities.HasFlag(FoundationModuleCapability.FeatureManagement));
        Assert.True(module.Capabilities.HasFlag(FoundationModuleCapability.Settings));
        Assert.True(module.Capabilities.HasFlag(FoundationModuleCapability.Localization));
        Assert.True(module.Capabilities.HasFlag(FoundationModuleCapability.Caching));
        Assert.True(module.Capabilities.HasFlag(FoundationModuleCapability.Notifications));
    }

    [Fact]
    public void Capability_enum_values_are_unique_single_bit_flags()
    {
        var values = Enum.GetValues<FoundationModuleCapability>()
            .Where(value => value != FoundationModuleCapability.None)
            .Select(value => (int)value)
            .ToArray();

        Assert.Equal(values.Length, values.Distinct().Count());
        Assert.All(values, value => Assert.True((value & (value - 1)) == 0));
    }

    [Fact]
    public void Unknown_capability_bits_fail_fast()
    {
        var unknown = (FoundationModuleCapability)(1 << 20);

        Assert.Throws<ArgumentOutOfRangeException>(() => FoundationModuleCapabilityRules.Expand(unknown));
        Assert.Throws<ArgumentOutOfRangeException>(() => FoundationModuleCapabilityRules.Enumerate(unknown));
    }

    [Fact]
    public void Composition_snapshot_keeps_declared_and_effective_intent_distinct()
    {
        var module = new FoundationModuleBuilder<TestEntity, Guid>()
            .Named("Catalog", "catalog")
            .Crud()
            .Api(api => api.RoutePrefix = "platform/v1")
            .FeatureManagement()
            .Build();

        var snapshot = FoundationModuleComposition.Describe(module);

        Assert.Equal("/platform/v1/catalog", snapshot.ApiRoute);
        Assert.Equal(["Crud", "FeatureManagement"], snapshot.DeclaredCapabilities);
        Assert.Equal(["Crud", "Settings", "FeatureManagement"], snapshot.EffectiveCapabilities);
    }

    [Fact]
    public void Registry_descriptions_are_deterministic()
    {
        var zeta = new FoundationModuleBuilder<TestEntity, Guid>()
            .Named("Zeta", "zeta")
            .Crud()
            .Build();
        var alpha = new FoundationModuleBuilder<SecondEntity, Guid>()
            .Named("Alpha", "alpha")
            .Crud()
            .Build();
        var registry = new FoundationModuleRegistry([zeta, alpha]);

        Assert.Equal(["Alpha", "Zeta"], registry.Describe().Select(module => module.Name));
    }

    [Fact]
    public void Legacy_external_module_definitions_inherit_compatible_defaults()
    {
        IFoundationModuleDefinition legacy = new LegacyModuleDefinition();

        Assert.Equal(legacy.Capabilities, legacy.DeclaredCapabilities);
        Assert.Equal(FoundationApiModuleOptions.Default, legacy.Api);
        Assert.Equal("/api/legacy", FoundationModuleComposition.Describe(legacy).ApiRoute);
    }

    private sealed class TestEntity(Guid id) : Entity<Guid>(id);

    private sealed class SecondEntity(Guid id) : Entity<Guid>(id);

    private sealed class LegacyModuleDefinition : IFoundationModuleDefinition
    {
        public string Name => "Legacy";

        public string Route => "legacy";

        public Type EntityType => typeof(TestEntity);

        public Type IdType => typeof(Guid);

        public FoundationModuleCapability Capabilities => FoundationModuleCapability.Crud;
    }
}
