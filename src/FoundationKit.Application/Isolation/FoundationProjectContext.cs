namespace FoundationKit.Application.Isolation;

public interface IFoundationProjectContext
{
    FoundationProjectId ProjectId { get; }
}

public sealed class FoundationProjectContext(FoundationProjectId projectId) : IFoundationProjectContext
{
    public FoundationProjectId ProjectId { get; } = projectId ?? throw new ArgumentNullException(nameof(projectId));
}

public sealed class FoundationResourceNamespace(IFoundationProjectContext projectContext)
{
    private const int MaximumResourceKindLength = 48;
    private const int MaximumLocalKeyLength = 256;

    public string Create(string resourceKind, string localKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(localKey);

        var normalizedKind = resourceKind.Trim().ToLowerInvariant();
        if (normalizedKind.Length > MaximumResourceKindLength ||
            !normalizedKind.All(character =>
                char.IsAsciiLetterOrDigit(character) || character is '-' or '.'))
        {
            throw new ArgumentException("Resource kind contains unsupported characters or is too long.", nameof(resourceKind));
        }

        var normalizedLocalKey = localKey.Trim();
        if (normalizedLocalKey.Length > MaximumLocalKeyLength || normalizedLocalKey.Any(char.IsControl))
            throw new ArgumentException("Resource local key contains control characters or is too long.", nameof(localKey));

        return $"foundation:{projectContext.ProjectId.Value}:{normalizedKind}:{normalizedLocalKey}";
    }
}
