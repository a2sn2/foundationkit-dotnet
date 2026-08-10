using FoundationKit.Application.Isolation;

namespace FoundationKit.Application.Idempotency;

public enum IdempotencyAcquireOutcome
{
    Acquired = 0,
    Replay = 1,
    FingerprintConflict = 2,
    InProgress = 3,
    NonReplayable = 4
}

public sealed record IdempotencyResponse(
    int StatusCode,
    string? ContentType,
    byte[] Body,
    string? Location,
    string? EntityTag)
{
    public const int MaximumContentTypeLength = 128;
    public const int MaximumLocationLength = 2048;
    public const int MaximumEntityTagLength = 256;

    public IdempotencyResponse Normalize(int maximumBodyBytes)
    {
        if (StatusCode is < 100 or > 599)
            throw new ArgumentOutOfRangeException(nameof(StatusCode));
        if (maximumBodyBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(maximumBodyBytes));
        ArgumentNullException.ThrowIfNull(Body);
        if (Body.Length > maximumBodyBytes)
            throw new ArgumentOutOfRangeException(nameof(Body), $"Replay body cannot exceed {maximumBodyBytes} bytes.");

        return this with
        {
            ContentType = NormalizeOptional(ContentType, MaximumContentTypeLength, nameof(ContentType)),
            Location = NormalizeOptional(Location, MaximumLocationLength, nameof(Location)),
            EntityTag = NormalizeOptional(EntityTag, MaximumEntityTagLength, nameof(EntityTag)),
            Body = Body.ToArray()
        };
    }

    private static string? NormalizeOptional(string? value, int maximumLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim();
        if (normalized.Length > maximumLength || normalized.Any(char.IsControl))
            throw new ArgumentException($"{parameterName} is too long or contains control characters.", parameterName);
        return normalized;
    }
}

public sealed record IdempotencyAcquireRequest(
    FoundationProjectId ProjectId,
    string OperationScope,
    string KeyHash,
    string RequestFingerprint,
    DateTimeOffset AcquiredUtc,
    DateTimeOffset ReplayUntilUtc)
{
    public const int MaximumOperationScopeLength = 160;
    public const int Sha256HexLength = 64;

    public IdempotencyAcquireRequest Normalize()
    {
        ArgumentNullException.ThrowIfNull(ProjectId);
        var scope = NormalizeScope(OperationScope);
        var keyHash = NormalizeSha256(KeyHash, nameof(KeyHash));
        var fingerprint = NormalizeSha256(RequestFingerprint, nameof(RequestFingerprint));
        if (ReplayUntilUtc <= AcquiredUtc)
            throw new ArgumentOutOfRangeException(nameof(ReplayUntilUtc), "Replay expiry must be later than acquisition time.");
        return this with
        {
            OperationScope = scope,
            KeyHash = keyHash,
            RequestFingerprint = fingerprint
        };
    }

    public static string NormalizeScope(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > MaximumOperationScopeLength ||
            normalized.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '.' and not ':' and not '/'))
        {
            throw new ArgumentException("Idempotency operation scope contains unsupported characters or is too long.", nameof(value));
        }
        return normalized;
    }

    public static string NormalizeSha256(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length != Sha256HexLength || normalized.Any(character => !Uri.IsHexDigit(character)))
            throw new ArgumentException("Value must be a SHA-256 hexadecimal digest.", parameterName);
        return normalized;
    }
}

public sealed record IdempotencyAcquireResult(
    IdempotencyAcquireOutcome Outcome,
    IdempotencyResponse? Response = null)
{
    public static IdempotencyAcquireResult Acquired() => new(IdempotencyAcquireOutcome.Acquired);
    public static IdempotencyAcquireResult Replay(IdempotencyResponse response) =>
        new(IdempotencyAcquireOutcome.Replay, response ?? throw new ArgumentNullException(nameof(response)));
    public static IdempotencyAcquireResult FingerprintConflict() => new(IdempotencyAcquireOutcome.FingerprintConflict);
    public static IdempotencyAcquireResult InProgress() => new(IdempotencyAcquireOutcome.InProgress);
    public static IdempotencyAcquireResult NonReplayable() => new(IdempotencyAcquireOutcome.NonReplayable);
}

public interface IIdempotencyStore
{
    Task<IdempotencyAcquireResult> AcquireAsync(
        IdempotencyAcquireRequest request,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        FoundationProjectId projectId,
        string operationScope,
        string keyHash,
        string requestFingerprint,
        IdempotencyResponse response,
        DateTimeOffset completedUtc,
        CancellationToken cancellationToken = default);

    Task MarkNonReplayableAsync(
        FoundationProjectId projectId,
        string operationScope,
        string keyHash,
        string requestFingerprint,
        DateTimeOffset markedUtc,
        CancellationToken cancellationToken = default);
}
