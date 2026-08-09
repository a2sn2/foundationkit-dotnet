#!/usr/bin/env python3
"""Report tracked support/documentation files with no textual repository references.

This is an audit aid, not an automatic deletion policy. Source/test/tool project files are
excluded because SDK/MSBuild conventions discover them without textual path references.
Historical security evidence is also excluded because its value is evidentiary, not current
navigation. Every reported path must still be reviewed before deletion.
"""

from __future__ import annotations

import subprocess
from pathlib import PurePosixPath


CANDIDATE_ROOTS = {"deploy", "docs", "postman", "scripts", "site"}
EXCLUDED_PREFIXES = (
    "docs/security/evidence/",
)
EXCLUDED_EXACT = {
    "site/index.html",
    "scripts/audit-repository-references.py",
}
TEXT_SUFFIXES = {
    ".cmd", ".css", ".csproj", ".html", ".js", ".json", ".md", ".props",
    ".ps1", ".py", ".sh", ".sln", ".targets", ".txt", ".xml", ".yml", ".yaml",
}


def tracked_files() -> list[str]:
    completed = subprocess.run(
        ["git", "ls-files", "-z"],
        check=True,
        capture_output=True,
    )
    return sorted(
        entry.decode("utf-8")
        for entry in completed.stdout.split(b"\0")
        if entry
    )


def read_text(path: str) -> str | None:
    file_path = PurePosixPath(path)
    if file_path.suffix.lower() not in TEXT_SUFFIXES:
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
    return bool(parts) and parts[0] in CANDIDATE_ROOTS


def main() -> int:
    files = tracked_files()
    corpus: dict[str, str] = {}
    for path in files:
        text = read_text(path)
        if text is not None:
            corpus[path] = text

    candidates: list[tuple[str, int]] = []
    for path in files:
        if not is_candidate(path):
            continue

        basename = PurePosixPath(path).name
        references = 0
        for other_path, text in corpus.items():
            if other_path == path:
                continue
            if path in text or basename in text:
                references += 1

        if references == 0:
            candidates.append((path, references))

    print(f"Repository reference audit: {len(files)} tracked files; {len(candidates)} support/document candidates with zero textual references.")
    for path, _ in candidates:
        print(f"ORPHAN-CANDIDATE {path}")

    print("Audit note: candidates are review-only; SDK-discovered source and historical security evidence are intentionally excluded.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
