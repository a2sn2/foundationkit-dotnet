using System.Globalization;
using System.Linq.Expressions;
using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Crud;
using FoundationKit.Application.Results;
using FoundationKit.WebApi.Api;
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
        Result.Failure(Error.PreconditionRequired(
            "CoreCrud.Version.Required",
            "An If-Match concurrency token is required."));

    public Result Validate(
        CoreCrudRecord entity,
        CoreCrudUpdateRequest request,
        CrudConcurrencyPrecondition? precondition)
    {
        if (precondition is null)
            return Validate(entity, request);

        var token = precondition.Token.Trim();
        if (token.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
            return Failed();

        if (token.Length >= 2 && token[0] == '"' && token[^1] == '"')
            token = token[1..^1];

        return int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out var expectedVersion) &&
               expectedVersion == entity.Version
            ? Result.Success()
            : Failed();
    }

    private static Result Failed() => Result.Failure(Error.PreconditionFailed(
        "CoreCrud.Version.PreconditionFailed",
        "The record changed after it was loaded. Reload it and retry with the current ETag."));
}

public sealed class CoreCrudEntityTagProvider : IFoundationApiEntityTagProvider<CoreCrudResponse>
{
    public string GetEntityTag(CoreCrudResponse response) => $"\"{response.Version}\"";
}

public sealed class CoreCrudQueryPolicy : ICrudQueryPolicy<CoreCrudRecord, Guid>
{
    public Result<CrudQueryPlan<CoreCrudRecord>> Build(CrudListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Filters.Count > 1 || request.Sorts.Count > 1)
        {
            return Failure(
                "CoreCrud.Query.TooComplex",
                "The Workbench reference allows at most one filter and one sort expression.");
        }

        Expression<Func<CoreCrudRecord, bool>>? criteria = null;
        if (request.Filters.Count == 1)
        {
            var filterResult = BuildFilter(request.Filters[0]);
            if (filterResult.IsFailure)
                return Result<CrudQueryPlan<CoreCrudRecord>>.Failure(filterResult.Error);
            criteria = filterResult.Value;
        }

        Expression<Func<CoreCrudRecord, object>>? order = null;
        var direction = CrudSortDirection.Ascending;
        if (request.Sorts.Count == 1)
        {
            var sort = request.Sorts[0];
            direction = sort.Direction;
            order = sort.Field.ToLowerInvariant() switch
            {
                "name" => record => record.Name,
                "version" => record => record.Version,
                "createdutc" => record => record.CreatedUtc,
                _ => null
            };

            if (order is null)
                return Failure("CoreCrud.Query.SortFieldUnsupported", "The requested sort field is not supported.");
        }

        return Result<CrudQueryPlan<CoreCrudRecord>>.Success(new(criteria, order, direction));
    }

    private static Result<Expression<Func<CoreCrudRecord, bool>>> BuildFilter(CrudFilter filter)
    {
        if (string.Equals(filter.Field, "name", StringComparison.OrdinalIgnoreCase))
        {
            Expression<Func<CoreCrudRecord, bool>>? expression = filter.Operator switch
            {
                CrudFilterOperator.Equal => record => record.Name == filter.Value,
                CrudFilterOperator.NotEqual => record => record.Name != filter.Value,
                CrudFilterOperator.Contains => record => record.Name.Contains(filter.Value),
                CrudFilterOperator.StartsWith => record => record.Name.StartsWith(filter.Value),
                CrudFilterOperator.EndsWith => record => record.Name.EndsWith(filter.Value),
                _ => null
            };
            return expression is null
                ? FilterFailure("CoreCrud.Query.NameOperatorUnsupported", "The requested name filter operator is not supported.")
                : Result<Expression<Func<CoreCrudRecord, bool>>>.Success(expression);
        }

        if (string.Equals(filter.Field, "version", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(filter.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var version) || version < 1)
                return FilterFailure("CoreCrud.Query.VersionInvalid", "Version filter values must be positive integers.");

            Expression<Func<CoreCrudRecord, bool>>? expression = filter.Operator switch
            {
                CrudFilterOperator.Equal => record => record.Version == version,
                CrudFilterOperator.NotEqual => record => record.Version != version,
                CrudFilterOperator.GreaterThan => record => record.Version > version,
                CrudFilterOperator.GreaterThanOrEqual => record => record.Version >= version,
                CrudFilterOperator.LessThan => record => record.Version < version,
                CrudFilterOperator.LessThanOrEqual => record => record.Version <= version,
                _ => null
            };
            return expression is null
                ? FilterFailure("CoreCrud.Query.VersionOperatorUnsupported", "The requested version filter operator is not supported.")
                : Result<Expression<Func<CoreCrudRecord, bool>>>.Success(expression);
        }

        return FilterFailure("CoreCrud.Query.FilterFieldUnsupported", "The requested filter field is not supported.");
    }

    private static Result<CrudQueryPlan<CoreCrudRecord>> Failure(string code, string description) =>
        Result<CrudQueryPlan<CoreCrudRecord>>.Failure(Error.Validation(code, description));

    private static Result<Expression<Func<CoreCrudRecord, bool>>> FilterFailure(string code, string description) =>
        Result<Expression<Func<CoreCrudRecord, bool>>>.Failure(Error.Validation(code, description));
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
