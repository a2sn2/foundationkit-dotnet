using FoundationKit.Workbench.Contracts;

namespace FoundationKit.Workbench.Endpoints;

public static class ComposerStudioEndpoints
{
    public static IEndpointRouteBuilder MapComposerStudioEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/studio/catalog", () => Results.Ok(ProjectStudioGenerator.GetCatalog()))
            .WithName("GetFoundationKitProjectStudioCatalog")
            .WithSummary("Returns the visual Project Studio feature/provider catalog, profiles and data-field types.")
            .Produces<StudioCatalogResponse>();

        endpoints.MapPost("/api/studio/preview", async (
                StudioProjectRequest request,
                IConfiguration configuration,
                IHostEnvironment environment,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                var roots = ResolveRoots(configuration, environment);
                var result = await ProjectStudioGenerator.PreviewAsync(
                    request,
                    roots.GenerationRoot,
                    roots.FoundationRoot,
                    cancellationToken);
                return Results.Ok(result);
            })
            .WithName("PreviewFoundationKitProjectStudioComposition")
            .WithSummary("Builds the complete project in an isolated temporary workspace and returns the safe regeneration diff without modifying the target project.")
            .Produces<StudioPreviewResponse>();

        endpoints.MapPost("/api/studio/generate", async (
                StudioProjectRequest request,
                IConfiguration configuration,
                IHostEnvironment environment,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                var roots = ResolveRoots(configuration, environment);
                var result = await ProjectStudioGenerator.GenerateAsync(
                    request,
                    roots.GenerationRoot,
                    roots.FoundationRoot,
                    cancellationToken);
                return Results.Ok(result);
            })
            .WithName("GenerateFoundationKitProjectStudioComposition")
            .WithSummary("Generates or safely regenerates a full FoundationKit project from visual Studio features, providers, typed resources and UI configuration while preserving consumer-owned code.")
            .Produces<StudioProjectGenerationResponse>();

        // Advanced Composer / engineering-proof endpoints remain available independently from Project Studio.
        endpoints.MapPost("/api/composer/validate", (ComposerValidationRequest request) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                return Results.Ok(ComposerStudioValidator.Validate(request.ManifestJson));
            })
            .WithName("ValidateFoundationKitComposerManifest")
            .WithSummary("Validates bounded schema-v2 Studio input through the canonical FoundationKit Composer parser and composition analyzer.")
            .Produces<ComposerValidationResponse>();

        endpoints.MapPost("/api/composer/generate", async (
                ComposerGenerationRequest request,
                IConfiguration configuration,
                IHostEnvironment environment,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);
                var roots = ResolveRoots(configuration, environment);
                var result = await ComposerStudioGenerator.GenerateAsync(
                    request.ManifestJson,
                    roots.GenerationRoot,
                    roots.FoundationRoot,
                    request.Force,
                    request.FoundationMode,
                    cancellationToken);
                return Results.Ok(result);
            })
            .WithName("GenerateFoundationKitComposerProject")
            .WithSummary("Generates a validated schema-v2 FoundationKit project inside the configured local Composer workspace using linked or standalone source-copy Foundation binding.")
            .Produces<ComposerGenerationResponse>();

        return endpoints;
    }

    private static (string GenerationRoot, string FoundationRoot) ResolveRoots(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var foundationRoot = ComposerStudioGenerator.ResolveFoundationRoot(
            environment.ContentRootPath,
            configuration[ComposerStudioGenerator.FoundationRootConfigurationKey]);
        var generationRoot = ComposerStudioGenerator.ResolveGenerationRoot(
            foundationRoot,
            configuration[ComposerStudioGenerator.GenerationRootConfigurationKey]);
        return (generationRoot, foundationRoot);
    }
}
