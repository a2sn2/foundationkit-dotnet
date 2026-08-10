using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using FoundationKit.Application.Crud;
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
    [MemberData(nameof(MappedExceptions))]
    public async Task Exception_handler_maps_known_failures(
        Exception exception,
        int expectedStatus,
        string expectedCode)
    {
        var response = await HandleAsync(exception);

        Assert.True(response.Handled);
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(expectedCode, response.Body.RootElement.GetProperty("code").GetString());
        Assert.Equal("correlation-test", response.Body.RootElement.GetProperty("correlationId").GetString());
        Assert.Equal("test-project", response.Body.RootElement.GetProperty("projectId").GetString());
    }

    [Fact]
    public async Task Unhandled_exception_is_safe_by_default()
    {
        const string sensitiveMessage = "secret-database-password";

        var response = await HandleAsync(new InvalidOperationException(sensitiveMessage));
        var json = response.Body.RootElement.GetRawText();

        Assert.True(response.Handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, response.StatusCode);
        Assert.Equal("Foundation.Unhandled", response.Body.RootElement.GetProperty("code").GetString());
        Assert.DoesNotContain(sensitiveMessage, json, StringComparison.Ordinal);
        Assert.DoesNotContain("exceptionType", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Custom_exception_mapper_extends_the_pipeline_without_replacing_it()
    {
        using var provider = BuildProvider(services =>
            services.AddSingleton<IFoundationExceptionMapper, CustomExceptionMapper>());

        var response = await HandleAsync(new CustomException(), provider);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, response.StatusCode);
        Assert.Equal("Test.Custom", response.Body.RootElement.GetProperty("code").GetString());
    }

    public static IEnumerable<object[]> MappedExceptions()
    {
        yield return [new ValidationException("invalid"), StatusCodes.Status400BadRequest, "Foundation.Request.Validation"];
        yield return [new BadHttpRequestException("bad"), StatusCodes.Status400BadRequest, "Foundation.Http.BadRequest"];
        yield return [new JsonException("bad json"), StatusCodes.Status400BadRequest, "Foundation.Http.InvalidJson"];
        yield return [new KeyNotFoundException(), StatusCodes.Status404NotFound, "Foundation.Http.NotFound"];
        yield return [new UnauthorizedAccessException(), StatusCodes.Status403Forbidden, "Foundation.Http.Forbidden"];
        yield return [new FoundationConcurrencyException("conflict"), StatusCodes.Status409Conflict, "Foundation.Crud.ConcurrencyConflict"];
        yield return [new TimeoutException(), StatusCodes.Status504GatewayTimeout, "Foundation.Http.Timeout"];
        yield return [new HttpRequestException(), StatusCodes.Status503ServiceUnavailable, "Foundation.Http.DownstreamUnavailable"];
    }

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
            var body = await JsonDocument.ParseAsync(context.Response.Body);
            return new HandledResponse(handled, context.Response.StatusCode, body);
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

    private sealed class CustomException : Exception;

    private sealed class CustomExceptionMapper : IFoundationExceptionMapper
    {
        public bool TryMap(Exception exception, out FoundationKit.Application.Results.Error error)
        {
            if (exception is CustomException)
            {
                error = FoundationKit.Application.Results.Error.BusinessRule(
                    "Test.Custom",
                    "Custom failure.");
                return true;
            }

            error = FoundationKit.Application.Results.Error.None;
            return false;
        }
    }

    private sealed record HandledResponse(bool Handled, int StatusCode, JsonDocument Body);
}
