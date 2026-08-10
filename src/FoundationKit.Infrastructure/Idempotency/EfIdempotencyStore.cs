using FoundationKit.Application.Idempotency;
using FoundationKit.Application.Isolation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace FoundationKit.Infrastructure.Idempotency;

internal static class FoundationIdempotencyStates
{
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string NonReplayable = "NonReplayable";
}

internal sealed class FoundationIdempotencyEntry
{
    public required string ProjectId { get; set; }
    public required string OperationScope { get; set; }
    public required string KeyHash { get; set; }
    public required string RequestFingerprint { get; set; }
    public required string State { get; set; }
    public DateTimeOffset AcquiredUtc { get; set; }
    public DateTimeOffset ReplayUntilUtc { get; set; }
    public DateTimeOffset? CompletedUtc { get; set; }
    public int? ResponseStatusCode { get; set; }
    public string? ResponseContentType { get; set; }
    public byte[]? ResponseBody { get; set; }
    public string? ResponseLocation { get; set; }
    public string? ResponseEntityTag { get; set; }
}

public static class FoundationIdempotencyModelBuilderExtensions
{
    public const string DefaultTableName = "FoundationIdempotencyEntries";

    public static ModelBuilder AddFoundationIdempotencyStore(
        this ModelBuilder modelBuilder,
        string tableName = DefaultTableName)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        var normalizedTable = tableName.Trim();
        if (normalizedTable.Length > 96 || normalizedTable.Any(char.IsControl))
            throw new ArgumentException("Idempotency table name is too long or contains control characters.", nameof(tableName));

        Configure(modelBuilder.Entity<FoundationIdempotencyEntry>(), normalizedTable);
        return modelBuilder;
    }

    private static void Configure(
        EntityTypeBuilder<FoundationIdempotencyEntry> entity,
        string tableName)
    {
        entity.ToTable(tableName);
        entity.HasKey(entry => new { entry.ProjectId, entry.OperationScope, entry.KeyHash });
        entity.Property(entry => entry.ProjectId).HasMaxLength(FoundationProjectId.MaximumLength).IsRequired();
        entity.Property(entry => entry.OperationScope).HasMaxLength(IdempotencyAcquireRequest.MaximumOperationScopeLength).IsRequired();
        entity.Property(entry => entry.KeyHash).HasMaxLength(IdempotencyAcquireRequest.Sha256HexLength).IsFixedLength().IsRequired();
        entity.Property(entry => entry.RequestFingerprint).HasMaxLength(IdempotencyAcquireRequest.Sha256HexLength).IsFixedLength().IsRequired();
        entity.Property(entry => entry.State).HasMaxLength(24).IsRequired();
        entity.Property(entry => entry.ResponseContentType).HasMaxLength(IdempotencyResponse.MaximumContentTypeLength);
        entity.Property(entry => entry.ResponseLocation).HasMaxLength(IdempotencyResponse.MaximumLocationLength);
        entity.Property(entry => entry.ResponseEntityTag).HasMaxLength(IdempotencyResponse.MaximumEntityTagLength);
        entity.HasIndex(entry => entry.ReplayUntilUtc);
    }
}

public static class FoundationIdempotencyServiceCollectionExtensions
{
    public static IServiceCollection AddFoundationEfIdempotencyStore<TDbContext>(
        this IServiceCollection services)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IIdempotencyStore, EfIdempotencyStore<TDbContext>>();
        return services;
    }
}

public sealed class EfIdempotencyStore<TDbContext>(TDbContext dbContext) : IIdempotencyStore
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<IdempotencyAcquireResult> AcquireAsync(
        IdempotencyAcquireRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalized = request.Normalize();
        var entry = new FoundationIdempotencyEntry
        {
            ProjectId = normalized.ProjectId.Value,
            OperationScope = normalized.OperationScope,
            KeyHash = normalized.KeyHash,
            RequestFingerprint = normalized.RequestFingerprint,
            State = FoundationIdempotencyStates.InProgress,
            AcquiredUtc = normalized.AcquiredUtc,
            ReplayUntilUtc = normalized.ReplayUntilUtc
        };

        _dbContext.Set<FoundationIdempotencyEntry>().Add(entry);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return IdempotencyAcquireResult.Acquired();
        }
        catch (DbUpdateException)
        {
            _dbContext.Entry(entry).State = EntityState.Detached;
            var existing = await FindAsync(
                normalized.ProjectId,
                normalized.OperationScope,
                normalized.KeyHash,
                tracking: false,
                cancellationToken).ConfigureAwait(false);
            if (existing is null)
                throw;
            return Classify(existing, normalized.RequestFingerprint, normalized.AcquiredUtc);
        }
    }

    public async Task CompleteAsync(
        FoundationProjectId projectId,
        string operationScope,
        string keyHash,
        string requestFingerprint,
        IdempotencyResponse response,
        DateTimeOffset completedUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projectId);
        ArgumentNullException.ThrowIfNull(response);
        var scope = IdempotencyAcquireRequest.NormalizeScope(operationScope);
        var normalizedKeyHash = IdempotencyAcquireRequest.NormalizeSha256(keyHash, nameof(keyHash));
        var normalizedFingerprint = IdempotencyAcquireRequest.NormalizeSha256(requestFingerprint, nameof(requestFingerprint));
        var normalizedResponse = response.Normalize(int.MaxValue);
        var entry = await FindAsync(projectId, scope, normalizedKeyHash, tracking: true, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The acquired idempotency entry no longer exists.");

        EnsureOwner(entry, normalizedFingerprint);
        if (!string.Equals(entry.State, FoundationIdempotencyStates.InProgress, StringComparison.Ordinal))
            throw new InvalidOperationException("Only an in-progress idempotency entry can be completed.");

        entry.State = FoundationIdempotencyStates.Completed;
        entry.CompletedUtc = completedUtc;
        entry.ResponseStatusCode = normalizedResponse.StatusCode;
        entry.ResponseContentType = normalizedResponse.ContentType;
        entry.ResponseBody = normalizedResponse.Body;
        entry.ResponseLocation = normalizedResponse.Location;
        entry.ResponseEntityTag = normalizedResponse.EntityTag;
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkNonReplayableAsync(
        FoundationProjectId projectId,
        string operationScope,
        string keyHash,
        string requestFingerprint,
        DateTimeOffset markedUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projectId);
        var scope = IdempotencyAcquireRequest.NormalizeScope(operationScope);
        var normalizedKeyHash = IdempotencyAcquireRequest.NormalizeSha256(keyHash, nameof(keyHash));
        var normalizedFingerprint = IdempotencyAcquireRequest.NormalizeSha256(requestFingerprint, nameof(requestFingerprint));
        var entry = await FindAsync(projectId, scope, normalizedKeyHash, tracking: true, cancellationToken).ConfigureAwait(false);
        if (entry is null)
            return;

        EnsureOwner(entry, normalizedFingerprint);
        if (string.Equals(entry.State, FoundationIdempotencyStates.InProgress, StringComparison.Ordinal))
        {
            entry.State = FoundationIdempotencyStates.NonReplayable;
            entry.CompletedUtc = markedUtc;
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<FoundationIdempotencyEntry?> FindAsync(
        FoundationProjectId projectId,
        string operationScope,
        string keyHash,
        bool tracking,
        CancellationToken cancellationToken)
    {
        IQueryable<FoundationIdempotencyEntry> query = _dbContext.Set<FoundationIdempotencyEntry>();
        if (!tracking)
            query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(entry =>
                entry.ProjectId == projectId.Value &&
                entry.OperationScope == operationScope &&
                entry.KeyHash == keyHash,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static IdempotencyAcquireResult Classify(
        FoundationIdempotencyEntry existing,
        string requestFingerprint,
        DateTimeOffset now)
    {
        if (!string.Equals(existing.RequestFingerprint, requestFingerprint, StringComparison.Ordinal))
            return IdempotencyAcquireResult.FingerprintConflict();

        if (string.Equals(existing.State, FoundationIdempotencyStates.InProgress, StringComparison.Ordinal))
            return IdempotencyAcquireResult.InProgress();
        if (string.Equals(existing.State, FoundationIdempotencyStates.NonReplayable, StringComparison.Ordinal))
            return IdempotencyAcquireResult.NonReplayable();
        if (string.Equals(existing.State, FoundationIdempotencyStates.Completed, StringComparison.Ordinal))
        {
            return now <= existing.ReplayUntilUtc
                ? IdempotencyAcquireResult.Replay(ToResponse(existing))
                : IdempotencyAcquireResult.NonReplayable();
        }

        throw new InvalidOperationException("Unknown idempotency entry state.");
    }

    private static IdempotencyResponse ToResponse(FoundationIdempotencyEntry entry)
    {
        if (entry.ResponseStatusCode is null)
            throw new InvalidOperationException("Completed idempotency entry has no response status code.");
        return new IdempotencyResponse(
            entry.ResponseStatusCode.Value,
            entry.ResponseContentType,
            entry.ResponseBody?.ToArray() ?? [],
            entry.ResponseLocation,
            entry.ResponseEntityTag);
    }

    private static void EnsureOwner(FoundationIdempotencyEntry entry, string requestFingerprint)
    {
        if (!string.Equals(entry.RequestFingerprint, requestFingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException("Idempotency fingerprint does not own this entry.");
    }
}
