using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Persistence;
using FoundationKit.Application.Results;
using FoundationKit.Auditing;
using FoundationKit.Authorization;
using Madar.Contracts.Cases;
using Madar.Domain.Cases;

namespace Madar.Application.Cases;

public interface ICaseAttachmentManager
{
    Task<Result<IReadOnlyList<CaseAttachmentDto>>> ListAsync(
        Guid caseId,
        CancellationToken cancellationToken = default);

    Task<Result<CaseAttachmentDto>> UploadAsync(
        Guid caseId,
        CaseAttachmentUpload upload,
        CancellationToken cancellationToken = default);

    Task<Result<CaseAttachmentDownload>> DownloadAsync(
        Guid caseId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);
}

public sealed class CaseAttachmentManager(
    ICurrentUser currentUser,
    IAuthorizationEvaluator authorization,
    IRepository<Case, Guid> caseRepository,
    ICaseAttachmentStore attachmentStore,
    ICaseAttachmentQueryService queryService,
    ICaseAttachmentContentStore contentStore,
    IUnitOfWork unitOfWork,
    IAuditRecorder auditRecorder,
    IClock clock) : ICaseAttachmentManager
{
    public async Task<Result<IReadOnlyList<CaseAttachmentDto>>> ListAsync(
        Guid caseId,
        CancellationToken cancellationToken = default)
    {
        var access = await AuthorizeCaseAsync(caseId, cancellationToken);
        if (access.IsFailure)
            return Result<IReadOnlyList<CaseAttachmentDto>>.Failure(access.Error);

        var attachments = await queryService.ListForCaseAsync(
            caseId,
            cancellationToken);
        return Result<IReadOnlyList<CaseAttachmentDto>>.Success(attachments);
    }

    public async Task<Result<CaseAttachmentDto>> UploadAsync(
        Guid caseId,
        CaseAttachmentUpload upload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upload);
        ArgumentNullException.ThrowIfNull(upload.Content);

        var access = await AuthorizeCaseAsync(caseId, cancellationToken);
        if (access.IsFailure)
            return Result<CaseAttachmentDto>.Failure(access.Error);

        var userId = currentUser.UserId!.Value;
        var creation = CaseAttachment.Create(
            caseId,
            userId,
            upload.FileName,
            upload.ContentType,
            upload.SizeBytes,
            clock.UtcNow);
        if (creation.IsFailure)
            return Result<CaseAttachmentDto>.Failure(creation.Error);

        var attachment = creation.Value;
        if (!await HasExpectedContentSignatureAsync(
                upload.Content,
                attachment.ContentType,
                cancellationToken))
        {
            return Result<CaseAttachmentDto>.Failure(
                CaseAttachmentErrors.InvalidContent);
        }

        try
        {
            await contentStore.SaveAsync(
                attachment.StorageKey,
                upload.Content,
                attachment.SizeBytes,
                cancellationToken);
        }
        catch (InvalidDataException)
        {
            return Result<CaseAttachmentDto>.Failure(
                CaseAttachmentErrors.InvalidContent);
        }
        catch (IOException)
        {
            return Result<CaseAttachmentDto>.Failure(
                CaseAttachmentErrors.ContentUnavailable);
        }
        catch (UnauthorizedAccessException)
        {
            return Result<CaseAttachmentDto>.Failure(
                CaseAttachmentErrors.ContentUnavailable);
        }

        try
        {
            await attachmentStore.AddAsync(attachment, cancellationToken);
            await auditRecorder.RecordAsync(
                new AuditRequest(
                    "madar.case.attachment-uploaded",
                    nameof(Case),
                    caseId.ToString("D"),
                    Attributes: new Dictionary<string, string>
                    {
                        ["attachmentId"] = attachment.Id.ToString("D")
                    }),
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await TryDeleteContentAsync(
                attachment.StorageKey,
                cancellationToken);
            throw;
        }

        var response = await queryService.GetByIdAsync(
            attachment.Id,
            cancellationToken);
        return response is null
            ? Result<CaseAttachmentDto>.Failure(CaseAttachmentErrors.NotFound)
            : Result<CaseAttachmentDto>.Success(response);
    }

    public async Task<Result<CaseAttachmentDownload>> DownloadAsync(
        Guid caseId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var access = await AuthorizeCaseAsync(caseId, cancellationToken);
        if (access.IsFailure)
            return Result<CaseAttachmentDownload>.Failure(access.Error);

        var record = await queryService.GetForCaseAsync(
            caseId,
            attachmentId,
            cancellationToken);
        if (record is null)
            return Result<CaseAttachmentDownload>.Failure(CaseAttachmentErrors.NotFound);

        Stream? content;
        try
        {
            content = await contentStore.OpenReadAsync(
                record.StorageKey,
                cancellationToken);
        }
        catch (IOException)
        {
            return Result<CaseAttachmentDownload>.Failure(
                CaseAttachmentErrors.ContentUnavailable);
        }
        catch (UnauthorizedAccessException)
        {
            return Result<CaseAttachmentDownload>.Failure(
                CaseAttachmentErrors.ContentUnavailable);
        }

        if (content is null)
        {
            return Result<CaseAttachmentDownload>.Failure(
                CaseAttachmentErrors.ContentUnavailable);
        }

        try
        {
            await auditRecorder.RecordAsync(
                new AuditRequest(
                    "madar.case.attachment-downloaded",
                    nameof(Case),
                    caseId.ToString("D"),
                    Attributes: new Dictionary<string, string>
                    {
                        ["attachmentId"] = attachmentId.ToString("D")
                    }),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await content.DisposeAsync();
            throw;
        }

        return Result<CaseAttachmentDownload>.Success(
            new CaseAttachmentDownload(record.Metadata, content));
    }

    private async Task<Result> AuthorizeCaseAsync(
        Guid caseId,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result.Failure(CaseApplicationErrors.AuthenticationRequired);

        var item = await caseRepository.GetByIdAsync(caseId, cancellationToken);
        if (item is null
            || !CaseAccessRules.CanRead(
                item,
                currentUser.UserId.Value,
                authorization))
        {
            return Result.Failure(CaseApplicationErrors.CaseNotFound);
        }

        return Result.Success();
    }

    private static async Task<bool> HasExpectedContentSignatureAsync(
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (!content.CanRead || !content.CanSeek)
            return false;

        var originalPosition = content.Position;
        var sample = new byte[4096];
        try
        {
            var read = await content.ReadAsync(
                sample.AsMemory(),
                cancellationToken);
            content.Position = originalPosition;
            if (read == 0)
                return false;

            return contentType switch
            {
                "application/pdf" => StartsWith(
                    sample,
                    read,
                    [0x25, 0x50, 0x44, 0x46, 0x2D]),
                "image/png" => StartsWith(
                    sample,
                    read,
                    [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
                "image/jpeg" => StartsWith(
                    sample,
                    read,
                    [0xFF, 0xD8, 0xFF]),
                "text/plain" => !sample.AsSpan(0, read).Contains((byte)0),
                _ => false
            };
        }
        catch (IOException)
        {
            return false;
        }
        finally
        {
            if (content.CanSeek)
                content.Position = originalPosition;
        }
    }

    private static bool StartsWith(
        byte[] source,
        int sourceLength,
        ReadOnlySpan<byte> signature) =>
        sourceLength >= signature.Length
        && source.AsSpan(0, signature.Length).SequenceEqual(signature);

    private async Task TryDeleteContentAsync(
        string storageKey,
        CancellationToken cancellationToken)
    {
        try
        {
            await contentStore.DeleteIfExistsAsync(
                storageKey,
                cancellationToken);
        }
        catch (IOException)
        {
            // Best-effort compensating cleanup. The original database failure remains authoritative.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort compensating cleanup. The original database failure remains authoritative.
        }
    }
}
