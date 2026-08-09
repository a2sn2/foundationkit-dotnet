using FoundationKit.Application.Abstractions;
using FoundationKit.Authorization;
using Madar.Application.Cases;
using Madar.Application.Security;
using Madar.Contracts.Cases;
using Madar.Contracts.Security;
using Madar.Domain.Cases;
using Xunit;

namespace Madar.Tests;

public sealed class CaseSearchTests
{
    [Fact]
    public async Task Search_UnauthenticatedUser_IsRejectedBeforeQueryExecution()
    {
        var currentUser = TestCurrentUser.Anonymous();
        var queryService = new RecordingQueryService();

        var result = await CaseSearchApplication.SearchAsync(
            new CaseSearchRequest(),
            currentUser,
            CreateAuthorization(currentUser),
            queryService,
            new TestClock());

        Assert.True(result.IsFailure);
        Assert.Equal(
            CaseSearchErrors.AuthenticationRequired.Code,
            result.Error.Code);
        Assert.Equal(0, queryService.CallCount);
    }

    [Fact]
    public async Task Search_Operator_UsesOnlyExistingCaseVisibilityScope()
    {
        var userId = Guid.NewGuid();
        var currentUser = TestCurrentUser.Authenticated(MadarRoles.Operator, userId);
        var queryService = new RecordingQueryService();

        var result = await CaseSearchApplication.SearchAsync(
            new CaseSearchRequest(),
            currentUser,
            CreateAuthorization(currentUser),
            queryService,
            new TestClock());

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, queryService.CurrentUserId);
        Assert.False(queryService.ReadAllCases);
        Assert.Equal(1, queryService.CallCount);
    }

    [Fact]
    public async Task Search_Supervisor_PassesReadAllScope()
    {
        var currentUser = TestCurrentUser.Authenticated(MadarRoles.Supervisor);
        var queryService = new RecordingQueryService();

        var result = await CaseSearchApplication.SearchAsync(
            new CaseSearchRequest(),
            currentUser,
            CreateAuthorization(currentUser),
            queryService,
            new TestClock());

        Assert.True(result.IsSuccess);
        Assert.True(queryService.ReadAllCases);
    }

    [Fact]
    public async Task Search_NormalizesBoundedFiltersBeforeQueryExecution()
    {
        var currentUser = TestCurrentUser.Authenticated(MadarRoles.Supervisor);
        var queryService = new RecordingQueryService();
        var departmentId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var from = Utc(1);
        var to = Utc(2);

        var result = await CaseSearchApplication.SearchAsync(
            new CaseSearchRequest(
                "  متابعة عميل  ",
                " OPERATIONAL-INCIDENT ",
                " HIGH ",
                " IN-PROGRESS ",
                " ACTIVE ",
                departmentId,
                assigneeId,
                from,
                to,
                25,
                50),
            currentUser,
            CreateAuthorization(currentUser),
            queryService,
            new TestClock());

        Assert.True(result.IsSuccess);
        var criteria = Assert.IsType<CaseSearchCriteria>(queryService.Criteria);
        Assert.Equal("متابعة عميل", criteria.Query);
        Assert.Equal(CaseTypes.OperationalIncident, criteria.CaseType);
        Assert.Equal(CasePriorities.High, criteria.Priority);
        Assert.Equal(CaseStatuses.InProgress, criteria.Status);
        Assert.Equal(CaseSlaStates.Active, criteria.SlaState);
        Assert.Equal(departmentId, criteria.DepartmentId);
        Assert.Equal(assigneeId, criteria.AssignedToUserId);
        Assert.Equal(from, criteria.CreatedFromUtc);
        Assert.Equal(to, criteria.CreatedToUtc);
        Assert.Equal(25, criteria.Offset);
        Assert.Equal(50, criteria.Limit);
    }

    [Theory]
    [InlineData(-1, 25, "Madar.Search.InvalidOffset")]
    [InlineData(10001, 25, "Madar.Search.InvalidOffset")]
    [InlineData(0, 0, "Madar.Search.InvalidLimit")]
    [InlineData(0, 101, "Madar.Search.InvalidLimit")]
    public async Task Search_InvalidPaging_IsRejected(
        int offset,
        int limit,
        string expectedCode)
    {
        var currentUser = TestCurrentUser.Authenticated(MadarRoles.Supervisor);
        var queryService = new RecordingQueryService();

        var result = await CaseSearchApplication.SearchAsync(
            new CaseSearchRequest(Offset: offset, Limit: limit),
            currentUser,
            CreateAuthorization(currentUser),
            queryService,
            new TestClock());

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
        Assert.Equal(0, queryService.CallCount);
    }

    [Fact]
    public async Task Search_InvalidDateRange_IsRejected()
    {
        var currentUser = TestCurrentUser.Authenticated(MadarRoles.Supervisor);
        var queryService = new RecordingQueryService();

        var result = await CaseSearchApplication.SearchAsync(
            new CaseSearchRequest(
                CreatedFromUtc: Utc(3),
                CreatedToUtc: Utc(2)),
            currentUser,
            CreateAuthorization(currentUser),
            queryService,
            new TestClock());

        Assert.True(result.IsFailure);
        Assert.Equal(CaseSearchErrors.InvalidDateRange.Code, result.Error.Code);
        Assert.Equal(0, queryService.CallCount);
    }

    [Fact]
    public async Task Search_QueryLongerThanPolicy_IsRejected()
    {
        var currentUser = TestCurrentUser.Authenticated(MadarRoles.Supervisor);
        var queryService = new RecordingQueryService();

        var result = await CaseSearchApplication.SearchAsync(
            new CaseSearchRequest(Query: new string('x', 201)),
            currentUser,
            CreateAuthorization(currentUser),
            queryService,
            new TestClock());

        Assert.True(result.IsFailure);
        Assert.Equal(CaseSearchErrors.QueryTooLong.Code, result.Error.Code);
        Assert.Equal(0, queryService.CallCount);
    }

    [Fact]
    public async Task Search_InvalidStatus_IsRejected()
    {
        var currentUser = TestCurrentUser.Authenticated(MadarRoles.Supervisor);
        var queryService = new RecordingQueryService();

        var result = await CaseSearchApplication.SearchAsync(
            new CaseSearchRequest(Status: "unknown"),
            currentUser,
            CreateAuthorization(currentUser),
            queryService,
            new TestClock());

        Assert.True(result.IsFailure);
        Assert.Equal(CaseSearchErrors.InvalidStatus.Code, result.Error.Code);
        Assert.Equal(0, queryService.CallCount);
    }

    private static RolePermissionAuthorizationEvaluator CreateAuthorization(
        TestCurrentUser currentUser) =>
        new(currentUser, MadarPermissions.CreateRolePermissionMap());

    private static DateTimeOffset Utc(int day) =>
        new(2026, 8, day, 9, 0, 0, TimeSpan.Zero);

    private sealed class RecordingQueryService : ICaseSearchQueryService
    {
        public int CallCount { get; private set; }

        public Guid CurrentUserId { get; private set; }

        public bool ReadAllCases { get; private set; }

        public CaseSearchCriteria? Criteria { get; private set; }

        public Task<CaseSearchResponseDto> SearchAsync(
            CaseSearchCriteria criteria,
            Guid currentUserId,
            bool readAllCases,
            DateTimeOffset evaluatedUtc,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            Criteria = criteria;
            CurrentUserId = currentUserId;
            ReadAllCases = readAllCases;
            return Task.FromResult(new CaseSearchResponseDto(
                [],
                0,
                criteria.Offset,
                criteria.Limit,
                new CaseSearchSummaryDto(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)));
        }
    }

    private sealed class TestCurrentUser : ICurrentUser, IAuthorizationSubject
    {
        private readonly HashSet<string> _roles = new(StringComparer.OrdinalIgnoreCase);

        public bool IsAuthenticated { get; private init; }

        public Guid? UserId { get; private init; }

        public string? Email { get; private init; }

        public bool IsInRole(string role) => _roles.Contains(role);

        public static TestCurrentUser Anonymous() => new();

        public static TestCurrentUser Authenticated(string role, Guid? userId = null)
        {
            var user = new TestCurrentUser
            {
                IsAuthenticated = true,
                UserId = userId ?? Guid.NewGuid(),
                Email = "madar-search@example.test"
            };
            user._roles.Add(role);
            return user;
        }
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow { get; } =
            new(2026, 8, 9, 8, 0, 0, TimeSpan.Zero);
    }
}
