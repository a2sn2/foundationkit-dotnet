using FoundationKit.Application.Isolation;
using FoundationKit.Application.Results;
using FoundationKit.Application.Validation;
using FoundationKit.Domain.Primitives;

namespace FoundationKit.Application.Crud;

public sealed record CrudItemResult<TId, TRead>(TId Id, TRead Item)
    where TId : notnull;

public sealed record CrudConcurrencyPrecondition(string Token)
{
    public CrudConcurrencyPrecondition Normalize()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Token);
        var normalized = Token.Trim();
        if (normalized.Length > 256 || normalized.Any(char.IsControl))
            throw new ArgumentException("Concurrency token is too long or contains control characters.", nameof(Token));
        return new CrudConcurrencyPrecondition(normalized);
    }
}

public sealed record CrudAuthorizationContext<TEntity, TId>(
    CrudOperation Operation,
    bool HasId,
    TId? Id,
    TEntity? Entity,
    object? Request)
    where TEntity : Entity<TId>
    where TId : notnull;

public interface ICrudMapper<TEntity, TId, in TCreate, in TUpdate, out TRead>
    where TEntity : Entity<TId>
    where TId : notnull
{
    TEntity Create(TCreate request);

    void ApplyUpdate(TEntity entity, TUpdate request);

    TRead ToReadModel(TEntity entity);
}

public interface ICrudAuthorizationPolicy<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    ValueTask<Result> AuthorizeAsync(
        CrudAuthorizationContext<TEntity, TId> context,
        CancellationToken cancellationToken = default);
}

public sealed class AllowAllCrudAuthorizationPolicy<TEntity, TId> : ICrudAuthorizationPolicy<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    public ValueTask<Result> AuthorizeAsync(
        CrudAuthorizationContext<TEntity, TId> context,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Result.Success());
}

public sealed class DenyAllCrudAuthorizationPolicy<TEntity, TId> : ICrudAuthorizationPolicy<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    public ValueTask<Result> AuthorizeAsync(
        CrudAuthorizationContext<TEntity, TId> context,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Result.Failure(Error.Forbidden(
            "Foundation.Crud.AuthorizationPolicyMissing",
            "This module requires an explicit CRUD authorization policy.")));
}

public interface ICrudConcurrencyPolicy<TEntity, in TUpdate>
{
    Result Validate(TEntity entity, TUpdate request);

    Result Validate(
        TEntity entity,
        TUpdate request,
        CrudConcurrencyPrecondition? precondition) => Validate(entity, request);
}

public sealed class NoOpCrudConcurrencyPolicy<TEntity, TUpdate> : ICrudConcurrencyPolicy<TEntity, TUpdate>
{
    public Result Validate(TEntity entity, TUpdate request) => Result.Success();
}

public interface ICrudManager<TEntity, TId, in TCreate, in TUpdate>
    where TEntity : Entity<TId>
    where TId : notnull
{
    ValueTask<Result> BeforeCreateAsync(
        TEntity entity,
        TCreate request,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Result.Success());

    ValueTask<Result> BeforeUpdateAsync(
        TEntity entity,
        TUpdate request,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Result.Success());

    ValueTask<Result> BeforeDeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Result.Success());
}

public sealed class DefaultCrudManager<TEntity, TId, TCreate, TUpdate> : ICrudManager<TEntity, TId, TCreate, TUpdate>
    where TEntity : Entity<TId>
    where TId : notnull;

public sealed record CrudOperationEvent<TEntity, TId>(
    FoundationProjectId ProjectId,
    string ModuleName,
    CrudOperation Operation,
    TId Id,
    TEntity Entity)
    where TEntity : Entity<TId>
    where TId : notnull;

public interface ICrudOperationObserver<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    ValueTask OnSucceededAsync(
        CrudOperationEvent<TEntity, TId> operation,
        CancellationToken cancellationToken = default);
}

public sealed class NoOpCrudValidator<T> : IValidator<T>
{
    private static readonly IReadOnlyList<ValidationFailure> NoFailures = Array.Empty<ValidationFailure>();

    public ValueTask<IReadOnlyList<ValidationFailure>> ValidateAsync(
        T instance,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(NoFailures);
}

public sealed class FoundationConcurrencyException : Exception
{
    public FoundationConcurrencyException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
