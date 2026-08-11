#!/usr/bin/env python3
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SITE = ROOT / "site"

page_names = [
    "index.html",
    "architecture.html",
    "capabilities.html",
    "packages.html",
    "composer.html",
    "frontend.html",
    "quality.html",
    "start.html",
    "developer.html",
]

required = [
    *(SITE / name for name in page_names),
    SITE / "styles.css",
    SITE / "pages.css",
    SITE / "app.js",
    SITE / "multipage.js",
    SITE / "portal-manifest.json",
]
for path in required:
    if not path.is_file():
        raise SystemExit(f"Pages asset missing: {path.relative_to(ROOT)}")

manifest = json.loads((SITE / "portal-manifest.json").read_text(encoding="utf-8"))
if manifest.get("product") != "FoundationKit Core":
    raise SystemExit("Pages manifest product must be FoundationKit Core")
if manifest.get("framework") != ".NET 10":
    raise SystemExit("Pages manifest must report the .NET 10 baseline")
if manifest.get("roadmapEndPhase") != 12:
    raise SystemExit("Pages manifest must report the approved roadmap ending at Phase 12")
if manifest.get("baseline") != "Core vNext · Phase 12":
    raise SystemExit("Pages manifest baseline is stale or inconsistent")
if manifest.get("workbench") != "/swagger":
    raise SystemExit("Pages manifest must expose the Workbench Swagger reference path")
if manifest.get("packageCount") != 17 or manifest.get("symbolPackageCount") != 17:
    raise SystemExit("Pages manifest must report 17 reusable packages + 17 symbol packages")
if manifest.get("verifiedTests") != 266:
    raise SystemExit("Pages manifest must match the verified local-generation baseline test count")
if len(manifest.get("profiles", [])) != 7:
    raise SystemExit("Pages manifest must expose the seven canonical composition profiles")
if manifest.get("closureTracks") != [
    "12.C1 Typed transport",
    "12.C2 SQL read engine",
    "12.C3 Frontend foundation",
    "12.C4 Tooling closure",
]:
    raise SystemExit("Pages manifest must expose the four Phase 12 closure tracks")

pages = {name: (SITE / name).read_text(encoding="utf-8") for name in page_names}
all_html = "\n".join(pages.values())

required_page_text = {
    "index.html": ("FoundationKit", "CORE-ONLY REPOSITORY", "EXPLORE THE CORE", "Developer"),
    "architecture.html": ("ARCHITECTURE", "Reusable Core.", "Five explicit layers.", "Authorization is server-authoritative"),
    "capabilities.html": ("MODULE / CRUD / API ENGINE", "SQL-FIRST READS", "PROJECT ISOLATION + RELIABILITY", "SUPPORTING CAPABILITIES"),
    "packages.html": ("17 REUSABLE PACKAGES", "FoundationKit.Domain", "FoundationKit.Blazor", "FoundationKit.Caching"),
    "composer.html": ("COMPOSER · SCHEMA V2", "Seven canonical profiles", "Visual and CLI share the same engine", "Safe regeneration"),
    "frontend.html": ("FOUNDATIONKIT.BLAZOR · SOFT ORBIT", "REUSABLE RAZOR LAYER", "RTL / LTR", "PRESENTATION STATES"),
    "quality.html": ("ENGINEERING EVIDENCE", "EXACT-HEAD QUALITY", "CONTRACT DRIFT", "Repository Complete ≠ Production Approved"),
    "start.html": ("START THE FIRST PROJECT", ".\\foundationkit.ps1 start -Target Workbench", "generated\\MySystem\\MySystem.sln", "Choose → Validate → Generate"),
    "developer.html": ("THE DEVELOPER BEHIND FOUNDATIONKIT", "SOURCE-DRIVEN PROFILE", "Waiting for the developer CV.", "Professional Positioning", "Selected Projects"),
}
for page, markers in required_page_text.items():
    for marker in markers:
        if marker not in pages[page]:
            raise SystemExit(f"Pages {page} missing required content: {marker}")

for page, html in pages.items():
    for target in page_names:
        if f'href="{target}"' not in html:
            raise SystemExit(f"Pages {page} missing navigation target: {target}")
    if 'src="app.js"' not in html or 'src="multipage.js"' not in html:
        raise SystemExit(f"Pages {page} must load shared interaction scripts")
    if 'href="styles.css"' not in html or 'href="pages.css"' not in html:
        raise SystemExit(f"Pages {page} must load both shared stylesheets")

packages_html = pages["packages.html"]
if packages_html.count('data-package-kind="') != 17:
    raise SystemExit("Packages page must present all 17 reusable package cards")
for package in (
    "FoundationKit.Domain", "FoundationKit.Application", "FoundationKit.Infrastructure", "FoundationKit.WebApi", "FoundationKit.Blazor",
    "FoundationKit.Auditing", "FoundationKit.Security", "FoundationKit.Identity", "FoundationKit.Authorization", "FoundationKit.Workflow",
    "FoundationKit.Approvals", "FoundationKit.Notifications", "FoundationKit.Notifications.Smtp", "FoundationKit.Settings",
    "FoundationKit.FeatureManagement", "FoundationKit.Localization", "FoundationKit.Caching",
):
    if package not in packages_html:
        raise SystemExit(f"Packages page missing: {package}")

if "phases 1-6" in all_html.lower() or "phases 1-6" in json.dumps(manifest).lower():
    raise SystemExit("Pages still contains the retired phases 1-6 baseline")

css = (SITE / "styles.css").read_text(encoding="utf-8")
for selector in (".hero", ".package-grid", ".core-orbit", "prefers-reduced-motion"):
    if selector not in css:
        raise SystemExit(f"Base Pages stylesheet missing required selector: {selector}")

pages_css = (SITE / "pages.css").read_text(encoding="utf-8")
for selector in (".page-hero", ".route-grid", ".content-grid", ".developer-hero-card", ".site-footer"):
    if selector not in pages_css:
        raise SystemExit(f"Multi-page stylesheet missing required selector: {selector}")

js = (SITE / "app.js").read_text(encoding="utf-8")
for behavior in ("data-package-filter", "foundationkit-theme", "IntersectionObserver", "portal-manifest.json"):
    if behavior not in js:
        raise SystemExit(f"Pages interactions missing required behavior: {behavior}")

multipage_js = (SITE / "multipage.js").read_text(encoding="utf-8")
for behavior in ("data-page", "location.pathname", "aria-expanded"):
    if behavior not in multipage_js:
        raise SystemExit(f"Multi-page navigation missing required behavior: {behavior}")

print("FoundationKit multi-page Core site assets verified.")
