#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
from pathlib import Path

IDENTIFIER = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*$")
DOTTED_IDENTIFIER = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*$")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate a deterministic Blazor WebAssembly reference app from runtime OpenAPI."
    )
    parser.add_argument("openapi", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--app-name", default="FoundationGeneratedClient")
    parser.add_argument("--namespace", dest="namespace_name", default=None)
    parser.add_argument("--client-class", default="GeneratedApiClient")
    parser.add_argument("--foundation-root", type=Path, required=True)
    parser.add_argument("--check", action="store_true")
    return parser.parse_args()


def require_identifier(value: str, label: str) -> str:
    if not IDENTIFIER.fullmatch(value):
        raise SystemExit(f"{label} must be a safe C# identifier: {value!r}")
    return value


def require_namespace(value: str) -> str:
    if not DOTTED_IDENTIFIER.fullmatch(value):
        raise SystemExit(f"namespace must be a safe dotted C# identifier: {value!r}")
    return value


def load_openapi(path: Path) -> dict:
    try:
        document = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        raise SystemExit(f"Could not read OpenAPI document: {error}") from error
    if not isinstance(document, dict) or not str(document.get("openapi", "")).startswith("3."):
        raise SystemExit("Expected an OpenAPI 3.x document.")
    if not isinstance(document.get("paths"), dict):
        raise SystemExit("OpenAPI document must contain a paths object.")
    return document


def write_or_check(path: Path, content: str, check: bool) -> None:
    normalized = content.rstrip() + "\n"
    if check:
        if not path.is_file():
            raise SystemExit(f"Generated frontend drift: missing {path}")
        current = path.read_text(encoding="utf-8")
        if current != normalized:
            raise SystemExit(f"Generated frontend drift: {path}")
        return
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(normalized, encoding="utf-8", newline="\n")


def relative_project_reference(output: Path, foundation_root: Path) -> str:
    project = foundation_root.resolve() / "src" / "FoundationKit.Blazor" / "FoundationKit.Blazor.csproj"
    if not project.is_file():
        raise SystemExit(f"FoundationKit.Blazor project was not found under {foundation_root}")
    return Path(os.path.relpath(project, output.resolve())).as_posix()


def generate_client(
    script_dir: Path,
    openapi: Path,
    client_path: Path,
    namespace_name: str,
    client_class: str,
    check: bool,
) -> None:
    command = [
        sys.executable,
        str(script_dir / "generate-csharp-client-from-openapi.py"),
        str(openapi),
        str(client_path),
        "--namespace",
        namespace_name,
        "--class-name",
        client_class,
    ]
    if check:
        command.append("--check")
    subprocess.run(command, check=True)


def build_files(app_name: str, namespace_name: str, client_class: str, project_reference: str) -> dict[Path, str]:
    return {
        Path(f"{app_name}.csproj"): f"""
<Project Sdk=\"Microsoft.NET.Sdk.BlazorWebAssembly\">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>10.0-recommended</AnalysisLevel>
    <RootNamespace>{namespace_name}</RootNamespace>
    <AssemblyName>{app_name}</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include=\"{project_reference}\" />
    <PackageReference Include=\"Microsoft.AspNetCore.Components.WebAssembly\" />
  </ItemGroup>
</Project>
""",
        Path("Program.cs"): f"""
using {namespace_name};
using {namespace_name}.Api;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>(\"#app\");
builder.RootComponents.Add<HeadOutlet>(\"head::after\");

var configuredBaseUrl = builder.Configuration[\"FoundationApiBaseUrl\"];
var apiBaseUrl = string.IsNullOrWhiteSpace(configuredBaseUrl)
    ? builder.HostEnvironment.BaseAddress
    : configuredBaseUrl;
builder.Services.AddScoped(_ => new HttpClient {{ BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute) }});
builder.Services.AddScoped<{client_class}>();

await builder.Build().RunAsync();
""",
        Path("App.razor"): """
<Router AppAssembly="@typeof(App).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
        <FocusOnNavigate RouteData="@routeData" Selector="h1" />
    </Found>
    <NotFound>
        <LayoutView Layout="@typeof(MainLayout)">
            <FkEmptyState Title="Page not found · الصفحة غير موجودة"
                          Description="The requested route does not exist in this generated FoundationKit application. · المسار المطلوب غير موجود في تطبيق FoundationKit المولد." />
        </LayoutView>
    </NotFound>
</Router>
""",
        Path("_Imports.razor"): f"""
@using System.Net.Http
@using Microsoft.AspNetCore.Components
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using FoundationKit.Blazor.Components
@using {namespace_name}
@using {namespace_name}.Api
@using {namespace_name}.Layout
""",
        Path("Layout/MainLayout.razor"): f"""
@inherits LayoutComponentBase

<FkAppShell Direction=\"auto\"
            Language=\"@_language\"
            LanguageChanged=\"SetLanguage\"
            NavigationLabel=\"@T(\"Application navigation\", \"التنقل في التطبيق\")\"
            DarkModeLabel=\"@T(\"Switch to dark mode\", \"التبديل إلى الوضع الداكن\")\"
            LightModeLabel=\"@T(\"Switch to light mode\", \"التبديل إلى الوضع الفاتح\")\">
    <Brand>
        <FkBrandMark Name=\"{app_name}\" Tagline=\"FoundationKit · Soft Orbit\" Href=\"/\" />
    </Brand>
    <Navigation>
        <FkNavItem Href=\"/\" Match=\"NavLinkMatch.All\">
            <Icon><span aria-hidden=\"true\">⌂</span></Icon>
            <ChildContent>@T(\"Overview\", \"نظرة عامة\")</ChildContent>
        </FkNavItem>
        <FkNavItem Href=\"contract\">
            <Icon><span aria-hidden=\"true\">◇</span></Icon>
            <ChildContent>@T(\"Contract\", \"العقد\")</ChildContent>
        </FkNavItem>
    </Navigation>
    <TopbarStart>
        <button class=\"fk-command\" type=\"button\" aria-label=\"Open command palette placeholder\">
            <span>@T(\"Search or command\", \"ابحث أو نفّذ أمرًا\")</span><kbd>Ctrl K</kbd>
        </button>
    </TopbarStart>
    <TopbarActions>
        <FkBadge Tone=\"FoundationBadgeTone.Aqua\">@T(\"Typed API ready\", \"Typed API جاهز\")</FkBadge>
    </TopbarActions>
    <ChildContent>
        <CascadingValue Value=\"@_language\" Name=\"FoundationLanguage\">
            @Body
        </CascadingValue>
    </ChildContent>
</FkAppShell>

@code {{
    private string _language = \"en\";
    private string T(string en, string ar) => _language == \"ar\" ? ar : en;
    private void SetLanguage(string language) => _language = language == \"ar\" ? \"ar\" : \"en\";
}}
""",
        Path("Pages/Home.razor"): f"""
@page "/"
@inject {client_class} Api

<PageTitle>{app_name}</PageTitle>

<FkPageHeader Eyebrow=\"FOUNDATIONKIT GENERATED APP\"
              Title=\"@T(\"A gentle starting point with a real typed transport.\", \"بداية خفيفة بعقد نقل Typed فعلي.\")\"
              Description=\"@T(\"This shell uses the same Soft Orbit tokens and first-party components as FoundationKit Core Studio. Product screens extend this baseline without duplicating the transport contract.\", \"تستخدم هذه الواجهة نفس Soft Orbit tokens ومكونات الطرف الأول في FoundationKit Core Studio، وتبني شاشات المنتج فوقها دون تكرار عقد النقل.\")\" />

<div class=\"fk-design-grid\">
    <div class=\"fk-col-8\">
        <FkCard>
            <div class=\"fk-stack\">
                <div class=\"fk-row fk-wrap\">
                    <FkBadge Tone=\"FoundationBadgeTone.Primary\">OpenAPI</FkBadge>
                    <FkBadge Tone=\"FoundationBadgeTone.Aqua\">Typed C#</FkBadge>
                    <FkBadge Tone=\"FoundationBadgeTone.Success\">@T(\"Shared UI baseline\", \"أساس واجهة مشترك\")</FkBadge>
                </div>
                <h2>@T(\"Transport is wired; product semantics stay yours.\", \"عقد النقل موصول؛ ودلالات المنتج تبقى ملك المشروع.\")</h2>
                <p class=\"fk-muted\">
                    @T(\"The generated client comes from runtime OpenAPI. Backend authorization, query policy, and read-model composition remain authoritative.\", \"العميل المولد يأتي من Runtime OpenAPI، بينما تبقى صلاحيات السيرفر وسياسة الاستعلام وتركيب Read Models هي المرجع الحاكم.\")
                    <code class=\"fk-mono\">{client_class}</code>
                </p>
                <div class=\"fk-row fk-wrap\">
                    <FkButton Href=\"contract\">@T(\"Inspect contract\", \"راجع العقد\")</FkButton>
                    <FkButton Variant=\"FoundationButtonVariant.Secondary\" Href=\"https://github.com/a2sn2/foundationkit-dotnet\">FoundationKit</FkButton>
                </div>
            </div>
        </FkCard>
    </div>
    <div class=\"fk-col-4\">
        <FkCard Variant=\"FoundationCardVariant.Muted\">
            <span class=\"fk-caption\">CLIENT TYPE</span>
            <p class=\"fk-mono generated-client-type\">@Api.GetType().FullName</p>
        </FkCard>
    </div>
</div>

<section class=\"generated-orbit\" aria-label=\"FoundationKit generation flow\">
    <div class=\"fk-orbit-stage\" aria-hidden=\"true\">
        <div class=\"fk-orbit-stage__ring\"></div>
        <div class=\"fk-orbit-stage__ring fk-orbit-stage__ring--inner\"></div>
        <div class=\"fk-orbit-stage__core\"></div>
        <span class=\"fk-orbit-stage__satellite fk-orbit-stage__satellite--one\">API</span>
        <span class=\"fk-orbit-stage__satellite fk-orbit-stage__satellite--two\">SQL</span>
        <span class=\"fk-orbit-stage__satellite fk-orbit-stage__satellite--three\">UI</span>
    </div>
</section>

@code {{
    [CascadingParameter(Name = \"FoundationLanguage\")]
    private string Language {{ get; set; }} = \"en\";
    private string T(string en, string ar) => Language == \"ar\" ? ar : en;
}}
""",
        Path("Pages/Contract.razor"): f"""
@page "/contract"

<PageTitle>Contract — {app_name}</PageTitle>

<FkPageHeader Eyebrow=\"TRANSPORT SSOT\"
              Title=\"@T(\"One serialized contract, no browser-side rewrite.\", \"عقد متسلسل واحد، بلا إعادة كتابة داخل المتصفح.\")\"
              Description=\"@T(\"Runtime OpenAPI produces the typed C# client used by this shell. Multi-table/report screens should consume backend read models rather than reproducing joins in the browser.\", \"ينتج Runtime OpenAPI عميل C# typed الذي تستخدمه الواجهة. شاشات التقارير والبيانات متعددة الجداول تستهلك Read Models من الخلفية بدل إعادة تنفيذ joins في المتصفح.\")\">
    <Actions>
        <FkButton Variant=\"FoundationButtonVariant.Secondary\" Href=\"/\">@T(\"Back to overview\", \"العودة للنظرة العامة\")</FkButton>
    </Actions>
</FkPageHeader>

<FkCard>
    <div class=\"contract-chain\">
        <div><span>01</span><strong>Runtime OpenAPI</strong><small>@T(\"serialized transport SSOT\", \"مصدر عقد النقل المتسلسل\")</small></div>
        <div><span>02</span><strong>Typed C# client</strong><small>@T(\"deterministic generation\", \"توليد حتمي\")</small></div>
        <div><span>03</span><strong>FoundationKit.Blazor</strong><small>@T(\"shared presentation system\", \"نظام عرض مشترك\")</small></div>
        <div><span>04</span><strong>Product UI</strong><small>@T(\"consumer-owned semantics\", \"دلالات يملكها المنتج\")</small></div>
    </div>
</FkCard>

@code {{
    [CascadingParameter(Name = \"FoundationLanguage\")]
    private string Language {{ get; set; }} = \"en\";
    private string T(string en, string ar) => Language == \"ar\" ? ar : en;
}}
""",
        Path("wwwroot/index.html"): f"""
<!DOCTYPE html>
<html lang=\"en\" dir=\"ltr\">
<head>
    <meta charset=\"utf-8\" />
    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />
    <meta name=\"theme-color\" content=\"#F7F8FC\" />
    <base href=\"/\" />
    <title>{app_name}</title>
    <link href=\"_content/FoundationKit.Blazor/foundationkit.css\" rel=\"stylesheet\" />
    <link href=\"css/app.css\" rel=\"stylesheet\" />
</head>
<body class=\"fk-app\">
    <div id=\"app\">
        <div class=\"fk-loading\" role=\"status\" aria-live=\"polite\">
            <div class=\"fk-loading__visual\" aria-hidden=\"true\">
                <span class=\"fk-orbit-path fk-orbit-path--one\"></span>
                <span class=\"fk-orbit-path fk-orbit-path--two\"></span>
                <span class=\"fk-orbit-node fk-loading__pulse\"></span>
                <span class=\"fk-orbit-node fk-orbit-node--aqua fk-loading__pulse\"></span>
                <span class=\"fk-orbit-node fk-orbit-node--warm fk-loading__pulse\"></span>
            </div>
            <h2>Preparing {app_name}</h2>
            <p>Connecting the generated client and shared FoundationKit UI baseline…</p>
        </div>
    </div>
    <div id=\"blazor-error-ui\" class=\"generated-error\">
        An unexpected error occurred. <a href=\"\" class=\"reload\">Reload</a>
    </div>
    <script src=\"_content/FoundationKit.Blazor/foundationkit.js\"></script>
    <script src=\"_framework/blazor.webassembly.js\"></script>
</body>
</html>
""",
        Path("wwwroot/css/app.css"): """
.generated-client-type { overflow-wrap: anywhere; margin-bottom: 0; }
.generated-orbit { min-height: 25rem; display: grid; place-items: center; padding-block: var(--fk-space-12); }
.contract-chain { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: var(--fk-space-3); }
.contract-chain > div { min-height: 9rem; padding: var(--fk-space-4); border: 1px solid var(--fk-border-default); border-radius: var(--fk-radius-card); background: var(--fk-surface-muted); display: grid; align-content: start; gap: var(--fk-space-2); }
.contract-chain span { width: 2rem; height: 2rem; border-radius: .65rem; display: grid; place-items: center; background: var(--fk-color-primary-soft); color: var(--fk-color-primary); font-family: var(--fk-font-mono); font-size: .72rem; font-weight: 700; }
.contract-chain small { color: var(--fk-text-muted); }
.generated-error { display: none; position: fixed; inset-inline: var(--fk-space-4); bottom: var(--fk-space-4); z-index: 1000; padding: var(--fk-space-4); border: 1px solid color-mix(in srgb, var(--fk-color-danger) 30%, var(--fk-border-default)); border-radius: var(--fk-radius-control); background: var(--fk-color-danger-soft); color: var(--fk-color-danger); box-shadow: var(--fk-shadow-md); }
@media (max-width: 760px) { .contract-chain { grid-template-columns: 1fr; } }
""",
        Path("wwwroot/appsettings.json"): """
{
  "FoundationApiBaseUrl": null
}
""",
        Path("GENERATED-FRONTEND.md"): f"""
# {app_name}

This Blazor WebAssembly shell was generated from runtime OpenAPI and uses the shared FoundationKit Soft Orbit design system.

Contract chain:

`runtime OpenAPI -> generate-csharp-client-from-openapi.py -> {client_class} -> FoundationKit.Blazor components/tokens -> generated product shell`

The shell is intentionally product-neutral. It does not infer authorization, relational joins, secrets, or business workflows in the browser. Product screens should consume typed methods in `Api/{client_class}.g.cs`, while backend policies and read models remain authoritative.

The generated shell is bilingual-first: FoundationKit.Blazor persists `en` / `ar`, switches LTR / RTL at the document boundary, and keeps product translations owned by the generated host instead of embedding business language in the reusable package.

The visual baseline comes from `_content/FoundationKit.Blazor/foundationkit.css`; applications should override semantic tokens at their host boundary instead of forking component CSS.
""",
    }


def main() -> int:
    args = parse_args()
    load_openapi(args.openapi)

    app_name = require_identifier(args.app_name, "app-name")
    namespace_name = require_namespace(args.namespace_name or f"{app_name}.Client")
    client_class = require_identifier(args.client_class, "client-class")
    output = args.output.resolve()
    foundation_root = args.foundation_root.resolve()
    project_reference = relative_project_reference(output, foundation_root)

    files = build_files(app_name, namespace_name, client_class, project_reference)
    for relative_path, content in sorted(files.items(), key=lambda item: item[0].as_posix()):
        write_or_check(output / relative_path, content, args.check)

    client_path = output / "Api" / f"{client_class}.g.cs"
    if not args.check:
        client_path.parent.mkdir(parents=True, exist_ok=True)
    generate_client(
        Path(__file__).resolve().parent,
        args.openapi.resolve(),
        client_path,
        f"{namespace_name}.Api",
        client_class,
        args.check,
    )

    print(f"{'Verified' if args.check else 'Generated'} deterministic Blazor app: {output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
