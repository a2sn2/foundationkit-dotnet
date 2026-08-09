namespace FoundationKit.Application.Capabilities;

public sealed record CapabilityContractDescriptor(
    string CapabilityId,
    int ContractVersion);

public sealed record CapabilityContractRequirement(
    string CapabilityId,
    int ContractVersion);

public sealed record CapabilityCompatibilityResult(
    string CapabilityId,
    int RequiredContractVersion,
    int AvailableContractVersion,
    bool IsCompatible);

public static class FoundationCapabilityContracts
{
    public const int CurrentContractVersion = 1;

    private static readonly IReadOnlyList<CapabilityContractDescriptor> Contracts =
        FoundationCapabilityCatalog.All
            .Select(capability => new CapabilityContractDescriptor(
                capability.Id,
                CurrentContractVersion))
            .ToArray();

    private static readonly Dictionary<string, CapabilityContractDescriptor> ById =
        Contracts.ToDictionary(
            contract => contract.CapabilityId,
            StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<CapabilityContractDescriptor> All => Contracts;

    public static CapabilityContractDescriptor Get(string capabilityId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(capabilityId);

        return ById.TryGetValue(capabilityId, out var contract)
            ? contract
            : throw new KeyNotFoundException(
                $"Unknown FoundationKit capability contract '{capabilityId}'.");
    }
}

public static class CapabilityCompatibility
{
    public static IReadOnlyList<CapabilityCompatibilityResult> Evaluate(
        IEnumerable<CapabilityDescriptor> resolvedCapabilities,
        IEnumerable<CapabilityContractRequirement> requirements)
    {
        ArgumentNullException.ThrowIfNull(resolvedCapabilities);
        ArgumentNullException.ThrowIfNull(requirements);

        var resolved = resolvedCapabilities.ToDictionary(
            capability => capability.Id,
            StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<CapabilityCompatibilityResult>();

        foreach (var requirement in requirements)
        {
            ArgumentNullException.ThrowIfNull(requirement);

            if (string.IsNullOrWhiteSpace(requirement.CapabilityId))
            {
                throw new ArgumentException(
                    "Capability contract requirement IDs cannot be empty.",
                    nameof(requirements));
            }

            if (requirement.ContractVersion <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requirements),
                    requirement.ContractVersion,
                    "Capability contract versions must be positive integers.");
            }

            var capabilityId = requirement.CapabilityId.Trim();
            if (!seen.Add(capabilityId))
            {
                throw new ArgumentException(
                    $"Duplicate capability contract requirement '{capabilityId}'.",
                    nameof(requirements));
            }

            if (!resolved.ContainsKey(capabilityId))
            {
                throw new InvalidOperationException(
                    $"Capability contract requirement '{capabilityId}' does not resolve in this composition.");
            }

            var available = FoundationCapabilityContracts.Get(capabilityId).ContractVersion;
            results.Add(new CapabilityCompatibilityResult(
                capabilityId,
                requirement.ContractVersion,
                available,
                requirement.ContractVersion == available));
        }

        return results;
    }

    public static void EnsureCompatible(
        IEnumerable<CapabilityDescriptor> resolvedCapabilities,
        IEnumerable<CapabilityContractRequirement> requirements)
    {
        var results = Evaluate(resolvedCapabilities, requirements);
        var incompatible = results.FirstOrDefault(result => !result.IsCompatible);

        if (incompatible is not null)
        {
            throw new InvalidOperationException(
                $"Capability '{incompatible.CapabilityId}' requires contract v{incompatible.RequiredContractVersion}, " +
                $"but FoundationKit provides v{incompatible.AvailableContractVersion}.");
        }
    }
}
