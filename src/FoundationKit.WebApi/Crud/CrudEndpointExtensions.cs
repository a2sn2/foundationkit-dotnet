using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using FoundationKit.Application.Crud;
using FoundationKit.Application.Modules;
using FoundationKit.Application.Pagination;
using FoundationKit.Application.Results;
using FoundationKit.Domain.Primitives;
using FoundationKit.WebApi.Api;
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

        var routeBase = $"/{module.Api.RoutePrefix}/{module.Route}";
        var group = endpoints.MapGroup(routeBase).WithTags(module.Name);
        if (module.HasCapability(FoundationModuleCapability.Authorization) &&
            !string.IsNullOrWhiteSpace(module.AuthorizationPolicyPrefix))
        {
            group.RequireAuthorization(module.AuthorizationPolicyPrefix);
        }

        if (!string.IsNullOrWhiteSpace(module.Api.RateLimitPolicyName))
            group.RequireRateLimiting(module.Api.RateLimitPolicyName);

        if (options.ListEnabled)
        {
            group.MapMethods(
                    "/",
                    [HttpMethods.Get],
                    CreateListDelegate<TEntity, TId, TCreate, TUpdate, TRead>(module, options))
                .WithName($"{module.Name}.List")
                .WithMetadata(ApiExplorerMetadata(GetMarker(nameof(ListApiMarker))))
                .WithMetadata(OperationMetadata(module, CrudOperation.List, HttpMethods.Get, routeBase))
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
                .WithMetadata(ApiExplorerMetadata(GetMarker(nameof(GetApiMarker), typeof(TId))))
                .WithMetadata(OperationMetadata(module, CrudOperation.Read, HttpMethods.Get, $"{routeBase}/{{id}}"))
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
                .WithMetadata(ApiExplorerMetadata(GetMarker(nameof(CreateApiMarker), typeof(TCreate))))
                .WithMetadata(OperationMetadata(module, CrudOperation.Create, HttpMethods.Post, routeBase))
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
                    CreatePutDelegate<TEntity, TId, TCreate, TUpdate, TRead>(module))
                .WithName($"{module.Name}.Update")
                .WithMetadata(ApiExplorerMetadata(GetMarker(nameof(UpdateApiMarker), typeof(TId), typeof(TUpdate))))
                .WithMetadata(OperationMetadata(module, CrudOperation.Update, HttpMethods.Put, $"{routeBase}/{{id}}"))
                .WithMetadata(
                    JsonRequest(typeof(TUpdate)),
                    JsonResponse(StatusCodes.Status200OK, typeof(TRead)),
                    ProblemResponse(StatusCodes.Status400BadRequest),
                    ProblemResponse(StatusCodes.Status403Forbidden),
                    ProblemResponse(StatusCodes.Status404NotFound),
                    ProblemResponse(StatusCodes.Status409Conflict),
                    ProblemResponse(StatusCodes.Status412PreconditionFailed),
                    ProblemResponse(StatusCodes.Status422UnprocessableEntity),
                    ProblemResponse(StatusCodes.Status428PreconditionRequired));
        }

        if (options.DeleteEnabled)
        {
            group.MapMethods(
                    "/{id}",
                    [HttpMethods.Delete],
                    CreateDeleteDelegate<TEntity, TId, TCreate, TUpdate, TRead>(module))
                .WithName($"{module.Name}.Delete")
                .WithMetadata(ApiExplorerMetadata(GetMarker(nameof(DeleteApiMarker), typeof(TId))))
                .WithMetadata(OperationMetadata(module, CrudOperation.Delete, HttpMethods.Delete, $"{routeBase}/{{id}}"))
                .WithMetadata(
                    new ProducesResponseTypeMetadata(StatusCodes.Status204NoContent, typeof(void), []),
                    ProblemResponse(StatusCodes.Status400BadRequest),
                    ProblemResponse(StatusCodes.Status403Forbidden),
                    ProblemResponse(StatusCodes.Status404NotFound));
        }

        return group;
    }

    private static RequestDelegate CreateListDelegate<TEntity, TId, TCreate, TUpdate, TRead>(
        FoundationModuleDefinition<TEntity, TId> module,
        CrudModuleOptions options)
        where TEntity : Entity<TId>
        where TId : notnull =>
        async context =>
        {
            if (!FoundationApiQueryParser.TryParseCrudList(
                    context,
                    options,
                    module.Api,
                    out var request,
                    out var error))
            {
                await WriteProblemAsync(context, error).ConfigureAwait(false);
                return;
            }

            var service = ResolveService<TEntity, TId, TCreate, TUpdate, TRead>(context);
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
                await WriteInvalidIdAsync(context).ConfigureAwait(false);
                return;
            }

            var service = ResolveService<TEntity, TId, TCreate, TUpdate, TRead>(context);
            var result = await service.GetAsync(id, context.RequestAborted).ConfigureAwait(false);
            if (result.IsSuccess)
                ApplyEntityTag<TUpdate, TRead>(context, result.Value);

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
            if (!FoundationApiQueryParser.TryValidateIdempotencyKey(
                    context,
                    module.Api.Idempotency,
                    out var idempotencyError))
            {
                await WriteProblemAsync(context, idempotencyError).ConfigureAwait(false);
                return;
            }

            var request = await TryReadJsonAsync<TCreate>(context).ConfigureAwait(false);
            if (!request.Success)
                return;

            var service = ResolveService<TEntity, TId, TCreate, TUpdate, TRead>(context);
            var result = await service.CreateAsync(request.Value!, context.RequestAborted).ConfigureAwait(false);
            if (result.IsSuccess)
                ApplyEntityTag<TUpdate, TRead>(context, result.Value.Item);

            await result.ToHttpResult(created =>
                    global::Microsoft.AspNetCore.Http.Results.Created(
                        $"/{module.Api.RoutePrefix}/{module.Route}/{created.Id}",
                        created.Item))
                .ExecuteAsync(context)
                .ConfigureAwait(false);
        };

    private static RequestDelegate CreatePutDelegate<TEntity, TId, TCreate, TUpdate, TRead>(
        FoundationModuleDefinition<TEntity, TId> module)
        where TEntity : Entity<TId>
        where TId : notnull =>
        async context =>
        {
            if (!FoundationApiQueryParser.TryValidateIdempotencyKey(
                    context,
                    module.Api.Idempotency,
                    out var idempotencyError))
            {
                await WriteProblemAsync(context, idempotencyError).ConfigureAwait(false);
                return;
            }

            if (!TryReadId(context, out TId id))
            {
                await WriteInvalidIdAsync(context).ConfigureAwait(false);
                return;
            }

            var request = await TryReadJsonAsync<TUpdate>(context).ConfigureAwait(false);
            if (!request.Success)
                return;

            var updateRequest = request.Value!;
            if (module.Api.Concurrency == FoundationApiConcurrencyMode.RequireIfMatch)
            {
                var ifMatch = context.Request.Headers.IfMatch.ToString();
                if (string.IsNullOrWhiteSpace(ifMatch))
                {
                    await WriteProblemAsync(
                        context,
                        Error.PreconditionRequired(
                            "Foundation.Api.IfMatch.Required",
                            "The If-Match header is required for this operation.")).ConfigureAwait(false);
                    return;
                }

                var adapter = context.RequestServices
                    .GetService<IFoundationApiConcurrencyAdapter<TUpdate, TRead>>()
                    ?? throw new InvalidOperationException(
                        $"Module '{module.Name}' requires If-Match but no {typeof(IFoundationApiConcurrencyAdapter<TUpdate, TRead>).Name} is registered.");
                var adapted = adapter.ApplyIfMatch(updateRequest, ifMatch.Trim());
                if (adapted.IsFailure)
                {
                    await WriteProblemAsync(context, adapted.Error).ConfigureAwait(false);
                    return;
                }

                updateRequest = adapted.Value;
            }

            var service = ResolveService<TEntity, TId, TCreate, TUpdate, TRead>(context);
            var result = await service.UpdateAsync(id, updateRequest, context.RequestAborted).ConfigureAwait(false);
            if (result.IsSuccess)
                ApplyEntityTag<TUpdate, TRead>(context, result.Value);

            await result.ToHttpResult(global::Microsoft.AspNetCore.Http.Results.Ok)
                .ExecuteAsync(context)
                .ConfigureAwait(false);
        };

    private static RequestDelegate CreateDeleteDelegate<TEntity, TId, TCreate, TUpdate, TRead>(
        FoundationModuleDefinition<TEntity, TId> module)
        where TEntity : Entity<TId>
        where TId : notnull =>
        async context =>
        {
            if (!FoundationApiQueryParser.TryValidateIdempotencyKey(
                    context,
                    module.Api.Idempotency,
                    out var idempotencyError))
            {
                await WriteProblemAsync(context, idempotencyError).ConfigureAwait(false);
                return;
            }

            if (!TryReadId(context, out TId id))
            {
                await WriteInvalidIdAsync(context).ConfigureAwait(false);
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

    private static void ApplyEntityTag<TUpdate, TRead>(HttpContext context, TRead response)
    {
        var adapter = context.RequestServices.GetService<IFoundationApiConcurrencyAdapter<TUpdate, TRead>>();
        var entityTag = adapter?.GetEntityTag(response);
        if (!string.IsNullOrWhiteSpace(entityTag))
            context.Response.Headers.ETag = entityTag;
    }

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
                "The JSON request body is missing or invalid.")).ConfigureAwait(false);
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

    private static FoundationApiOperationMetadata OperationMetadata<TEntity, TId>(
        FoundationModuleDefinition<TEntity, TId> module,
        CrudOperation operation,
        string method,
        string route)
        where TEntity : Entity<TId>
        where TId : notnull =>
        new(
            module.Name,
            operation,
            method,
            route,
            operation is CrudOperation.Create or CrudOperation.Update or CrudOperation.Delete
                ? module.Api.Idempotency
                : FoundationApiIdempotencyMode.Disabled,
            operation == CrudOperation.Update
                ? module.Api.Concurrency
                : FoundationApiConcurrencyMode.ApplicationPolicy,
            module.HasCapability(FoundationModuleCapability.Authorization),
            module.AuthorizationPolicyPrefix,
            module.Api.RateLimitPolicyName);

    private static object[] ApiExplorerMetadata(MethodInfo methodInfo)
    {
        var parameters = methodInfo.GetParameters();
        var metadata = new object[parameters.Length + 1];
        metadata[0] = methodInfo;
        for (var index = 0; index < parameters.Length; index++)
            metadata[index + 1] = new CrudParameterBindingMetadata(parameters[index]);
        return metadata;
    }

    private static MethodInfo GetMarker(string methodName, params Type[] genericArguments)
    {
        var method = typeof(CrudEndpointExtensions).GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"CRUD ApiExplorer marker '{methodName}' was not found.");

        return method.IsGenericMethodDefinition
            ? method.MakeGenericMethod(genericArguments)
            : method;
    }

    private static void ListApiMarker(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string[]? filter = null,
        [FromQuery] string[]? sort = null)
    {
    }

    private static void GetApiMarker<TId>([FromRoute] TId id)
    {
    }

    private static void CreateApiMarker<TCreate>(
        [FromBody] TCreate request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null)
    {
    }

    private static void UpdateApiMarker<TId, TUpdate>(
        [FromRoute] TId id,
        [FromBody] TUpdate request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null,
        [FromHeader(Name = "If-Match")] string? ifMatch = null)
    {
    }

    private static void DeleteApiMarker<TId>(
        [FromRoute] TId id,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null)
    {
    }

    private static AcceptsMetadata JsonRequest(Type requestType) =>
        new(["application/json"], requestType, isOptional: false);

    private static ProducesResponseTypeMetadata JsonResponse(int statusCode, Type responseType) =>
        new(statusCode, responseType, ["application/json"]);

    private static ProducesResponseTypeMetadata ProblemResponse(int statusCode) =>
        new(statusCode, typeof(ProblemDetails), ["application/problem+json"]);

    private sealed class CrudParameterBindingMetadata(ParameterInfo parameterInfo) : IParameterBindingMetadata
    {
        public string Name { get; } = parameterInfo.Name ?? string.Empty;

        public bool HasTryParse { get; } = HasTryParseSupport(parameterInfo.ParameterType);

        public bool HasBindAsync => false;

        public ParameterInfo ParameterInfo { get; } = parameterInfo;

        public bool IsOptional { get; } =
            parameterInfo.HasDefaultValue ||
            Nullable.GetUnderlyingType(parameterInfo.ParameterType) is not null;

        private static bool HasTryParseSupport(Type type)
        {
            var effectiveType = Nullable.GetUnderlyingType(type) ?? type;
            return effectiveType == typeof(string) ||
                   effectiveType.IsArray ||
                   effectiveType.IsEnum ||
                   TypeDescriptor.GetConverter(effectiveType).CanConvertFrom(typeof(string));
        }
    }
}
