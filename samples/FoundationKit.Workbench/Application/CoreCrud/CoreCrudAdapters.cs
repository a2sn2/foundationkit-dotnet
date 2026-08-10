using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Crud;
using FoundationKit.Application.Results;
using FoundationKit.Application.Validation;
using FoundationKit.Workbench.Contracts;
using FoundationKit.Workbench.Domain;

namespace FoundationKit.Workbench.Application.CoreCrud;

public sealed class CoreCrudMapper(IClock clock)
    : ICrudMapper<CoreCrudRecord, Guid, CoreCrudCreateRequest, CoreCrudUpdateRequest, CoreCrudResponse>
{
    public CoreCrudRecord Create(CoreCrudCreateRequest request) =>
        new(Guid.NewGuid(), request.Name, clock.UtcNow);

    public void ApplyUpdate(CoreCrudRecord entity, CoreCrudUpdateRequest request)
    {
        entity.Rename(request.Name);
        entity.AdvanceVersion();
    }

    public CoreCrudResponse ToReadModel(CoreCrudRecord entity) =>
        new(entity.Id, entity.Name, entity.Version, entity.CreatedUtc);
}

public sealed class CoreCrudValidator :
    IValidator<CoreCrudCreateRequest>,
    IValidator<CoreCrudUpdateRequest>
{
    public ValueTask<IReadOnlyList<ValidationFailure>> ValidateAsync(
        CoreCrudCreateRequest instance,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValidateName(instance.Name));

    public ValueTask<IReadOnlyList<ValidationFailure>> ValidateAsync(
        CoreCrudUpdateRequest instance,
        CancellationToken cancellationToken = default)
    {
        var failures = ValidateName(instance.Name).ToList();
        if (instance.ExpectedVersion < 1)
        {
            failures.Add(new ValidationFailure(
                nameof(instance.ExpectedVersion),
                "CoreCrud.Version.Invalid",
                "ExpectedVersion must be at least 1."));
        }

        return ValueTask.FromResult<IReadOnlyList<ValidationFailure>>(failures);
    }

    private static IReadOnlyList<ValidationFailure> ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return [new ValidationFailure(
                "Name",
                "CoreCrud.Name.Required",
                "Name is required.")];
        }

        if (name.Trim().Length > 120)
        {
            return [new ValidationFailure(
                "Name",
                "CoreCrud.Name.TooLong",
                "Name cannot exceed 120 characters.")];
        }

        return Array.Empty<ValidationFailure>();
    }
}

public sealed class CoreCrudAuthorizationPolicy : ICrudAuthorizationPolicy<CoreCrudRecord, Guid>
{
    public ValueTask<Result> AuthorizeAsync(
        CrudAuthorizationContext<CoreCrudRecord, Guid> context,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Result.Success());
}

public sealed class CoreCrudConcurrencyPolicy : ICrudConcurrencyPolicy<CoreCrudRecord, CoreCrudUpdateRequest>
{
    public Result Validate(CoreCrudRecord entity, CoreCrudUpdateRequest request) =>
        entity.Version == request.ExpectedVersion
            ? Result.Success()
            : Result.Failure(Error.Conflict(
                "CoreCrud.Version.Conflict",
                "The record changed after it was loaded. Reload it and retry."));
}

public sealed class CoreCrudManager : ICrudManager<CoreCrudRecord, Guid, CoreCrudCreateRequest, CoreCrudUpdateRequest>
{
    public ValueTask<Result> BeforeCreateAsync(
        CoreCrudRecord entity,
        CoreCrudCreateRequest request,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(RejectReservedName(entity.Name));

    public ValueTask<Result> BeforeUpdateAsync(
        CoreCrudRecord entity,
        CoreCrudUpdateRequest request,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(RejectReservedName(request.Name));

    private static Result RejectReservedName(string name) =>
        string.Equals(name.Trim(), "foundation", StringComparison.OrdinalIgnoreCase)
            ? Result.Failure(Error.BusinessRule(
                "CoreCrud.Name.Reserved",
                "The reference module reserves the name 'foundation' to prove manager-level business overrides."))
            : Result.Success();
}
