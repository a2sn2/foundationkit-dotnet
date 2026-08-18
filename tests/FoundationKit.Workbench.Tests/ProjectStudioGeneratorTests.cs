using FoundationKit.Workbench.Contracts;
using FoundationKit.Workbench.Endpoints;

namespace FoundationKit.Workbench.Tests;

public sealed class ProjectStudioGeneratorTests
{
    private const string ProofRootEnvironmentVariable = "FOUNDATIONKIT_PROJECT_STUDIO_PROOF_ROOT";

    [Fact]
    public async Task Generate_builds_typed_full_stack_project_with_abp_platform_and_business_ui()
    {
        var foundationRoot = ComposerStudioGenerator.ResolveFoundationRoot(AppContext.BaseDirectory, null);
        var workspace = CreateWorkspace("full-stack");
        var generationRoot = Path.Combine(workspace, "generated");

        try
        {
            var result = await ProjectStudioGenerator.GenerateAsync(
                FullProjectRequest("StudioFactoryProof", "standalone"),
                generationRoot,
                foundationRoot);

            Assert.True(result.Generated, result.Error);
            Assert.Equal("source-copy", result.ReferenceMode);
            Assert.True(result.ResolvedFeatureCount >= 10);
            Assert.Contains("Volo.Abp.MultiTenancy", result.AbpPackages);
            Assert.Contains("Volo.Abp.BackgroundJobs", result.AbpPackages);

            var root = Path.Combine(generationRoot, "StudioFactoryProof");
            Assert.True(File.Exists(Path.Combine(root, "foundationkit.studio.json")));
            Assert.True(File.Exists(Path.Combine(root, "STUDIO-COMPOSITION.md")));
            Assert.True(File.Exists(Path.Combine(root, "STUDIO-DATA-MODEL.md")));
            Assert.True(File.Exists(Path.Combine(root, "CUSTOMIZATION.md")));
            Assert.True(File.Exists(Path.Combine(root, "src", "StudioFactoryProof.Api", "GeneratedPlatform", "GeneratedAbpPlatformModule.cs")));
            Assert.True(File.Exists(Path.Combine(root, "src", "StudioFactoryProof.Client", "Pages", "Generated", "PeopleEmployee.razor")));

            var employee = await File.ReadAllTextAsync(Path.Combine(root, "src", "StudioFactoryProof.Domain", "GeneratedModules", "People", "Employee.cs"));
            Assert.Contains("Guid DepartmentId", employee, StringComparison.Ordinal);
            Assert.Contains("decimal Salary", employee, StringComparison.Ordinal);
            Assert.Contains("bool IsActive", employee, StringComparison.Ordinal);
            Assert.Contains("DateOnly StartDate", employee, StringComparison.Ordinal);

            var apiProject = await File.ReadAllTextAsync(Path.Combine(root, "src", "StudioFactoryProof.Api", "StudioFactoryProof.Api.csproj"));
            Assert.Contains("Volo.Abp.MultiTenancy", apiProject, StringComparison.Ordinal);
            Assert.Contains("Volo.Abp.BackgroundJobs", apiProject, StringComparison.Ordinal);

            var program = await File.ReadAllTextAsync(Path.Combine(root, "src", "StudioFactoryProof.Api", "Program.cs"));
            Assert.Contains("AddApplicationAsync", program, StringComparison.Ordinal);
            Assert.Contains("InitializeApplicationAsync", program, StringComparison.Ordinal);
            Assert.Contains("GeneratedCustomization.ConfigureServices", program, StringComparison.Ordinal);

            var page = await File.ReadAllTextAsync(Path.Combine(root, "src", "StudioFactoryProof.Client", "Pages", "Generated", "PeopleEmployee.razor"));
            Assert.Contains("ParseDecimal", page, StringComparison.Ordinal);
            Assert.Contains("Idempotency-Key", page, StringComparison.Ordinal);
            Assert.Contains("If-Match", page, StringComparison.Ordinal);
            Assert.Contains("X-Foundation-Roles", page, StringComparison.Ordinal);

            var relations = await File.ReadAllTextAsync(Path.Combine(root, "src", "StudioFactoryProof.Infrastructure", "GeneratedPlatform", "Migrations", "20260811001000_StudioRelations.cs"));
            Assert.Contains("AddForeignKey", relations, StringComparison.Ordinal);
            Assert.Contains("DepartmentId", relations, StringComparison.Ordinal);
        }
        finally
        {
            CleanupWorkspace(workspace);
        }
    }

    [Fact]
    public async Task Preview_is_non_destructive_and_reports_consumer_files_as_preserved()
    {
        var foundationRoot = ComposerStudioGenerator.ResolveFoundationRoot(AppContext.BaseDirectory, null);
        var workspace = CreateWorkspace("preview");
        var generationRoot = Path.Combine(workspace, "generated");
        var request = FullProjectRequest("StudioPreviewProof", "linked");

        try
        {
            var first = await ProjectStudioGenerator.GenerateAsync(request, generationRoot, foundationRoot);
            Assert.True(first.Generated, first.Error);
            var root = Path.Combine(generationRoot, "StudioPreviewProof");
            var custom = Path.Combine(root, "src", "StudioPreviewProof.Api", "Custom", "ProductRules.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(custom)!);
            await File.WriteAllTextAsync(custom, "namespace StudioPreviewProof.Api.Custom; public sealed class ProductRules { }\n");
            var before = await File.ReadAllTextAsync(custom);

            var preview = await ProjectStudioGenerator.PreviewAsync(request, generationRoot, foundationRoot);

            Assert.True(preview.Valid, preview.Error);
            Assert.True(preview.ConsumerFilesPreserved >= 1);
            Assert.Equal(before, await File.ReadAllTextAsync(custom));
            Assert.DoesNotContain(preview.SampleChanges, change => change.Contains("ProductRules.cs", StringComparison.Ordinal));
        }
        finally
        {
            CleanupWorkspace(workspace);
        }
    }

    [Fact]
    public async Task Regenerate_preserves_custom_and_arbitrary_consumer_files()
    {
        var foundationRoot = ComposerStudioGenerator.ResolveFoundationRoot(AppContext.BaseDirectory, null);
        var workspace = CreateWorkspace("regeneration");
        var generationRoot = Path.Combine(workspace, "generated");
        var request = FullProjectRequest("StudioRegenerationProof", "linked");

        try
        {
            var first = await ProjectStudioGenerator.GenerateAsync(request, generationRoot, foundationRoot);
            Assert.True(first.Generated, first.Error);
            var root = Path.Combine(generationRoot, "StudioRegenerationProof");
            var custom = Path.Combine(root, "src", "StudioRegenerationProof.Api", "Custom", "ProductRules.cs");
            var arbitrary = Path.Combine(root, "PRODUCT-NOTES.md");
            Directory.CreateDirectory(Path.GetDirectoryName(custom)!);
            await File.WriteAllTextAsync(custom, "namespace StudioRegenerationProof.Api.Custom; public sealed class ProductRules { }\n");
            await File.WriteAllTextAsync(arbitrary, "consumer owned\n");

            var changed = request with
            {
                Modules = request.Modules.Select(module => module.Name == "People"
                    ? module with
                    {
                        Resources = module.Resources.Select(resource => resource.Name == "Employee"
                            ? resource with
                            {
                                Fields = resource.Fields.Append(new StudioFieldContract("Notes", "Text", MaximumLength: 400)).ToArray()
                            }
                            : resource).ToArray()
                    }
                    : module).ToArray()
            };

            var regenerated = await ProjectStudioGenerator.GenerateAsync(changed, generationRoot, foundationRoot);

            Assert.True(regenerated.Generated, regenerated.Error);
            Assert.True(regenerated.ConsumerFilesPreserved >= 2);
            Assert.True(File.Exists(custom));
            Assert.Equal("consumer owned\n", await File.ReadAllTextAsync(arbitrary));
            var employee = await File.ReadAllTextAsync(Path.Combine(root, "src", "StudioRegenerationProof.Domain", "GeneratedModules", "People", "Employee.cs"));
            Assert.Contains("Notes", employee, StringComparison.Ordinal);
        }
        finally
        {
            CleanupWorkspace(workspace);
        }
    }

    [Fact]
    public async Task Regenerate_refuses_direct_edits_to_generated_files()
    {
        var foundationRoot = ComposerStudioGenerator.ResolveFoundationRoot(AppContext.BaseDirectory, null);
        var workspace = CreateWorkspace("generated-edit");
        var generationRoot = Path.Combine(workspace, "generated");
        var request = FullProjectRequest("StudioEditProof", "linked");

        try
        {
            var first = await ProjectStudioGenerator.GenerateAsync(request, generationRoot, foundationRoot);
            Assert.True(first.Generated, first.Error);
            var root = Path.Combine(generationRoot, "StudioEditProof");
            var generatedFile = Path.Combine(root, "src", "StudioEditProof.Domain", "GeneratedModules", "People", "Employee.cs");
            await File.AppendAllTextAsync(generatedFile, "// direct consumer edit\n");

            var blocked = await ProjectStudioGenerator.GenerateAsync(request, generationRoot, foundationRoot);

            Assert.False(blocked.Generated);
            Assert.Contains("modified after generation", blocked.Error, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("direct consumer edit", await File.ReadAllTextAsync(generatedFile), StringComparison.Ordinal);
        }
        finally
        {
            CleanupWorkspace(workspace);
        }
    }

    private static StudioProjectRequest FullProjectRequest(string name, string mode) => new(
        1,
        name,
        "minimal",
        mode,
        [
            "security",
            "identity",
            "authorization",
            "auditing",
            "settings",
            "feature-management",
            "localization",
            "caching",
            "observability",
            "http-resilience",
            "multi-tenancy",
            "jobs"
        ],
        [
            new StudioModuleContract(
                "Organization",
                [
                    new StudioResourceContract(
                        "Department",
                        "departments",
                        [
                            new StudioFieldContract("Name", "Text", Required: true, MaximumLength: 160, Indexed: true, Filterable: true, Sortable: true),
                            new StudioFieldContract("Code", "Text", Required: true, MaximumLength: 40, Indexed: true, Unique: true)
                        ])
                ]),
            new StudioModuleContract(
                "People",
                [
                    new StudioResourceContract(
                        "Employee",
                        "employees",
                        [
                            new StudioFieldContract("Name", "Text", Required: true, MaximumLength: 180, Indexed: true, Filterable: true, Sortable: true),
                            new StudioFieldContract("DepartmentId", "Reference", Required: true, Indexed: true, ReferenceResource: "Department"),
                            new StudioFieldContract("Salary", "Decimal", Required: true),
                            new StudioFieldContract("IsActive", "Boolean", Required: true),
                            new StudioFieldContract("StartDate", "Date", Required: true)
                        ])
                ])
        ],
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["identity"] = "abp-oss",
            ["authorization"] = "abp-oss",
            ["settings"] = "abp-oss",
            ["feature-management"] = "abp-oss",
            ["multi-tenancy"] = "abp-oss",
            ["jobs"] = "abp-oss"
        });

    private static string CreateWorkspace(string mode)
    {
        var proofRoot = Environment.GetEnvironmentVariable(ProofRootEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(proofRoot))
            return Path.Combine(Path.GetTempPath(), $"foundationkit-project-studio-{mode}-{Guid.NewGuid():N}");

        var workspace = Path.GetFullPath(Path.Combine(proofRoot, mode));
        if (Directory.Exists(workspace))
            Directory.Delete(workspace, recursive: true);
        Directory.CreateDirectory(workspace);
        return workspace;
    }

    private static void CleanupWorkspace(string workspace)
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ProofRootEnvironmentVariable)))
            return;
        if (Directory.Exists(workspace))
            Directory.Delete(workspace, recursive: true);
    }
}
