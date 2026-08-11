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
            <h1>Not found</h1>
            <p>The requested route does not exist in this generated FoundationKit client.</p>
        </LayoutView>
    </NotFound>
</Router>
""",
        Path("_Imports.razor"): f"""
@using System.Net.Http
@using Microsoft.AspNetCore.Components
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using {namespace_name}
@using {namespace_name}.Api
@using {namespace_name}.Layout
""",
        Path("Layout/MainLayout.razor"): """
@inherits LayoutComponentBase

<div class="shell">
    <header>
        <a href="/" class="brand">FoundationKit Generated Client</a>
        <span>Runtime OpenAPI → deterministic typed transport</span>
    </header>
    <main>@Body</main>
</div>
""",
        Path("Pages/Home.razor"): f"""
@page "/"
@inject {client_class} Api

<PageTitle>FoundationKit Generated Client</PageTitle>

<section class="hero">
    <p class="eyebrow">GENERATED FROM RUNTIME OPENAPI</p>
    <h1>Typed transport is wired and ready for product-owned screens.</h1>
    <p>
        This shell registers <code>{client_class}</code>, generated by the canonical
        FoundationKit OpenAPI client generator. Product UI may call its typed operations;
        backend authorization and read-model/query policy remain authoritative.
    </p>
    <div class="contract-card">
        <strong>Client type</strong>
        <code>@Api.GetType().FullName</code>
    </div>
</section>
""",
        Path("wwwroot/index.html"): f"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    <title>{app_name}</title>
    <link href="css/app.css" rel="stylesheet" />
</head>
<body>
    <div id="app">Loading FoundationKit generated client...</div>
    <div id="blazor-error-ui">
        An unexpected error occurred. <a href="" class="reload">Reload</a>
    </div>
    <script src="_framework/blazor.webassembly.js"></script>
</body>
</html>
""",
        Path("wwwroot/css/app.css"): """
:root {
    color-scheme: dark;
    font-family: Inter, ui-sans-serif, system-ui, sans-serif;
    background: #07111f;
    color: #e7eef8;
}

* { box-sizing: border-box; }
body { margin: 0; background: radial-gradient(circle at 80% 0%, #12304d, #07111f 45%); }
.shell header { display: flex; justify-content: space-between; gap: 1rem; padding: 1rem 5vw; border-bottom: 1px solid #23364e; }
.brand { color: #67e8f9; text-decoration: none; font-weight: 800; }
main { width: min(1100px, 90vw); margin: 0 auto; }
.hero { min-height: 72vh; display: grid; align-content: center; gap: 1rem; }
.hero h1 { max-width: 16ch; font-size: clamp(2.4rem, 7vw, 5rem); line-height: .95; margin: 0; }
.hero p { max-width: 70ch; color: #a9b7ca; line-height: 1.75; }
.eyebrow { color: #67e8f9 !important; font-family: ui-monospace, monospace; letter-spacing: .14em; }
.contract-card { width: fit-content; display: grid; gap: .5rem; padding: 1rem 1.2rem; border: 1px solid #2d4b69; border-radius: 16px; background: #0b192b; }
code { color: #c4f1ff; }
#blazor-error-ui { display: none; }
@media (max-width: 720px) { .shell header { flex-direction: column; } }
""",
        Path("wwwroot/appsettings.json"): """
{
  "FoundationApiBaseUrl": null
}
""",
        Path("GENERATED-FRONTEND.md"): f"""
# {app_name}

This Blazor WebAssembly reference shell was generated from a runtime OpenAPI document.

Contract chain:

`runtime OpenAPI -> generate-csharp-client-from-openapi.py -> {client_class} -> DI-registered Blazor app`

The shell is intentionally product-neutral. It does not infer authorization, relational joins,
or business workflows from the browser. Product screens should consume the typed methods in
`Api/{client_class}.g.cs`, while the backend remains authoritative.
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
