using FoundationKit.Application.Crud;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
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

        operation.Extensions["x-foundation-module"] =
            new OpenApiString(metadata.ModuleName);
        operation.Extensions["x-foundation-operation"] =
            new OpenApiString(ToContractOperation(metadata.Operation));
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
