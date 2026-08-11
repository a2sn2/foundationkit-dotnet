using System.Text.Json;
using System.Text.Json.Serialization;

namespace FoundationKit.Composer;

public enum ComposerReadModelKind
{
    Query = 0,
    Report = 1
}

public enum ComposerReadModelFieldType
{
    Text = 0,
    Guid = 1
}

public sealed record ComposerReadModelJoin(
    string Resource,
    string LeftField,
    string RightField);

public sealed record ComposerReadModelField(
    string Name,
    string SourceResource,
    string SourceField,
    ComposerReadModelFieldType Type,
    bool Required,
    int? MaximumLength)
{
    public ComposerResourceFieldFilterMode FilterMode { get; init; } = ComposerResourceFieldFilterMode.None;
    public bool Sortable { get; init; }
}

public sealed record ComposerReadModelDefinition(
    string Name,
    string Route,
    ComposerReadModelKind Kind,
    string SourceResource,
    ComposerReadModelJoin Join,
    IReadOnlyList<ComposerReadModelField> Fields,
    ComposerResourceApi Api);

internal static class ComposerReadModelManifestNormalizer
{
    private const int MaximumReadModels = 32;
    private const int MaximumFields = 64;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static IReadOnlyList<ComposerReadModelDefinition> Normalize(
        JsonElement? element,
        string moduleName,
        IReadOnlyList<ComposerResourceDefinition> resources)
    {
        if (element is null || element.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return Array.Empty<ComposerReadModelDefinition>();
        if (element.Value.ValueKind != JsonValueKind.Array)
            throw new ComposerManifestException($"Module '{moduleName}' field 'readModels' must be an array.");

        ReadModelDocument[] documents;
        try
        {
            documents = JsonSerializer.Deserialize<ReadModelDocument[]>(
                element.Value.GetRawText(),
                JsonOptions) ?? Array.Empty<ReadModelDocument>();
        }
        catch (JsonException exception)
        {
            throw new ComposerManifestException(
                $"Module '{moduleName}' contains an invalid read-model declaration: {exception.Message}",
                exception);
        }

        if (documents.Length > MaximumReadModels)
            throw new ComposerManifestException($"Module '{moduleName}' supports at most {MaximumReadModels} read models.");

        var resourceMap = resources.ToDictionary(resource => resource.Name, StringComparer.OrdinalIgnoreCase);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<ComposerReadModelDefinition>(documents.Length);
        foreach (var document in documents)
        {
            var name = RequireIdentifier(document.Name, $"read-model name in module '{moduleName}'");
            if (!names.Add(name))
                throw new ComposerManifestException($"Module '{moduleName}' contains duplicate read model '{name}'.");
            var route = NormalizeRoute(document.Route, name);
            var kind = document.Kind?.Trim().ToLowerInvariant() switch
            {
                "query" => ComposerReadModelKind.Query,
                "report" => ComposerReadModelKind.Report,
                _ => throw new ComposerManifestException(
                    $"Read model '{moduleName}.{name}' kind must be 'query' or 'report'.")
            };
            var source = RequireIdentifier(document.Source, $"source for '{moduleName}.{name}'");
            if (!resourceMap.TryGetValue(source, out var sourceResource) || !sourceResource.IsExecutable)
            {
                throw new ComposerManifestException(
                    $"Read model '{moduleName}.{name}' source resource '{source}' must be an executable resource in the same module.");
            }

            var join = NormalizeJoin(document.Join, moduleName, name, sourceResource, resourceMap);
            var fields = NormalizeFields(
                document.Fields,
                moduleName,
                name,
                sourceResource,
                join,
                resourceMap);
            var api = NormalizeApi(document.Api, moduleName, name, fields);
            result.Add(new ComposerReadModelDefinition(name, route, kind, source, join, fields, api));
        }

        return result.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static ComposerReadModelJoin NormalizeJoin(
        JoinDocument? document,
        string moduleName,
        string readModelName,
        ComposerResourceDefinition source,
        IReadOnlyDictionary<string, ComposerResourceDefinition> resources)
    {
        if (document is null)
            throw new ComposerManifestException($"Read model '{moduleName}.{readModelName}' requires one bounded left join.");
        if (!string.Equals(document.Type?.Trim(), "left", StringComparison.OrdinalIgnoreCase))
        {
            throw new ComposerManifestException(
                $"Read model '{moduleName}.{readModelName}' currently supports only join.type='left'.");
        }

        var resourceName = RequireIdentifier(
            document.Resource,
            $"join resource for '{moduleName}.{readModelName}'");
        if (!resources.TryGetValue(resourceName, out var joined) || !joined.IsExecutable)
        {
            throw new ComposerManifestException(
                $"Read model '{moduleName}.{readModelName}' join resource '{resourceName}' must be executable and in the same module.");
        }
        if (string.Equals(resourceName, source.Name, StringComparison.OrdinalIgnoreCase))
            throw new ComposerManifestException($"Read model '{moduleName}.{readModelName}' cannot join its source resource to itself yet.");

        var leftField = ResolveResourceField(
            source,
            RequireIdentifier(document.LeftField, "join leftField"),
            moduleName,
            readModelName);
        var rightField = ResolveResourceField(
            joined,
            RequireIdentifier(document.RightField, "join rightField"),
            moduleName,
            readModelName);
        if (leftField.Type != rightField.Type)
            throw new ComposerManifestException($"Read model '{moduleName}.{readModelName}' join fields must have matching types.");
        if (!leftField.Indexed || !rightField.Indexed)
        {
            throw new ComposerManifestException(
                $"Read model '{moduleName}.{readModelName}' join fields must be explicitly indexed on both source tables.");
        }

        return new ComposerReadModelJoin(resourceName, leftField.Name, rightField.Name);
    }

    private static IReadOnlyList<ComposerReadModelField> NormalizeFields(
        IReadOnlyList<ReadModelFieldDocument>? documents,
        string moduleName,
        string readModelName,
        ComposerResourceDefinition source,
        ComposerReadModelJoin join,
        IReadOnlyDictionary<string, ComposerResourceDefinition> resources)
    {
        if (documents is null || documents.Count == 0)
            throw new ComposerManifestException($"Read model '{moduleName}.{readModelName}' must declare explicit projection fields.");
        if (documents.Count > MaximumFields)
            throw new ComposerManifestException($"Read model '{moduleName}.{readModelName}' supports at most {MaximumFields} projection fields.");

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<ComposerReadModelField>(documents.Count);
        foreach (var document in documents)
        {
            var name = RequireIdentifier(document.Name, $"projection field in '{moduleName}.{readModelName}'");
            if (!names.Add(name))
                throw new ComposerManifestException($"Read model '{moduleName}.{readModelName}' contains duplicate projection field '{name}'.");

            var sourceReference = RequireValue(document.From, $"from for '{moduleName}.{readModelName}.{name}'");
            var parts = sourceReference.Split('.', StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                throw new ComposerManifestException(
                    $"Read model field '{moduleName}.{readModelName}.{name}' must use from='Resource.Field'.");
            }
            var sourceResourceName = RequireIdentifier(parts[0], "read-model projection resource");
            var sourceFieldName = RequireIdentifier(parts[1], "read-model projection field");
            if (!string.Equals(sourceResourceName, source.Name, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(sourceResourceName, join.Resource, StringComparison.OrdinalIgnoreCase))
            {
                throw new ComposerManifestException(
                    $"Read model field '{moduleName}.{readModelName}.{name}' may project only from source '{source.Name}' or join '{join.Resource}'.");
            }
            var sourceResource = resources[sourceResourceName];
            var resolved = ResolveProjectionSource(sourceResource, sourceFieldName, moduleName, readModelName);
            var declaredType = document.Type?.Trim().ToLowerInvariant() switch
            {
                "guid" => ComposerReadModelFieldType.Guid,
                "text" => ComposerReadModelFieldType.Text,
                _ => throw new ComposerManifestException(
                    $"Read model field '{moduleName}.{readModelName}.{name}' type must be 'guid' or 'text'.")
            };
            if (declaredType != resolved.Type)
            {
                throw new ComposerManifestException(
                    $"Read model field '{moduleName}.{readModelName}.{name}' type does not match source '{sourceReference}'.");
            }

            var fromJoinedSide = string.Equals(sourceResourceName, join.Resource, StringComparison.OrdinalIgnoreCase);
            var required = document.Required ?? (resolved.Required && !fromJoinedSide);
            if (fromJoinedSide && required)
            {
                throw new ComposerManifestException(
                    $"Read model field '{moduleName}.{readModelName}.{name}' comes from a LEFT JOIN and must be nullable.");
            }

            int? maximumLength = null;
            if (declaredType == ComposerReadModelFieldType.Text)
            {
                maximumLength = document.MaximumLength ?? resolved.MaximumLength;
                if (maximumLength != resolved.MaximumLength)
                {
                    throw new ComposerManifestException(
                        $"Read model field '{moduleName}.{readModelName}.{name}' maximumLength must match source '{sourceReference}' ({resolved.MaximumLength}).");
                }
            }
            else if (document.MaximumLength is not null)
            {
                throw new ComposerManifestException(
                    $"Read model GUID field '{moduleName}.{readModelName}.{name}' cannot declare maximumLength.");
            }

            var filterMode = ParseFilterMode(document.Query?.Filter, moduleName, readModelName, name);
            var sortable = document.Query?.Sortable ?? false;
            if ((filterMode != ComposerResourceFieldFilterMode.None || sortable) && !resolved.Indexed)
            {
                throw new ComposerManifestException(
                    $"Read model query field '{moduleName}.{readModelName}.{name}' must project an indexed source column.");
            }

            result.Add(new ComposerReadModelField(
                name,
                sourceResourceName,
                sourceFieldName,
                declaredType,
                required,
                maximumLength)
            {
                FilterMode = filterMode,
                Sortable = sortable
            });
        }

        return result.OrderBy(field => field.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static ComposerResourceApi NormalizeApi(
        ReadModelApiDocument? document,
        string moduleName,
        string readModelName,
        IReadOnlyList<ComposerReadModelField> fields)
    {
        var routePrefix = NormalizeRoutePrefix(document?.RoutePrefix ?? "api");
        var maximumFilters = document?.MaximumFilters ?? 10;
        var maximumSorts = document?.MaximumSorts ?? 5;
        if (maximumFilters is < 0 or > 25)
            throw new ComposerManifestException("Read-model API maximumFilters must be between 0 and 25.");
        if (maximumSorts is < 0 or > 1)
            throw new ComposerManifestException("Read-model API maximumSorts must be 0 or 1 in the current query plan.");

        var hasFilters = fields.Any(field => field.FilterMode != ComposerResourceFieldFilterMode.None);
        var hasSorts = fields.Any(field => field.Sortable);
        if (hasFilters && maximumFilters == 0)
            throw new ComposerManifestException($"Read model '{moduleName}.{readModelName}' declares filterable fields but maximumFilters is 0.");
        if (!hasFilters && maximumFilters > 0)
            throw new ComposerManifestException($"Read model '{moduleName}.{readModelName}' exposes filters but declares no filterable fields.");
        if (hasSorts && maximumSorts == 0)
            throw new ComposerManifestException($"Read model '{moduleName}.{readModelName}' declares sortable fields but maximumSorts is 0.");
        if (!hasSorts && maximumSorts > 0)
            throw new ComposerManifestException($"Read model '{moduleName}.{readModelName}' exposes sorts but declares no sortable fields.");

        var rateLimit = string.IsNullOrWhiteSpace(document?.RateLimitPolicyName)
            ? null
            : document!.RateLimitPolicyName!.Trim();
        if (rateLimit is { Length: > 96 } || rateLimit?.Any(char.IsControl) == true)
            throw new ComposerManifestException($"Read model '{moduleName}.{readModelName}' rateLimitPolicyName is invalid.");

        return new ComposerResourceApi(
            routePrefix,
            ComposerApiIdempotencyMode.Disabled,
            ComposerApiConcurrencyMode.ApplicationPolicy,
            maximumFilters,
            maximumSorts,
            rateLimit);
    }

    private static ResolvedField ResolveProjectionSource(
        ComposerResourceDefinition resource,
        string fieldName,
        string moduleName,
        string readModelName)
    {
        if (string.Equals(fieldName, "Id", StringComparison.OrdinalIgnoreCase))
            return new ResolvedField("Id", ComposerReadModelFieldType.Guid, true, null, true);

        var field = ResolveResourceField(resource, fieldName, moduleName, readModelName);
        return new ResolvedField(
            field.Name,
            ComposerReadModelFieldType.Text,
            field.Required,
            field.MaximumLength,
            field.Indexed);
    }

    private static ComposerResourceField ResolveResourceField(
        ComposerResourceDefinition resource,
        string fieldName,
        string moduleName,
        string readModelName) =>
        resource.Fields.FirstOrDefault(field => string.Equals(field.Name, fieldName, StringComparison.OrdinalIgnoreCase))
        ?? throw new ComposerManifestException(
            $"Read model '{moduleName}.{readModelName}' references unknown field '{resource.Name}.{fieldName}'.");

    private static ComposerResourceFieldFilterMode ParseFilterMode(
        string? value,
        string moduleName,
        string readModelName,
        string fieldName) => value?.Trim().ToLowerInvariant() switch
        {
            null or "" or "none" => ComposerResourceFieldFilterMode.None,
            "exact" => ComposerResourceFieldFilterMode.Exact,
            "prefix" => ComposerResourceFieldFilterMode.Prefix,
            _ => throw new ComposerManifestException(
                $"Read model field '{moduleName}.{readModelName}.{fieldName}' filter must be none, exact, or prefix.")
        };

    private static string NormalizeRoute(string? value, string name)
    {
        var route = RequireValue(value, $"route for read model '{name}'").Trim('/').ToLowerInvariant();
        ValidateRoute(route, 96, $"read model '{name}' route");
        return route;
    }

    private static string NormalizeRoutePrefix(string value)
    {
        var route = RequireValue(value, "read-model routePrefix").Trim('/').ToLowerInvariant();
        ValidateRoute(route, 48, "read-model routePrefix");
        return route;
    }

    private static void ValidateRoute(string route, int maximumLength, string label)
    {
        if (route.Length is 0 || route.Length > maximumLength)
            throw new ComposerManifestException($"{label} is empty or too long.");
        var segments = route.Split('/');
        if (segments.Any(segment =>
                segment.Length == 0 ||
                !char.IsAsciiLetterOrDigit(segment[0]) ||
                !char.IsAsciiLetterOrDigit(segment[^1]) ||
                segment.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-')))
        {
            throw new ComposerManifestException($"{label} contains an invalid route segment.");
        }
    }

    private static string RequireIdentifier(string? value, string fieldName)
    {
        var identifier = RequireValue(value, fieldName);
        if (identifier.Length > 96 || !char.IsAsciiLetter(identifier[0]) ||
            identifier.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_'))
        {
            throw new ComposerManifestException($"{fieldName} must be a safe ASCII C# identifier.");
        }
        return identifier;
    }

    private static string RequireValue(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ComposerManifestException($"Manifest field '{fieldName}' is required.");
        return value.Trim();
    }

    private sealed record ReadModelDocument(
        string? Name,
        string? Route,
        string? Kind,
        string? Source,
        JoinDocument? Join,
        IReadOnlyList<ReadModelFieldDocument>? Fields,
        ReadModelApiDocument? Api);

    private sealed record JoinDocument(
        string? Type,
        string? Resource,
        string? LeftField,
        string? RightField);

    private sealed record ReadModelFieldDocument(
        string? Name,
        string? From,
        string? Type,
        bool? Required,
        int? MaximumLength,
        QueryDocument? Query);

    private sealed record QueryDocument(string? Filter, bool? Sortable);

    private sealed record ReadModelApiDocument(
        string? RoutePrefix,
        int? MaximumFilters,
        int? MaximumSorts,
        string? RateLimitPolicyName);

    private sealed record ResolvedField(
        string Name,
        ComposerReadModelFieldType Type,
        bool Required,
        int? MaximumLength,
        bool Indexed);
}
