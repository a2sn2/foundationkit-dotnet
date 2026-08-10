using System.ComponentModel.DataAnnotations;

namespace FoundationKit.Workbench.Contracts;

public sealed record CoreCrudCreateRequest(
    [property: Required]
    [property: StringLength(120, MinimumLength = 1)]
    string Name);

public sealed record CoreCrudUpdateRequest(
    [property: Required]
    [property: StringLength(120, MinimumLength = 1)]
    string Name,
    [property: Range(1, int.MaxValue)]
    int ExpectedVersion);

public sealed record CoreCrudResponse(
    Guid Id,
    string Name,
    int Version,
    DateTimeOffset CreatedUtc);
