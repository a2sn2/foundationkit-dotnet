using System.Net.Http.Json;
using FoundationKit.Blazor.Api;
using FoundationKit.Workbench.Contracts;
using FoundationKit.Workbench.Contracts.Admin;
using FoundationKit.Workbench.Contracts.User;

namespace FoundationKit.Workbench.Client.Services;

public sealed record StudioClientError(string Message);

public sealed record StudioClientResult<T>(
    bool IsSuccess,
    T? Value,
    StudioClientError? Error);

public sealed class WorkbenchApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public Task<ApiResult<RuntimeResponse>> GetRuntimeAsync(
        CancellationToken cancellationToken = default) =>
        SendAsync<RuntimeResponse>(
            new HttpRequestMessage(HttpMethod.Get, ApiRoutes.Runtime),
            cancellationToken);

    public async Task<ApiResult<CatalogResponse>> GetCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        var apiResult = await SendAsync<CatalogResponse>(
            new HttpRequestMessage(HttpMethod.Get, ApiRoutes.Catalog),
            cancellationToken);

        if (apiResult.IsSuccess)
            return apiResult;

        return await SendAsync<CatalogResponse>(
            new HttpRequestMessage(HttpMethod.Get, "catalog/foundationkit.catalog.json"),
            cancellationToken);
    }

    public Task<ApiResult<IReadOnlyList<ModuleCompositionResponse>>> GetModulesAsync(
        CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<ModuleCompositionResponse>>(
            new HttpRequestMessage(HttpMethod.Get, "/api/modules"),
            cancellationToken);

    public Task<ApiResult<PlatformReferenceResponse>> GetPlatformReferenceAsync(
        CancellationToken cancellationToken = default) =>
        SendAsync<PlatformReferenceResponse>(
            new HttpRequestMessage(HttpMethod.Get, "/api/platform-reference"),
            cancellationToken);

    public async Task<StudioClientResult<StudioCatalogResponse>> GetProjectStudioCatalogAsync(
        CancellationToken cancellationToken = default) =>
        ToStudioResult(await SendAsync<StudioCatalogResponse>(
            new HttpRequestMessage(HttpMethod.Get, "/api/studio/catalog"),
            cancellationToken));

    public async Task<StudioClientResult<StudioPreviewResponse>> PreviewProjectStudioAsync(
        StudioProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ToStudioResult(await SendAsync<StudioPreviewResponse>(
            new HttpRequestMessage(HttpMethod.Post, "/api/studio/preview")
            {
                Content = JsonContent.Create(request)
            },
            cancellationToken));
    }

    public async Task<StudioClientResult<StudioProjectGenerationResponse>> GenerateProjectStudioAsync(
        StudioProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ToStudioResult(await SendAsync<StudioProjectGenerationResponse>(
            new HttpRequestMessage(HttpMethod.Post, "/api/studio/generate")
            {
                Content = JsonContent.Create(request)
            },
            cancellationToken));
    }

    public Task<ApiResult<ComposerValidationResponse>> ValidateComposerManifestAsync(
        string manifestJson,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestJson);
        return SendAsync<ComposerValidationResponse>(
            new HttpRequestMessage(HttpMethod.Post, "/api/composer/validate")
            {
                Content = JsonContent.Create(new ComposerValidationRequest(manifestJson))
            },
            cancellationToken);
    }

    public Task<ApiResult<ComposerGenerationResponse>> GenerateComposerProjectAsync(
        string manifestJson,
        bool force = false,
        string foundationMode = "linked",
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(foundationMode);
        return SendAsync<ComposerGenerationResponse>(
            new HttpRequestMessage(HttpMethod.Post, "/api/composer/generate")
            {
                Content = JsonContent.Create(new ComposerGenerationRequest(manifestJson, force, foundationMode))
            },
            cancellationToken);
    }

    public Task<ApiResult<HealthResponse>> GetHealthAsync(
        CancellationToken cancellationToken = default) =>
        SendAsync<HealthResponse>(
            new HttpRequestMessage(HttpMethod.Get, ApiRoutes.Health),
            cancellationToken);

    public Task<ApiResult<UserRequestResponse>> CreateUserRequestAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return SendAsync<UserRequestResponse>(
            new HttpRequestMessage(HttpMethod.Post, ApiRoutes.User.Requests)
            {
                Content = JsonContent.Create(request)
            },
            cancellationToken);
    }

    public Task<ApiResult<UserRequestResponse>> GetUserRequestAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        SendAsync<UserRequestResponse>(
            new HttpRequestMessage(HttpMethod.Get, ApiRoutes.User.Request(id)),
            cancellationToken);

    public Task<ApiResult<IReadOnlyList<AdminQueueItemResponse>>> GetAdminQueueAsync(
        string status = "submitted",
        CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<AdminQueueItemResponse>>(
            new HttpRequestMessage(
                HttpMethod.Get,
                $"{ApiRoutes.Admin.Requests}?status={Uri.EscapeDataString(status)}"),
            cancellationToken);

    public Task<ApiResult<AdminReviewResponse>> ReviewUserRequestAsync(
        Guid id,
        AdminReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return SendAsync<AdminReviewResponse>(
            new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Admin.Review(id))
            {
                Content = JsonContent.Create(request)
            },
            cancellationToken);
    }

    private static StudioClientResult<T> ToStudioResult<T>(ApiResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new StudioClientResult<T>(
            result.IsSuccess,
            result.Value,
            result.IsSuccess || string.IsNullOrWhiteSpace(result.Error)
                ? null
                : new StudioClientError(result.Error));
    }
}