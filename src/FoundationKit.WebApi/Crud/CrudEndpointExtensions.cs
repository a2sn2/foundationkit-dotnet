using System.ComponentModel;
using System.Text.Json;
using FoundationKit.Application.Crud;
using FoundationKit.Application.Modules;
using FoundationKit.Application.Pagination;
using FoundationKit.Application.Results;
using FoundationKit.Domain.Primitives;
using FoundationKit.WebApi.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

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
        if (module.HasCapability(FoundationModuleCapabilities.Authorization) &&
            !string.IsNullOrWhiteSpace(module.AuthorizationPolicyPrefix))
        {
            group.RequireAuthorization(module.AuthorizationPolicyPrefix);
        }

        if (options.ListEnabled)
        {
            group.MapMethods(
                    "/",
                    [HttpMethods.Get],
                    CreateListDelegate<TEntity, TId, TCreate, TUpdate, TRead>(options))
                .WithName($"{module.Name}.List")
                .WithMetadata(
                    JsonResponse(StatusCodes.Status200OK, typeof(PagedResult<TRead>)),
                    ProblemResponse(StatusCodes.Status400BadRequest),
                    ProblemResponse(StatusCodes.Status403Forbidden));
        }

        if (options.ReadEnabled)
        {
            group.MapMethods(
                    "/{id}",
                    [HttpMethods.Get],
                    CreateGetDelegate<TEntity, TId, TCreate, TUpdate, TRead>())
                .WithName($"{module.Name}.Get")
                .WithMetadata(
                    JsonResponse(StatusCodes.Status200OK, typeof(TRead)),
                    ProblemResponse(StatusCodes.Status400BadRequest),
                    ProblemResponse(StatusCodes.Status403Forbidden),
                    ProblemResponse(StatusCodes.Status404NotFound));
        }

        if (options.CreateEnabled)
        {
            group.MapMethods(
                    "/",
                    [HttpMethods.Post],
                    CreatePostDelegate<TEntity, TId, TCreate, TUpdate, TRead>(module))
                .WithName($"{module.Name}.Create")
                .WithMetadata(
                    JsonRequest(typeof(TCreate)),
                    JsonResponse(StatusCodes.Status201Created, typeof(TRead)),
                    ProblemResponse(StatusCodes.Status400BadRequest),
                    ProblemResponse(StatusCodes.Status403Forbidden),
                    ProblemResponse(StatusCodes.Status422UnprocessableEntity));
        }

        if (options.UpdateEnabled)
        {
            group.MapMethods(
                    "/{id}",
                    [HttpMethods.Put],
                    CreatePutDelegate<TEntity, TId, TCreate, TUpdate, TRead>())
                .WithName($"{module.Name}.Update")
                .WithMetadata(
                    JsonRequest(typeof(TUpdate)),
                    JsonResponse(StatusCodes.Status200OK, typeof(TRead)),
                    ProblemResponse(StatusCodes.Status400BadRequest),
                    ProblemResponse(StatusCodes.Status403Forbidden),
                    ProblemResponse(StatusCodes.Status404NotFound),
                    ProblemResponse(StatusCodes.Status409Conflict),
                    ProblemResponse(StatusCodes.Status422UnprocessableEntity));
        }

        if (options.DeleteEnabled)
        {
            group.MapMethods(
                    "/{id}",
                    [HttpMethods.Delete],
                    CreateDeleteDelegate<TEntity, TId, TCreate, TUpdate, TRead>())
                .WithName($"{module.Name}.Delete")
                .WithMetadata(
                    new ProducesResponseTypeMetadata(StatusCodes.Status204NoContent),
                    ProblemResponse(StatusCodes.Status400BadRequest),
                    ProblemResponse(StatusCodes.Status403Forbidden),
                    ProblemResponse(StatusCodes.Status404NotFound));
        }

        return group;
    }

    private static RequestDelegate CreateListDelegate<TEntity, TId, TCreate, TUpdate, TRead>(CrudModuleOptions options)
        where TEntity : Entity<TId>
        where TId : notnull =>
        async context =>
        {
            if (!TryReadPositiveInt(context.Request.Query["page"], 1, out var page) ||
                !TryReadPositiveInt(
                    context.Request.Query["pageSize"],
                    PageRequest.DefaultPageSize,
                    out var requestedPageSize))
            {
                await WriteProblemAsync(
                    context,
                    Error.Validation(
                        "Foundation.Crud.Pagination.Invalid",
                        "Page and pageSize must be positive integers."));
                return;
            }

            var service = ResolveService<TEntity, TId, TCreate, TUpdate, TRead>(context);
            var request = new PageRequest(page, Math.Min(requestedPageSize, options.MaximumPageSize));
            var result = await service.ListAsync(request, context.RequestAborted).ConfigureAwait(false);
            await result.ToHttpResult(global::Microsoft.AspNetCore.Http.Results.Ok)
                .ExecuteAsync(context)
                .ConfigureAwait(false);
        };

    private static RequestDelegate CreateGetDelegate<TEntity, TId, TCreate, TUpdate, TRead>()
        where TEntity : Entity<TId>
        where TId : notnull =>
        async context =>
        {
            if (!TryReadId(context, out TId id))
            {
                await WriteInvalidIdAsync(context);
                return;
            }

            var service = ResolveService<TEntity, TId, TCreate, TUpdate, TRead>(context);
            var result = await service.GetAsync(id, context.RequestAborted).ConfigureAwait(false);
            await result.ToHttpResult(global::Microsoft.AspNetCore.Http.Results.Ok)
                .ExecuteAsync(context)
                .ConfigureAwait(false);
        };

    private static RequestDelegate CreatePostDelegate<TEntity, TId, TCreate, TUpdate, TRead>(
        FoundationModuleDefinition<TEntity, TId> module)
        where TEntity : Entity<TId>
        where TId : notnull =>
        async context =>
        {
            var request = await TryReadJsonAsync<TCreate>(context).ConfigureAwait(false);
            if (!request.Success)
                return;

            var service = ResolveService<TEntity, TId, TCreate, TUpdate, TRead>(context);
            var result = await service.CreateAsync(request.Value!, context.RequestAborted).ConfigureAwait(false);
            await result.ToHttpResult(created =>
                    global::Microsoft.AspNetCore.Http.Results.Created(
                        $"/api/{module.Route}/{created.Id}",
                        created.Item))
                .ExecuteAsync(context)
                .ConfigureAwait(false);
        };

    private static RequestDelegate CreatePutDelegate<TEntity, TId, TCreate, TUpdate, TRead>()
        where TEntity : Entity<TId>
        where TId : notnull =>
        async context =>
        {
            if (!TryReadId(context, out TId id))
            {
                await WriteInvalidIdAsync(context);
                return;
            }

            var request = await TryReadJsonAsync<TUpdate>(context).ConfigureAwait(false);
            if (!request.Success)
                return;

            var service = ResolveService<TEntity, TId, TCreate, TUpdate, TRead>(context);
            var result = await service.UpdateAsync(id, request.Value!, context.RequestAborted).ConfigureAwait(false);
            await result.ToHttpResult(global::Microsoft.AspNetCore.Http.Results.Ok)
                .ExecuteAsync(context)
                .ConfigureAwait(false);
        };

    private static RequestDelegate CreateDeleteDelegate<TEntity, TId, TCreate, TUpdate, TRead>()
        where TEntity : Entity<TId>
        where TId : notnull =>
        async context =>
        {
            if (!TryReadId(context, out TId id))
            {
                await WriteInvalidIdAsync(context);
                return;
            }

            var service = ResolveService<TEntity, TId, TCreate, TUpdate, TRead>(context);
            var result = await service.DeleteAsync(id, context.RequestAborted).ConfigureAwait(false);
            await result.ToHttpResult().ExecuteAsync(context).ConfigureAwait(false);
        };

    private static CrudApplicationService<TEntity, TId, TCreate, TUpdate, TRead> ResolveService<
        TEntity,
        TId,
        TCreate,
        TUpdate,
        TRead>(HttpContext context)
        where TEntity : Entity<TId>
        where TId : notnull =>
        context.RequestServices.GetRequiredService<CrudApplicationService<TEntity, TId, TCreate, TUpdate, TRead>>();

    private static bool TryReadId<TId>(HttpContext context, out TId id)
        where TId : notnull
    {
        var raw = Convert.ToString(
            context.Request.RouteValues["id"],
            System.Globalization.CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(raw))
        {
            id = default!;
            return false;
        }

        try
        {
            if (typeof(TId) == typeof(string))
            {
                id = (TId)(object)raw;
                return true;
            }

            var converter = TypeDescriptor.GetConverter(typeof(TId));
            if (converter.CanConvertFrom(typeof(string)) &&
                converter.ConvertFromInvariantString(raw) is TId converted)
            {
                id = converted;
                return true;
            }
        }
        catch (Exception exception) when (
            exception is FormatException or NotSupportedException or ArgumentException)
        {
            // Converted to the bounded invalid-id Problem Details response below.
        }

        id = default!;
        return false;
    }

    private static bool TryReadPositiveInt(string? raw, int fallback, out int value)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            value = fallback;
            return true;
        }

        return int.TryParse(
                   raw,
                   System.Globalization.NumberStyles.None,
                   System.Globalization.CultureInfo.InvariantCulture,
                   out value) &&
               value > 0;
    }

    private static async Task<(bool Success, T? Value)> TryReadJsonAsync<T>(HttpContext context)
    {
        try
        {
            var value = await context.Request.ReadFromJsonAsync<T>(
                    cancellationToken: context.RequestAborted)
                .ConfigureAwait(false);
            if (value is not null)
                return (true, value);
        }
        catch (Exception exception) when (
            exception is JsonException or NotSupportedException or BadHttpRequestException)
        {
            // Converted to the bounded invalid-request Problem Details response below.
        }

        await WriteProblemAsync(
            context,
            Error.Validation(
                "Foundation.Crud.Request.Invalid",
                "The JSON request body is missing or invalid."));
        return (false, default);
    }

    private static Task WriteInvalidIdAsync(HttpContext context) =>
        WriteProblemAsync(
            context,
            Error.Validation(
                "Foundation.Crud.Id.Invalid",
                "The route id is missing or invalid."));

    private static Task WriteProblemAsync(HttpContext context, Error error) =>
        error.ToProblem().ExecuteAsync(context);

    private static AcceptsMetadata JsonRequest(Type requestType) =>
        new(["application/json"], requestType, isOptional: false);

    private static ProducesResponseTypeMetadata JsonResponse(int statusCode, Type responseType) =>
        new(statusCode, responseType, ["application/json"]);

    private static ProducesResponseTypeMetadata ProblemResponse(int statusCode) =>
        new(statusCode, typeof(ProblemDetails), ["application/problem+json"]);
}
