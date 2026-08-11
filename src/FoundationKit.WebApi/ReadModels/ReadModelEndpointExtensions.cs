using System.ComponentModel;
using System.Reflection;
using FoundationKit.Application.Crud;
using FoundationKit.Application.Modules;
using FoundationKit.Application.Pagination;
using FoundationKit.Application.ReadModels;
using FoundationKit.WebApi.Api;
using FoundationKit.WebApi.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FoundationKit.WebApi.ReadModels;

public sealed class FoundationReadModelApiOptions
{
    public string RoutePrefix { get; set; } = "api";

    public int MaximumPageSize { get; set; } = PageRequest.MaximumPageSize;

    public int MaximumFilters { get; set; } = 10;

    public int MaximumSorts { get; set; } = 5;

    public string? AuthorizationPolicy { get; set; }

    public string? RateLimitPolicyName { get; set; }
}

public static class ReadModelEndpointExtensions
{
    public static RouteGroupBuilder MapFoundationReadModel<TReadModel, TResponse>(
        this IEndpointRouteBuilder endpoints,
        string name,
        string route,
        Action<FoundationReadModelApiOptions>? configure = null)
        where TReadModel : class
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(route);

        var options = new FoundationReadModelApiOptions();
        configure?.Invoke(options);
        Validate(
            options.MaximumPageSize,
            options.MaximumFilters,
            options.MaximumSorts);

        var normalizedName = name.Trim();
        var normalizedRoute = NormalizeRoute(route, nameof(route), 96);
        var routePrefix = NormalizeRoute(options.RoutePrefix, nameof(options.RoutePrefix), 48);
        var routeBase = $"/{routePrefix}/{normalizedRoute}";
        var group = endpoints.MapGroup(routeBase).WithTags(normalizedName);

        if (!string.IsNullOrWhiteSpace(options.AuthorizationPolicy))
            group.RequireAuthorization(options.AuthorizationPolicy);
        if (!string.IsNullOrWhiteSpace(options.RateLimitPolicyName))
            group.RequireRateLimiting(options.RateLimitPolicyName);

        var crudOptions = new CrudModuleOptions(
            CreateEnabled: false,
            ReadEnabled: false,
            ListEnabled: true,
            UpdateEnabled: false,
            DeleteEnabled: false,
            options.MaximumPageSize);
        var apiOptions = new FoundationApiModuleOptions(
            routePrefix,
            FoundationApiIdempotencyMode.Disabled,
            FoundationApiConcurrencyMode.ApplicationPolicy,
            options.MaximumFilters,
            options.MaximumSorts,
            options.RateLimitPolicyName);

        group.MapMethods("/", [HttpMethods.Get], async context =>
        {
            if (!FoundationApiQueryParser.TryParseCrudList(
                    context,
                    crudOptions,
                    apiOptions,
                    out var request,
                    out var error))
            {
                await error.ToProblem().ExecuteAsync(context).ConfigureAwait(false);
                return;
            }

            var service = context.RequestServices
                .GetRequiredService<ReadModelQueryService<TReadModel, TResponse>>();
            var result = await service.ListAsync(request, context.RequestAborted).ConfigureAwait(false);
            await result.ToHttpResult(global::Microsoft.AspNetCore.Http.Results.Ok)
                .ExecuteAsync(context).ConfigureAwait(false);
        })
        .WithName($"{normalizedName}.List")
        .WithMetadata(ApiExplorerMetadata(GetListMarker()))
        .WithMetadata(new FoundationApiOperationMetadata(
            normalizedName,
            CrudOperation.List,
            HttpMethods.Get,
            routeBase,
            FoundationApiIdempotencyMode.Disabled,
            FoundationApiConcurrencyMode.ApplicationPolicy,
            !string.IsNullOrWhiteSpace(options.AuthorizationPolicy),
            options.AuthorizationPolicy,
            options.RateLimitPolicyName))
        .WithMetadata(
            new ProducesResponseTypeMetadata(
                StatusCodes.Status200OK,
                typeof(PagedResult<TResponse>),
                ["application/json"]),
            new ProducesResponseTypeMetadata(
                StatusCodes.Status400BadRequest,
                typeof(ProblemDetails),
                ["application/problem+json"]),
            new ProducesResponseTypeMetadata(
                StatusCodes.Status403Forbidden,
                typeof(ProblemDetails),
                ["application/problem+json"]));

        return group;
    }

    private static void Validate(
        int maximumPageSize,
        int maximumFilters,
        int maximumSorts)
    {
        if (maximumPageSize is < 1 or > PageRequest.MaximumPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumPageSize),
                maximumPageSize,
                $"Maximum page size must be between 1 and {PageRequest.MaximumPageSize}.");
        }

        if (maximumFilters is < 0 or > 25)
            throw new ArgumentOutOfRangeException(nameof(maximumFilters), maximumFilters, "Maximum filters must be between 0 and 25.");

        if (maximumSorts is < 0 or > 10)
            throw new ArgumentOutOfRangeException(nameof(maximumSorts), maximumSorts, "Maximum sorts must be between 0 and 10.");
    }

    private static string NormalizeRoute(string value, string parameterName, int maximumLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var route = value.Trim().Trim('/').ToLowerInvariant();
        if (route.Length is 0 || route.Length > maximumLength)
            throw new ArgumentException("Route is empty or too long.", parameterName);
        var segments = route.Split('/');
        if (segments.Any(segment =>
                segment.Length == 0 ||
                !char.IsAsciiLetterOrDigit(segment[0]) ||
                !char.IsAsciiLetterOrDigit(segment[^1]) ||
                segment.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-')))
        {
            throw new ArgumentException(
                "Route segments must start/end with an ASCII letter or digit and may contain only letters, digits, and '-'.",
                parameterName);
        }
        return route;
    }

    private static object[] ApiExplorerMetadata(MethodInfo methodInfo)
    {
        var parameters = methodInfo.GetParameters();
        var metadata = new object[parameters.Length + 1];
        metadata[0] = methodInfo;
        for (var index = 0; index < parameters.Length; index++)
            metadata[index + 1] = new ReadModelParameterBindingMetadata(parameters[index]);
        return metadata;
    }

    private static MethodInfo GetListMarker() =>
        typeof(ReadModelEndpointExtensions).GetMethod(
            nameof(ListApiMarker),
            BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("Read-model ApiExplorer marker was not found.");

    private static void ListApiMarker(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string[]? filter = null,
        [FromQuery] string[]? sort = null) { }

    private sealed class ReadModelParameterBindingMetadata(ParameterInfo parameterInfo) : IParameterBindingMetadata
    {
        public string Name { get; } = parameterInfo.Name ?? string.Empty;
        public bool HasTryParse { get; } = HasTryParseSupport(parameterInfo.ParameterType);
        public bool HasBindAsync => false;
        public ParameterInfo ParameterInfo { get; } = parameterInfo;
        public bool IsOptional { get; } = parameterInfo.HasDefaultValue ||
            Nullable.GetUnderlyingType(parameterInfo.ParameterType) is not null;

        private static bool HasTryParseSupport(Type type)
        {
            var effectiveType = Nullable.GetUnderlyingType(type) ?? type;
            return effectiveType == typeof(string) || effectiveType.IsArray || effectiveType.IsEnum ||
                   TypeDescriptor.GetConverter(effectiveType).CanConvertFrom(typeof(string));
        }
    }
}
