using FoundationKit.Application.Crud;
using FoundationKit.Application.Pagination;

namespace FoundationKit.Tests;

public sealed class ConfiguredCrudQueryPolicyTests
{
    [Fact]
    public void Prefix_filter_and_sort_compose_expression_plan()
    {
        var policy = new ConfiguredCrudQueryPolicy<QueryEntity, Guid>(
        [
            new CrudStringQueryField<QueryEntity>(
                "Name",
                entity => entity.Name,
                CrudStringFilterMode.Prefix,
                Sortable: true)
        ]);
        var request = new CrudListRequest(
            new PageRequest(1, 20),
            [new CrudFilter("Name", CrudFilterOperator.StartsWith, "Al")],
            [new CrudSort("Name", CrudSortDirection.Descending)]);

        var result = policy.Build(request);

        Assert.True(result.IsSuccess);
        var plan = Assert.IsType<CrudQueryPlan<QueryEntity>>(result.Value);
        var criteria = Assert.IsAssignableFrom<Delegate>(plan.Criteria!.Compile());
        Assert.True((bool)criteria.DynamicInvoke(new QueryEntity("Alpha"))!);
        Assert.False((bool)criteria.DynamicInvoke(new QueryEntity("Beta"))!);
        Assert.NotNull(plan.OrderBy);
        Assert.Equal(CrudSortDirection.Descending, plan.SortDirection);
    }

    [Fact]
    public void Undeclared_filter_field_fails_closed()
    {
        var policy = new ConfiguredCrudQueryPolicy<QueryEntity, Guid>(
        [new CrudStringQueryField<QueryEntity>("Name", entity => entity.Name, CrudStringFilterMode.Exact)]);
        var request = new CrudListRequest(
            new PageRequest(1, 20),
            [new CrudFilter("Note", CrudFilterOperator.Equal, "x")],
            []);

        var result = policy.Build(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Foundation.Crud.Query.FilterFieldUnsupported", result.Error!.Code);
    }

    [Fact]
    public void Unsupported_operator_fails_closed()
    {
        var policy = new ConfiguredCrudQueryPolicy<QueryEntity, Guid>(
        [new CrudStringQueryField<QueryEntity>("Name", entity => entity.Name, CrudStringFilterMode.Prefix)]);
        var request = new CrudListRequest(
            new PageRequest(1, 20),
            [new CrudFilter("Name", CrudFilterOperator.Contains, "pha")],
            []);

        var result = policy.Build(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Foundation.Crud.Query.FilterOperatorUnsupported", result.Error!.Code);
    }

    [Fact]
    public void More_than_one_sort_fails_until_query_plan_supports_then_by()
    {
        var policy = new ConfiguredCrudQueryPolicy<QueryEntity, Guid>(
        [
            new CrudStringQueryField<QueryEntity>("Name", entity => entity.Name, Sortable: true),
            new CrudStringQueryField<QueryEntity>("Note", entity => entity.Note, Sortable: true)
        ]);
        var request = new CrudListRequest(
            new PageRequest(1, 20),
            [],
            [
                new CrudSort("Name", CrudSortDirection.Ascending),
                new CrudSort("Note", CrudSortDirection.Ascending)
            ]);

        var result = policy.Build(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Foundation.Crud.Query.MultipleSortsUnsupported", result.Error!.Code);
    }

    private sealed record QueryEntity(string Name, string? Note = null);
}
