using FoundationKit.Blazor.Api;

namespace FoundationKit.Blazor.State;

public enum PresentationStateKind
{
    Idle = 0,
    Loading = 1,
    Ready = 2,
    Empty = 3,
    Error = 4
}

/// <summary>
/// Framework-agnostic presentation state for Blazor screens and components.
/// It carries transport errors without turning the browser into an authorization
/// or business-rule boundary.
/// </summary>
public sealed record PresentationState<T>(
    PresentationStateKind Kind,
    T? Value = default,
    ApiError? Error = null)
{
    public bool IsBusy => Kind == PresentationStateKind.Loading;
    public bool HasValue => Kind == PresentationStateKind.Ready && Value is not null;
    public bool HasError => Kind == PresentationStateKind.Error && Error is not null;

    public static PresentationState<T> Idle() => new(PresentationStateKind.Idle);

    public static PresentationState<T> Loading() => new(PresentationStateKind.Loading);

    public static PresentationState<T> FromResult(
        ApiResult<T> result,
        Func<T, bool>? isEmpty = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsFailure)
            return new PresentationState<T>(PresentationStateKind.Error, Error: result.ErrorDetails);

        if (result.Value is null)
            return new PresentationState<T>(PresentationStateKind.Empty);

        if (isEmpty?.Invoke(result.Value) == true)
            return new PresentationState<T>(PresentationStateKind.Empty, result.Value);

        return new PresentationState<T>(PresentationStateKind.Ready, result.Value);
    }
}

/// <summary>
/// Bounded UI representation of list-query intent. The server remains responsible
/// for validating the actual API query policy and authorization.
/// </summary>
public sealed record PagedQueryState
{
    public PagedQueryState(
        int page = 1,
        int pageSize = 20,
        IReadOnlyList<string>? filters = null,
        IReadOnlyList<string>? sorts = null)
    {
        if (page < 1)
            throw new ArgumentOutOfRangeException(nameof(page), page, "Page must be at least 1.");
        if (pageSize is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be between 1 and 100.");

        Page = page;
        PageSize = pageSize;
        Filters = Normalize(filters, nameof(filters), 25);
        Sorts = Normalize(sorts, nameof(sorts), 10);
    }

    public int Page { get; }
    public int PageSize { get; }
    public IReadOnlyList<string> Filters { get; }
    public IReadOnlyList<string> Sorts { get; }

    private static string[] Normalize(
        IReadOnlyList<string>? values,
        string parameterName,
        int maximumCount)
    {
        if (values is null || values.Count == 0)
            return Array.Empty<string>();
        if (values.Count > maximumCount)
            throw new ArgumentOutOfRangeException(parameterName, $"At most {maximumCount} entries are supported.");

        var result = new string[values.Count];
        for (var index = 0; index < values.Count; index++)
        {
            var value = values[index];
            if (string.IsNullOrWhiteSpace(value) || value.Length > 512 || value.Any(char.IsControl))
                throw new ArgumentException("Query entries must be non-empty, bounded text without control characters.", parameterName);
            result[index] = value.Trim();
        }

        return result;
    }
}

/// <summary>
/// Stable display metadata that a UI may use to render a resource or read model
/// without duplicating its server-side business logic.
/// </summary>
public sealed record ResourceDisplayDescriptor(
    string Name,
    string Route,
    bool ReadOnly,
    IReadOnlyList<string> Capabilities)
{
    public ResourceDisplayDescriptor Normalize()
    {
        var name = RequireText(Name, nameof(Name), 96);
        var route = RequireText(Route, nameof(Route), 160);
        var capabilities = (Capabilities ?? Array.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => RequireText(value, nameof(Capabilities), 96))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return this with { Name = name, Route = route, Capabilities = capabilities };
    }

    private static string RequireText(string value, string parameterName, int maximumLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > maximumLength || normalized.Any(char.IsControl))
            throw new ArgumentException("Display metadata is too long or contains control characters.", parameterName);
        return normalized;
    }
}
