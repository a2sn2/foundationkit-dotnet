using Madar.Application.Cases;
using Madar.Domain.Cases;

namespace Madar.Infrastructure.Cases;

public sealed class FileSystemCaseAttachmentContentStore(
    string storageRoot) : ICaseAttachmentContentStore
{
    private const int BufferSize = 81920;
    private readonly string _storageRoot = NormalizeRoot(storageRoot);

    public async Task SaveAsync(
        string storageKey,
        Stream content,
        long expectedSizeBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanRead || !content.CanSeek)
            throw new InvalidDataException("Attachment content must be readable and seekable.");

        if (expectedSizeBytes is < 1 or > CaseAttachmentPolicy.MaxSizeBytes)
            throw new InvalidDataException("Attachment size is outside the allowed range.");

        var remainingLength = content.Length - content.Position;
        if (remainingLength != expectedSizeBytes)
            throw new InvalidDataException("Attachment content length does not match the declared size.");

        var destination = ResolveStoragePath(storageKey);
        var directory = Path.GetDirectoryName(destination)
            ?? throw new InvalidDataException("Attachment storage directory is invalid.");
        Directory.CreateDirectory(directory);

        await using var output = new FileStream(
            destination,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            BufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        var buffer = new byte[BufferSize];
        long totalBytes = 0;
        while (true)
        {
            var read = await content.ReadAsync(
                buffer.AsMemory(),
                cancellationToken);
            if (read == 0)
                break;

            totalBytes += read;
            if (totalBytes > expectedSizeBytes
                || totalBytes > CaseAttachmentPolicy.MaxSizeBytes)
            {
                throw new InvalidDataException(
                    "Attachment content exceeded the declared or allowed size.");
            }

            await output.WriteAsync(
                buffer.AsMemory(0, read),
                cancellationToken);
        }

        if (totalBytes != expectedSizeBytes)
            throw new InvalidDataException("Attachment content length changed during upload.");

        await output.FlushAsync(cancellationToken);
    }

    public Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolveStoragePath(storageKey);
        if (!File.Exists(path))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            BufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult<Stream?>(stream);
    }

    private string ResolveStoragePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new InvalidDataException("Attachment storage key is required.");

        var segments = storageKey.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length != 2
            || !Guid.TryParseExact(segments[0], "N", out _)
            || !Guid.TryParseExact(segments[1], "N", out _))
        {
            throw new InvalidDataException("Attachment storage key is invalid.");
        }

        var candidate = Path.GetFullPath(
            Path.Combine(_storageRoot, segments[0], segments[1]));
        var rootWithSeparator = _storageRoot.EndsWith(
            Path.DirectorySeparatorChar.ToString(),
            StringComparison.Ordinal)
            ? _storageRoot
            : _storageRoot + Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(rootWithSeparator, StringComparison.Ordinal))
            throw new InvalidDataException("Attachment storage path is invalid.");

        return candidate;
    }

    private static string NormalizeRoot(string storageRoot)
    {
        if (string.IsNullOrWhiteSpace(storageRoot))
            throw new ArgumentException(
                "Attachment storage root is required.",
                nameof(storageRoot));

        return Path.GetFullPath(storageRoot.Trim());
    }
}
