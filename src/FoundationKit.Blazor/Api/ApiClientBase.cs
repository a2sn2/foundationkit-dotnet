using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FoundationKit.Blazor.Api;

public abstract class ApiClientBase(HttpClient httpClient)
{
    protected HttpClient HttpClient { get; } = httpClient;

    protected static JsonSerializerOptions JsonOptions { get; } =
        new(JsonSerializerDefaults.Web);

    protected static string FormatQueryValue<T>(T value) =>
        value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty
            : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

    protected async Task<ApiResult> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default) =>
        (await SendWithMetadataAsync(request, cancellationToken).ConfigureAwait(false)).Result;

    protected async Task<ApiResult<T>> SendAsync<T>(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default) =>
        (await SendWithMetadataAsync<T>(request, cancellationToken).ConfigureAwait(false)).Result;

    protected async Task<ApiResponse> SendWithMetadataAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            using var response = await HttpClient
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            var metadata = ApiResponseMetadata.FromHttpResponse(response);
            var result = response.IsSuccessStatusCode
                ? ApiResult.Success(response.StatusCode)
                : ApiResult.Failure(
                    await ApiResponseReader
                        .ReadErrorAsync(response, cancellationToken)
                        .ConfigureAwait(false));

            return new ApiResponse(result, metadata);
        }
        catch (HttpRequestException exception)
        {
            return new ApiResponse(
                ApiResult.Failure(ApiResponseReader.NetworkFailure(exception)));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ApiResponse(ApiResult.Failure(ApiResponseReader.Timeout()));
        }
    }

    protected async Task<ApiResponse<T>> SendWithMetadataAsync<T>(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        HttpStatusCode? successStatusCode = null;
        var metadata = ApiResponseMetadata.Empty;

        try
        {
            using var response = await HttpClient
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            metadata = ApiResponseMetadata.FromHttpResponse(response);

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse<T>(
                    ApiResult<T>.Failure(
                        await ApiResponseReader
                            .ReadErrorAsync(response, cancellationToken)
                            .ConfigureAwait(false)),
                    metadata);
            }

            successStatusCode = response.StatusCode;
            var value = await response.Content
                .ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            var result = value is null
                ? ApiResult<T>.Failure(new ApiError(
                    "Response.Empty",
                    "The API returned an empty response.",
                    response.StatusCode))
                : ApiResult<T>.Success(value, response.StatusCode);

            return new ApiResponse<T>(result, metadata);
        }
        catch (JsonException)
        {
            return new ApiResponse<T>(
                ApiResult<T>.Failure(ApiResponseReader.InvalidPayload(successStatusCode)),
                metadata);
        }
        catch (NotSupportedException)
        {
            return new ApiResponse<T>(
                ApiResult<T>.Failure(ApiResponseReader.InvalidPayload(successStatusCode)),
                metadata);
        }
        catch (HttpRequestException exception)
        {
            return new ApiResponse<T>(
                ApiResult<T>.Failure(ApiResponseReader.NetworkFailure(exception)));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ApiResponse<T>(ApiResult<T>.Failure(ApiResponseReader.Timeout()));
        }
    }
}
