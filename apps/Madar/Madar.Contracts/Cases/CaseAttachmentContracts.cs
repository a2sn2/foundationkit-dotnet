namespace Madar.Contracts.Cases;

public sealed record CaseAttachmentDto(
    Guid Id,
    Guid CaseId,
    Guid UploadedByUserId,
    string UploadedByDisplayName,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset CreatedUtc);

public static class CaseAttachmentRoutes
{
    public static string ForCase(Guid caseId) =>
        $"{CaseRoutes.ById(caseId)}/attachments";

    public static string Download(Guid caseId, Guid attachmentId) =>
        $"{ForCase(caseId)}/{attachmentId:D}/content";
}
