using System.Security.Cryptography;
using System.Text;
using FoundationKit.Application.Idempotency;
using FoundationKit.Application.Isolation;
using FoundationKit.Application.Modules;
using FoundationKit.Application.Results;
using FoundationKit.WebApi.Api;
using FoundationKit.WebApi.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoundationKit.WebApi.Idempotency;

public sealed class FoundationIdempotencyOptions
{
    public const int MaximumConfiguredBodyBytes = 16 * 1024 * 1024;

    public TimeSpan ReplayWindow { get; set; } = TimeSpan.FromHours(24);
    public int MaximumRequestBodyBytes { get; set; } = 1024 * 1024;
    public int MaximumReplayBodyBytes { get; set; } = 1024 * 1024;

    internal void Validate()
    {
        if (ReplayWindow < TimeSpan.FromMinutes(1) || ReplayWindow > TimeSpan.FromDays(7))
            throw new InvalidOperationException("Foundation idempotency replay window must be between 1 minute and 7 days.");
        if (MaximumRequestBodyBytes is < 0 or > MaximumConfiguredBodyBytes)
            throw new InvalidOperationException($"Foundation idempotency request capture must be between 0 and {MaximumConfiguredBodyBytes} bytes.");
        if (MaximumReplayBodyBytes is < 0 or > MaximumConfiguredBodyBytes)
            throw new InvalidOperationException($"Foundation idempotency response capture must be between 0 and {MaximumConfiguredBodyBytes} bytes.");
    }
}

internal sealed class FoundationIdempotencyMiddleware(
    RequestDelegate next,
    IOptions<FoundationIdempotencyOptions> options,
    TimeProvider timeProvider,
    ILogger<FoundationIdempotencyMiddleware> logger)
{
    private static readonly Action<ILogger, string, string, Exception?> FinalizationFailureLog =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(2100, nameof(FinalizationFailureLog)),
            "Durable idempotency finalization failed for operation {OperationScope}. CorrelationId: {CorrelationId}. The key remains fail-closed.");

    private static readonly Action<ILogger, string, string, Exception?> NonReplayableFailureLog =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(2101, nameof(NonReplayableFailureLog)),
            "Failed to mark durable idempotency operation {OperationScope} non-replayable. CorrelationId: {CorrelationId}. The in-progress lease remains fail-closed.");

    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly FoundationIdempotencyOptions _options =
        (options ?? throw new ArgumentNullException(nameof(options))).Value;
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly ILogger<FoundationIdempotencyMiddleware> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _options.Validate();

        var metadata = context.GetEndpoint()?.Metadata.GetMetadata<FoundationApiOperationMetadata>();
        if (metadata is null || metadata.Idempotency == FoundationApiIdempotencyMode.Disabled)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (!FoundationApiQueryParser.TryValidateIdempotencyKey(context, metadata.Idempotency, out var keyError))
        {
            await WriteProblemAsync(context, keyError).ConfigureAwait(false);
            return;
        }

        var keyValues = context.Request.Headers["Idempotency-Key"];
        if (keyValues.Count == 0 || string.IsNullOrWhiteSpace(keyValues[0]))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        string? ifMatch = null;
        if (metadata.Concurrency == FoundationApiConcurrencyMode.RequireIfMatch)
        {
            if (!FoundationApiQueryParser.TryReadIfMatch(context, out var precondition, out var ifMatchError))
            {
                await WriteProblemAsync(context, ifMatchError).ConfigureAwait(false);
                return;
            }
            ifMatch = precondition.Token;
        }

        var store = context.RequestServices.GetService<IIdempotencyStore>();
        if (store is null)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var projectContext = context.RequestServices.GetRequiredService<IFoundationProjectContext>();
        var requestBody = await ReadRequestBodyAsync(context).ConfigureAwait(false);
        if (requestBody is null)
        {
            await WritePayloadTooLargeAsync(context).ConfigureAwait(false);
            return;
        }

        var operationScope = IdempotencyAcquireRequest.NormalizeScope($"{metadata.ModuleName}:{metadata.Operation}");
        var keyHash = Sha256Hex(keyValues[0]!.Trim());
        var fingerprint = BuildFingerprint(context, operationScope, ifMatch, requestBody);
        var now = _timeProvider.GetUtcNow();
        var acquire = await store.AcquireAsync(
            new IdempotencyAcquireRequest(
                projectContext.ProjectId,
                operationScope,
                keyHash,
                fingerprint,
                now,
                now.Add(_options.ReplayWindow)),
            context.RequestAborted).ConfigureAwait(false);

        switch (acquire.Outcome)
        {
            case IdempotencyAcquireOutcome.Replay:
                await ReplayAsync(context, acquire.Response!).ConfigureAwait(false);
                return;
            case IdempotencyAcquireOutcome.FingerprintConflict:
                await WriteProblemAsync(context, Error.Conflict(
                    "Foundation.Api.Idempotency.FingerprintConflict",
                    "This Idempotency-Key was already used for a different request. Use a new key for the changed request.")).ConfigureAwait(false);
                return;
            case IdempotencyAcquireOutcome.InProgress:
                await WriteProblemAsync(context, Error.Conflict(
                    "Foundation.Api.Idempotency.InProgress",
                    "A request with this Idempotency-Key is already in progress. Retry the same request later.")).ConfigureAwait(false);
                return;
            case IdempotencyAcquireOutcome.NonReplayable:
                await WriteProblemAsync(context, Error.Conflict(
                    "Foundation.Api.Idempotency.NonReplayable",
                    "This Idempotency-Key can no longer be replayed safely. Use a new key after reconciling the prior request outcome.")).ConfigureAwait(false);
                return;
            case IdempotencyAcquireOutcome.Acquired:
                break;
            default:
                throw new InvalidOperationException("Unknown idempotency acquisition outcome.");
        }

        var originalBody = context.Response.Body;
        var capture = new CapturingResponseStream(originalBody, _options.MaximumReplayBodyBytes);
        context.Response.Body = capture;
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch
        {
            await TryMarkNonReplayableAsync(
                store,
                projectContext.ProjectId,
                operationScope,
                keyHash,
                fingerprint,
                context.TraceIdentifier).ConfigureAwait(false);
            throw;
        }
        finally
        {
            context.Response.Body = originalBody;
        }

        if (capture.Overflowed || context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            await TryMarkNonReplayableAsync(
                store,
                projectContext.ProjectId,
                operationScope,
                keyHash,
                fingerprint,
                context.TraceIdentifier).ConfigureAwait(false);
            return;
        }

        var replay = new IdempotencyResponse(
            context.Response.StatusCode,
            context.Response.ContentType,
            capture.CapturedBytes,
            context.Response.Headers.Location.ToString(),
            context.Response.Headers.ETag.ToString()).Normalize(_options.MaximumReplayBodyBytes);

        try
        {
            await store.CompleteAsync(
                projectContext.ProjectId,
                operationScope,
                keyHash,
                fingerprint,
                replay,
                _timeProvider.GetUtcNow(),
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            FinalizationFailureLog(_logger, operationScope, context.TraceIdentifier, exception);
        }
    }

    private async Task<byte[]?> ReadRequestBodyAsync(HttpContext context)
    {
        if (context.Request.ContentLength is > 0 && context.Request.ContentLength > _options.MaximumRequestBodyBytes)
            return null;
        if (context.Request.ContentLength is null or 0)
            return [];

        context.Request.EnableBuffering(64 * 1024, _options.MaximumRequestBodyBytes + 1L);
        try
        {
            using var buffer = new MemoryStream(Math.Min(_options.MaximumRequestBodyBytes, 64 * 1024));
            var rented = new byte[16 * 1024];
            var total = 0;
            while (true)
            {
                var read = await context.Request.Body.ReadAsync(rented.AsMemory(), context.RequestAborted).ConfigureAwait(false);
                if (read == 0)
                    break;
                total += read;
                if (total > _options.MaximumRequestBodyBytes)
                    return null;
                await buffer.WriteAsync(rented.AsMemory(0, read), context.RequestAborted).ConfigureAwait(false);
            }
            return buffer.ToArray();
        }
        catch (IOException)
        {
            return null;
        }
        finally
        {
            if (context.Request.Body.CanSeek)
                context.Request.Body.Position = 0;
        }
    }

    private static string BuildFingerprint(
        HttpContext context,
        string operationScope,
        string? ifMatch,
        byte[] requestBody)
    {
        var bodyHash = Sha256Hex(requestBody);
        var canonical = string.Join('\n',
            context.Request.Method.ToUpperInvariant(),
            operationScope,
            $"{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}",
            context.Request.ContentType?.Trim().ToLowerInvariant() ?? string.Empty,
            ifMatch ?? string.Empty,
            bodyHash);
        return Sha256Hex(Encoding.UTF8.GetBytes(canonical));
    }

    private static string Sha256Hex(string value) => Sha256Hex(Encoding.UTF8.GetBytes(value));

    private static string Sha256Hex(byte[] value) => Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();

    private async Task TryMarkNonReplayableAsync(
        IIdempotencyStore store,
        FoundationProjectId projectId,
        string operationScope,
        string keyHash,
        string fingerprint,
        string correlationId)
    {
        try
        {
            await store.MarkNonReplayableAsync(
                projectId,
                operationScope,
                keyHash,
                fingerprint,
                _timeProvider.GetUtcNow(),
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            NonReplayableFailureLog(_logger, operationScope, correlationId, exception);
        }
    }

    private static async Task ReplayAsync(HttpContext context, IdempotencyResponse response)
    {
        context.Response.StatusCode = response.StatusCode;
        context.Response.ContentType = response.ContentType;
        if (!string.IsNullOrWhiteSpace(response.Location))
            context.Response.Headers.Location = response.Location;
        if (!string.IsNullOrWhiteSpace(response.EntityTag))
            context.Response.Headers.ETag = response.EntityTag;
        if (response.Body.Length > 0)
        {
            context.Response.ContentLength = response.Body.Length;
            await context.Response.Body.WriteAsync(response.Body, context.RequestAborted).ConfigureAwait(false);
        }
    }

    private static Task WritePayloadTooLargeAsync(HttpContext context)
    {
        var error = Error.Validation(
            "Foundation.Api.Idempotency.PayloadTooLarge",
            "The request body is too large for durable idempotency capture.");
        var details = FoundationHttpProblemDetails.Create(context, error, StatusCodes.Status413PayloadTooLarge);
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        return context.Response.WriteAsJsonAsync(details, cancellationToken: context.RequestAborted);
    }

    private static async Task WriteProblemAsync(HttpContext context, Error error)
    {
        var statusCode = FoundationHttpErrorMapping.GetStatusCode(error.Type);
        var details = FoundationHttpProblemDetails.Create(context, error, statusCode);
        context.Response.StatusCode = statusCode;
        var service = context.RequestServices.GetRequiredService<IProblemDetailsService>();
        if (!await service.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = details
            }).ConfigureAwait(false))
        {
            await context.Response.WriteAsJsonAsync(details, cancellationToken: context.RequestAborted).ConfigureAwait(false);
        }
    }

    private sealed class CapturingResponseStream(Stream inner, int maximumBytes) : Stream
    {
        private readonly Stream _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        private readonly int _maximumBytes = maximumBytes >= 0 ? maximumBytes : throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        private readonly MemoryStream _capture = new(Math.Min(maximumBytes, 64 * 1024));

        public bool Overflowed { get; private set; }
        public byte[] CapturedBytes => _capture.ToArray();
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => _inner.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            _inner.Write(buffer, offset, count);
            Capture(buffer.AsSpan(offset, count));
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await _inner.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
            Capture(buffer.Span);
        }

        private void Capture(ReadOnlySpan<byte> bytes)
        {
            if (Overflowed)
                return;
            var remaining = _maximumBytes - checked((int)_capture.Length);
            if (bytes.Length > remaining)
            {
                if (remaining > 0)
                    _capture.Write(bytes[..remaining]);
                Overflowed = true;
                return;
            }
            _capture.Write(bytes);
        }
    }
}
