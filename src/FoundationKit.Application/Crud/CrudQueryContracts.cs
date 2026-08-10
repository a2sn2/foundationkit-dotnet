using System.Linq.Expressions;
using FoundationKit.Application.Pagination;
using FoundationKit.Application.Results;

namespace FoundationKit.Application.Crud;

public sealed record CrudFilter(
    string Field,
    CrudFilterOperator Operator,
    string Value);

public sealed record CrudSort(
    string Field,
    CrudSortDirection Direction);

public sealed record CrudListRequest(
    PageRequest Page,
    IReadOnlyList<CrudFilter> Filters,
    IReadOnlyList<CrudSort> Sorts)
{
    public static CrudListRequest FromPage(PageRequest page) =>
        new(page, Array.Empty<CrudFilter>(), Array.Empty<CrudSort>());
}

public sealed record CrudQueryPlan<TEntity>(
    Expression<Func<TEntity, bool>>? Criteria,
    Expression<Func<TEntity, object>>? OrderBy,
    CrudSortDirection SortDirection = CrudSortDirection.Ascending);

public interface ICrudQueryPolicy<TEntity, TId>
{
    Result<CrudQueryPlan<TEntity>> Build(CrudListRequest request);
}

public sealed class DefaultCrudQueryPolicy<TEntity, TId> : ICrudQueryPolicy<TEntity, TId>
    where TId : notnull
{
    public Result<CrudQueryPlan<TEntity>> Build(CrudListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Filters.Count > 0 || request.Sorts.Count > 0)
        {
            return Result<CrudQueryPlan<TEntity>>.Failure(Error.Validation(
                "Foundation.Crud.Query.Unsupported",
                "This module does not define filter or sort fields."));
        }

        return Result<CrudQueryPlan<TEntity>>.Success(
            new CrudQueryPlan<TEntity>(null, null));
    }
}
