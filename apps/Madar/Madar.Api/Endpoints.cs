using FoundationKit.Application.Abstractions;
using FoundationKit.Authorization;
using FoundationKit.WebApi.Results;
using Madar.Api.Security;
using Madar.Application.Cases;
using Madar.Application.Security;
using Madar.Contracts.Cases;
using Madar.Contracts.Security;
using Madar.Infrastructure;
using Madar.Infrastructure.Cases;
using Madar.Infrastructure.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;

namespace Madar.Api;

public static class MadarEndpoints
{
    public static IEndpointRouteBuilder MapMadarEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", () => Results.Ok(new
            {
                status = "healthy",
                service = "madar-api"
            }))
            .WithTags("Health")
            .WithName("MadarLive");

        endpoints.MapGet(
                "/health/ready",
                async (
                    IMadarReadinessProbe readiness,
                    CancellationToken cancellationToken) =>
                {
                    var ready = await readiness.IsReadyAsync(cancellationToken);
                    return ready
                        ? Results.Ok(new
                        {
                            status = "ready",
                            service = "madar-api"
                        })
                        : Results.Problem(
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "ServiceNotReady",
                            detail: "Madar is not ready to serve database-backed requests yet.");
                })
            .WithTags("Health")
            .WithName("MadarReady")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        endpoints.MapGet(
                MadarSecurityRoutes.Antiforgery,
                (IAntiforgery antiforgery, HttpContext context) =>
                {
                    var tokens = antiforgery.GetAndStoreTokens(context);
                    return Results.Ok(new AntiforgeryTokenResponse(
                        tokens.RequestToken
                        ?? throw new InvalidOperationException(
                            "Antiforgery request token was not generated.")));
                })
            .WithTags("Security")
            .WithName("GetMadarAntiforgeryToken");

        MapAuthentication(endpoints);
        MapCases(endpoints);
        MapOperatorDirectory(endpoints);
        return endpoints;
    }

    private static void MapAuthentication(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
                MadarSecurityRoutes.Login,
                LoginAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireRateLimiting("auth")
            .WithTags("Authentication")
            .WithName("LoginMadarUser")
            .Produces<CurrentUserResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        endpoints.MapPost(
                MadarSecurityRoutes.Logout,
                async (SignInManager<MadarUser> signInManager) =>
                {
                    await signInManager.SignOutAsync();
                    return Results.Ok(new ApiMessageResponse(
                        "تم تسجيل الخروج بنجاح."));
                })
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireAuthorization()
            .RequireRateLimiting("write")
            .WithTags("Authentication")
            .WithName("LogoutMadarUser");

        endpoints.MapGet(
                MadarSecurityRoutes.CurrentUser,
                GetCurrentUserAsync)
            .WithTags("Authentication")
            .WithName("GetMadarCurrentUser")
            .Produces<CurrentUserResponse>();
    }

    private static void MapCases(IEndpointRouteBuilder endpoints)
    {
        var cases = endpoints
            .MapGroup(CaseRoutes.Root)
            .RequireAuthorization()
            .WithTags("Cases");

        cases.MapPost(
                "/",
                async (
                    CreateCaseRequest request,
                    ICaseManager manager,
                    CancellationToken cancellationToken) =>
                    (await manager.CreateAsync(request, cancellationToken))
                        .ToHttpResult(value => Results.Created(
                            CaseRoutes.ById(value.Id),
                            value)))
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireRateLimiting("write")
            .WithName("CreateMadarCase")
            .Produces<CaseDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        cases.MapGet(
                "/",
                async (
                    ICaseManager manager,
                    CancellationToken cancellationToken) =>
                    (await manager.ListAsync(cancellationToken))
                        .ToHttpResult(Results.Ok))
            .WithName("ListMadarCases")
            .Produces<IReadOnlyList<CaseDto>>();

        cases.MapGet(
                "/search",
                async (
                    string? query,
                    string? caseType,
                    string? priority,
                    string? status,
                    string? slaState,
                    Guid? departmentId,
                    Guid? assignedToUserId,
                    DateTimeOffset? createdFromUtc,
                    DateTimeOffset? createdToUtc,
                    int? offset,
                    int? limit,
                    ICurrentUser currentUser,
                    IAuthorizationEvaluator authorization,
                    CaseQueryService queryService,
                    IClock clock,
                    CancellationToken cancellationToken) =>
                    (await CaseSearchApplication.SearchAsync(
                        new CaseSearchRequest(
                            query,
                            caseType,
                            priority,
                            status,
                            slaState,
                            departmentId,
                            assignedToUserId,
                            createdFromUtc,
                            createdToUtc,
                            offset ?? 0,
                            limit ?? CaseSearchApplication.DefaultLimit),
                        currentUser,
                        authorization,
                        queryService,
                        clock,
                        cancellationToken))
                    .ToHttpResult(Results.Ok))
            .WithName("SearchMadarCases")
            .Produces<CaseSearchResponseDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        cases.MapPost(
                "/sla/evaluate",
                async (
                    EvaluateCaseSlaRequest request,
                    ICaseSlaManager manager,
                    CancellationToken cancellationToken) =>
                    (await manager.EvaluateAsync(request, cancellationToken))
                        .ToHttpResult(Results.Ok))
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireRateLimiting("write")
            .WithName("EvaluateMadarCaseSla")
            .Produces<CaseSlaEvaluationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        cases.MapGet(
                "/{caseId:guid}",
                async (
                    Guid caseId,
                    ICaseManager manager,
                    CancellationToken cancellationToken) =>
                    (await manager.GetAsync(caseId, cancellationToken))
                        .ToHttpResult(Results.Ok))
            .WithName("GetMadarCase")
            .Produces<CaseDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        cases.MapGet(
                "/{caseId:guid}/timeline",
                async (
                    Guid caseId,
                    ICaseTimelineService timelineService,
                    CancellationToken cancellationToken) =>
                    (await timelineService.GetAsync(caseId, cancellationToken))
                        .ToHttpResult(Results.Ok))
            .WithName("GetMadarCaseTimeline")
            .Produces<IReadOnlyList<CaseTimelineEntryDto>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        cases.MapPost(
                "/{caseId:guid}/assignment",
                async (
                    Guid caseId,
                    AssignCaseRequest request,
                    ICaseManager manager,
                    CancellationToken cancellationToken) =>
                    (await manager.AssignAsync(
                        caseId,
                        request,
                        cancellationToken))
                    .ToHttpResult(Results.Ok))
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireRateLimiting("write")
            .WithName("AssignMadarCase")
            .Produces<CaseDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        cases.MapPost(
                "/{caseId:guid}/transition",
                async (
                    Guid caseId,
                    TransitionCaseRequest request,
                    ICaseManager manager,
                    CancellationToken cancellationToken) =>
                    (await manager.TransitionAsync(
                        caseId,
                        request,
                        cancellationToken))
                    .ToHttpResult(Results.Ok))
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireRateLimiting("write")
            .WithName("TransitionMadarCase")
            .Produces<CaseDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapOperatorDirectory(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                MadarSecurityRoutes.Operators,
                async (
                    UserManager<MadarUser> userManager,
                    IAuthorizationEvaluator authorization) =>
                {
                    if (!authorization.HasPermission(MadarPermissions.AssignCases))
                    {
                        return Results.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Madar.OperatorDirectoryForbidden",
                            detail: "لا تملك صلاحية عرض قائمة الموظفين المتاحين للإسناد.");
                    }

                    var users = await userManager.GetUsersInRoleAsync(
                        MadarRoles.Operator);
                    var response = users
                        .OrderBy(user => user.DisplayName, StringComparer.OrdinalIgnoreCase)
                        .Select(user => new OperatorOptionDto(
                            user.Id,
                            user.DisplayName,
                            user.Email ?? string.Empty))
                        .ToArray();

                    return Results.Ok(response);
                })
            .RequireAuthorization()
            .WithTags("Users")
            .WithName("ListMadarOperators")
            .Produces<IReadOnlyList<OperatorOptionDto>>()
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        UserManager<MadarUser> userManager,
        SignInManager<MadarUser> signInManager)
    {
        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Email)] = ["البريد الإلكتروني وكلمة المرور مطلوبان."]
            });
        }

        var email = request.Email.Trim();
        var result = await signInManager.PasswordSignInAsync(
            email,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: result.IsLockedOut
                    ? "AccountLocked"
                    : "InvalidCredentials",
                detail: result.IsLockedOut
                    ? "تم قفل الحساب مؤقتًا بسبب محاولات تسجيل دخول متكررة."
                    : "البريد الإلكتروني أو كلمة المرور غير صحيحة.");
        }

        var user = await userManager.FindByEmailAsync(email)
            ?? throw new InvalidOperationException(
                "Authenticated Madar user was not found.");

        return Results.Ok(await ToCurrentUserAsync(user, userManager));
    }

    private static async Task<CurrentUserResponse> GetCurrentUserAsync(
        ICurrentUser currentUser,
        UserManager<MadarUser> userManager)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return new CurrentUserResponse(false, null, null, null, []);

        var user = await userManager.FindByIdAsync(
            currentUser.UserId.Value.ToString("D"));
        return user is null
            ? new CurrentUserResponse(false, null, null, null, [])
            : await ToCurrentUserAsync(user, userManager);
    }

    private static async Task<CurrentUserResponse> ToCurrentUserAsync(
        MadarUser user,
        UserManager<MadarUser> userManager)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new CurrentUserResponse(
            true,
            user.Id,
            user.Email,
            user.DisplayName,
            roles.ToArray());
    }
}
