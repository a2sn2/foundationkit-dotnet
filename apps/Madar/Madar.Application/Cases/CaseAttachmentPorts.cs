using Madar.Contracts.Cases;
using Madar.Domain.Cases;

namespace Madar.Application.Cases;

public sealed record CaseAttachmentStoredRecord(
    CaseAttachmentDto Metadata,
    string StorageKey);

public sealed record CaseAttachmentDownload(
    CaseAttachmentDto Metadata,
    Stream Content);

public sealed record CaseAttachmentUpload(
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content);

public interface ICaseAttachmentStore
{
    Task AddAsync(
        CaseAttachment attachment,
        CancellationToken cancellationToken = default);
}

public interface ICaseAttachmentQueryService
{
    Task<CaseAttachmentStoredRecord?> GetForCaseAsync(
        Guid caseId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    Task<CaseAttachmentDto?> GetByIdAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CaseAttachmentDto>> ListForCaseAsync(
        Guid caseId,
        CancellationToken cancellationToken = default);
}

public interface ICaseAttachmentContentStore
{
    Task SaveAsync(
        string storageKey,
        Stream content,
        long expectedSizeBytes,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default);
}
