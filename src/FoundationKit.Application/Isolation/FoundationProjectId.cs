namespace FoundationKit.Application.Isolation;

public sealed record FoundationProjectId
{
    public const int MaximumLength = 64;

    public FoundationProjectId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > MaximumLength)
            throw new ArgumentOutOfRangeException(nameof(value), $"Project id cannot exceed {MaximumLength} characters.");

        if (!char.IsAsciiLetterOrDigit(normalized[0]) ||
            !char.IsAsciiLetterOrDigit(normalized[^1]) ||
            normalized.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '.'))
        {
            throw new ArgumentException(
                "Project id must start and end with an ASCII letter or digit and may contain only letters, digits, '-' and '.'.",
                nameof(value));
        }

        Value = normalized;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
