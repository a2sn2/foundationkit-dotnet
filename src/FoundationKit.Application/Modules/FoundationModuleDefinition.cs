using FoundationKit.Application.Pagination;
using FoundationKit.Domain.Primitives;

namespace FoundationKit.Application.Modules;

public interface IFoundationModuleDefinition
{
    string Name { get; }

    string Route { get; }

    Type EntityType { get; }

    Type IdType { get; }

    FoundationModuleCapability Capabilities { get; }

    FoundationModuleCapability DeclaredCapabilities => Capabilities;

    FoundationApiModuleOptions Api => FoundationApiModuleOptions.Default;
}

public sealed record CrudModuleOptions(
    bool CreateEnabled,
    bool ReadEnabled,
    bool ListEnabled,
    bool UpdateEnabled,
    bool DeleteEnabled,
    int MaximumPageSize);

public sealed class CrudModuleOptionsBuilder
{
    public bool CreateEnabled { get; set; } = true;

    public bool ReadEnabled { get; set; } = true;

    public bool ListEnabled { get; set; } = true;

    public bool UpdateEnabled { get; set; } = true;

    public bool DeleteEnabled { get; set; } = true;

    public int MaximumPageSize { get; set; } = PageRequest.MaximumPageSize;

    internal CrudModuleOptions Build()
    {
        if (MaximumPageSize is < 1 or > PageRequest.MaximumPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaximumPageSize),
                $"Maximum page size must be between 1 and {PageRequest.MaximumPageSize}.");
        }

        return new CrudModuleOptions(
            CreateEnabled,
            ReadEnabled,
            ListEnabled,
            UpdateEnabled,
            DeleteEnabled,
            MaximumPageSize);
    }
}

public sealed class FoundationModuleDefinition<TEntity, TId> : IFoundationModuleDefinition
    where TEntity : Entity<TId>
    where TId : notnull
{
    internal FoundationModuleDefinition(
        string name,
        string route,
        FoundationModuleCapability capabilities,
        CrudModuleOptions? crud,
        FoundationApiModuleOptions api,
        string? authorizationPolicyPrefix,
        Type? managerType)
    {
        Name = name;
        Route = route;
        FoundationModuleCapabilityRules.ValidateKnown(capabilities);
        DeclaredCapabilities = capabilities;
        Capabilities = FoundationModuleCapabilityRules.Expand(capabilities);
        Crud = crud;
        Api = api;
        AuthorizationPolicyPrefix = authorizationPolicyPrefix;
        ManagerType = managerType;
    }

    public string Name { get; }

    public string Route { get; }

    public Type EntityType => typeof(TEntity);

    public Type IdType => typeof(TId);

    public FoundationModuleCapability DeclaredCapabilities { get; }

    public FoundationModuleCapability Capabilities { get; }

    public CrudModuleOptions? Crud { get; }

    public FoundationApiModuleOptions Api { get; }

    public string? AuthorizationPolicyPrefix { get; }

    public Type? ManagerType { get; }

    public bool HasCapability(FoundationModuleCapability capability) =>
        capability != FoundationModuleCapability.None &&
        (Capabilities & capability) == capability;
}

public sealed class FoundationModuleBuilder<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    private FoundationModuleCapability _capabilities = FoundationModuleCapability.None;
    private string _name = typeof(TEntity).Name;
    private string _route = typeof(TEntity).Name.ToLowerInvariant();
    private CrudModuleOptions? _crud;
    private FoundationApiModuleOptions _api = FoundationApiModuleOptions.Default;
    private string? _authorizationPolicyPrefix;
    private Type? _managerType;

    public FoundationModuleBuilder<TEntity, TId> Named(string name, string route)
    {
        _name = ValidateName(name, nameof(name));
        _route = ValidateRoute(route);
        return this;
    }

    public FoundationModuleBuilder<TEntity, TId> Crud(Action<CrudModuleOptionsBuilder>? configure = null)
    {
        var builder = new CrudModuleOptionsBuilder();
        configure?.Invoke(builder);
        _crud = builder.Build();
        _capabilities |= FoundationModuleCapability.Crud;
        return this;
    }

    public FoundationModuleBuilder<TEntity, TId> Api(Action<FoundationApiModuleOptionsBuilder>? configure = null)
    {
        var builder = new FoundationApiModuleOptionsBuilder();
        configure?.Invoke(builder);
        _api = builder.Build();
        return this;
    }

    public FoundationModuleBuilder<TEntity, TId> Auditing() => Add(FoundationModuleCapability.Auditing);

    public FoundationModuleBuilder<TEntity, TId> Authorization(string? policyPrefix = null)
    {
        _authorizationPolicyPrefix = policyPrefix is null
            ? null
            : ValidateName(policyPrefix, nameof(policyPrefix));
        return Add(FoundationModuleCapability.Authorization);
    }

    public FoundationModuleBuilder<TEntity, TId> Concurrency() => Add(FoundationModuleCapability.Concurrency);

    public FoundationModuleBuilder<TEntity, TId> Workflow() => Add(FoundationModuleCapability.Workflow);

    public FoundationModuleBuilder<TEntity, TId> Caching() => Add(FoundationModuleCapability.Caching);

    public FoundationModuleBuilder<TEntity, TId> Security() => Add(FoundationModuleCapability.Security);

    public FoundationModuleBuilder<TEntity, TId> Identity() => Add(FoundationModuleCapability.Identity);

    public FoundationModuleBuilder<TEntity, TId> Approvals() => Add(FoundationModuleCapability.Approvals);

    public FoundationModuleBuilder<TEntity, TId> Notifications() => Add(FoundationModuleCapability.Notifications);

    public FoundationModuleBuilder<TEntity, TId> Settings() => Add(FoundationModuleCapability.Settings);

    public FoundationModuleBuilder<TEntity, TId> FeatureManagement() => Add(FoundationModuleCapability.FeatureManagement);

    public FoundationModuleBuilder<TEntity, TId> Localization() => Add(FoundationModuleCapability.Localization);

    public FoundationModuleBuilder<TEntity, TId> UseManager<TManager>()
        where TManager : class
    {
        _managerType = typeof(TManager);
        return this;
    }

    public FoundationModuleDefinition<TEntity, TId> Build()
    {
        if (_crud is null)
            throw new InvalidOperationException("A v1 executable module must enable Crud().");

        return new FoundationModuleDefinition<TEntity, TId>(
            _name,
            _route,
            _capabilities,
            _crud,
            _api,
            _authorizationPolicyPrefix,
            _managerType);
    }

    private FoundationModuleBuilder<TEntity, TId> Add(FoundationModuleCapability capability)
    {
        FoundationModuleCapabilityRules.ValidateKnown(capability);
        _capabilities |= capability;
        return this;
    }

    private static string ValidateName(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var trimmed = value.Trim();
        if (trimmed.Length > 96 || trimmed.Any(char.IsControl))
            throw new ArgumentException("Module value is too long or contains control characters.", parameterName);
        return trimmed;
    }

    private static string ValidateRoute(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var route = value.Trim().Trim('/').ToLowerInvariant();
        if (route.Length is 0 or > 96 ||
            route.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '/'))
        {
            throw new ArgumentException("Module route may contain only ASCII letters, digits, '-' and '/'.", nameof(value));
        }

        return route;
    }
}

public interface IFoundationModuleRegistry
{
    IReadOnlyCollection<IFoundationModuleDefinition> Modules { get; }

    IFoundationModuleDefinition? Find(string name);
}

public sealed class FoundationModuleRegistry : IFoundationModuleRegistry
{
    private readonly IReadOnlyDictionary<string, IFoundationModuleDefinition> _modules;

    public FoundationModuleRegistry(IEnumerable<IFoundationModuleDefinition> modules)
    {
        ArgumentNullException.ThrowIfNull(modules);
        var materialized = modules.ToArray();

        var duplicateName = materialized
            .GroupBy(module => module.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateName is not null)
            throw new InvalidOperationException($"Duplicate Foundation module name '{duplicateName.Key}'.");

        var duplicateRoute = materialized
            .GroupBy(module => $"{module.Api.RoutePrefix}/{module.Route}", StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateRoute is not null)
            throw new InvalidOperationException($"Duplicate Foundation module API route '{duplicateRoute.Key}'.");

        _modules = materialized.ToDictionary(module => module.Name, StringComparer.OrdinalIgnoreCase);
        Modules = Array.AsReadOnly(materialized);
    }

    public IReadOnlyCollection<IFoundationModuleDefinition> Modules { get; }

    public IFoundationModuleDefinition? Find(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _modules.GetValueOrDefault(name.Trim());
    }
}
