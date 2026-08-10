using FoundationKit.Application.Crud;
using FoundationKit.Application.Modules;
using FoundationKit.Application.Pagination;
using FoundationKit.Domain.Primitives;
using FoundationKit.WebApi.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FoundationKit.WebApi.Crud;

public static class CrudEndpointExtensions
{
    public static RouteGroupBuilder MapFoundationCrud<
        TEntity,
        TId,
        TCreate,
        TUpdate,
        TRead>(
        this IEndpointRouteBuilder endpoints,
        FoundationModuleDefinition<TEntity, TId> module)
        where TEntity : Entity<TId>
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(module);
        var options = module.Crud ?? throw new InvalidOperationException(
            $"Module '{module.Name}' does not enable CRUD.");

        var group = endpoints.MapGroup($"/api/{module.Route}").WithTags(module.Name);
        if (module.HasCapability(FoundationModuleCapabilities.Authorization))
            group.RequireAuthorization();

        if (options.ListEnabled)
        {
            group.MapGet("/", async (
                int? page,
                int? pageSize,
                CrudApplicationService<TEntity, TId, TCreate, TUpdate, TRead> service,
                CancellationToken cancellationToken) =>
            {
                var request = new PageRequest(
                    page ?? 1,
                    Math.Min(pageSize ?? PageRequest.DefaultPageSize, options.MaximumPageSize));
                var result = await service.ListAsync(request, cancellationToken).ConfigureAwait(false);
                return result.ToHttpResult(global::Microsoft.AspNetCore.Http.Results.Ok);
            }).WithName($"{module.Name}.List");
        }

        if (options.ReadEnabled)
        {
            group.MapGet("/{id}", async (
                TId id,
                CrudApplicationService<TEntity, TId, TCreate, TUpdate, TRead> service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetAsync(id, cancellationToken).ConfigureAwait(false);
                return result.ToHttpResult(global::Microsoft.AspNetCore.Http.Results.Ok);
            }).WithName($"{module.Name}.Get");
        }

        if (options.CreateEnabled)
        {
            group.MapPost("/", async (
                TCreate request,
                CrudApplicationService<TEntity, TId, TCreate, TUpdate, TRead> service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.CreateAsync(request, cancellationToken).ConfigureAwait(false);
                return result.ToHttpResult(created =>
                    global::Microsoft.AspNetCore.Http.Results.Created(
                        $"/api/{module.Route}/{created.Id}",
                        created.Item));
            }).WithName($"{module.Name}.Create");
        }

        if (options.UpdateEnabled)
        {
            group.MapPut("/{id}", async (
                TId id,
                TUpdate request,
                CrudApplicationService<TEntity, TId, TCreate, TUpdate, TRead> service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
                return result.ToHttpResult(global::Microsoft.AspNetCore.Http.Results.Ok);
            }).WithName($"{module.Name}.Update");
        }

        if (options.DeleteEnabled)
        {
            group.MapDelete("/{id}", async (
                TId id,
                CrudApplicationService<TEntity, TId, TCreate, TUpdate, TRead> service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
                return result.ToHttpResult();
            }).WithName($"{module.Name}.Delete");
        }

        return group;
    }
}
