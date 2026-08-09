namespace Madar.Contracts.Cases;

public sealed record CaseSearchRequest(
    string? Query = null,
    string? CaseType = null,
    string? Priority = null,
    string? Status = null,
    string? SlaState = null,
    Guid? DepartmentId = null,
    Guid? AssignedToUserId = null,
    DateTimeOffset? CreatedFromUtc = null,
    DateTimeOffset? CreatedToUtc = null,
    int Offset = 0,
    int Limit = 25);

public sealed record CaseSearchSummaryDto(
    int Total,
    int Unassigned,
    int New,
    int Assigned,
    int InProgress,
    int Resolved,
    int Closed,
    int SlaNotApplicable,
    int SlaActive,
    int SlaMet,
    int SlaBreached);

public sealed record CaseSearchResponseDto(
    IReadOnlyList<CaseDto> Items,
    int Total,
    int Offset,
    int Limit,
    CaseSearchSummaryDto Summary);

public static class CaseSearchRoutes
{
    public const string Search = "/api/cases/search";
}
