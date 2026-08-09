using FoundationKit.Application.Abstractions;
using Madar.Application.Cases;
using Madar.Contracts.Cases;
using Madar.Domain.Cases;
using Microsoft.EntityFrameworkCore;

namespace Madar.Infrastructure.Cases;

public sealed class CaseQueryService(
    MadarDbContext dbContext,
    IClock clock) : ICaseQueryService, ICaseSlaQueryService, ICaseSearchQueryService
{
    public async Task<CaseDto?> GetByIdAsync(
        Guid caseId,
        CancellationToken cancellationToken = default)
    {
        var item = await dbContext.Cases
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == caseId, cancellationToken);

        return item is null ? null : ToDto(item, clock.UtcNow);
    }

    public async Task<IReadOnlyList<CaseDto>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var items = await dbContext.Cases
            .AsNoTracking()
            .Where(item =>
                item.CreatedByUserId == userId
                || item.AssignedToUserId == userId)
            .OrderByDescending(item => item.UpdatedUtc)
            .ToListAsync(cancellationToken);

        var evaluatedUtc = clock.UtcNow;
        return items.Select(item => ToDto(item, evaluatedUtc)).ToArray();
    }

    public async Task<IReadOnlyList<CaseDto>> ListAllAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await dbContext.Cases
            .AsNoTracking()
            .OrderByDescending(item => item.UpdatedUtc)
            .ToListAsync(cancellationToken);

        var evaluatedUtc = clock.UtcNow;
        return items.Select(item => ToDto(item, evaluatedUtc)).ToArray();
    }

    public async Task<IReadOnlyList<CaseDto>> ListDepartmentQueueAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        var items = await dbContext.Cases
            .AsNoTracking()
            .Where(item =>
                item.DepartmentId == departmentId
                && item.Status == CaseStatuses.New
                && item.AssignedToUserId == null)
            .OrderBy(item => item.CreatedUtc)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

        var evaluatedUtc = clock.UtcNow;
        return items.Select(item => ToDto(item, evaluatedUtc)).ToArray();
    }

    public async Task<IReadOnlyList<Guid>> ListDueCaseIdsAsync(
        DateTimeOffset evaluatedUtc,
        int limit,
        CancellationToken cancellationToken = default) =>
        await dbContext.Cases
            .AsNoTracking()
            .Where(item =>
                item.SlaTargetUtc != null
                && item.SlaTargetUtc < evaluatedUtc
                && item.SlaBreachedUtc == null
                && item.ResolvedUtc == null)
            .OrderBy(item => item.SlaTargetUtc)
            .ThenBy(item => item.Id)
            .Select(item => item.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<CaseSearchResponseDto> SearchAsync(
        CaseSearchCriteria criteria,
        Guid currentUserId,
        bool readAllCases,
        DateTimeOffset evaluatedUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var query = dbContext.Cases.AsNoTracking().AsQueryable();

        if (!readAllCases)
        {
            query = query.Where(item =>
                item.CreatedByUserId == currentUserId
                || item.AssignedToUserId == currentUserId);
        }

        if (criteria.Query is not null)
        {
            var text = criteria.Query;
            if (Guid.TryParse(text, out var caseId))
            {
                query = query.Where(item =>
                    item.Id == caseId
                    || item.Title.Contains(text)
                    || item.Description.Contains(text));
            }
            else
            {
                query = query.Where(item =>
                    item.Title.Contains(text)
                    || item.Description.Contains(text));
            }
        }

        if (criteria.CaseType is not null)
            query = query.Where(item => item.CaseType == criteria.CaseType);

        if (criteria.Priority is not null)
            query = query.Where(item => item.Priority == criteria.Priority);

        if (criteria.Status is not null)
            query = query.Where(item => item.Status == criteria.Status);

        if (criteria.DepartmentId.HasValue)
            query = query.Where(item => item.DepartmentId == criteria.DepartmentId.Value);

        if (criteria.AssignedToUserId.HasValue)
        {
            query = query.Where(item =>
                item.AssignedToUserId == criteria.AssignedToUserId.Value);
        }

        if (criteria.CreatedFromUtc.HasValue)
            query = query.Where(item => item.CreatedUtc >= criteria.CreatedFromUtc.Value);

        if (criteria.CreatedToUtc.HasValue)
            query = query.Where(item => item.CreatedUtc <= criteria.CreatedToUtc.Value);

        query = ApplySlaFilter(query, criteria.SlaState, evaluatedUtc);

        var summary = await query
            .GroupBy(_ => 1)
            .Select(group => new CaseSearchSummaryDto(
                group.Count(),
                group.Count(item => item.AssignedToUserId == null),
                group.Count(item => item.Status == CaseStatuses.New),
                group.Count(item => item.Status == CaseStatuses.Assigned),
                group.Count(item => item.Status == CaseStatuses.InProgress),
                group.Count(item => item.Status == CaseStatuses.Resolved),
                group.Count(item => item.Status == CaseStatuses.Closed),
                group.Count(item => item.SlaTargetUtc == null),
                group.Count(item =>
                    item.SlaTargetUtc != null
                    && item.ResolvedUtc == null
                    && item.SlaBreachedUtc == null
                    && evaluatedUtc <= item.SlaTargetUtc),
                group.Count(item =>
                    item.SlaTargetUtc != null
                    && item.ResolvedUtc != null
                    && item.ResolvedUtc <= item.SlaTargetUtc),
                group.Count(item =>
                    item.SlaTargetUtc != null
                    && ((item.ResolvedUtc != null
                            && item.ResolvedUtc > item.SlaTargetUtc)
                        || (item.ResolvedUtc == null
                            && (item.SlaBreachedUtc != null
                                || evaluatedUtc > item.SlaTargetUtc))))))
            .SingleOrDefaultAsync(cancellationToken)
            ?? EmptySummary();

        var items = await query
            .OrderByDescending(item => item.UpdatedUtc)
            .ThenBy(item => item.Id)
            .Skip(criteria.Offset)
            .Take(criteria.Limit)
            .ToListAsync(cancellationToken);

        return new CaseSearchResponseDto(
            items.Select(item => ToDto(item, evaluatedUtc)).ToArray(),
            summary.Total,
            criteria.Offset,
            criteria.Limit,
            summary);
    }

    private static IQueryable<Case> ApplySlaFilter(
        IQueryable<Case> query,
        string? slaState,
        DateTimeOffset evaluatedUtc) =>
        slaState switch
        {
            CaseSlaStates.NotApplicable => query.Where(item => item.SlaTargetUtc == null),
            CaseSlaStates.Active => query.Where(item =>
                item.SlaTargetUtc != null
                && item.ResolvedUtc == null
                && item.SlaBreachedUtc == null
                && evaluatedUtc <= item.SlaTargetUtc),
            CaseSlaStates.Met => query.Where(item =>
                item.SlaTargetUtc != null
                && item.ResolvedUtc != null
                && item.ResolvedUtc <= item.SlaTargetUtc),
            CaseSlaStates.Breached => query.Where(item =>
                item.SlaTargetUtc != null
                && ((item.ResolvedUtc != null
                        && item.ResolvedUtc > item.SlaTargetUtc)
                    || (item.ResolvedUtc == null
                        && (item.SlaBreachedUtc != null
                            || evaluatedUtc > item.SlaTargetUtc)))),
            _ => query
        };

    private static CaseSearchSummaryDto EmptySummary() =>
        new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    private static CaseDto ToDto(Case item, DateTimeOffset evaluatedUtc) =>
        new(
            item.Id,
            item.CreatedByUserId,
            item.Title,
            item.Description,
            item.CaseType,
            item.Priority,
            item.Status,
            item.DepartmentId,
            item.RoutedUtc,
            item.AssignedToUserId,
            item.CreatedUtc,
            item.UpdatedUtc,
            item.ResolvedUtc,
            item.ClosedUtc,
            item.SlaTargetUtc,
            item.SlaBreachedUtc,
            item.EscalatedUtc,
            item.GetSlaState(evaluatedUtc));
}
