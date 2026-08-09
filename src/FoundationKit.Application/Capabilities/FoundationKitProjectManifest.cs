namespace FoundationKit.Application.Capabilities;

public sealed record FoundationKitProjectManifest(
    string Name,
    string Profile,
    IReadOnlyList<string> IncludeCapabilities,
    IReadOnlyList<string> ExcludeCapabilities,
    IReadOnlyList<string> Providers,
    IReadOnlyList<CapabilityContractRequirement>? CapabilityContracts = null)
{
    public IReadOnlyList<CapabilityContractRequirement> ContractRequirements =>
        CapabilityContracts ?? Array.Empty<CapabilityContractRequirement>();

    public IReadOnlyList<CapabilityDescriptor> Resolve(CapabilityResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);

        var selected = resolver.ResolveSelection(Profile, IncludeCapabilities, ExcludeCapabilities);
        var allRequested = selected.Select(descriptor => descriptor.Id).Concat(Providers);
        var resolved = resolver.Resolve(allRequested);

        CapabilityCompatibility.EnsureCompatible(resolved, ContractRequirements);
        return resolved;
    }
}
