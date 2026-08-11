using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FoundationKit.WebApi.Api;

/// <summary>
/// Aligns OpenAPI required-property metadata with the CLR nullability contract.
/// Swashbuckle 6.x can describe nullability without necessarily populating the
/// schema-level required set, which would make generated clients weaken
/// non-null response contracts.
/// </summary>
public sealed class FoundationRequiredPropertiesSchemaFilter : ISchemaFilter
{
    private static readonly NullabilityInfoContext Nullability = new();

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        if (schema.Properties.Count == 0)
            return;

        foreach (var property in context.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!IsRequired(property))
                continue;

            var wireName = ResolveSchemaPropertyName(schema, property);
            if (wireName is not null)
                schema.Required.Add(wireName);
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
            return schema.Properties.Keys.FirstOrDefault(
                candidate => string.Equals(candidate, explicitName, StringComparison.Ordinal));
        }

        return schema.Properties.Keys.FirstOrDefault(
            candidate => string.Equals(candidate, property.Name, StringComparison.OrdinalIgnoreCase));
    }
}
