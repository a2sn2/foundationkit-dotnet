using System.Security;
using System.Text.Json;
using FoundationKit.Application.Capabilities;

namespace FoundationKit.Composer;

internal static class ComposerBlazorRuntimeGenerator
{
    private const string AspNetCoreVersion = "10.0.10";
    private const string WebAssemblyPackage = "Microsoft.AspNetCore.Components.WebAssembly";
    private const string WebAssemblyDevServerPackage = "Microsoft.AspNetCore.Components.WebAssembly.DevServer";
    private const string LocalCorsPolicy = "GeneratedLocalClient";

    public static void Apply(
        CompositionAnalysis analysis,
        string outputDirectory,
        string projectPrefix,
        SortedDictionary<string, string> overlay)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPrefix);
        ArgumentNullException.ThrowIfNull(overlay);

        var hasBlazor = analysis.Entries.Any(entry =>
            entry.Capability.Id.Equals(FoundationCapabilityIds.Blazor, StringComparison.OrdinalIgnoreCase));
        if (!hasBlazor)
            return;

        var clientProjectRelativePath = $"src/{projectPrefix}.Client/{projectPrefix}.Client.csproj";
        var clientProjectPath = Path.Combine(outputDirectory, ToPlatformPath(clientProjectRelativePath));
        if (!File.Exists(clientProjectPath))
        {
            throw new ComposerGenerationException(
                "Blazor capability resolved but the generated Client project was not found.");
        }

        var hasExecutableRuntime = analysis.Manifest.ProjectModel?.Resources.Any(resource => resource.IsExecutable) == true;
        var ports = StableLocalPorts(projectPrefix);

        AddWebAssemblyPackageVersions(overlay, outputDirectory);
        overlay[clientProjectRelativePath] = BuildRunnableClientProject(File.ReadAllText(clientProjectPath));
        AddClientFiles(
            overlay,
            analysis.Manifest,
            projectPrefix,
            ports.ApiPort,
            ports.ClientPort,
            hasExecutableRuntime);

        if (hasExecutableRuntime)
            EnableLocalRuntimeConnectivity(overlay, outputDirectory, projectPrefix, ports.ApiPort);
    }

    private static void AddWebAssemblyPackageVersions(
        SortedDictionary<string, string> overlay,
        string outputDirectory)
    {
        const string relativePath = "Directory.Packages.props";
        var packages = ReadOverlayOrFile(overlay, outputDirectory, relativePath);
        packages = EnsurePackageVersion(packages, WebAssemblyPackage, AspNetCoreVersion);
        packages = EnsurePackageVersion(packages, WebAssemblyDevServerPackage, AspNetCoreVersion);
        overlay[relativePath] = packages;
    }

    private static string EnsurePackageVersion(string packages, string packageId, string version)
    {
        if (packages.Contains($"Include=\"{packageId}\"", StringComparison.Ordinal))
            return packages;

        const string itemGroupEnd = "  </ItemGroup>";
        var index = packages.IndexOf(itemGroupEnd, StringComparison.Ordinal);
        if (index < 0)
            throw new ComposerGenerationException("Generated central package file has an unexpected shape.");

        return packages.Insert(
            index,
            $"    <PackageVersion Include=\"{packageId}\" Version=\"{version}\" />\n");
    }

    private static string BuildRunnableClientProject(string source)
    {
        var project = NormalizeLineEndings(source)
            .Replace(
                "<Project Sdk=\"Microsoft.NET.Sdk.Razor\">",
                "<Project Sdk=\"Microsoft.NET.Sdk.BlazorWebAssembly\">",
                StringComparison.Ordinal);

        const string frameworkReference =
            "  <ItemGroup>\n" +
            "    <FrameworkReference Include=\"Microsoft.AspNetCore.App\" />\n" +
            "  </ItemGroup>\n";
        project = project.Replace(frameworkReference, string.Empty, StringComparison.Ordinal);

        if (!project.Contains($"PackageReference Include=\"{WebAssemblyPackage}\"", StringComparison.Ordinal))
        {
            const string projectEnd = "</Project>";
            var index = project.LastIndexOf(projectEnd, StringComparison.Ordinal);
            if (index < 0)
                throw new ComposerGenerationException("Generated Client project has an unexpected shape.");

            var insertion = $$"""
              <ItemGroup>
                <PackageReference Include="{{WebAssemblyPackage}}" />
                <PackageReference Include="{{WebAssemblyDevServerPackage}}">
                  <PrivateAssets>all</PrivateAssets>
                </PackageReference>
              </ItemGroup>
            """ + "\n";
            project = project.Insert(index, insertion);
        }

        if (!project.Contains("Microsoft.NET.Sdk.BlazorWebAssembly", StringComparison.Ordinal) ||
            project.Contains("FrameworkReference Include=\"Microsoft.AspNetCore.App\"", StringComparison.Ordinal))
        {
            throw new ComposerGenerationException("Could not convert the generated Client project to Blazor WebAssembly.");
        }

        return project;
    }

    private static void AddClientFiles(
        SortedDictionary<string, string> overlay,
        ComposerManifest manifest,
        string projectPrefix,
        int apiPort,
        int clientPort,
        bool hasExecutableRuntime)
    {
        var root = $"src/{projectPrefix}.Client";
        var productName = SecurityElement.Escape(manifest.Name) ?? manifest.Name;
        var apiBaseUrl = $"http://localhost:{apiPort}/";
        var runtimeStatementEn = hasExecutableRuntime
            ? "The generated client probes the live health endpoint and runtime OpenAPI contract without duplicating backend business rules."
            : "This manifest does not declare executable resources, so the generated client is a runnable presentation shell only.";
        var runtimeStatementAr = hasExecutableRuntime
            ? "تفحص الواجهة المولدة Health والعقد الفعلي Runtime OpenAPI دون تكرار قواعد الأعمال الخلفية."
            : "لا يحتوي هذا الـManifest على موارد تنفيذية، لذلك الواجهة المولدة هنا هي غلاف عرض قابل للتشغيل فقط.";

        overlay[$"{root}/Program.cs"] = $$"""
            using {{projectPrefix}}.Client;
            using {{projectPrefix}}.Client.Api;
            using Microsoft.AspNetCore.Components.Web;
            using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            var configuredBaseUrl = builder.Configuration["FoundationApiBaseUrl"];
            var apiBaseUrl = string.IsNullOrWhiteSpace(configuredBaseUrl)
                ? builder.HostEnvironment.BaseAddress
                : configuredBaseUrl;
            builder.Services.AddScoped(_ => new HttpClient
            {
                BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute)
            });
            builder.Services.AddScoped<GeneratedApiProbe>();

            await builder.Build().RunAsync();
            """;

        overlay[$"{root}/App.razor"] = """
            <Router AppAssembly="@typeof(App).Assembly">
                <Found Context="routeData">
                    <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
                    <FocusOnNavigate RouteData="@routeData" Selector="h1" />
                </Found>
                <NotFound>
                    <LayoutView Layout="@typeof(MainLayout)">
                        <FkEmptyState Title="Page not found · الصفحة غير موجودة"
                                      Description="The requested route does not exist in this generated application. · المسار المطلوب غير موجود في التطبيق المولد." />
                    </LayoutView>
                </NotFound>
            </Router>
            """;

        overlay[$"{root}/_Imports.razor"] = $$"""
            @using System.Net.Http
            @using Microsoft.AspNetCore.Components
            @using Microsoft.AspNetCore.Components.Routing
            @using Microsoft.AspNetCore.Components.Web
            @using FoundationKit.Blazor.Components
            @using {{projectPrefix}}.Client
            @using {{projectPrefix}}.Client.Api
            @using {{projectPrefix}}.Client.Layout
            """;

        overlay[$"{root}/Layout/MainLayout.razor"] = $$"""
            @inherits LayoutComponentBase

            <FkAppShell Direction="auto"
                        Language="@_language"
                        LanguageChanged="SetLanguage"
                        NavigationLabel="@T("Application navigation", "التنقل في التطبيق")"
                        DarkModeLabel="@T("Switch to dark mode", "التبديل إلى الوضع الداكن")"
                        LightModeLabel="@T("Switch to light mode", "التبديل إلى الوضع الفاتح")">
                <Brand>
                    <FkBrandMark Name="{{productName}}" Tagline="FoundationKit · Soft Orbit" Href="/" />
                </Brand>
                <Navigation>
                    <FkNavItem Href="/" Match="NavLinkMatch.All">
                        <Icon><span aria-hidden="true">⌂</span></Icon>
                        <ChildContent>@T("Overview", "نظرة عامة")</ChildContent>
                    </FkNavItem>
                    <FkNavItem Href="runtime">
                        <Icon><span aria-hidden="true">◇</span></Icon>
                        <ChildContent>@T("Runtime", "التشغيل")</ChildContent>
                    </FkNavItem>
                </Navigation>
                <TopbarStart>
                    <button class="fk-command" type="button" aria-label="Open command palette placeholder">
                        <span>@T("Search or command", "ابحث أو نفّذ أمرًا")</span><kbd>Ctrl K</kbd>
                    </button>
                </TopbarStart>
                <TopbarActions>
                    <FkBadge Tone="FoundationBadgeTone.Aqua">@T("Generated app", "تطبيق مولد")</FkBadge>
                </TopbarActions>
                <ChildContent>
                    <CascadingValue Value="@_language" Name="FoundationLanguage">
                        @Body
                    </CascadingValue>
                </ChildContent>
            </FkAppShell>

            @code {
                private string _language = "en";
                private string T(string en, string ar) => _language == "ar" ? ar : en;
                private void SetLanguage(string language) => _language = language == "ar" ? "ar" : "en";
            }
            """;

        overlay[$"{root}/Pages/Home.razor"] = $$"""
            @page "/"

            <PageTitle>{{productName}}</PageTitle>

            <FkPageHeader Eyebrow="FOUNDATIONKIT GENERATED APP"
                          Title="@T("A runnable product shell, not a component placeholder.", "واجهة مشروع قابلة للتشغيل، وليست Component تجريبيًا.")"
                          Description="@T({{JsonSerializer.Serialize(runtimeStatementEn)}}, {{JsonSerializer.Serialize(runtimeStatementAr)}})" />

            <div class="fk-design-grid">
                <div class="fk-col-8">
                    <FkCard>
                        <div class="fk-stack">
                            <div class="fk-row fk-wrap">
                                <FkBadge Tone="FoundationBadgeTone.Primary">Blazor WASM</FkBadge>
                                <FkBadge Tone="FoundationBadgeTone.Aqua">Soft Orbit</FkBadge>
                                <FkBadge Tone="FoundationBadgeTone.Success">AR / EN · RTL / LTR</FkBadge>
                            </div>
                            <h2>@T("The generated Client is now an executable application.", "أصبح الـClient المولد تطبيقًا تنفيذيًا كاملًا.")</h2>
                            <p class="fk-muted">
                                @T("FoundationKit.Blazor supplies the shared design system. Runtime API behavior and authorization remain authoritative on the backend.",
                                   "يوفر FoundationKit.Blazor نظام التصميم المشترك، بينما تبقى صلاحيات وسلوك الـAPI الفعلي مرجعها الخلفية.")
                            </p>
                            <div class="fk-row fk-wrap">
                                <FkButton Href="runtime">@T("Inspect runtime", "افحص التشغيل")</FkButton>
                                <FkButton Variant="FoundationButtonVariant.Secondary" Href="/runtime">Runtime OpenAPI</FkButton>
                            </div>
                        </div>
                    </FkCard>
                </div>
                <div class="fk-col-4">
                    <FkCard Variant="FoundationCardVariant.Muted">
                        <span class="fk-caption">LOCAL API</span>
                        <p class="fk-mono generated-client-type">{{apiBaseUrl}}</p>
                    </FkCard>
                </div>
            </div>

            <section class="generated-orbit" aria-label="FoundationKit generation flow">
                <div class="fk-orbit-stage" aria-hidden="true">
                    <div class="fk-orbit-stage__ring"></div>
                    <div class="fk-orbit-stage__ring fk-orbit-stage__ring--inner"></div>
                    <div class="fk-orbit-stage__core"></div>
                    <span class="fk-orbit-stage__satellite fk-orbit-stage__satellite--one">API</span>
                    <span class="fk-orbit-stage__satellite fk-orbit-stage__satellite--two">SQL</span>
                    <span class="fk-orbit-stage__satellite fk-orbit-stage__satellite--three">UI</span>
                </div>
            </section>

            @code {
                [CascadingParameter(Name = "FoundationLanguage")]
                private string Language { get; set; } = "en";
                private string T(string en, string ar) => Language == "ar" ? ar : en;
            }
            """;

        overlay[$"{root}/Pages/Runtime.razor"] = BuildRuntimePage(hasExecutableRuntime);
        overlay[$"{root}/Api/GeneratedApiProbe.cs"] = BuildApiProbe(projectPrefix, hasExecutableRuntime);

        overlay[$"{root}/wwwroot/index.html"] = $$"""
            <!DOCTYPE html>
            <html lang="en" dir="ltr">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <meta name="theme-color" content="#F7F8FC" />
                <base href="/" />
                <title>{{productName}}</title>
                <link href="_content/FoundationKit.Blazor/foundationkit.css" rel="stylesheet" />
                <link href="css/app.css" rel="stylesheet" />
            </head>
            <body class="fk-app">
                <div id="app">
                    <div class="fk-loading" role="status" aria-live="polite">
                        <div class="fk-loading__visual" aria-hidden="true">
                            <span class="fk-orbit-path fk-orbit-path--one"></span>
                            <span class="fk-orbit-path fk-orbit-path--two"></span>
                            <span class="fk-orbit-node fk-loading__pulse"></span>
                            <span class="fk-orbit-node fk-orbit-node--aqua fk-loading__pulse"></span>
                            <span class="fk-orbit-node fk-orbit-node--warm fk-loading__pulse"></span>
                        </div>
                        <h2>Preparing {{productName}}</h2>
                        <p>Connecting the generated app and FoundationKit Soft Orbit baseline…</p>
                    </div>
                </div>
                <div id="blazor-error-ui" class="generated-error">
                    An unexpected error occurred. <a href="" class="reload">Reload</a>
                </div>
                <script src="_content/FoundationKit.Blazor/foundationkit.js"></script>
                <script src="_framework/blazor.webassembly.js"></script>
            </body>
            </html>
            """;

        overlay[$"{root}/wwwroot/css/app.css"] = """
            .generated-client-type { overflow-wrap: anywhere; margin-bottom: 0; }
            .generated-orbit { min-height: 24rem; display: grid; place-items: center; padding-block: var(--fk-space-12); }
            .runtime-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: var(--fk-space-4); }
            .runtime-value { margin: .35rem 0 0; font-size: 1.05rem; font-weight: 650; overflow-wrap: anywhere; }
            .runtime-error { padding: var(--fk-space-4); border: 1px solid color-mix(in srgb, var(--fk-color-danger) 28%, var(--fk-border-default)); border-radius: var(--fk-radius-control); background: var(--fk-color-danger-soft); color: var(--fk-color-danger); }
            .generated-error { display: none; position: fixed; inset-inline: var(--fk-space-4); bottom: var(--fk-space-4); z-index: 1000; padding: var(--fk-space-4); border: 1px solid color-mix(in srgb, var(--fk-color-danger) 30%, var(--fk-border-default)); border-radius: var(--fk-radius-control); background: var(--fk-color-danger-soft); color: var(--fk-color-danger); box-shadow: var(--fk-shadow-md); }
            @media (max-width: 760px) { .runtime-grid { grid-template-columns: 1fr; } }
            """;

        overlay[$"{root}/wwwroot/appsettings.json"] = $$"""
            {
              "FoundationApiBaseUrl": "{{apiBaseUrl}}"
            }
            """;

        overlay[$"{root}/Properties/launchSettings.json"] = $$"""
            {
              "$schema": "http://json.schemastore.org/launchsettings.json",
              "profiles": {
                "http": {
                  "commandName": "Project",
                  "launchBrowser": true,
                  "applicationUrl": "http://localhost:{{clientPort}}",
                  "environmentVariables": {
                    "ASPNETCORE_ENVIRONMENT": "Development"
                  }
                }
              }
            }
            """;

        overlay["GENERATED-FRONTEND.md"] = $$"""
            # {{manifest.Name}} generated frontend

            The `{{projectPrefix}}.Client` project is a runnable .NET 10 Blazor WebAssembly application using the shared `FoundationKit.Blazor` Soft Orbit design system.

            Local development endpoints are deterministic for this generated project:

            - API: `http://localhost:{{apiPort}}/`
            - Client: `http://localhost:{{clientPort}}/`

            The client probes the backend health endpoint and, when executable resources exist, reads `/swagger/v1/swagger.json` as the runtime transport source of truth. It deliberately does not recreate authorization rules, relational joins, or product business logic in browser code.

            Run the API and Client as separate startup projects. Development CORS is restricted to loopback origins and is emitted only for the generated local runtime proof.
            """;
    }

    private static string BuildRuntimePage(bool hasExecutableRuntime)
    {
        if (!hasExecutableRuntime)
        {
            return """
                @page "/runtime"

                <PageTitle>Runtime</PageTitle>
                <FkPageHeader Eyebrow="RUNTIME"
                              Title="@T("Presentation shell ready", "غلاف العرض جاهز")"
                              Description="@T("No executable resources were declared in this manifest, so no runtime API probe is required.", "لا توجد موارد تنفيذية في هذا الـManifest، لذلك لا يلزم فحص API فعلي.")" />
                <FkEmptyState Title="No executable runtime · لا يوجد Runtime تنفيذي"
                              Description="Add executable resource fields to the Composer v2 manifest when a live generated API is required." />

                @code {
                    [CascadingParameter(Name = "FoundationLanguage")]
                    private string Language { get; set; } = "en";
                    private string T(string en, string ar) => Language == "ar" ? ar : en;
                }
                """;
        }

        return """
            @page "/runtime"
            @inject GeneratedApiProbe Api

            <PageTitle>Runtime</PageTitle>

            <FkPageHeader Eyebrow="RUNTIME OPENAPI"
                          Title="@T("Live generated runtime proof", "إثبات التشغيل الفعلي للمشروع المولد")"
                          Description="@T("Health and OpenAPI are read from the running generated API. Backend policy remains authoritative.", "يتم قراءة Health وOpenAPI من الـAPI المولد أثناء التشغيل، وتبقى سياسات الخلفية هي المرجع.")">
                <Actions>
                    <FkButton OnClick="RefreshAsync" Loading="@_loading">@T("Refresh", "تحديث")</FkButton>
                </Actions>
            </FkPageHeader>

            @if (!string.IsNullOrWhiteSpace(_error))
            {
                <div class="runtime-error" role="alert">@_error</div>
            }

            <div class="runtime-grid">
                <FkCard>
                    <span class="fk-caption">STATUS</span>
                    <p class="runtime-value">@(_health?.Status ?? "—")</p>
                </FkCard>
                <FkCard>
                    <span class="fk-caption">PROJECT ID</span>
                    <p class="runtime-value fk-mono">@(_health?.ProjectId ?? "—")</p>
                </FkCard>
                <FkCard>
                    <span class="fk-caption">OPENAPI PATHS</span>
                    <p class="runtime-value">@(_openApiPaths?.ToString() ?? "—")</p>
                </FkCard>
            </div>

            @code {
                [CascadingParameter(Name = "FoundationLanguage")]
                private string Language { get; set; } = "en";

                private GeneratedHealthResponse? _health;
                private int? _openApiPaths;
                private string? _error;
                private bool _loading;

                protected override Task OnInitializedAsync() => RefreshAsync();

                private async Task RefreshAsync()
                {
                    _loading = true;
                    _error = null;
                    try
                    {
                        _health = await Api.GetHealthAsync();
                        _openApiPaths = await Api.GetOpenApiPathCountAsync();
                    }
                    catch (Exception exception)
                    {
                        _error = T(
                            $"Could not reach the generated API: {exception.Message}",
                            $"تعذر الوصول إلى الـAPI المولد: {exception.Message}");
                    }
                    finally
                    {
                        _loading = false;
                    }
                }

                private string T(string en, string ar) => Language == "ar" ? ar : en;
            }
            """;
    }

    private static string BuildApiProbe(string projectPrefix, bool hasExecutableRuntime)
    {
        if (!hasExecutableRuntime)
        {
            return $$"""
                namespace {{projectPrefix}}.Client.Api;

                public sealed class GeneratedApiProbe
                {
                }
                """;
        }

        return $$"""
            #nullable enable

            using System.Net.Http.Json;
            using System.Text.Json;

            namespace {{projectPrefix}}.Client.Api;

            public sealed class GeneratedApiProbe(HttpClient httpClient)
            {
                private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

                public Task<GeneratedHealthResponse?> GetHealthAsync(CancellationToken cancellationToken = default) =>
                    _httpClient.GetFromJsonAsync<GeneratedHealthResponse>("api/foundationkit/health", cancellationToken);

                public async Task<int> GetOpenApiPathCountAsync(CancellationToken cancellationToken = default)
                {
                    await using var stream = await _httpClient.GetStreamAsync("swagger/v1/swagger.json", cancellationToken);
                    using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                    return document.RootElement.TryGetProperty("paths", out var paths) && paths.ValueKind == JsonValueKind.Object
                        ? paths.EnumerateObject().Count()
                        : 0;
                }
            }

            public sealed record GeneratedHealthResponse(
                string Status,
                string ProjectId,
                string DatabaseNamespace);
            """;
    }

    private static void EnableLocalRuntimeConnectivity(
        SortedDictionary<string, string> overlay,
        string outputDirectory,
        string projectPrefix,
        int apiPort)
    {
        var apiProgramRelativePath = $"src/{projectPrefix}.Api/Program.cs";
        var program = ReadOverlayOrFile(overlay, outputDirectory, apiProgramRelativePath);

        const string serviceNeedle = "builder.Services.AddEndpointsApiExplorer();";
        if (!program.Contains(LocalCorsPolicy, StringComparison.Ordinal))
        {
            if (!program.Contains(serviceNeedle, StringComparison.Ordinal))
                throw new ComposerGenerationException("Generated API Program.cs has an unexpected service-registration shape.");

            var serviceReplacement = $$"""
                {{serviceNeedle}}
                if (builder.Environment.IsDevelopment())
                {
                    builder.Services.AddCors(options =>
                    {
                        options.AddPolicy("{{LocalCorsPolicy}}", policy =>
                        {
                            policy
                                .SetIsOriginAllowed(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.IsLoopback)
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                        });
                    });
                }
                """;
            program = program.Replace(serviceNeedle, serviceReplacement, StringComparison.Ordinal);

            const string pipelineNeedle = "app.UseFoundationRequestDiagnostics();";
            if (!program.Contains(pipelineNeedle, StringComparison.Ordinal))
                throw new ComposerGenerationException("Generated API Program.cs has an unexpected middleware shape.");

            var pipelineReplacement = $$"""
                {{pipelineNeedle}}
                if (app.Environment.IsDevelopment())
                    app.UseCors("{{LocalCorsPolicy}}");
                """;
            program = program.Replace(pipelineNeedle, pipelineReplacement, StringComparison.Ordinal);
        }
        overlay[apiProgramRelativePath] = program;

        overlay[$"src/{projectPrefix}.Api/Properties/launchSettings.json"] = $$"""
            {
              "$schema": "http://json.schemastore.org/launchsettings.json",
              "profiles": {
                "http": {
                  "commandName": "Project",
                  "launchBrowser": true,
                  "launchUrl": "swagger",
                  "applicationUrl": "http://localhost:{{apiPort}}",
                  "environmentVariables": {
                    "ASPNETCORE_ENVIRONMENT": "Development"
                  }
                }
              }
            }
            """;
    }

    private static string ReadOverlayOrFile(
        SortedDictionary<string, string> overlay,
        string outputDirectory,
        string relativePath)
    {
        if (overlay.TryGetValue(relativePath, out var value))
            return value;

        var path = Path.Combine(outputDirectory, ToPlatformPath(relativePath));
        if (!File.Exists(path))
            throw new ComposerGenerationException($"Generated file was not found: {relativePath}");
        return File.ReadAllText(path);
    }

    private static (int ApiPort, int ClientPort) StableLocalPorts(string projectPrefix)
    {
        var seed = 17;
        foreach (var character in projectPrefix)
            seed = ((seed * 31) + character) % 400;
        return (5200 + seed, 7200 + seed);
    }

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal);

    private static string ToPlatformPath(string value) =>
        value.Replace('/', Path.DirectorySeparatorChar);
}
