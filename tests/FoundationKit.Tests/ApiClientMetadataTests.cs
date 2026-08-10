using System.Net;
using System.Text;
using FoundationKit.Blazor.Api;

namespace FoundationKit.Tests;

public sealed class ApiClientMetadataTests
{
    [Fact]
    public async Task Metadata_aware_send_preserves_etag_location_and_correlation()
    {
        using var httpClient = new HttpClient(new StubHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("{\"name\":\"Alpha\"}", Encoding.UTF8, "application/json")
            };
            response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"7\"");
            response.Headers.Location = new Uri("https://example.test/api/items/1");
            response.Headers.TryAddWithoutValidation("X-Correlation-ID", "corr-123");
            return response;
        }));
        var client = new TestApiClient(httpClient);

        var response = await client.ReadWithMetadataAsync();

        Assert.True(response.Result.IsSuccess);
        Assert.Equal("Alpha", response.Result.Value.Name);
        Assert.Equal(HttpStatusCode.Created, response.Result.StatusCode);
        Assert.Equal("\"7\"", response.Metadata.EntityTag);
        Assert.Equal(new Uri("https://example.test/api/items/1"), response.Metadata.Location);
        Assert.Equal("corr-123", response.Metadata.CorrelationId);
    }

    [Fact]
    public async Task Failure_still_exposes_transport_metadata()
    {
        using var httpClient = new HttpClient(new StubHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.PreconditionFailed)
            {
                Content = new StringContent(
                    "{\"title\":\"Precondition Failed\",\"status\":412,\"detail\":\"stale\",\"extensions\":{\"code\":\"Version.Stale\"}}",
                    Encoding.UTF8,
                    "application/problem+json")
            };
            response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"8\"");
            response.Headers.TryAddWithoutValidation("X-Correlation-ID", "corr-failure");
            return response;
        }));
        var client = new TestApiClient(httpClient);

        var response = await client.ReadWithMetadataAsync();

        Assert.True(response.Result.IsFailure);
        Assert.Equal(HttpStatusCode.PreconditionFailed, response.Result.StatusCode);
        Assert.Equal("\"8\"", response.Metadata.EntityTag);
        Assert.Equal("corr-failure", response.Metadata.CorrelationId);
    }

    [Fact]
    public async Task Legacy_send_keeps_existing_result_only_contract()
    {
        using var httpClient = new HttpClient(new StubHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"name\":\"Legacy\"}", Encoding.UTF8, "application/json")
            };
            response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"3\"");
            return response;
        }));
        var client = new TestApiClient(httpClient);

        var result = await client.ReadLegacyAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("Legacy", result.Value.Name);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task Network_failure_returns_empty_metadata()
    {
        using var httpClient = new HttpClient(new ThrowingHandler());
        var client = new TestApiClient(httpClient);

        var response = await client.ReadWithMetadataAsync();

        Assert.True(response.Result.IsFailure);
        Assert.Equal(ApiResponseMetadata.Empty, response.Metadata);
    }

    private sealed record Payload(string Name);

    private sealed class TestApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
    {
        public Task<ApiResponse<Payload>> ReadWithMetadataAsync() =>
            SendWithMetadataAsync<Payload>(new HttpRequestMessage(HttpMethod.Get, "https://example.test/api/items/1"));

        public Task<ApiResult<Payload>> ReadLegacyAsync() =>
            SendAsync<Payload>(new HttpRequestMessage(HttpMethod.Get, "https://example.test/api/items/1"));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("offline");
    }
}
