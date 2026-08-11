#!/usr/bin/env python3
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SITE = ROOT / "site"

required = [
    SITE / "index.html",
    SITE / "styles.css",
    SITE / "app.js",
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

html = (SITE / "index.html").read_text(encoding="utf-8")
required_text = (
    "FoundationKit",
    "Core vNext",
    "ARCHITECTURE",
    "17 REUSABLE PACKAGES",
    "Module / CRUD / API Engine",
    "COMPOSER · SCHEMA V2",
    "MULTI-TABLE / REPORT READ",
    "TRANSPORT SOURCE OF TRUTH",
    "PROJECT ISOLATION + RELIABILITY",
    "FOUNDATIONKIT.BLAZOR · SOFT ORBIT",
    "The approved Core vNext roadmap ends at Phase 12.",
    "ENGINEERING EVIDENCE",
    "PRODUCTION BOUNDARY",
    "START THE FIRST PROJECT",
    "generated\\MySystem\\MySystem.sln",
)
for text in required_text:
    if text not in html:
        raise SystemExit(f"Pages Core showcase missing required text: {text}")

if html.count('data-package-kind="') != 17:
    raise SystemExit("Pages must present all 17 reusable package cards")

for package in (
    "FoundationKit.Domain",
    "FoundationKit.Application",
    "FoundationKit.Infrastructure",
    "FoundationKit.WebApi",
    "FoundationKit.Blazor",
    "FoundationKit.Auditing",
    "FoundationKit.Security",
    "FoundationKit.Identity",
    "FoundationKit.Authorization",
    "FoundationKit.Workflow",
    "FoundationKit.Approvals",
    "FoundationKit.Notifications",
    "FoundationKit.Notifications.Smtp",
    "FoundationKit.Settings",
    "FoundationKit.FeatureManagement",
    "FoundationKit.Localization",
    "FoundationKit.Caching",
):
    if package not in html:
        raise SystemExit(f"Pages package presentation missing: {package}")

if "phases 1-6" in html.lower() or "phases 1-6" in json.dumps(manifest).lower():
    raise SystemExit("Pages still contains the retired phases 1-6 baseline")

css = (SITE / "styles.css").read_text(encoding="utf-8")
for selector in (".hero", ".package-grid", ".core-orbit", ".start-section", "prefers-reduced-motion"):
    if selector not in css:
        raise SystemExit(f"Pages stylesheet missing required Core showcase selector: {selector}")

js = (SITE / "app.js").read_text(encoding="utf-8")
for behavior in ("data-package-filter", "foundationkit-theme", "IntersectionObserver", "portal-manifest.json"):
    if behavior not in js:
        raise SystemExit(f"Pages interactions missing required behavior: {behavior}")

print("FoundationKit complete Core Pages assets verified.")
