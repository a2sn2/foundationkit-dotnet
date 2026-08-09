using FoundationKit.WebApi.Results;
using Madar.Api.Security;
using Madar.Application.Cases;
using Madar.Contracts.Cases;

namespace Madar.Api;

public static class CaseAttachmentEndpoints
{
    public static IEndpointRouteBuilder MapMadarCaseAttachmentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                "/api/cases/{caseId:guid}/attachments",
                async (
                    Guid caseId,
                    ICaseAttachmentManager manager,
                    CancellationToken cancellationToken) =>
                    (await manager.ListAsync(caseId, cancellationToken))
                        .ToHttpResult(Results.Ok))
            .RequireAuthorization()
            .WithTags("Case Attachments")
            .WithName("ListMadarCaseAttachments")
            .Produces<IReadOnlyList<CaseAttachmentDto>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapPost(
                "/api/cases/{caseId:guid}/attachments",
                async (
                    Guid caseId,
                    IFormFile file,
                    ICaseAttachmentManager manager,
                    CancellationToken cancellationToken) =>
                {
                    await using var stream = file.OpenReadStream();
                    return (await manager.UploadAsync(
                            caseId,
                            new CaseAttachmentUpload(
                                file.FileName,
                                file.ContentType,
                                file.Length,
                                stream),
                            cancellationToken))
                        .ToHttpResult(value => Results.Created(
                            CaseAttachmentRoutes.Download(caseId, value.Id),
                            value));
                })
            // IFormFile adds ASP.NET Core's automatic antiforgery metadata. Madar
            // already validates the same antiforgery service explicitly through
            // AntiforgeryEndpointFilter so the endpoint disables only the automatic
            // middleware requirement, not antiforgery validation itself.
            .DisableAntiforgery()
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireAuthorization()
            .RequireRateLimiting("write")
            .WithTags("Case Attachments")
            .WithName("UploadMadarCaseAttachment")
            .Produces<CaseAttachmentDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapGet(
                "/api/cases/{caseId:guid}/attachments/{attachmentId:guid}/content",
                async (
                    Guid caseId,
                    Guid attachmentId,
                    ICaseAttachmentManager manager,
                    CancellationToken cancellationToken) =>
                    (await manager.DownloadAsync(
                            caseId,
                            attachmentId,
                            cancellationToken))
                        .ToHttpResult(download => Results.Stream(
                            download.Content,
                            download.Metadata.ContentType,
                            download.Metadata.OriginalFileName,
                            enableRangeProcessing: false)))
            .RequireAuthorization()
            .WithTags("Case Attachments")
            .WithName("DownloadMadarCaseAttachment")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
