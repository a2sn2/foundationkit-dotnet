using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using FoundationKit.Application.Crud;
using FoundationKit.Application.Isolation;
using FoundationKit.Application.Results;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoundationKit.WebApi.Errors;

public sealed class FoundationErrorHandlingOptions
{
    public bool IncludeExceptionDetails { get; set; }
}

public interface IFoundationExceptionMapper
{
    bool TryMap(Exception exception, out Error error);
}

public sealed class DefaultFoundationExceptionMapper : IFoundationExceptionMapper
{
    public bool TryMap(Exception exception, out Error error)
    {
        error = exception switch
        {
            ValidationException validation => Error.Validation(
                "Foundation.Request.Validation",
                string.IsNullOrWhiteSpace(validation.Message)
                    ? "The request failed validation."
                    : validation.Message),
            BadHttpRequestException => Error.Validation(
                "Foundation.Http.BadRequest",
                "The request could not be processed because it is malformed or invalid."),
            JsonException => Error.Validation(
                "Foundation.Http.InvalidJson",
                "The JSON request body is malformed or invalid."),
            KeyNotFoundException => Error.NotFound(
                "Foundation.Http.NotFound",
                "The requested resource was not found."),
            UnauthorizedAccessException => Error.Forbidden(
                "Foundation.Http.Forbidden",
                "You are not allowed to perform this operation."),
            FoundationConcurrencyException => Error.Conflict(
                "Foundation.Crud.ConcurrencyConflict",
                "The resource changed after it was loaded. Reload it and retry the operation."),
            TimeoutException => Error.Timeout(
                "Foundation.Http.Timeout",
                "The operation timed out before it could complete."),
            OperationCanceledException => Error.Timeout(
                "Foundation.Http.Timeout",
                "The operation was cancelled before it could complete."),
            HttpRequestException => Error.ServiceUnavailable(
                "Foundation.Http.DownstreamUnavailable",
                "A required downstream service is currently unavailable."),
            _ => Error.None
        };

        return error != Error.None;
    }
}

public sealed class FoundationExceptionHandler(
    IEnumerable<IFoundationExceptionMapper> mappers,
    IProblemDetailsService problemDetailsService,
    IOptions<FoundationErrorHandlingOptions> options,
    ILogger<FoundationExceptionHandler> logger) : IExceptionHandler
{
    private readonly IFoundationExceptionMapper[] _mappers =
        (mappers ?? throw new ArgumentNullException(nameof(mappers))).ToArray();
    private readonly IProblemDetailsService _problemDetailsService =
        problemDetailsService ?? throw new ArgumentNullException(nameof(problemDetailsService));
    private readonly FoundationErrorHandlingOptions _options =
        (options ?? throw new ArgumentNullException(nameof(options))).Value;
    private readonly ILogger<FoundationExceptionHandler> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Request {CorrelationId} was cancelled by the client.",
                httpContext.TraceIdentifier);
            return false;
        }

        var mapped = TryMap(exception, out var error);
        if (!mapped)
        {
            error = Error.Failure(
                "Foundation.Unhandled",
                _options.IncludeExceptionDetails
                    ? exception.Message
                    : "An unexpected error occurred. Use the correlationId when contacting support.");
        }

        var statusCode = FoundationHttpErrorMapping.GetStatusCode(error.Type);
        if (mapped)
        {
            _logger.LogWarning(
                exception,
                "Handled request error {ErrorCode} with HTTP {StatusCode}. CorrelationId: {CorrelationId}",
                error.Code,
                statusCode,
                httpContext.TraceIdentifier);
        }
        else
        {
            _logger.LogError(
                exception,
                "Unhandled request exception. CorrelationId: {CorrelationId}",
                httpContext.TraceIdentifier);
        }

        var details = FoundationHttpProblemDetails.Create(
            httpContext,
            error,
            statusCode,
            _options.IncludeExceptionDetails ? exception : null);

        httpContext.Response.StatusCode = statusCode;
        var written = await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = details
        }).ConfigureAwait(false);

        if (!written)
        {
            await httpContext.Response.WriteAsJsonAsync(
                details,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    private bool TryMap(Exception exception, out Error error)
    {
        foreach (var mapper in _mappers)
        {
            if (mapper.TryMap(exception, out error))
                return true;
        }

        error = Error.None;
        return false;
    }
}

internal static class FoundationHttpProblemDetails
{
    public static ProblemDetails Create(
        HttpContext httpContext,
        Error error,
        int statusCode,
        Exception? exception = null)
    {
        var details = new ProblemDetails
        {
            Status = statusCode,
            Title = error.Code,
            Detail = error.Description,
            Instance = httpContext.Request.Path
        };

        details.Extensions["code"] = error.Code;
        details.Extensions["errorType"] = error.Type.ToString();
        details.Extensions["correlationId"] = httpContext.TraceIdentifier;

        var projectContext = httpContext.RequestServices.GetService<IFoundationProjectContext>();
        if (projectContext is not null)
            details.Extensions["projectId"] = projectContext.ProjectId.Value;

        if (exception is not null)
            details.Extensions["exceptionType"] = exception.GetType().FullName;

        return details;
    }

    public static Error FromStatusCode(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => Error.Validation("Foundation.Http.BadRequest", "The request is invalid."),
        StatusCodes.Status401Unauthorized => Error.Unauthorized("Foundation.Http.Unauthorized", "Authentication is required."),
        StatusCodes.Status403Forbidden => Error.Forbidden("Foundation.Http.Forbidden", "You are not allowed to access this resource."),
        StatusCodes.Status404NotFound => Error.NotFound("Foundation.Http.NotFound", "The requested endpoint or resource was not found."),
        StatusCodes.Status405MethodNotAllowed => Error.Validation("Foundation.Http.MethodNotAllowed", "The HTTP method is not allowed for this endpoint."),
        StatusCodes.Status408RequestTimeout => Error.Timeout("Foundation.Http.RequestTimeout", "The request timed out."),
        StatusCodes.Status413PayloadTooLarge => Error.Validation("Foundation.Http.PayloadTooLarge", "The request payload is too large."),
        StatusCodes.Status415UnsupportedMediaType => Error.Validation("Foundation.Http.UnsupportedMediaType", "The request media type is not supported."),
        StatusCodes.Status429TooManyRequests => Error.TooManyRequests("Foundation.Http.TooManyRequests", "Too many requests were received. Retry later."),
        StatusCodes.Status502BadGateway => Error.ServiceUnavailable("Foundation.Http.BadGateway", "A downstream service returned an invalid response."),
        StatusCodes.Status503ServiceUnavailable => Error.ServiceUnavailable("Foundation.Http.ServiceUnavailable", "The service is temporarily unavailable."),
        StatusCodes.Status504GatewayTimeout => Error.Timeout("Foundation.Http.GatewayTimeout", "A downstream operation timed out."),
        _ => Error.Failure("Foundation.Http.Error", "The request could not be completed.")
    };
}

internal static class FoundationHttpErrorMapping
{
    public static int GetStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        ErrorType.TooManyRequests => StatusCodes.Status429TooManyRequests,
        ErrorType.ServiceUnavailable => StatusCodes.Status503ServiceUnavailable,
        ErrorType.Timeout => StatusCodes.Status504GatewayTimeout,
        _ => StatusCodes.Status500InternalServerError
    };
}
