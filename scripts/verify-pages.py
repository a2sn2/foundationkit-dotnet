#!/usr/bin/env python3
"""Validate the static GitHub Pages portal and keep it aligned with Blazor routes."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SITE = ROOT / "site"
MANIFEST_PATH = SITE / "portal-manifest.json"

REQUIRED_SITE_FILES = {
    "index.html",
    "styles.css",
    "app.js",
    "portal-manifest.json",
    "favicon.svg",
}
REQUIRED_MADAR_HANDOFF_PATHS = (
    ROOT / "docs/MADAR-SPECIFICATION-AR.md",
    ROOT / "docs/MADAR-LOCAL-RUN-PUBLISH-AR.md",
    ROOT / "docs/MADAR-ACCEPTANCE-CHECKLIST-AR.md",
    SITE / "madar-demo/index.html",
    SITE / "madar-demo/styles.css",
    SITE / "madar-demo/app.js",
)
REQUIRED_GROUPS = {"overview", "core", "workbench", "athar", "madar", "docs", "operations"}
ALLOWED_KINDS = {"ui", "api", "package", "document", "guide", "automation", "tool"}
ALLOWED_RUNTIMES = {
    "static",
    "library",
    "static-and-local",
    "local-write",
    "local",
    "github",
    "local-and-ci",
}
RAZOR_APPLICATIONS = {
    "workbench": ROOT / "samples/FoundationKit.Workbench.Client/Pages",
    "athar": ROOT / "examples/Athar/Athar.Client/Pages",
    "madar": ROOT / "apps/Madar/Madar.Client/Pages",
}
PAGE_PATTERN = re.compile(r'^\s*@page\s+"([^"]+)"', re.MULTILINE)


def fail(message: str) -> None:
    print(f"Pages portal verification failed: {message}", file=sys.stderr)
    raise SystemExit(1)


def load_manifest() -> dict:
    try:
        return json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))
    except FileNotFoundError:
        fail(f"missing {MANIFEST_PATH.relative_to(ROOT)}")
    except json.JSONDecodeError as error:
        fail(f"invalid manifest JSON: {error}")
    raise AssertionError("unreachable")


def require_nonempty(page: dict, field: str) -> None:
    value = page.get(field)
    if value is None or value == "" or value == []:
        fail(f"page '{page.get('id', '<unknown>')}' has no {field}")


def verify_sources(pages: list[dict]) -> None:
    for page in pages:
        source = page["source"].strip("/")
        path = ROOT / source
        if not path.exists():
            fail(f"manifest source does not exist: {source}")


def verify_razor_routes(pages: list[dict]) -> None:
    manifest_routes = {
        (page.get("app"), page["source"], page["route"])
        for page in pages
        if page["kind"] == "ui"
    }

    discovered_routes: set[tuple[str, str, str]] = set()
    for app, directory in RAZOR_APPLICATIONS.items():
        if not directory.exists():
            fail(f"missing Razor page directory: {directory.relative_to(ROOT)}")

        for razor_file in sorted(directory.glob("*.razor")):
            content = razor_file.read_text(encoding="utf-8")
            routes = PAGE_PATTERN.findall(content)
            relative = razor_file.relative_to(ROOT).as_posix()
            for route in routes:
                discovered_routes.add((app, relative, route))

    missing = discovered_routes - manifest_routes
    stale = manifest_routes - discovered_routes

    if missing:
        formatted = ", ".join(f"{app}:{route} ({source})" for app, source, route in sorted(missing))
        fail(f"Blazor routes missing from portal manifest: {formatted}")

    if stale:
        formatted = ", ".join(f"{app}:{route} ({source})" for app, source, route in sorted(stale))
        fail(f"portal UI entries do not match a Razor @page: {formatted}")


def verify_madar_handoff() -> None:
    missing = [path.relative_to(ROOT).as_posix() for path in REQUIRED_MADAR_HANDOFF_PATHS if not path.is_file()]
    if missing:
        fail(f"missing Madar handoff assets: {', '.join(missing)}")

    demo_index = (SITE / "madar-demo/index.html").read_text(encoding="utf-8")
    if "DEMO · بدون خادم أو SQL" not in demo_index:
        fail("Madar static demo must remain explicitly labeled as no-server/no-SQL")
    if "MADAR-LOCAL-RUN-PUBLISH-AR.md" not in demo_index:
        fail("Madar static demo must link to the real local-run/publish guide")

    portal_index = (SITE / "index.html").read_text(encoding="utf-8")
    if 'href="madar-demo/"' not in portal_index:
        fail("Atlas must expose the Madar static demo entry point")

    product_readme = (ROOT / "apps/Madar/README.md").read_text(encoding="utf-8")
    for required_doc in ("MADAR-SPECIFICATION-AR.md", "MADAR-LOCAL-RUN-PUBLISH-AR.md"):
        if required_doc not in product_readme:
            fail(f"Madar README must link to {required_doc}")

    for required_ua_text in (
        "start -Target Madar -Mode Native",
        "TunnelProvider Microsoft",
        "TunnelProvider Cloudflare",
    ):
        if required_ua_text not in product_readme:
            fail(f"Madar README must preserve Native UAT handoff text: {required_ua_text}")

    local_guide = (ROOT / "docs/MADAR-LOCAL-RUN-PUBLISH-AR.md").read_text(encoding="utf-8")
    for required_guide_text in (
        "start -Target Madar -Mode Native",
        "TunnelProvider Microsoft",
        "TunnelProvider Cloudflare",
        "Production Approval",
    ):
        if required_guide_text not in local_guide:
            fail(f"Madar local-run guide must preserve handoff boundary text: {required_guide_text}")


def main() -> None:
    missing_files = REQUIRED_SITE_FILES - {path.name for path in SITE.iterdir()} if SITE.exists() else REQUIRED_SITE_FILES
    if missing_files:
        fail(f"missing site files: {', '.join(sorted(missing_files))}")

    verify_madar_handoff()

    manifest = load_manifest()
    if manifest.get("schemaVersion") != 1:
        fail("schemaVersion must be 1")

    groups = manifest.get("groups")
    pages = manifest.get("pages")
    if not isinstance(groups, list) or not isinstance(pages, list):
        fail("groups and pages must be arrays")

    group_ids = [group.get("id") for group in groups]
    if len(group_ids) != len(set(group_ids)):
        fail("group IDs must be unique")
    if set(group_ids) != REQUIRED_GROUPS:
        fail(f"group IDs must be exactly: {', '.join(sorted(REQUIRED_GROUPS))}")

    page_ids: set[str] = set()
    ui_route_keys: set[tuple[str, str]] = set()
    for page in pages:
        for field in ("id", "group", "title", "route", "source", "kind", "runtime", "benefit", "details", "flow", "preview"):
            require_nonempty(page, field)

        page_id = page["id"]
        if page_id in page_ids:
            fail(f"duplicate page ID: {page_id}")
        page_ids.add(page_id)

        if page["group"] not in REQUIRED_GROUPS:
            fail(f"page '{page_id}' references unknown group '{page['group']}'")
        if page["kind"] not in ALLOWED_KINDS:
            fail(f"page '{page_id}' has unsupported kind '{page['kind']}'")
        if page["runtime"] not in ALLOWED_RUNTIMES:
            fail(f"page '{page_id}' has unsupported runtime '{page['runtime']}'")

        if page["kind"] == "ui":
            app = page.get("app")
            if app not in RAZOR_APPLICATIONS:
                fail(f"UI page '{page_id}' must identify a known app")
            route_key = (app, page["route"])
            if route_key in ui_route_keys:
                fail(f"duplicate UI route for {app}: {page['route']}")
            ui_route_keys.add(route_key)

    if "overview-home" not in page_ids:
        fail("overview-home entry is required")

    verify_sources(pages)
    verify_razor_routes(pages)

    index = (SITE / "index.html").read_text(encoding="utf-8")
    for required_reference in ("<base href=\"./\">", "styles.css", "app.js"):
        if required_reference not in index:
            fail(f"index.html does not reference {required_reference}")

    app_js = (SITE / "app.js").read_text(encoding="utf-8")
    if 'fetch("portal-manifest.json"' not in app_js:
        fail("app.js must load portal-manifest.json")
    if "escapeHtml" not in app_js:
        fail("app.js must escape manifest content before rendering")

    print(
        "FoundationKit Pages portal verification passed: "
        f"{len(groups)} groups, {len(pages)} entries, {len(ui_route_keys)} Blazor routes, Madar handoff assets verified."
    )


if __name__ == "__main__":
    main()
