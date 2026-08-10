namespace FoundationKit.Workbench.Contracts;

public sealed record CoreCrudCreateRequest(string Name);

public sealed record CoreCrudUpdateRequest(string Name, int ExpectedVersion);

public sealed record CoreCrudResponse(
    Guid Id,
    string Name,
    int Version,
    DateTimeOffset CreatedUtc);
