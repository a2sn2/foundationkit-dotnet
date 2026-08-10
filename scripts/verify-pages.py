#!/usr/bin/env python3
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SITE = ROOT / "site"

required = [SITE / "index.html", SITE / "app.js", SITE / "portal-manifest.json"]
for path in required:
    if not path.is_file():
        raise SystemExit(f"Pages asset missing: {path.relative_to(ROOT)}")

manifest = json.loads((SITE / "portal-manifest.json").read_text(encoding="utf-8"))
if manifest.get("product") != "FoundationKit Core":
    raise SystemExit("Pages manifest product must be FoundationKit Core")
if manifest.get("workbench") != "/swagger":
    raise SystemExit("Pages manifest must expose the Workbench Swagger reference path")
if manifest.get("packageCount") != 17:
    raise SystemExit("Pages manifest must report 17 reusable packages")

html = (SITE / "index.html").read_text(encoding="utf-8")
for required_text in ("FoundationKit", "Core vNext", "Module / CRUD Engine", "Project Isolation"):
    if required_text not in html:
        raise SystemExit(f"Pages landing missing required text: {required_text}")

print("FoundationKit Core Pages assets verified.")
