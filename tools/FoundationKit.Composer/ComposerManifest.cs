using System.Text.Json;
using System.Text.Json.Serialization;
using FoundationKit.Application.Capabilities;

namespace FoundationKit.Composer;

public enum ComposerResourceIdType
{
    Guid = 0,
    String = 1,
    Long = 2,
    Int = 3
}

public enum ComposerResourceFieldType
{
    Text = 0
}

public enum ComposerResourceFieldFilterMode
{
    None = 0,
    Exact = 1,
    Prefix = 2
}

public enum ComposerResourceBehavior
{
    Crud = 0,
    Auditing = 1,
    Authorization = 2,
    Concurrency = 3,
    Workflow = 4,
    Caching = 5,
    Security = 6,
    Identity = 7,
    Approvals = 8,
    Notifications = 9,
    Settings = 10,
    FeatureManagement = 11,
    Localization = 12
}

public enum ComposerApiIdempotencyMode
{
    Disabled = 0,
    Optional = 1,
    Required = 2
}

public enum ComposerApiConcurrencyMode
{
    ApplicationPolicy = 0,
    RequireIfMatch = 1
}

public sealed record ComposerResourceField(
    string Name,
    ComposerResourceFieldType Type,
    bool Required,
    int MaximumLength)
{
    public ComposerResourceFieldFilterMode FilterMode { get; init; } = ComposerResourceFieldFilterMode.None;
    public bool Sortable { get; init; }
    public bool Indexed { get; init; }
    public bool Unique { get; init; }
}

public sealed record ComposerResourceApi(
    string RoutePrefix,
    ComposerApiIdempotencyMode Idempotency,
    ComposerApiConcurrencyMode Concurrency,
    int MaximumFilters,
    int MaximumSorts,
    string? RateLimitPolicyName);

public sealed record ComposerResourceOverrides(string? Manager);

public sealed record ComposerResourceDefinition(
    string Name,
    string Route,
    ComposerResourceIdType IdType,
    IReadOnlyList<ComposerResourceBehavior> Behaviors,
    IReadOnlyList<ComposerResourceField> Fields,
    ComposerResourceOverrides Overrides,
    ComposerResourceApi Api)
{
    public bool IsExecutable => Fields.Count > 0;
}

public sealed record ComposerModuleDefinition(
    string Name,
    IReadOnlyList<ComposerResourceDefinition> Resources);

public sealed record ComposerProjectModel(IReadOnlyList<ComposerModuleDefinition> Modules)
{
    public IReadOnlyList<ComposerResourceDefinition> Resources =>
        Modules.SelectMany(module => module.Resources).ToArray();
}

public sealed record ComposerManifest(
    int SchemaVersion,
    string Name,
    string Profile,
    IReadOnlyList<string> IncludeCapabilities,
    IReadOnlyList<string> ExcludeCapabilities,
    IReadOnlyList<string> Providers,
    IReadOnlyList<CapabilityContractRequirement>? CapabilityContracts = null)
{
    public ComposerProjectModel? ProjectModel { get; init; }

    public IReadOnlyList<CapabilityContractRequirement> ContractRequirements =>
        CapabilityContracts ?? Array.Empty<CapabilityContractRequirement>();

    public IReadOnlyList<string> ResourceCapabilityIds => ProjectModel is null
        ? Array.Empty<string>()
        : ProjectModel.Resources
            .SelectMany(resource => resource.Behaviors)
            .Select(MapBehaviorCapability)
            .Where(id => id is not null)
            .Select(id => id!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public FoundationKitProjectManifest ToProjectManifest()
    {
        var effectiveInclude = IncludeCapabilities
            .Concat(ResourceCapabilityIds)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new FoundationKitProjectManifest(
            Name,
            Profile,
            effectiveInclude,
            ExcludeCapabilities,
            Providers,
            ContractRequirements);
    }

    private static string? MapBehaviorCapability(ComposerResourceBehavior behavior) => behavior switch
    {
        ComposerResourceBehavior.Crud => null,
        ComposerResourceBehavior.Concurrency => null,
        ComposerResourceBehavior.Auditing => FoundationCapabilityIds.Auditing,
        ComposerResourceBehavior.Authorization => FoundationCapabilityIds.Authorization,
        ComposerResourceBehavior.Workflow => FoundationCapabilityIds.Workflow,
        ComposerResourceBehavior.Caching => FoundationCapabilityIds.Caching,
        ComposerResourceBehavior.Security => FoundationCapabilityIds.Security,
        ComposerResourceBehavior.Identity => FoundationCapabilityIds.Identity,
        ComposerResourceBehavior.Approvals => FoundationCapabilityIds.Approvals,
        ComposerResourceBehavior.Notifications => FoundationCapabilityIds.Notifications,
        ComposerResourceBehavior.Settings => FoundationCapabilityIds.Settings,
        ComposerResourceBehavior.FeatureManagement => FoundationCapabilityIds.FeatureManagement,
        ComposerResourceBehavior.Localization => FoundationCapabilityIds.Localization,
        _ => throw new InvalidOperationException($"Unsupported resource behavior '{behavior}'.")
    };
}

public static class ComposerManifestParser
{
    private const int MaxContractVersion = 9999;
    private const int MaxModules = 32;
    private const int MaxResourcesPerModule = 64;
    private const int MaxResources = 256;
    private const int MaxFieldsPerResource = 32;
    private const int MaxTextFieldLength = 4000;

    private static readonly HashSet<string> ReservedGeneratedFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id",
        "Version"
    };

    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class",
        "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event",
        "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if",
        "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null",
        "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref",
        "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
        "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
        "virtual", "void", "volatile", "while"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static ComposerManifest Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        ManifestDocument document;
        try
        {
            document = JsonSerializer.Deserialize<ManifestDocument>(json, JsonOptions)
                ?? throw new ComposerManifestException("The manifest is empty.");
        }
        catch (JsonException exception)
        {
            throw new ComposerManifestException(
                $"The manifest is not valid FoundationKit JSON: {exception.Message}",
                exception);
        }

        if (document.SchemaVersion is not 1 and not 2)
        {
            throw new ComposerManifestException(
                $"Unsupported manifest schemaVersion '{document.SchemaVersion}'. Expected 1 or 2.");
        }
        if (document.SchemaVersion == 1 && document.Modules is not null)
        {
            throw new ComposerManifestException("Manifest field 'modules' requires schemaVersion 2.");
        }

        var name = RequireProjectName(document.Name);
        var profile = RequireValue(document.Profile, "profile");
        var include = NormalizeIds(document.IncludeCapabilities, "includeCapabilities");
        var exclude = NormalizeIds(document.ExcludeCapabilities, "excludeCapabilities");
        var providers = NormalizeIds(document.Providers, "providers");
        var contracts = NormalizeContracts(document.CapabilityContracts);
        var projectModel = document.SchemaVersion == 2
            ? NormalizeProjectModel(document.Modules)
            : null;

        if (include.Intersect(exclude, StringComparer.OrdinalIgnoreCase).FirstOrDefault() is { } conflict)
        {
            throw new ComposerManifestException(
                $"Capability '{conflict}' cannot appear in both includeCapabilities and excludeCapabilities.");
        }

        var manifest = new ComposerManifest(
            document.SchemaVersion,
            name,
            profile,
            include,
            exclude,
            providers,
            contracts)
        {
            ProjectModel = projectModel
        };

        var excludedRequiredByResource = manifest.ResourceCapabilityIds
            .Intersect(exclude, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (excludedRequiredByResource is not null)
        {
            throw new ComposerManifestException(
                $"Capability '{excludedRequiredByResource}' is required by a resource behavior and cannot be excluded globally.");
        }

        return manifest;
    }

    public static async Task<ComposerManifest> ParseFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            throw new ComposerManifestException($"Manifest file was not found: {path}");

        var json = await File.ReadAllTextAsync(path, cancellationToken);
        return Parse(json);
    }

    private static ComposerProjectModel NormalizeProjectModel(IReadOnlyList<ModuleDocument>? modules)
    {
        if (modules is null || modules.Count == 0)
            throw new ComposerManifestException("Schema v2 requires at least one module.");
        if (modules.Count > MaxModules)
            throw new ComposerManifestException($"Schema v2 supports at most {MaxModules} modules.");

        var normalizedModules = new List<ComposerModuleDefinition>(modules.Count);
        var moduleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var apiRoutes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var totalResources = 0;

        foreach (var module in modules)
        {
            var moduleName = RequireIdentifier(module.Name, "module name");
            if (!moduleNames.Add(moduleName))
                throw new ComposerManifestException($"Duplicate module name '{moduleName}'.");
            if (module.Resources is null || module.Resources.Count == 0)
                throw new ComposerManifestException($"Module '{moduleName}' must contain at least one resource.");
            if (module.Resources.Count > MaxResourcesPerModule)
                throw new ComposerManifestException($"Module '{moduleName}' supports at most {MaxResourcesPerModule} resources.");

            totalResources += module.Resources.Count;
            if (totalResources > MaxResources)
                throw new ComposerManifestException($"Schema v2 supports at most {MaxResources} resources in total.");

            var resourceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var resources = new List<ComposerResourceDefinition>(module.Resources.Count);
            foreach (var resource in module.Resources)
            {
                var normalized = NormalizeResource(moduleName, resource);
                if (!resourceNames.Add(normalized.Name))
                    throw new ComposerManifestException($"Module '{moduleName}' contains duplicate resource '{normalized.Name}'.");

                var apiRoute = $"{normalized.Api.RoutePrefix}/{normalized.Route}";
                if (!apiRoutes.Add(apiRoute))
                    throw new ComposerManifestException($"Duplicate resource API route '{apiRoute}'.");
                resources.Add(normalized);
            }

            normalizedModules.Add(new ComposerModuleDefinition(
                moduleName,
                resources.OrderBy(resource => resource.Name, StringComparer.OrdinalIgnoreCase).ToArray()));
        }

        return new ComposerProjectModel(
            normalizedModules.OrderBy(module => module.Name, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static ComposerResourceDefinition NormalizeResource(string moduleName, ResourceDocument resource)
    {
        var name = RequireIdentifier(resource.Name, $"resource name in module '{moduleName}'");
        var route = NormalizeRoute(resource.Route, name);
        var idType = ParseIdType(resource.IdType);
        var behaviors = NormalizeBehaviors(resource.Behaviors, moduleName, name);
        var fields = NormalizeFields(resource.Fields, moduleName, name);
        var manager = resource.Overrides?.Manager is null
            ? null
            : RequireIdentifier(resource.Overrides.Manager, $"manager override for '{moduleName}.{name}'");
        var api = NormalizeApi(resource.Api);
        var hasConfiguredFilters = fields.Any(field => field.FilterMode != ComposerResourceFieldFilterMode.None);
        var hasConfiguredSorts = fields.Any(field => field.Sortable);
        if (hasConfiguredFilters && api.MaximumFilters == 0)
        {
            throw new ComposerManifestException(
                $"Resource '{moduleName}.{name}' declares filterable fields but api.maximumFilters is 0.");
        }
        if (hasConfiguredSorts && api.MaximumSorts == 0)
        {
            throw new ComposerManifestException(
                $"Resource '{moduleName}.{name}' declares sortable fields but api.maximumSorts is 0.");
        }
        if (hasConfiguredSorts && api.MaximumSorts > 1)
        {
            throw new ComposerManifestException(
                $"Resource '{moduleName}.{name}' currently supports at most one SQL sort because CrudQueryPlan exposes one OrderBy selector.");
        }

        return new ComposerResourceDefinition(
            name,
            route,
            idType,
            behaviors,
            fields,
            new ComposerResourceOverrides(manager),
            api);
    }

    private static ComposerResourceField[] NormalizeFields(
        IReadOnlyList<FieldDocument>? values,
        string moduleName,
        string resourceName)
    {
        if (values is null)
            return Array.Empty<ComposerResourceField>();
        if (values.Count == 0)
        {
            throw new ComposerManifestException(
                $"Resource '{moduleName}.{resourceName}' declares 'fields' but the list is empty.");
        }
        if (values.Count > MaxFieldsPerResource)
        {
            throw new ComposerManifestException(
                $"Resource '{moduleName}.{resourceName}' supports at most {MaxFieldsPerResource} executable fields.");
        }

        var fields = new List<ComposerResourceField>(values.Count);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            var name = RequireIdentifier(value.Name, $"field name in '{moduleName}.{resourceName}'");
            if (ReservedGeneratedFieldNames.Contains(name))
            {
                throw new ComposerManifestException(
                    $"Resource field '{moduleName}.{resourceName}.{name}' is reserved by generated infrastructure.");
            }
            if (!names.Add(name))
                throw new ComposerManifestException($"Resource '{moduleName}.{resourceName}' contains duplicate field '{name}'.");

            var type = value.Type?.Trim().ToLowerInvariant() switch
            {
                "text" => ComposerResourceFieldType.Text,
                null or "" => throw new ComposerManifestException(
                    $"Resource field '{moduleName}.{resourceName}.{name}' requires a field type."),
                _ => throw new ComposerManifestException(
                    $"Resource field '{moduleName}.{resourceName}.{name}' has unsupported type '{value.Type}'. Allowed: text.")
            };
            var maximumLength = value.MaximumLength
                ?? throw new ComposerManifestException(
                    $"Resource field '{moduleName}.{resourceName}.{name}' requires maximumLength.");
            if (maximumLength is < 1 or > MaxTextFieldLength)
            {
                throw new ComposerManifestException(
                    $"Resource field '{moduleName}.{resourceName}.{name}' maximumLength must be between 1 and {MaxTextFieldLength}.");
            }

            var filterMode = value.Query?.Filter?.Trim().ToLowerInvariant() switch
  {
      null or "" or "none" => ComposerResourceFieldFilterMode.None,
      "exact" => ComposerResourceFieldFilterMode.Exact,
      "prefix" => ComposerResourceFieldFilterMode.Prefix,
      _ => throw new ComposerManifestException(
          $"Resource field '{moduleName}.{resourceName}.{name}' has unsupported query.filter '{value.Query!.Filter}'. Allowed: none, exact, prefix.")
  };
  var sortable = value.Query?.Sortable ?? false;
  var indexed = value.Index?.Enabled ?? false;
  var unique = value.Index?.Unique ?? false;
  if (unique && !indexed)
  {
      throw new ComposerManifestException(
          $"Resource field '{moduleName}.{resourceName}.{name}' cannot be unique unless index.enabled is true.");
  }
  if ((filterMode != ComposerResourceFieldFilterMode.None || sortable) && !indexed)
  {
      throw new ComposerManifestException(
          $"Resource field '{moduleName}.{resourceName}.{name}' must enable an index before it can be filterable or sortable in the generated SQL path.");
  }
  if (indexed && maximumLength > 450)
  {
      throw new ComposerManifestException(
          $"Indexed text field '{moduleName}.{resourceName}.{name}' maximumLength cannot exceed 450 characters in the conservative SQL Server generated contract.");
  }

  fields.Add(new ComposerResourceField(
      name,
      type,
      value.Required ?? true,
      maximumLength)
  {
      FilterMode = filterMode,
      Sortable = sortable,
      Indexed = indexed,
      Unique = unique
  });
        }

        return fields.OrderBy(field => field.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static IReadOnlyList<ComposerResourceBehavior> NormalizeBehaviors(
        IReadOnlyList<string>? values,
        string moduleName,
        string resourceName)
    {
        if (values is null || values.Count == 0)
            throw new ComposerManifestException($"Resource '{moduleName}.{resourceName}' must declare behaviors.");

        var result = new List<ComposerResourceBehavior>(values.Count);
        var seen = new HashSet<ComposerResourceBehavior>();
        foreach (var value in values)
        {
            var behavior = (value?.Trim().ToLowerInvariant()) switch
            {
                "crud" => ComposerResourceBehavior.Crud,
                "auditing" => ComposerResourceBehavior.Auditing,
                "authorization" => ComposerResourceBehavior.Authorization,
                "concurrency" => ComposerResourceBehavior.Concurrency,
                "workflow" => ComposerResourceBehavior.Workflow,
                "caching" => ComposerResourceBehavior.Caching,
                "security" => ComposerResourceBehavior.Security,
                "identity" => ComposerResourceBehavior.Identity,
                "approvals" => ComposerResourceBehavior.Approvals,
                "notifications" => ComposerResourceBehavior.Notifications,
                "settings" => ComposerResourceBehavior.Settings,
                "feature-management" => ComposerResourceBehavior.FeatureManagement,
                "localization" => ComposerResourceBehavior.Localization,
                _ => throw new ComposerManifestException(
                    $"Resource '{moduleName}.{resourceName}' contains unknown behavior '{value}'.")
            };
            if (!seen.Add(behavior))
                throw new ComposerManifestException($"Resource '{moduleName}.{resourceName}' contains duplicate behavior '{value}'.");
            result.Add(behavior);
        }

        if (!seen.Contains(ComposerResourceBehavior.Crud))
        {
            throw new ComposerManifestException(
                $"Resource '{moduleName}.{resourceName}' must include 'crud' in schema v2 because the current executable Module Engine is CRUD-based.");
        }

        return result.OrderBy(item => item).ToArray();
    }

    private static ComposerResourceApi NormalizeApi(ApiDocument? api)
    {
        var routePrefix = NormalizeRoutePrefix(api?.RoutePrefix ?? "api");
        var idempotency = (api?.Idempotency?.Trim().ToLowerInvariant()) switch
        {
            null or "" or "disabled" => ComposerApiIdempotencyMode.Disabled,
            "optional" => ComposerApiIdempotencyMode.Optional,
            "required" => ComposerApiIdempotencyMode.Required,
            _ => throw new ComposerManifestException($"Unknown API idempotency mode '{api!.Idempotency}'.")
        };
        var concurrency = (api?.Concurrency?.Trim().ToLowerInvariant()) switch
        {
            null or "" or "application-policy" => ComposerApiConcurrencyMode.ApplicationPolicy,
            "require-if-match" => ComposerApiConcurrencyMode.RequireIfMatch,
            _ => throw new ComposerManifestException($"Unknown API concurrency mode '{api!.Concurrency}'.")
        };
        var maximumFilters = api?.MaximumFilters ?? 10;
        var maximumSorts = api?.MaximumSorts ?? 5;
        if (maximumFilters is < 0 or > 25)
            throw new ComposerManifestException("API maximumFilters must be between 0 and 25.");
        if (maximumSorts is < 0 or > 10)
            throw new ComposerManifestException("API maximumSorts must be between 0 and 10.");
        var rateLimit = string.IsNullOrWhiteSpace(api?.RateLimitPolicyName)
            ? null
            : NormalizeToken(api!.RateLimitPolicyName!, "rateLimitPolicyName", 96);

        return new ComposerResourceApi(
            routePrefix,
            idempotency,
            concurrency,
            maximumFilters,
            maximumSorts,
            rateLimit);
    }

    private static ComposerResourceIdType ParseIdType(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "guid" => ComposerResourceIdType.Guid,
        "string" => ComposerResourceIdType.String,
        "long" => ComposerResourceIdType.Long,
        "int" => ComposerResourceIdType.Int,
        null or "" => throw new ComposerManifestException("Resource field 'idType' is required."),
        _ => throw new ComposerManifestException($"Unsupported resource idType '{value}'. Allowed: guid, string, long, int.")
    };

    private static string NormalizeRoute(string? value, string resourceName)
    {
        var route = RequireValue(value, $"route for resource '{resourceName}'").Trim('/').ToLowerInvariant();
        if (route.Length is 0 or > 96)
            throw new ComposerManifestException($"Resource route for '{resourceName}' must contain between 1 and 96 characters.");
        var segments = route.Split('/');
        if (segments.Any(segment =>
                segment.Length == 0 ||
                !char.IsAsciiLetterOrDigit(segment[0]) ||
                !char.IsAsciiLetterOrDigit(segment[^1]) ||
                segment.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-')))
        {
            throw new ComposerManifestException(
                $"Resource route '{route}' must use non-empty ASCII segments containing only letters, digits and '-'.");
        }
        return route;
    }

    private static string NormalizeRoutePrefix(string value)
    {
        var route = RequireValue(value, "routePrefix").Trim('/').ToLowerInvariant();
        if (route.Length is 0 or > 48)
            throw new ComposerManifestException("API routePrefix must contain between 1 and 48 characters.");
        var segments = route.Split('/');
        if (segments.Any(segment =>
                segment.Length == 0 ||
                !char.IsAsciiLetterOrDigit(segment[0]) ||
                !char.IsAsciiLetterOrDigit(segment[^1]) ||
                segment.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-')))
        {
            throw new ComposerManifestException(
                "API routePrefix segments must contain only ASCII letters, digits and '-' and cannot be empty.");
        }
        return route;
    }

    private static string RequireProjectName(string? value)
    {
        var name = RequireValue(value, "name");
        if (name.Length > 100)
            throw new ComposerManifestException("Project name cannot exceed 100 characters.");

        if (!char.IsLetter(name[0]) || name.Any(character =>
                !(char.IsLetterOrDigit(character) || character is '.' or '-' or '_')))
        {
            throw new ComposerManifestException(
                "Project name must start with a letter and contain only letters, digits, '.', '-', or '_'.");
        }

        return name;
    }

    private static string RequireIdentifier(string? value, string fieldName)
    {
        var identifier = RequireValue(value, fieldName);
        if (identifier.Length > 96 || !char.IsAsciiLetter(identifier[0]) ||
            identifier.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_') ||
            CSharpKeywords.Contains(identifier))
        {
            throw new ComposerManifestException(
                $"Manifest field '{fieldName}' must be a safe C# identifier (ASCII letter first; then letters, digits or '_'; not a C# keyword)."
            );
        }
        return identifier;
    }

    private static string RequireValue(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ComposerManifestException($"Manifest field '{fieldName}' is required.");
        return value.Trim();
    }

    private static string NormalizeToken(string value, string fieldName, int maximumLength)
    {
        var token = RequireValue(value, fieldName);
        if (token.Length > maximumLength || token.Any(char.IsControl))
            throw new ComposerManifestException($"Manifest field '{fieldName}' is too long or contains control characters.");
        return token;
    }

    private static IReadOnlyList<string> NormalizeIds(
        IReadOnlyList<string>? values,
        string fieldName)
    {
        if (values is null)
            return Array.Empty<string>();

        var result = new List<string>(values.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ComposerManifestException($"Manifest field '{fieldName}' cannot contain an empty capability ID.");

            var normalized = value.Trim();
            if (!seen.Add(normalized))
                throw new ComposerManifestException($"Manifest field '{fieldName}' contains duplicate capability '{normalized}'.");
            result.Add(normalized);
        }

        return result;
    }

    private static CapabilityContractRequirement[] NormalizeContracts(
        IReadOnlyDictionary<string, int>? values)
    {
        if (values is null)
            return Array.Empty<CapabilityContractRequirement>();

        var result = new List<CapabilityContractRequirement>(values.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in values)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
                throw new ComposerManifestException("Manifest field 'capabilityContracts' cannot contain an empty capability ID.");

            var capabilityId = pair.Key.Trim();
            if (!seen.Add(capabilityId))
                throw new ComposerManifestException($"Manifest field 'capabilityContracts' contains duplicate capability '{capabilityId}'.");

            if (pair.Value is <= 0 or > MaxContractVersion)
                throw new ComposerManifestException($"Capability contract '{capabilityId}' must be an integer from 1 to {MaxContractVersion}.");

            result.Add(new CapabilityContractRequirement(capabilityId, pair.Value));
        }

        return result
            .OrderBy(requirement => requirement.CapabilityId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private sealed record ManifestDocument(
        int SchemaVersion,
        string? Name,
        string? Profile,
        IReadOnlyList<string>? IncludeCapabilities,
        IReadOnlyList<string>? ExcludeCapabilities,
        IReadOnlyList<string>? Providers,
        IReadOnlyDictionary<string, int>? CapabilityContracts,
        IReadOnlyList<ModuleDocument>? Modules);

    private sealed record ModuleDocument(string? Name, IReadOnlyList<ResourceDocument>? Resources);

    private sealed record ResourceDocument(
        string? Name,
        string? Route,
        string? IdType,
        IReadOnlyList<string>? Behaviors,
        IReadOnlyList<FieldDocument>? Fields,
        OverridesDocument? Overrides,
        ApiDocument? Api);

    private sealed record FieldDocument(
        string? Name,
        string? Type,
        bool? Required,
        int? MaximumLength,
        QueryDocument? Query,
        IndexDocument? Index);

    private sealed record QueryDocument(string? Filter, bool? Sortable);

    private sealed record IndexDocument(bool? Enabled, bool? Unique);

    private sealed record OverridesDocument(string? Manager);

    private sealed record ApiDocument(
        string? RoutePrefix,
        string? Idempotency,
        string? Concurrency,
        int? MaximumFilters,
        int? MaximumSorts,
        string? RateLimitPolicyName);
}

public sealed class ComposerManifestException : Exception
{
    public ComposerManifestException(string message) : base(message) { }
    public ComposerManifestException(string message, Exception innerException) : base(message, innerException) { }
}
