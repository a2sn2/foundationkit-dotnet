using FoundationKit.Application.Modules;
using FoundationKit.FeatureManagement;
using FoundationKit.Localization;
using FoundationKit.Settings;
using FoundationKit.Workbench.Contracts;
using FoundationKit.Workbench.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FoundationKit.Workbench.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api")
            .WithTags("Shared platform");

        api.MapGet("/runtime", () => TypedResults.Ok(new RuntimeResponse(
                "local",
                "sql-server",
                "FoundationKitWorkbench",
                "ALHassan ALShami")))
            .WithName("GetWorkbenchRuntime")
            .WithSummary("Returns the shared runtime used by both full-stack portals.")
            .Produces<RuntimeResponse>();

        api.MapGet("/platform-reference", async (
                ISettingReader settings,
                IFeatureEvaluator features,
                SupportedCultureSet supportedCultures,
                CancellationToken cancellationToken) =>
            {
                var cultureSetting = await settings.ResolveAsync(
                    WorkbenchPlatformReference.DefaultCultureSetting,
                    SettingResolutionContext.Global,
                    cancellationToken);
                var timeZoneSetting = await settings.ResolveAsync(
                    WorkbenchPlatformReference.DefaultTimeZoneSetting,
                    SettingResolutionContext.Global,
                    cancellationToken);

                if (cultureSetting is null || timeZoneSetting is null)
                {
                    return Results.Problem(
                        title: "Workbench platform reference is not configured.",
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }

                var culture = supportedCultures.Resolve(cultureSetting.Value);
                var timeZone = new TimeZoneId(timeZoneSetting.Value);
                var feature = await features.EvaluateAsync(
                    new FeatureDefinition(
                        WorkbenchPlatformReference.CatalogPreviewFeature,
                        defaultEnabled: false),
                    FeatureEvaluationContext.Global,
                    cancellationToken);

                return Results.Ok(new PlatformReferenceResponse(
                    culture.Culture.Name,
                    culture.Culture.Direction.ToString(),
                    culture.Source.ToString(),
                    cultureSetting.Scope.ToString(),
                    timeZone.Value,
                    timeZoneSetting.Scope.ToString(),
                    feature.IsEnabled,
                    feature.Source.ToString(),
                    feature.MatchedScope?.ToString()));
            })
            .WithName("GetWorkbenchPlatformReference")
            .WithSummary("Proves the reusable Settings, Feature Management, and Localization capabilities through a live Workbench consumer.")
            .Produces<PlatformReferenceResponse>()
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        api.MapGet("/modules", (IFoundationModuleRegistry registry) =>
                TypedResults.Ok(registry.Describe()))
            .WithName("GetFoundationKitModules")
            .WithSummary("Returns deterministic declared/effective module capability composition evidence.")
            .Produces<IReadOnlyList<FoundationModuleCompositionSnapshot>>();

        api.MapGet("/catalog", async (
                CatalogService catalog,
                CancellationToken cancellationToken) =>
                Results.Json(await catalog.ReadAsync(cancellationToken)))
            .WithName("GetFoundationKitCatalog")
            .WithSummary("Returns the reusable FoundationKit capability catalog.")
            .Produces<CatalogResponse>();

        api.MapGet("/health", async (
                WorkbenchDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                var connected = await dbContext.Database.CanConnectAsync(cancellationToken);
                return connected
                    ? Results.Ok(new HealthResponse("healthy", "sql-server"))
                    : Results.Json(
                        new HealthResponse("unhealthy", "sql-server"),
                        statusCode: StatusCodes.Status503ServiceUnavailable);
            })
            .WithName("GetWorkbenchHealth")
            .WithSummary("Checks the shared API host and SQL Server connection.")
            .Produces<HealthResponse>()
            .Produces<HealthResponse>(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }
}
