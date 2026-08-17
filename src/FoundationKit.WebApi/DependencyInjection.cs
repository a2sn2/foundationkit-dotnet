using FoundationKit.Application.Isolation;
using FoundationKit.WebApi.Api;
using FoundationKit.WebApi.Errors;
using FoundationKit.WebApi.Idempotency;
using FoundationKit.WebApi.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FoundationKit.WebApi;

public static class DependencyInjection
{
    public static IServiceCollection AddFoundationWebApi(
        this IServiceCollection services,
        Action<FoundationErrorHandlingOptions>? configureErrorHandling = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<FoundationErrorHandlingOptions>();
        if (configureErrorHandling is not null)
            services.Configure(configureErrorHandling);

        services.AddOptions<FoundationIdempotencyOptions>();
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);

        // Native ASP.NET Core OpenAPI is exposed as a parallel contract surface while
        // Swagger remains the canonical serialized transport used by deterministic
        // Postman and typed-client generation until parity is proven.
        services.AddOpenApi();
        services.Configure<SwaggerGenOptions>(options =>
        {
            options.OperationFilter<FoundationApiOperationFilter>();
            options.SchemaFilter<FoundationRequiredPropertiesSchemaFilter>();
        });

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IFoundationExceptionMapper, DefaultFoundationExceptionMapper>());
        services.AddExceptionHandler<FoundationExceptionHandler>();

        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
                context.ProblemDetails.Extensions["correlationId"] =
                    context.HttpContext.TraceIdentifier;

                var projectContext = context.HttpContext.RequestServices
                    .GetService<IFoundationProjectContext>();
                if (projectContext is not null)
                {
                    context.ProblemDetails.Extensions["projectId"] =
                        projectContext.ProjectId.Value;
                }
            };
        });

        return services;
    }

    public static IServiceCollection ConfigureFoundationIdempotency(
        this IServiceCollection services,
        Action<FoundationIdempotencyOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        services.Configure(configure);
        return services;
    }

    /// <summary>
    /// Adds the complete FoundationKit HTTP request pipeline in its compatibility order:
    /// diagnostics/security first, followed by durable-idempotency orchestration.
    /// </summary>
    public static IApplicationBuilder UseFoundationRequestPipeline(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseFoundationRequestDiagnostics();
        app.UseFoundationIdempotency();
        return app;
    }

    /// <summary>
    /// Adds correlation, exception/Problem Details handling, status-code Problem Details,
    /// and security headers without adding idempotency. Register this before authentication
    /// when authentication failures must retain the FoundationKit diagnostics/security envelope.
    /// </summary>
    public static IApplicationBuilder UseFoundationRequestDiagnostics(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseExceptionHandler();
        app.UseStatusCodePages(async statusCodeContext =>
        {
            var httpContext = statusCodeContext.HttpContext;
            var error = FoundationHttpProblemDetails.FromStatusCode(httpContext.Response.StatusCode);
            var details = FoundationHttpProblemDetails.Create(
                httpContext,
                error,
                httpContext.Response.StatusCode);
            var problemDetailsService =
                httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();

            var written = await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = details
            }).ConfigureAwait(false);

            if (!written)
            {
                await httpContext.Response.WriteAsJsonAsync(
                    details,
                    cancellationToken: httpContext.RequestAborted).ConfigureAwait(false);
            }
        });
        app.UseMiddleware<SecurityHeadersMiddleware>();
        return app;
    }

    /// <summary>
    /// Adds FoundationKit durable-idempotency orchestration only. Hosts that authenticate
    /// requests should normally register this after authentication/authorization so a replay
    /// can never bypass the current request's authorization gate.
    /// </summary>
    public static IApplicationBuilder UseFoundationIdempotency(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseMiddleware<FoundationIdempotencyMiddleware>();
        return app;
    }
}
