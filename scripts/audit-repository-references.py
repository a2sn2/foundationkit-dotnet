#!/usr/bin/env python3
"""Report tracked support/documentation files with no active textual repository references.

This is an audit aid, not an automatic deletion policy. Source/test/tool project files are
excluded because SDK/MSBuild conventions discover them without textual path references.
Historical security evidence is also excluded because its value is evidentiary, not current
navigation. The repository required-file verifier is ignored as a reference source so a file
is not considered active merely because a check asserts that it must exist.
"""

from __future__ import annotations

import subprocess
from pathlib import PurePosixPath


CANDIDATE_ROOTS = {"deploy", "docs", "postman", "scripts", "site"}
EXCLUDED_PREFIXES = ("docs/security/evidence/",)
EXCLUDED_EXACT = {
    "site/index.html",
    "scripts/audit-repository-references.py",
}
IGNORED_REFERENCE_SOURCES = {"scripts/verify-repository.sh"}
TEXT_SUFFIXES = {
    ".cmd", ".css", ".csproj", ".html", ".js", ".json", ".md", ".props",
    ".ps1", ".py", ".sh", ".sln", ".targets", ".txt", ".xml", ".yml", ".yaml",
}


def tracked_files() -> list[str]:
    completed = subprocess.run(["git", "ls-files", "-z"], check=True, capture_output=True)
    return sorted(entry.decode("utf-8") for entry in completed.stdout.split(b"\0") if entry)


def read_text(path: str) -> str | None:
    if PurePosixPath(path).suffix.lower() not in TEXT_SUFFIXES:
        return None
    try:
        with open(path, encoding="utf-8") as handle:
            return handle.read()
    except (UnicodeDecodeError, OSError):
        return None


def is_candidate(path: str) -> bool:
    if path in EXCLUDED_EXACT or path.startswith(EXCLUDED_PREFIXES):
        return False

    parts = PurePosixPath(path).parts
    if not parts:
        return False

    if len(parts) == 1:
        return PurePosixPath(path).suffix.lower() == ".cmd"

    return parts[0] in CANDIDATE_ROOTS


def main() -> int:
    files = tracked_files()
    corpus: dict[str, str] = {}
    for path in files:
        if path in IGNORED_REFERENCE_SOURCES:
            continue
        text = read_text(path)
        if text is not None:
            corpus[path] = text

    candidates: list[str] = []
    for path in files:
        if not is_candidate(path):
            continue

        basename = PurePosixPath(path).name
        references = sum(
            1
            for other_path, text in corpus.items()
            if other_path != path and (path in text or basename in text)
        )
        if references == 0:
            candidates.append(path)

    print(
        f"Repository reference audit: {len(files)} tracked files; "
        f"{len(candidates)} support/document candidates with zero active textual references."
    )
    for path in candidates:
        print(f"ORPHAN-CANDIDATE {path}")

    print(
        "Audit note: candidates are review-only; SDK-discovered source, historical security "
        "evidence, and circular required-file assertions are intentionally excluded."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
