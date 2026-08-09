using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Results;
using FoundationKit.Authorization;
using Madar.Application.Security;
using Madar.Contracts.Cases;
using Madar.Domain.Cases;

namespace Madar.Application.Cases;

public sealed record CaseSearchCriteria(
    string? Query,
    string? CaseType,
    string? Priority,
    string? Status,
    string? SlaState,
    Guid? DepartmentId,
    Guid? AssignedToUserId,
    DateTimeOffset? CreatedFromUtc,
    DateTimeOffset? CreatedToUtc,
    int Offset,
    int Limit);

public interface ICaseSearchQueryService
{
    Task<CaseSearchResponseDto> SearchAsync(
        CaseSearchCriteria criteria,
        Guid currentUserId,
        bool readAllCases,
        DateTimeOffset evaluatedUtc,
        CancellationToken cancellationToken = default);
}

public static class CaseSearchApplication
{
    public const int MaxQueryLength = 200;
    public const int DefaultLimit = 25;
    public const int MaxLimit = 100;
    public const int MaxOffset = 10_000;
    public static readonly TimeSpan MaxDateRange = TimeSpan.FromDays(366);

    public static async Task<Result<CaseSearchResponseDto>> SearchAsync(
        CaseSearchRequest request,
        ICurrentUser currentUser,
        IAuthorizationEvaluator authorization,
        ICaseSearchQueryService queryService,
        IClock clock,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(authorization);
        ArgumentNullException.ThrowIfNull(queryService);
        ArgumentNullException.ThrowIfNull(clock);

        if (!currentUser.IsAuthenticated
            || !currentUser.UserId.HasValue
            || currentUser.UserId.Value == Guid.Empty)
        {
            return Result<CaseSearchResponseDto>.Failure(
                CaseSearchErrors.AuthenticationRequired);
        }

        var normalization = Normalize(request);
        if (normalization.IsFailure)
            return Result<CaseSearchResponseDto>.Failure(normalization.Error);

        var response = await queryService.SearchAsync(
            normalization.Value,
            currentUser.UserId.Value,
            authorization.HasPermission(MadarPermissions.ReadAllCases),
            clock.UtcNow,
            cancellationToken);

        return Result<CaseSearchResponseDto>.Success(response);
    }

    private static Result<CaseSearchCriteria> Normalize(CaseSearchRequest request)
    {
        var query = NormalizeText(request.Query);
        var caseType = NormalizeCode(request.CaseType);
        var priority = NormalizeCode(request.Priority);
        var status = NormalizeCode(request.Status);
        var slaState = NormalizeCode(request.SlaState);

        if (query is { Length: > MaxQueryLength })
            return Result<CaseSearchCriteria>.Failure(CaseSearchErrors.QueryTooLong);

        if (caseType is not null && !CaseTypes.IsValid(caseType))
            return Result<CaseSearchCriteria>.Failure(CaseSearchErrors.InvalidCaseType);

        if (priority is not null && !CasePriorities.IsValid(priority))
            return Result<CaseSearchCriteria>.Failure(CaseSearchErrors.InvalidPriority);

        if (status is not null && !IsValidStatus(status))
            return Result<CaseSearchCriteria>.Failure(CaseSearchErrors.InvalidStatus);

        if (slaState is not null && !IsValidSlaState(slaState))
            return Result<CaseSearchCriteria>.Failure(CaseSearchErrors.InvalidSlaState);

        if (request.DepartmentId == Guid.Empty)
            return Result<CaseSearchCriteria>.Failure(CaseSearchErrors.InvalidDepartment);

        if (request.AssignedToUserId == Guid.Empty)
            return Result<CaseSearchCriteria>.Failure(CaseSearchErrors.InvalidAssignee);

        if (request.Offset is < 0 or > MaxOffset)
            return Result<CaseSearchCriteria>.Failure(CaseSearchErrors.InvalidOffset);

        if (request.Limit is < 1 or > MaxLimit)
            return Result<CaseSearchCriteria>.Failure(CaseSearchErrors.InvalidLimit);

        if (request.CreatedFromUtc.HasValue
            && request.CreatedToUtc.HasValue)
        {
            if (request.CreatedFromUtc.Value > request.CreatedToUtc.Value)
                return Result<CaseSearchCriteria>.Failure(CaseSearchErrors.InvalidDateRange);

            if (request.CreatedToUtc.Value - request.CreatedFromUtc.Value > MaxDateRange)
                return Result<CaseSearchCriteria>.Failure(CaseSearchErrors.DateRangeTooLarge);
        }

        return Result<CaseSearchCriteria>.Success(new CaseSearchCriteria(
            query,
            caseType,
            priority,
            status,
            slaState,
            request.DepartmentId,
            request.AssignedToUserId,
            request.CreatedFromUtc,
            request.CreatedToUtc,
            request.Offset,
            request.Limit));
    }

    private static string? NormalizeText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? NormalizeCode(string? value) =>
        NormalizeText(value)?.ToLowerInvariant();

    private static bool IsValidStatus(string value) =>
        value is CaseStatuses.New
            or CaseStatuses.Assigned
            or CaseStatuses.InProgress
            or CaseStatuses.Resolved
            or CaseStatuses.Closed;

    private static bool IsValidSlaState(string value) =>
        value is CaseSlaStates.NotApplicable
            or CaseSlaStates.Active
            or CaseSlaStates.Met
            or CaseSlaStates.Breached;
}

public static class CaseSearchErrors
{
    public static readonly Error AuthenticationRequired = Error.Unauthorized(
        "Madar.Search.AuthenticationRequired",
        "يجب تسجيل الدخول للبحث في الحالات.");

    public static readonly Error QueryTooLong = Error.Validation(
        "Madar.Search.QueryTooLong",
        $"نص البحث يجب ألا يتجاوز {CaseSearchApplication.MaxQueryLength} حرفًا.");

    public static readonly Error InvalidCaseType = Error.Validation(
        "Madar.Search.InvalidCaseType",
        "نوع الحالة المحدد للبحث غير صالح.");

    public static readonly Error InvalidPriority = Error.Validation(
        "Madar.Search.InvalidPriority",
        "أولوية الحالة المحددة للبحث غير صالحة.");

    public static readonly Error InvalidStatus = Error.Validation(
        "Madar.Search.InvalidStatus",
        "حالة سير العمل المحددة للبحث غير صالحة.");

    public static readonly Error InvalidSlaState = Error.Validation(
        "Madar.Search.InvalidSlaState",
        "حالة SLA المحددة للبحث غير صالحة.");

    public static readonly Error InvalidDepartment = Error.Validation(
        "Madar.Search.InvalidDepartment",
        "معرف القسم المحدد للبحث غير صالح.");

    public static readonly Error InvalidAssignee = Error.Validation(
        "Madar.Search.InvalidAssignee",
        "معرف الموظف المحدد للبحث غير صالح.");

    public static readonly Error InvalidOffset = Error.Validation(
        "Madar.Search.InvalidOffset",
        $"إزاحة نتائج البحث يجب أن تكون بين 0 و{CaseSearchApplication.MaxOffset}.");

    public static readonly Error InvalidLimit = Error.Validation(
        "Madar.Search.InvalidLimit",
        $"حجم صفحة البحث يجب أن يكون بين 1 و{CaseSearchApplication.MaxLimit}.");

    public static readonly Error InvalidDateRange = Error.Validation(
        "Madar.Search.InvalidDateRange",
        "بداية نطاق تاريخ الإنشاء يجب ألا تكون بعد نهايته.");

    public static readonly Error DateRangeTooLarge = Error.Validation(
        "Madar.Search.DateRangeTooLarge",
        "نطاق تاريخ الإنشاء يجب ألا يتجاوز 366 يومًا.");
}
