using System.Linq.Expressions;
using FoundationKit.Application.Results;

namespace FoundationKit.Application.Crud;

public sealed record CrudStringQueryField<TEntity>(
    string Name,
    Expression<Func<TEntity, string?>> Selector,
    CrudStringFilterMode FilterMode = CrudStringFilterMode.None,
    bool Sortable = false);

public sealed class ConfiguredCrudQueryPolicy<TEntity, TId> : ICrudQueryPolicy<TEntity, TId>
    where TId : notnull
{
    private const int MaximumConfiguredFields = 64;
    private readonly Dictionary<string, CrudStringQueryField<TEntity>> _fields;

    public ConfiguredCrudQueryPolicy(IEnumerable<CrudStringQueryField<TEntity>> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        var configured = fields.ToArray();
        if (configured.Length > MaximumConfiguredFields)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fields),
                $"At most {MaximumConfiguredFields} configured query fields are supported.");
        }

        var dictionary = new Dictionary<string, CrudStringQueryField<TEntity>>(
            configured.Length,
            StringComparer.OrdinalIgnoreCase);
        foreach (var field in configured)
        {
            ArgumentNullException.ThrowIfNull(field);
            ArgumentException.ThrowIfNullOrWhiteSpace(field.Name);
            ArgumentNullException.ThrowIfNull(field.Selector);
            if (!Enum.IsDefined(field.FilterMode))
                throw new ArgumentOutOfRangeException(nameof(fields), "A configured filter mode is invalid.");
            if (!dictionary.TryAdd(field.Name.Trim(), field))
                throw new ArgumentException($"Duplicate configured query field '{field.Name}'.", nameof(fields));
        }

        _fields = dictionary;
    }

    public Result<CrudQueryPlan<TEntity>> Build(CrudListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Sorts.Count > 1)
        {
            return Failure(
                "Foundation.Crud.Query.MultipleSortsUnsupported",
                "The current query plan supports at most one sort field.");
        }

        var parameter = Expression.Parameter(typeof(TEntity), "entity");
        Expression? criteriaBody = null;
        foreach (var filter in request.Filters)
        {
            if (!_fields.TryGetValue(filter.Field, out var field))
            {
                return Failure(
                    "Foundation.Crud.Query.FilterFieldUnsupported",
                    $"Filter field '{filter.Field}' is not enabled for this module.");
            }

            var predicate = BuildFilterExpression(parameter, field, filter);
            if (predicate.IsFailure)
                return Result<CrudQueryPlan<TEntity>>.Failure(predicate.Error!);

            criteriaBody = criteriaBody is null
                ? predicate.Value
                : Expression.AndAlso(criteriaBody, predicate.Value!);
        }

        Expression<Func<TEntity, bool>>? criteria = criteriaBody is null
            ? null
            : Expression.Lambda<Func<TEntity, bool>>(criteriaBody, parameter);

        Expression<Func<TEntity, object>>? orderBy = null;
        var sortDirection = CrudSortDirection.Ascending;
        if (request.Sorts.Count == 1)
        {
            var sort = request.Sorts[0];
            if (!_fields.TryGetValue(sort.Field, out var field) || !field.Sortable)
            {
                return Failure(
                    "Foundation.Crud.Query.SortFieldUnsupported",
                    $"Sort field '{sort.Field}' is not enabled for this module.");
            }

            var sortBody = ReplaceParameter(field.Selector, parameter);
            orderBy = Expression.Lambda<Func<TEntity, object>>(
                Expression.Convert(sortBody, typeof(object)),
                parameter);
            sortDirection = sort.Direction;
        }

        return Result<CrudQueryPlan<TEntity>>.Success(
            new CrudQueryPlan<TEntity>(criteria, orderBy, sortDirection));
    }

    private static Result<Expression> BuildFilterExpression(
        ParameterExpression parameter,
        CrudStringQueryField<TEntity> field,
        CrudFilter filter)
    {
        var member = ReplaceParameter(field.Selector, parameter);
        var value = Expression.Constant(filter.Value, typeof(string));

        return (field.FilterMode, filter.Operator) switch
        {
            (CrudStringFilterMode.Exact, CrudFilterOperator.Equal) =>
                Result<Expression>.Success(Expression.Equal(member, value)),
            (CrudStringFilterMode.Prefix, CrudFilterOperator.Equal) =>
                Result<Expression>.Success(Expression.Equal(member, value)),
            (CrudStringFilterMode.Prefix, CrudFilterOperator.StartsWith) =>
                Result<Expression>.Success(BuildStartsWith(member, value)),
            _ => Result<Expression>.Failure(Error.Validation(
                "Foundation.Crud.Query.FilterOperatorUnsupported",
                $"Filter operator '{filter.Operator}' is not enabled for field '{filter.Field}'."))
        };
    }

    private static BinaryExpression BuildStartsWith(Expression member, Expression value)
    {
        var notNull = Expression.NotEqual(member, Expression.Constant(null, typeof(string)));
        var startsWith = Expression.Call(
            member,
            nameof(string.StartsWith),
            Type.EmptyTypes,
            value);
        return Expression.AndAlso(notNull, startsWith);
    }

    private static Expression ReplaceParameter(
        Expression<Func<TEntity, string?>> selector,
        ParameterExpression parameter) =>
        new ParameterReplacementVisitor(selector.Parameters[0], parameter)
            .Visit(selector.Body)
        ?? throw new InvalidOperationException("Configured query selector could not be composed.");

    private static Result<CrudQueryPlan<TEntity>> Failure(string code, string message) =>
        Result<CrudQueryPlan<TEntity>>.Failure(Error.Validation(code, message));

    private sealed class ParameterReplacementVisitor(
        ParameterExpression source,
        ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == source ? target : base.VisitParameter(node);
    }
}
