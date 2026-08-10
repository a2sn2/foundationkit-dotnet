using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Crud;
using FoundationKit.Application.Results;
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
