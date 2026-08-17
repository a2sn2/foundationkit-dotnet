using System.Text.Json.Nodes;
using FoundationKit.Application.Crud;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FoundationKit.WebApi.Api;

public sealed class FoundationApiOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<FoundationApiOperationMetadata>()
            .SingleOrDefault();
        if (metadata is null)
            return;

        operation.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        operation.Extensions["x-foundation-module"] =
            new JsonNodeExtension(JsonValue.Create(metadata.ModuleName)!);
        operation.Extensions["x-foundation-operation"] =
            new JsonNodeExtension(JsonValue.Create(ToContractOperation(metadata.Operation))!);
    }

    private static string ToContractOperation(CrudOperation operation) => operation switch
    {
        CrudOperation.List => "list",
        CrudOperation.Read => "get",
        CrudOperation.Create => "create",
        CrudOperation.Update => "update",
        CrudOperation.Delete => "delete",
        _ => throw new InvalidOperationException(
            $"Unsupported Foundation CRUD operation '{operation}'.")
    };
}
