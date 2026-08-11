using FoundationKit.Workbench.Contracts;

namespace FoundationKit.Workbench.Endpoints;

public static class ComposerStudioEndpoints
{
    public static IEndpointRouteBuilder MapComposerStudioEndpoints(this IEndpointRouteBuilder endpoints)
    {
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

                var foundationRoot = ComposerStudioGenerator.ResolveFoundationRoot(
                    environment.ContentRootPath,
                    configuration[ComposerStudioGenerator.FoundationRootConfigurationKey]);
                var generationRoot = ComposerStudioGenerator.ResolveGenerationRoot(
                    foundationRoot,
                    configuration[ComposerStudioGenerator.GenerationRootConfigurationKey]);

                var result = await ComposerStudioGenerator.GenerateAsync(
                    request.ManifestJson,
                    generationRoot,
                    foundationRoot,
                    request.Force,
                    cancellationToken);
                return Results.Ok(result);
            })
            .WithName("GenerateFoundationKitComposerProject")
            .WithSummary("Generates a validated schema-v2 FoundationKit project inside the configured local Studio workspace.")
            .Produces<ComposerGenerationResponse>();

        return endpoints;
    }
}
