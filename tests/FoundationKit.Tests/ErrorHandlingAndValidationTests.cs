using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using FoundationKit.Application.Crud;
using FoundationKit.Application.Results;
using FoundationKit.Application.Validation;
using FoundationKit.Infrastructure.Platform;
using FoundationKit.WebApi;
using FoundationKit.WebApi.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FoundationKit.Tests;

public sealed class ErrorHandlingAndValidationTests
{
    [Fact]
    public async Task Data_annotations_validator_reports_structural_failures()
    {
        var validator = new DataAnnotationsValidator<AnnotatedRequest>();

        var failures = await validator.ValidateAsync(new AnnotatedRequest(string.Empty, 0));

        Assert.Contains(failures, failure => failure.PropertyName == nameof(AnnotatedRequest.Name));
        Assert.Contains(failures, failure => failure.PropertyName == nameof(AnnotatedRequest.Count));
        Assert.All(failures, failure => Assert.Equal("Foundation.Validation.DataAnnotation", failure.ErrorCode));
    }

    [Theory]
    [InlineData(ExceptionScenario.Validation, StatusCodes.Status400BadRequest, "Foundation.Request.Validation")]
    [InlineData(ExceptionScenario.BadRequest, StatusCodes.Status400BadRequest, "Foundation.Http.BadRequest")]
    [InlineData(ExceptionScenario.InvalidJson, StatusCodes.Status400BadRequest, "Foundation.Http.InvalidJson")]
    [InlineData(ExceptionScenario.NotFound, StatusCodes.Status404NotFound, "Foundation.Http.NotFound")]
    [InlineData(ExceptionScenario.Forbidden, StatusCodes.Status403Forbidden, "Foundation.Http.Forbidden")]
    [InlineData(ExceptionScenario.Concurrency, StatusCodes.Status409Conflict, "Foundation.Crud.ConcurrencyConflict")]
    [InlineData(ExceptionScenario.Timeout, StatusCodes.Status504GatewayTimeout, "Foundation.Http.Timeout")]
    [InlineData(ExceptionScenario.DownstreamUnavailable, StatusCodes.Status503ServiceUnavailable, "Foundation.Http.DownstreamUnavailable")]
    public async Task Exception_handler_maps_known_failures(
        ExceptionScenario scenario,
        int expectedStatus,
        string expectedCode)
    {
        var response = await HandleAsync(CreateException(scenario));

        Assert.True(response.Handled);
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(expectedCode, response.Code);
        Assert.Equal("correlation-test", response.CorrelationId);
        Assert.Equal("test-project", response.ProjectId);
    }

    [Fact]
    public async Task Unhandled_exception_is_safe_by_default()
    {
        const string sensitiveMessage = "secret-database-password";

        var response = await HandleAsync(new InvalidOperationException(sensitiveMessage));

        Assert.True(response.Handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, response.StatusCode);
        Assert.Equal("Foundation.Unhandled", response.Code);
        Assert.DoesNotContain(sensitiveMessage, response.RawJson, StringComparison.Ordinal);
        Assert.DoesNotContain("exceptionType", response.RawJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Custom_exception_mapper_extends_the_pipeline_without_replacing_it()
    {
        await using var provider = BuildProvider(services =>
            services.AddSingleton<IFoundationExceptionMapper, CustomExceptionMapper>());

        var response = await HandleAsync(new CustomException(), provider);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, response.StatusCode);
        Assert.Equal("Test.Custom", response.Code);
    }

    private static Exception CreateException(ExceptionScenario scenario) => scenario switch
    {
        ExceptionScenario.Validation => new ValidationException("invalid"),
        ExceptionScenario.BadRequest => new BadHttpRequestException("bad"),
        ExceptionScenario.InvalidJson => new JsonException("bad json"),
        ExceptionScenario.NotFound => new KeyNotFoundException(),
        ExceptionScenario.Forbidden => new UnauthorizedAccessException(),
        ExceptionScenario.Concurrency => new FoundationConcurrencyException("conflict"),
        ExceptionScenario.Timeout => new TimeoutException(),
        ExceptionScenario.DownstreamUnavailable => new HttpRequestException(),
        _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
    };

    private static ServiceProvider BuildProvider(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFoundationWebApi();
        services.AddFoundationProject("test-project");
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    private static async Task<HandledResponse> HandleAsync(
        Exception exception,
        ServiceProvider? existingProvider = null)
    {
        var ownsProvider = existingProvider is null;
        var provider = existingProvider ?? BuildProvider();
        try
        {
            var handler = provider
                .GetServices<IExceptionHandler>()
                .OfType<FoundationExceptionHandler>()
                .Single();
            var context = new DefaultHttpContext
            {
                RequestServices = provider,
                TraceIdentifier = "correlation-test"
            };
            context.Request.Path = "/test";
            context.Response.Body = new MemoryStream();

            var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);
            context.Response.Body.Position = 0;
            using var body = await JsonDocument.ParseAsync(context.Response.Body);
            var root = body.RootElement;
            return new HandledResponse(
                handled,
                context.Response.StatusCode,
                root.GetProperty("code").GetString(),
                root.GetProperty("correlationId").GetString(),
                root.TryGetProperty("projectId", out var projectId) ? projectId.GetString() : null,
                root.GetRawText());
        }
        finally
        {
            if (ownsProvider)
                await provider.DisposeAsync();
        }
    }

    private sealed record AnnotatedRequest(
        [property: Required] string Name,
        [property: Range(1, 10)] int Count);

    private enum ExceptionScenario
    {
        Validation,
        BadRequest,
        InvalidJson,
        NotFound,
        Forbidden,
        Concurrency,
        Timeout,
        DownstreamUnavailable
    }

    private sealed class CustomException : Exception;

    private sealed class CustomExceptionMapper : IFoundationExceptionMapper
    {
        public bool TryMap(Exception exception, out Error error)
        {
            if (exception is CustomException)
            {
                error = Error.BusinessRule("Test.Custom", "Custom failure.");
                return true;
            }

            error = Error.None;
            return false;
        }
    }

    private sealed record HandledResponse(
        bool Handled,
        int StatusCode,
        string? Code,
        string? CorrelationId,
        string? ProjectId,
        string RawJson);
}
