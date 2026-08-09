using FoundationKit.Application.Capabilities;

namespace FoundationKit.Tests;

public sealed class CapabilityModelTests
{
    [Fact]
    public void Approval_capability_resolves_required_dependencies()
    {
        var resolver = CapabilityResolver.CreateDefault();

        var resolved = resolver.Resolve([FoundationCapabilityIds.Approvals]);
        var ids = resolved.Select(capability => capability.Id).ToArray();

        Assert.Contains(FoundationCapabilityIds.Kernel, ids);
        Assert.Contains(FoundationCapabilityIds.Security, ids);
        Assert.Contains(FoundationCapabilityIds.Identity, ids);
        Assert.Contains(FoundationCapabilityIds.Authorization, ids);
        Assert.Contains(FoundationCapabilityIds.Auditing, ids);
        Assert.Contains(FoundationCapabilityIds.Workflow, ids);
        Assert.Equal(FoundationCapabilityIds.Approvals, ids[^1]);
    }

    [Fact]
    public void Resolver_deduplicates_transitive_dependencies()
    {
        var resolver = CapabilityResolver.CreateDefault();

        var resolved = resolver.Resolve(
            [FoundationCapabilityIds.Approvals, FoundationCapabilityIds.Documents]);

        Assert.Single(resolved.Where(capability => capability.Id == FoundationCapabilityIds.Auditing));
        Assert.Single(resolved.Where(capability => capability.Id == FoundationCapabilityIds.Authorization));
    }

    [Fact]
    public void Selection_allows_removing_independent_profile_capability()
    {
        var resolver = CapabilityResolver.CreateDefault();

        var resolved = resolver.ResolveSelection(
            FoundationCapabilityProfiles.Standard,
            exclude: [FoundationCapabilityIds.Localization]);

        Assert.DoesNotContain(resolved, capability => capability.Id == FoundationCapabilityIds.Localization);
        Assert.Contains(resolved, capability => capability.Id == FoundationCapabilityIds.Identity);
    }

    [Fact]
    public void Selection_rejects_excluding_required_dependency()
    {
        var resolver = CapabilityResolver.CreateDefault();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            resolver.ResolveSelection(
                FoundationCapabilityProfiles.Standard,
                include: [FoundationCapabilityIds.Approvals],
                exclude: [FoundationCapabilityIds.Auditing]));

        Assert.Contains(FoundationCapabilityIds.Auditing, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Unknown_capability_is_rejected()
    {
        var resolver = CapabilityResolver.CreateDefault();

        Assert.Throws<KeyNotFoundException>(() => resolver.Resolve(["not-a-capability"]));
    }

    [Fact]
    public void Dependency_cycle_is_rejected()
    {
        CapabilityDescriptor[] descriptors =
        [
            new("a", "A", CapabilityKind.Optional, CapabilityMaturity.Planned, "Test", "A", ["b"]),
            new("b", "B", CapabilityKind.Optional, CapabilityMaturity.Planned, "Test", "B", ["a"])
        ];
        var resolver = new CapabilityResolver(descriptors);

        Assert.Throws<InvalidOperationException>(() => resolver.Resolve(["a"]));
    }

    [Fact]
    public void Manifest_composes_profile_additions_and_provider_dependencies()
    {
        var resolver = CapabilityResolver.CreateDefault();
        var manifest = new FoundationKitProjectManifest(
            "Example",
            FoundationCapabilityProfiles.Minimal,
            [FoundationCapabilityIds.Caching],
            Array.Empty<string>(),
            [FoundationCapabilityIds.RedisProvider]);

        var resolved = manifest.Resolve(resolver);

        Assert.Contains(resolved, capability => capability.Id == FoundationCapabilityIds.Caching);
        Assert.Contains(resolved, capability => capability.Id == FoundationCapabilityIds.RedisProvider);
        Assert.Contains(resolved, capability => capability.Id == FoundationCapabilityIds.Kernel);
    }

    [Fact]
    public void Contract_catalog_covers_every_capability_with_v1()
    {
        var capabilities = FoundationCapabilityCatalog.All;
        var contracts = FoundationCapabilityContracts.All;

        Assert.Equal(capabilities.Count, contracts.Count);
        Assert.Equal(
            capabilities.Select(capability => capability.Id).Order(StringComparer.OrdinalIgnoreCase),
            contracts.Select(contract => contract.CapabilityId).Order(StringComparer.OrdinalIgnoreCase));
        Assert.All(contracts, contract => Assert.Equal(1, contract.ContractVersion));
    }

    [Fact]
    public void Maturity_evidence_catalog_covers_every_capability_and_passes_policy()
    {
        var capabilities = FoundationCapabilityCatalog.All;
        var evidence = FoundationCapabilityMaturityEvidence.All;

        Assert.Equal(capabilities.Count, evidence.Count);
        Assert.Equal(
            capabilities.Select(capability => capability.Id).Order(StringComparer.OrdinalIgnoreCase),
            evidence.Select(item => item.CapabilityId).Order(StringComparer.OrdinalIgnoreCase));

        var results = CapabilityMaturityEvidencePolicy.EvaluateCatalog(capabilities, evidence);

        Assert.Equal(capabilities.Count, results.Count);
        Assert.All(results, result => Assert.True(result.IsValid));
    }

    [Fact]
    public void Planned_maturity_requires_only_a_bounded_rationale()
    {
        var capability = TestCapability(CapabilityMaturity.Planned);
        var evidence = TestEvidence(CapabilityMaturity.Planned);

        var result = CapabilityMaturityEvidencePolicy.Evaluate(capability, evidence);

        Assert.True(result.IsValid);
        Assert.Empty(result.MissingEvidence);
    }

    [Fact]
    public void ReferenceOnly_requires_implementation_or_proof_evidence()
    {
        var capability = TestCapability(CapabilityMaturity.ReferenceOnly);
        var evidence = TestEvidence(CapabilityMaturity.ReferenceOnly);

        var result = CapabilityMaturityEvidencePolicy.Evaluate(capability, evidence);

        Assert.False(result.IsValid);
        Assert.Contains("implementation-or-proof", result.MissingEvidence);
    }

    [Fact]
    public void Preview_requires_implementation_and_quality_evidence()
    {
        var capability = TestCapability(CapabilityMaturity.Preview);
        var evidence = TestEvidence(
            CapabilityMaturity.Preview,
            implementation: true);

        var result = CapabilityMaturityEvidencePolicy.Evaluate(capability, evidence);

        Assert.False(result.IsValid);
        Assert.DoesNotContain("implementation-or-proof", result.MissingEvidence);
        Assert.Contains("quality-gates", result.MissingEvidence);
    }

    [Fact]
    public void Stable_requires_all_four_evidence_signals()
    {
        var capability = TestCapability(CapabilityMaturity.Stable);
        var evidence = TestEvidence(
            CapabilityMaturity.Stable,
            implementation: true,
            quality: true);

        var result = CapabilityMaturityEvidencePolicy.Evaluate(capability, evidence);

        Assert.False(result.IsValid);
        Assert.Contains("adoption", result.MissingEvidence);
        Assert.Contains("compatibility-support", result.MissingEvidence);
    }

    [Fact]
    public void Maturity_evidence_must_match_declared_maturity()
    {
        var capability = TestCapability(CapabilityMaturity.ReferenceOnly);
        var evidence = TestEvidence(
            CapabilityMaturity.Preview,
            implementation: true,
            quality: true);

        var result = CapabilityMaturityEvidencePolicy.Evaluate(capability, evidence);

        Assert.False(result.IsValid);
        Assert.Contains("declared-maturity-match", result.MissingEvidence);
    }

    [Fact]
    public void Maturity_evidence_requires_bounded_rationale()
    {
        var capability = TestCapability(CapabilityMaturity.Planned);
        var evidence = TestEvidence(CapabilityMaturity.Planned, rationale: " ");

        var result = CapabilityMaturityEvidencePolicy.Evaluate(capability, evidence);

        Assert.False(result.IsValid);
        Assert.Contains("bounded-rationale", result.MissingEvidence);
    }

    [Fact]
    public void Catalog_validation_rejects_missing_maturity_evidence()
    {
        CapabilityDescriptor[] capabilities = [TestCapability(CapabilityMaturity.Planned)];

        var exception = Assert.Throws<InvalidOperationException>(() =>
            CapabilityMaturityEvidencePolicy.EnsureCatalogValid(
                capabilities,
                Array.Empty<CapabilityMaturityEvidenceDescriptor>()));

        Assert.Contains("has no maturity evidence assessment", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_validation_rejects_maturity_promotion_without_required_evidence()
    {
        CapabilityDescriptor[] capabilities = [TestCapability(CapabilityMaturity.Stable)];
        CapabilityMaturityEvidenceDescriptor[] evidence =
        [
            TestEvidence(
                CapabilityMaturity.Stable,
                implementation: true,
                quality: true,
                adoption: false,
                compatibility: false)
        ];

        var exception = Assert.Throws<InvalidOperationException>(() =>
            CapabilityMaturityEvidencePolicy.EnsureCatalogValid(capabilities, evidence));

        Assert.Contains("adoption", exception.Message, StringComparison.Ordinal);
        Assert.Contains("compatibility-support", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Project_manifest_accepts_compatible_contract_requirement()
    {
        var resolver = CapabilityResolver.CreateDefault();
        var manifest = new FoundationKitProjectManifest(
            "ApprovalSystem",
            FoundationCapabilityProfiles.Minimal,
            [FoundationCapabilityIds.Approvals],
            Array.Empty<string>(),
            Array.Empty<string>(),
            [new CapabilityContractRequirement(FoundationCapabilityIds.Authorization, 1)]);

        var resolved = manifest.Resolve(resolver);

        Assert.Contains(resolved, capability => capability.Id == FoundationCapabilityIds.Authorization);
    }

    [Fact]
    public void Project_manifest_rejects_incompatible_contract_requirement()
    {
        var resolver = CapabilityResolver.CreateDefault();
        var manifest = new FoundationKitProjectManifest(
            "ApprovalSystem",
            FoundationCapabilityProfiles.Minimal,
            [FoundationCapabilityIds.Approvals],
            Array.Empty<string>(),
            Array.Empty<string>(),
            [new CapabilityContractRequirement(FoundationCapabilityIds.Approvals, 2)]);

        var exception = Assert.Throws<InvalidOperationException>(() => manifest.Resolve(resolver));

        Assert.Contains("requires contract v2", exception.Message, StringComparison.Ordinal);
        Assert.Contains("provides v1", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Contract_requirement_for_unresolved_capability_is_rejected()
    {
        var resolver = CapabilityResolver.CreateDefault();
        var manifest = new FoundationKitProjectManifest(
            "MinimalApi",
            FoundationCapabilityProfiles.Minimal,
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            [new CapabilityContractRequirement(FoundationCapabilityIds.Approvals, 1)]);

        var exception = Assert.Throws<InvalidOperationException>(() => manifest.Resolve(resolver));

        Assert.Contains("does not resolve in this composition", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_distinguishes_extracted_reference_capabilities_from_future_features()
    {
        var workflow = FoundationCapabilityCatalog.All.Single(
            capability => capability.Id == FoundationCapabilityIds.Workflow);
        var approvals = FoundationCapabilityCatalog.All.Single(
            capability => capability.Id == FoundationCapabilityIds.Approvals);
        var notifications = FoundationCapabilityCatalog.All.Single(
            capability => capability.Id == FoundationCapabilityIds.Notifications);
        var settings = FoundationCapabilityCatalog.All.Single(
            capability => capability.Id == FoundationCapabilityIds.Settings);
        var featureManagement = FoundationCapabilityCatalog.All.Single(
            capability => capability.Id == FoundationCapabilityIds.FeatureManagement);
        var localization = FoundationCapabilityCatalog.All.Single(
            capability => capability.Id == FoundationCapabilityIds.Localization);
        var caching = FoundationCapabilityCatalog.All.Single(
            capability => capability.Id == FoundationCapabilityIds.Caching);
        var cliTooling = FoundationCapabilityCatalog.All.Single(
            capability => capability.Id == FoundationCapabilityIds.CliTooling);
        var files = FoundationCapabilityCatalog.All.Single(
            capability => capability.Id == FoundationCapabilityIds.Files);
        var kernel = FoundationCapabilityCatalog.All.Single(
            capability => capability.Id == FoundationCapabilityIds.Kernel);

        Assert.Equal(CapabilityMaturity.ReferenceOnly, workflow.Maturity);
        Assert.Equal(CapabilityMaturity.ReferenceOnly, approvals.Maturity);
        Assert.Equal(CapabilityMaturity.ReferenceOnly, notifications.Maturity);
        Assert.Equal(CapabilityMaturity.ReferenceOnly, settings.Maturity);
        Assert.Equal(CapabilityMaturity.ReferenceOnly, featureManagement.Maturity);
        Assert.Equal(CapabilityMaturity.ReferenceOnly, localization.Maturity);
        Assert.Equal(CapabilityMaturity.ReferenceOnly, caching.Maturity);
        Assert.Equal(CapabilityMaturity.ReferenceOnly, cliTooling.Maturity);
        Assert.Equal(CapabilityMaturity.Planned, files.Maturity);
        Assert.Equal(CapabilityMaturity.Stable, kernel.Maturity);
    }

    private static CapabilityDescriptor TestCapability(CapabilityMaturity maturity) =>
        new("test", "Test", CapabilityKind.Optional, maturity, "Test", "Test capability", Array.Empty<string>());

    private static CapabilityMaturityEvidenceDescriptor TestEvidence(
        CapabilityMaturity maturity,
        bool implementation = false,
        bool quality = false,
        bool adoption = false,
        bool compatibility = false,
        string rationale = "Test evidence rationale") =>
        new(
            "test",
            maturity,
            implementation,
            quality,
            adoption,
            compatibility,
            rationale);
}
