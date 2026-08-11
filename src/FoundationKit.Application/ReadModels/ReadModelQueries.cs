using System.Linq.Expressions;
using FoundationKit.Application.Crud;
using FoundationKit.Application.Pagination;
using FoundationKit.Application.Persistence;
using FoundationKit.Application.Results;

namespace FoundationKit.Application.ReadModels;

public interface IReadModelMapper<in TReadModel, out TResponse>
{
    TResponse Map(TReadModel model);
}

public interface IReadModelQueryPolicy<TReadModel>
{
    Result<CrudQueryPlan<TReadModel>> Build(CrudListRequest request);
}

public sealed class ConfiguredReadModelQueryPolicy<TReadModel> : IReadModelQueryPolicy<TReadModel>
{
    private readonly ConfiguredCrudQueryPolicy<TReadModel, Guid> _inner;

    public ConfiguredReadModelQueryPolicy(IEnumerable<CrudStringQueryField<TReadModel>> fields) =>
        _inner = new ConfiguredCrudQueryPolicy<TReadModel, Guid>(fields);

    public Result<CrudQueryPlan<TReadModel>> Build(CrudListRequest request) =>
        _inner.Build(request);
}

public sealed class ReadModelQueryService<TReadModel, TResponse>(
    IReadModelStore<TReadModel> store,
    IReadModelQueryPolicy<TReadModel> queryPolicy,
    IReadModelMapper<TReadModel, TResponse> mapper)
    where TReadModel : class
{
    private readonly IReadModelStore<TReadModel> _store = store;
    private readonly IReadModelQueryPolicy<TReadModel> _queryPolicy = queryPolicy;
    private readonly IReadModelMapper<TReadModel, TResponse> _mapper = mapper;

    public async Task<Result<PagedResult<TResponse>>> ListAsync(
        CrudListRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var planned = _queryPolicy.Build(request);
        if (planned.IsFailure)
            return Result<PagedResult<TResponse>>.Failure(planned.Error!);

        var plan = planned.Value!;
        var totalCount = await _store.CountAsync(
            new ReadModelSpecification(plan.Criteria),
            cancellationToken).ConfigureAwait(false);

        var pageSpecification = new ReadModelSpecification(
            plan,
            request.Page.Skip,
            request.Page.PageSize);
        var rows = await _store.ListAsync(pageSpecification, cancellationToken).ConfigureAwait(false);
        var responses = rows.Select(_mapper.Map).ToArray();

        return Result<PagedResult<TResponse>>.Success(
            new PagedResult<TResponse>(
                responses,
                request.Page.Page,
                request.Page.PageSize,
                totalCount));
    }

    private sealed class ReadModelSpecification : Specification<TReadModel>
    {
        public ReadModelSpecification(Expression<Func<TReadModel, bool>>? criteria)
            : base(criteria) => UseNoTracking();

        public ReadModelSpecification(
            CrudQueryPlan<TReadModel> plan,
            int skip,
            int take)
            : base(plan.Criteria)
        {
            if (plan.OrderBy is not null)
            {
                if (plan.SortDirection == CrudSortDirection.Descending)
                    ApplyOrderByDescending(plan.OrderBy);
                else
                    ApplyOrderBy(plan.OrderBy);
            }

            ApplyPaging(skip, take);
            UseNoTracking();
        }
    }
}
