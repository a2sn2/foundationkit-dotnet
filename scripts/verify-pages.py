#!/usr/bin/env python3
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SITE = ROOT / "site"

primary_page_names = [
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
auxiliary_page_names = ["developer-projects.html"]
page_names = [*primary_page_names, *auxiliary_page_names]

required = [
    *(SITE / name for name in page_names),
    SITE / "styles.css",
    SITE / "pages.css",
    SITE / "developer.css",
    SITE / "creative.css",
    SITE / "app.js",
    SITE / "multipage.js",
    SITE / "portal-manifest.json",
    SITE / "assets" / "developer-avatar.png",
]
for path in required:
    if not path.is_file():
        raise SystemExit(f"Pages asset missing: {path.relative_to(ROOT)}")

avatar = (SITE / "assets" / "developer-avatar.png").read_bytes()
if len(avatar) < 5_000 or not avatar.startswith(b"\x89PNG\r\n\x1a\n"):
    raise SystemExit("Developer portrait asset must be a real PNG extracted from the supplied CV")

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
    "index.html": ("FoundationKit", "CORE-ONLY REPOSITORY", "EXPLORE THE CORE", "Developer", 'data-ar='),
    "architecture.html": ("ARCHITECTURE", "Reusable Core.", "Five explicit layers.", "Authorization is server-authoritative"),
    "capabilities.html": ("MODULE / CRUD / API ENGINE", "SQL-FIRST READS", "PROJECT ISOLATION + RELIABILITY", "SUPPORTING CAPABILITIES"),
    "packages.html": ("17 REUSABLE PACKAGES", "FoundationKit.Domain", "FoundationKit.Blazor", "FoundationKit.Caching"),
    "composer.html": ("COMPOSER · SCHEMA V2", "Seven canonical profiles", "Visual and CLI share the same engine", "Safe regeneration"),
    "frontend.html": ("FOUNDATIONKIT.BLAZOR · SOFT ORBIT", "REUSABLE RAZOR LAYER", "RTL / LTR", "PRESENTATION STATES"),
    "quality.html": ("ENGINEERING EVIDENCE", "EXACT-HEAD QUALITY", "CONTRACT DRIFT", "Repository Complete ≠ Production Approved"),
    "start.html": ("START THE FIRST PROJECT", ".\\foundationkit.ps1 start -Target Workbench", "generated\\MySystem\\MySystem.sln", "Choose → Validate → Generate"),
    "developer.html": (
        "THE DEVELOPER BEHIND FOUNDATIONKIT",
        "ALHassan ALShami",
        "AHD Financial Services · Jaib Wallet",
        "Asaas · Co-founder · QA Engineer · Frontend & RAG Systems",
        "B.Sc. in Computer Science",
        "Explore 16 projects",
        "assets/developer-avatar.png",
        "dev-hero-v2",
        "dev-orbit-scene",
        'data-ar=',
    ),
    "developer-projects.html": (
        "CV-BACKED PROJECT CATALOG",
        "Sixteen projects.",
        "Pump Station Analytics",
        "RoboCam Controller",
        "MikroTik Hotspot Portal",
        "Arduino Traffic Light Controller",
        "Object Tracking Algorithms",
        'data-ar=',
    ),
}
for page, markers in required_page_text.items():
    for marker in markers:
        if marker not in pages[page]:
            raise SystemExit(f"Pages {page} missing required content: {marker}")

for page, html in pages.items():
    for target in primary_page_names:
        if f'href="{target}"' not in html:
            raise SystemExit(f"Pages {page} missing navigation target: {target}")
    if 'src="app.js"' not in html or 'src="multipage.js"' not in html:
        raise SystemExit(f"Pages {page} must load shared interaction scripts")
    if 'href="styles.css"' not in html or 'href="pages.css"' not in html:
        raise SystemExit(f"Pages {page} must load both shared stylesheets")

for page in ("developer.html", "developer-projects.html"):
    if 'href="developer.css"' not in pages[page]:
        raise SystemExit(f"Pages {page} must load the Developer portfolio stylesheet")
    if 'assets/developer-avatar.png' not in pages[page]:
        raise SystemExit(f"Pages {page} must use the CV portrait as a small avatar/icon")
    if pages[page].count('data-en=') < 15 or pages[page].count('data-ar=') < 15:
        raise SystemExit(f"Pages {page} must expose a substantive EN/AR content contract")

if pages["index.html"].count('data-en=') < 20 or pages["index.html"].count('data-ar=') < 20:
    raise SystemExit("Core overview must expose a substantive EN/AR content contract")
if "Waiting for the developer CV." in pages["developer.html"] or "CV content pending" in pages["developer.html"]:
    raise SystemExit("Developer page still contains the pre-CV placeholder")
if "01/10/2002" in pages["developer.html"]:
    raise SystemExit("Developer page must not publish date of birth")
if pages["developer-projects.html"].count('class="all-project-card"') != 16:
    raise SystemExit("Developer Projects page must present all 16 CV-backed projects")

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

developer_css = (SITE / "developer.css").read_text(encoding="utf-8")
for selector in (".dev-photo", ".dev-hero-layout", ".timeline", ".project-feature-grid", ".all-projects-grid"):
    if selector not in developer_css:
        raise SystemExit(f"Developer stylesheet missing required selector: {selector}")

creative_css = (SITE / "creative.css").read_text(encoding="utf-8")
for selector in (".language-toggle", ".dev-hero-v2", ".dev-orbit-scene", ".dev-satellite", "prefers-reduced-motion"):
    if selector not in creative_css:
        raise SystemExit(f"Creative Pages layer missing required selector: {selector}")

js = (SITE / "app.js").read_text(encoding="utf-8")
for behavior in ("data-package-filter", "foundationkit-theme", "IntersectionObserver", "portal-manifest.json"):
    if behavior not in js:
        raise SystemExit(f"Pages interactions missing required behavior: {behavior}")

multipage_js = (SITE / "multipage.js").read_text(encoding="utf-8")
for behavior in ("data-page", "location.pathname", "aria-expanded", "foundationkit-language", "data-language-toggle", "document.documentElement.dir", "creative.css"):
    if behavior not in multipage_js:
        raise SystemExit(f"Multi-page navigation/localization missing required behavior: {behavior}")

print("FoundationKit bilingual Soft Orbit Core + source-grounded Developer portfolio assets verified.")
