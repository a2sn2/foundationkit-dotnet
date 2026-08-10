using System.ComponentModel.DataAnnotations;

namespace FoundationKit.Workbench.Contracts;

public sealed record CoreCrudCreateRequest(
    [property: Required]
    [property: StringLength(120, MinimumLength = 1)]
    [property: RegularExpression(@".*\S.*", ErrorMessage = "Name must contain at least one non-whitespace character.")]
    string Name);

public sealed record CoreCrudUpdateRequest(
    [property: Required]
    [property: StringLength(120, MinimumLength = 1)]
    [property: RegularExpression(@".*\S.*", ErrorMessage = "Name must contain at least one non-whitespace character.")]
    string Name);

public sealed record CoreCrudResponse(
    Guid Id,
    string Name,
    int Version,
    DateTimeOffset CreatedUtc);
