using FoundationKit.Application.Results;

namespace Madar.Domain.Cases;

public sealed class CaseAttachment
{
    private CaseAttachment()
    {
    }

    private CaseAttachment(
        Guid id,
        Guid caseId,
        Guid uploadedByUserId,
        string originalFileName,
        string contentType,
        long sizeBytes,
        string storageKey,
        DateTimeOffset createdUtc)
    {
        Id = id;
        CaseId = caseId;
        UploadedByUserId = uploadedByUserId;
        OriginalFileName = originalFileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        StorageKey = storageKey;
        CreatedUtc = createdUtc;
    }

    public Guid Id { get; private set; }

    public Guid CaseId { get; private set; }

    public Guid UploadedByUserId { get; private set; }

    public string OriginalFileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public string StorageKey { get; private set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static Result<CaseAttachment> Create(
        Guid caseId,
        Guid uploadedByUserId,
        string? originalFileName,
        string? contentType,
        long sizeBytes,
        DateTimeOffset createdUtc)
    {
        if (caseId == Guid.Empty)
            return Result<CaseAttachment>.Failure(CaseAttachmentErrors.InvalidCase);

        if (uploadedByUserId == Guid.Empty)
            return Result<CaseAttachment>.Failure(CaseAttachmentErrors.InvalidUploader);

        var normalizedFileName = originalFileName?.Trim() ?? string.Empty;
        if (!CaseAttachmentPolicy.IsSafeFileName(normalizedFileName))
            return Result<CaseAttachment>.Failure(CaseAttachmentErrors.InvalidFileName);

        var normalizedContentType = CaseAttachmentPolicy.NormalizeContentType(contentType);
        if (!CaseAttachmentPolicy.IsAllowedFileType(
                normalizedFileName,
                normalizedContentType))
        {
            return Result<CaseAttachment>.Failure(CaseAttachmentErrors.UnsupportedFileType);
        }

        if (sizeBytes is < 1 or > CaseAttachmentPolicy.MaxSizeBytes)
            return Result<CaseAttachment>.Failure(CaseAttachmentErrors.InvalidSize);

        var id = Guid.NewGuid();
        return Result<CaseAttachment>.Success(
            new CaseAttachment(
                id,
                caseId,
                uploadedByUserId,
                normalizedFileName,
                normalizedContentType,
                sizeBytes,
                $"{caseId:N}/{id:N}",
                createdUtc));
    }
}

public static class CaseAttachmentPolicy
{
    public const long MaxSizeBytes = 10 * 1024 * 1024;

    public static string NormalizeContentType(string? contentType)
    {
        var value = contentType?.Split(';', 2)[0].Trim() ?? string.Empty;
        return value.ToLowerInvariant();
    }

    public static bool IsSafeFileName(string fileName)
    {
        if (fileName.Length is < 1 or > 255)
            return false;

        if (fileName.Any(char.IsControl))
            return false;

        if (fileName.IndexOfAny(['/', '\\']) >= 0)
            return false;

        return string.Equals(
            Path.GetFileName(fileName),
            fileName,
            StringComparison.Ordinal);
    }

    public static bool IsAllowedFileType(
        string fileName,
        string normalizedContentType)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return normalizedContentType switch
        {
            "application/pdf" => extension == ".pdf",
            "image/png" => extension == ".png",
            "image/jpeg" => extension is ".jpg" or ".jpeg",
            "text/plain" => extension == ".txt",
            _ => false
        };
    }
}

public static class CaseAttachmentErrors
{
    public static readonly Error InvalidCase = Error.Validation(
        "Madar.AttachmentInvalidCase",
        "تعذر تحديد الحالة المرتبطة بالمرفق.");

    public static readonly Error InvalidUploader = Error.Unauthorized(
        "Madar.AttachmentInvalidUploader",
        "تعذر تحديد رافع المرفق.");

    public static readonly Error InvalidFileName = Error.Validation(
        "Madar.AttachmentInvalidFileName",
        "اسم الملف غير صالح أو يتجاوز 255 حرفًا.");

    public static readonly Error UnsupportedFileType = Error.Validation(
        "Madar.AttachmentUnsupportedFileType",
        "نوع الملف غير مدعوم. الأنواع المتاحة: PDF وPNG وJPEG وTXT.");

    public static readonly Error InvalidSize = Error.Validation(
        "Madar.AttachmentInvalidSize",
        "حجم الملف يجب أن يكون أكبر من صفر وألا يتجاوز 10 ميجابايت.");

    public static readonly Error InvalidContent = Error.Validation(
        "Madar.AttachmentInvalidContent",
        "محتوى الملف لا يطابق النوع المعلن أو تعذر قراءته بأمان.");

    public static readonly Error NotFound = Error.NotFound(
        "Madar.AttachmentNotFound",
        "المرفق غير موجود أو لا يمكن الوصول إليه.");

    public static readonly Error ContentUnavailable = Error.Failure(
        "Madar.AttachmentContentUnavailable",
        "تعذر قراءة محتوى المرفق من التخزين الخاص.");
}
