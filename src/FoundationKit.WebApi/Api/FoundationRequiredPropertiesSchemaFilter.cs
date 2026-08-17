using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FoundationKit.WebApi.Api;

/// <summary>
/// Aligns OpenAPI required-property metadata with the CLR nullability contract.
/// The filter keeps generated-client requiredness deterministic when the source
/// CLR contract is non-nullable.
/// </summary>
public sealed class FoundationRequiredPropertiesSchemaFilter : ISchemaFilter
{
    private static readonly NullabilityInfoContext Nullability = new();

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        if (schema is not OpenApiSchema concrete
            || concrete.Properties is not { Count: > 0 })
        {
            return;
        }

        concrete.Required ??= new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in context.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!IsRequired(property))
                continue;

            var wireName = ResolveSchemaPropertyName(concrete, property);
            if (wireName is not null)
                concrete.Required.Add(wireName);
        }
    }

    private static bool IsRequired(PropertyInfo property)
    {
        var propertyType = property.PropertyType;
        if (propertyType.IsValueType)
            return Nullable.GetUnderlyingType(propertyType) is null;

        return Nullability.Create(property).ReadState == NullabilityState.NotNull;
    }

    private static string? ResolveSchemaPropertyName(OpenApiSchema schema, PropertyInfo property)
    {
        var explicitName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;
        if (!string.IsNullOrWhiteSpace(explicitName))
        {
            return schema.Properties?.Keys.FirstOrDefault(
                candidate => string.Equals(candidate, explicitName, StringComparison.Ordinal));
        }

        return schema.Properties?.Keys.FirstOrDefault(
            candidate => string.Equals(candidate, property.Name, StringComparison.OrdinalIgnoreCase));
    }
}
