#!/usr/bin/env python3
"""Fail when local/generated/sensitive artifacts are tracked by Git.

This check deliberately inspects Git's tracked file set instead of the working tree.
A developer may have ignored local files on disk; the repository baseline only fails
when one of those artifacts is actually committed.
"""

from __future__ import annotations

import subprocess
from pathlib import PurePosixPath


FORBIDDEN_DIRECTORY_NAMES = {
    ".idea",
    ".local",
    ".vs",
    ".vscode",
    "BenchmarkDotNet.Artifacts",
    "TestResults",
    "artifacts",
    "bin",
    "coverage",
    "logs",
    "node_modules",
    "obj",
}

FORBIDDEN_SUFFIXES = {
    ".bak",
    ".coverage",
    ".coveragexml",
    ".db",
    ".key",
    ".log",
    ".nupkg",
    ".p12",
    ".pfx",
    ".snupkg",
    ".sqlite",
    ".sqlite3",
    ".suo",
    ".trx",
    ".user",
}

ALLOWED_ENV_TEMPLATES = {
    ".env.example",
    ".env.sample",
    ".env.template",
}


def tracked_files() -> list[str]:
    completed = subprocess.run(
        ["git", "ls-files", "-z"],
        check=True,
        capture_output=True,
    )
    return [
        entry.decode("utf-8")
        for entry in completed.stdout.split(b"\0")
        if entry
    ]


def violation_reason(path_text: str) -> str | None:
    path = PurePosixPath(path_text)
    parts = set(path.parts)

    forbidden_directories = sorted(parts & FORBIDDEN_DIRECTORY_NAMES)
    if forbidden_directories:
        return f"forbidden generated/local directory: {', '.join(forbidden_directories)}"

    name_lower = path.name.lower()
    if name_lower == ".env" or (
        name_lower.startswith(".env.") and name_lower not in ALLOWED_ENV_TEMPLATES
    ):
        return "local environment/secrets file"

    for suffix in FORBIDDEN_SUFFIXES:
        if name_lower.endswith(suffix.lower()):
            return f"forbidden local/generated suffix: {suffix}"

    if name_lower.endswith(".secrets.json"):
        return "local secrets file"

    return None


def main() -> int:
    violations: list[tuple[str, str]] = []

    for path in tracked_files():
        reason = violation_reason(path)
        if reason:
            violations.append((path, reason))

    if violations:
        print("Tracked repository hygiene violations detected:")
        for path, reason in violations:
            print(f"  - {path}: {reason}")
        print(
            "Remove these files from Git and keep local/generated/sensitive artifacts "
            "outside the tracked repository."
        )
        return 1

    print("Tracked repository hygiene passed: no local/generated/sensitive artifacts are committed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
