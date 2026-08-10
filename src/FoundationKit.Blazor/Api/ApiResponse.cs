using System.Net.Http.Headers;

namespace FoundationKit.Blazor.Api;

public sealed record ApiResponseMetadata(
    string? EntityTag,
    Uri? Location,
    string? CorrelationId)
{
    public static ApiResponseMetadata Empty { get; } = new(null, null, null);

    public static ApiResponseMetadata FromHttpResponse(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return new ApiResponseMetadata(
            EntityTag: response.Headers.ETag?.ToString(),
            Location: response.Headers.Location,
            CorrelationId: ReadCorrelationId(response.Headers));
    }

    private static string? ReadCorrelationId(HttpResponseHeaders headers) =>
        headers.TryGetValues("X-Correlation-ID", out var values)
            ? values.FirstOrDefault()
            : null;
}

public sealed record ApiResponse(ApiResult Result, ApiResponseMetadata Metadata)
{
    public ApiResponse(ApiResult result)
        : this(result, ApiResponseMetadata.Empty)
    {
    }
}

public sealed record ApiResponse<T>(ApiResult<T> Result, ApiResponseMetadata Metadata)
{
    public ApiResponse(ApiResult<T> result)
        : this(result, ApiResponseMetadata.Empty)
    {
    }
}
