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

        return endpoints;
    }
}
