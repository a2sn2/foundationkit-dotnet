using FoundationKit.Blazor.Api;
using FoundationKit.Blazor.State;

namespace FoundationKit.Tests;

public sealed class PresentationStateTests
{
    [Fact]
    public void FromResult_maps_success_empty_and_failure_without_hiding_transport_error()
    {
        var ready = PresentationState<string>.FromResult(ApiResult<string>.Success("value"));
        var empty = PresentationState<IReadOnlyList<string>>.FromResult(
            ApiResult<IReadOnlyList<string>>.Success(Array.Empty<string>()),
            items => items.Count == 0);
        var error = new ApiError("demo.error", "failed");
        var failed = PresentationState<string>.FromResult(ApiResult<string>.Failure(error));

        Assert.Equal(PresentationStateKind.Ready, ready.Kind);
        Assert.Equal("value", ready.Value);
        Assert.Equal(PresentationStateKind.Empty, empty.Kind);
        Assert.Equal(PresentationStateKind.Error, failed.Kind);
        Assert.Same(error, failed.Error);
    }

    [Fact]
    public void PagedQueryState_normalizes_bounded_query_intent()
    {
        var state = new PagedQueryState(
            page: 2,
            pageSize: 50,
            filters: [" Name|startswith|Al "],
            sorts: ["Name|desc"]);

        Assert.Equal(2, state.Page);
        Assert.Equal(50, state.PageSize);
        Assert.Equal("Name|startswith|Al", Assert.Single(state.Filters));
        Assert.Equal("Name|desc", Assert.Single(state.Sorts));
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void PagedQueryState_rejects_invalid_paging(int page, int pageSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PagedQueryState(page, pageSize));
    }

    [Fact]
    public void ResourceDisplayDescriptor_normalizes_and_deduplicates_capabilities()
    {
        var descriptor = new ResourceDisplayDescriptor(
            " CustomerDirectory ",
            " /api/customer-directory ",
            ReadOnly: true,
            ["Authorization", "authorization", "Crud"])
            .Normalize();

        Assert.Equal("CustomerDirectory", descriptor.Name);
        Assert.Equal("/api/customer-directory", descriptor.Route);
        Assert.Equal(["Authorization", "Crud"], descriptor.Capabilities);
        Assert.True(descriptor.ReadOnly);
    }
}
