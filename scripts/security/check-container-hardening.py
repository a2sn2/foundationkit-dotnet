#!/usr/bin/env python3
"""Static hardening policy for the repository-owned Workbench container."""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DOCKERFILE = ROOT / "samples/FoundationKit.Workbench/Dockerfile"
COMPOSE = ROOT / "deploy/docker-compose.yml"


def main() -> int:
    errors: list[str] = []
    dockerfile = DOCKERFILE.read_text(encoding="utf-8")
    compose = COMPOSE.read_text(encoding="utf-8")

    if not re.search(r"(?mi)^FROM\s+mcr\.microsoft\.com/dotnet/aspnet:[^\s]+\s+AS\s+final\s*$", dockerfile):
        errors.append("final runtime stage must use the ASP.NET runtime image")
    if not re.search(r"(?mi)^USER\s+\S+\s*$", dockerfile):
        errors.append("final image must declare a non-root USER")
    if re.search(r"(?mi)^FROM\s+[^\s]+:latest(?:\s|$)", dockerfile):
        errors.append("Dockerfile must not use an unqualified :latest base tag")
    if "no-new-privileges:true" not in compose:
        errors.append("Workbench Compose service must set no-new-privileges:true")
    if "cap_drop:" not in compose or "- ALL" not in compose:
        errors.append("Workbench Compose service must drop Linux capabilities")
    if "healthcheck:" not in compose:
        errors.append("Workbench Compose topology must define a health check")

    if errors:
        for error in errors:
            print(f"container-hardening: {error}", file=sys.stderr)
        return 1

    print("Container hardening policy check passed for Workbench.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
