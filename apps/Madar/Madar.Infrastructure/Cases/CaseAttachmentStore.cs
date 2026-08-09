using Madar.Application.Cases;
using Madar.Contracts.Cases;
using Madar.Domain.Cases;
using Madar.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace Madar.Infrastructure.Cases;

public sealed class CaseAttachmentStore(MadarDbContext dbContext)
    : ICaseAttachmentStore, ICaseAttachmentQueryService
{
    public async Task AddAsync(
        CaseAttachment attachment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        await dbContext.CaseAttachments.AddAsync(attachment, cancellationToken);
    }

    public Task<CaseAttachmentStoredRecord?> GetForCaseAsync(
        Guid caseId,
        Guid attachmentId,
        CancellationToken cancellationToken = default) =>
        (
            from attachment in dbContext.CaseAttachments.AsNoTracking()
            join user in dbContext.Set<MadarUser>().AsNoTracking()
                on attachment.UploadedByUserId equals user.Id
            where attachment.CaseId == caseId
                && attachment.Id == attachmentId
            select new CaseAttachmentStoredRecord(
                new CaseAttachmentDto(
                    attachment.Id,
                    attachment.CaseId,
                    attachment.UploadedByUserId,
                    user.DisplayName,
                    attachment.OriginalFileName,
                    attachment.ContentType,
                    attachment.SizeBytes,
                    attachment.CreatedUtc),
                attachment.StorageKey)
        ).SingleOrDefaultAsync(cancellationToken);

    public Task<CaseAttachmentDto?> GetByIdAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default) =>
        (
            from attachment in dbContext.CaseAttachments.AsNoTracking()
            join user in dbContext.Set<MadarUser>().AsNoTracking()
                on attachment.UploadedByUserId equals user.Id
            where attachment.Id == attachmentId
            select new CaseAttachmentDto(
                attachment.Id,
                attachment.CaseId,
                attachment.UploadedByUserId,
                user.DisplayName,
                attachment.OriginalFileName,
                attachment.ContentType,
                attachment.SizeBytes,
                attachment.CreatedUtc)
        ).SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<CaseAttachmentDto>> ListForCaseAsync(
        Guid caseId,
        CancellationToken cancellationToken = default) =>
        await (
            from attachment in dbContext.CaseAttachments.AsNoTracking()
            join user in dbContext.Set<MadarUser>().AsNoTracking()
                on attachment.UploadedByUserId equals user.Id
            where attachment.CaseId == caseId
            orderby attachment.CreatedUtc, attachment.Id
            select new CaseAttachmentDto(
                attachment.Id,
                attachment.CaseId,
                attachment.UploadedByUserId,
                user.DisplayName,
                attachment.OriginalFileName,
                attachment.ContentType,
                attachment.SizeBytes,
                attachment.CreatedUtc)
        ).ToListAsync(cancellationToken);
}
