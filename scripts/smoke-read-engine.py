#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import dataclass
from pathlib import Path
from typing import Any

ADMIN_HEADERS = {
    "X-Foundation-User": "33333333-3333-3333-3333-333333333333",
    "X-Foundation-Roles": "admin",
    "X-Foundation-Email": "read-engine@example.com",
}


@dataclass(frozen=True)
class Response:
    status: int
    headers: Any
    body: bytes

    def json(self) -> Any:
        if not self.body:
            return None
        return json.loads(self.body.decode("utf-8"))


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Prove generated FoundationKit filtering/sorting/paging remains SQL-side and bounded."
    )
    parser.add_argument("--url", required=True)
    parser.add_argument("--evidence", type=Path, required=True)
    return parser.parse_args()


def request(
    base_url: str,
    method: str,
    path: str,
    *,
    body: dict[str, Any] | None = None,
    headers: dict[str, str] | None = None,
) -> Response:
    data = None
    request_headers = dict(headers or {})
    if body is not None:
        data = json.dumps(body, separators=(",", ":")).encode("utf-8")
        request_headers["Content-Type"] = "application/json"

    req = urllib.request.Request(
        base_url.rstrip("/") + path,
        data=data,
        headers=request_headers,
        method=method,
    )
    try:
        with urllib.request.urlopen(req, timeout=20) as response:
            return Response(response.status, response.headers, response.read())
    except urllib.error.HTTPError as error:
        return Response(error.code, error.headers, error.read())


def require_status(response: Response, expected: int, label: str) -> None:
    if response.status != expected:
        body = response.body.decode("utf-8", errors="replace")
        raise AssertionError(f"{label}: expected HTTP {expected}, got {response.status}: {body}")


def admin_headers(**extra: str) -> dict[str, str]:
    result = dict(ADMIN_HEADERS)
    result.update(extra)
    return result


def create_customer(base_url: str, name: str, key: str) -> dict[str, Any]:
    response = request(
        base_url,
        "POST",
        "/api/customers",
        body={"name": name, "note": f"proof-{name.lower()}"},
        headers=admin_headers(**{"Idempotency-Key": key}),
    )
    require_status(response, 201, f"create {name}")
    payload = response.json()
    if not isinstance(payload, dict) or payload.get("name") != name:
        raise AssertionError(f"create {name}: unexpected payload {payload!r}")
    return payload


def require_problem_code(response: Response, code: str, label: str) -> None:
    require_status(response, 400, label)
    body = response.body.decode("utf-8", errors="replace")
    if code not in body:
        raise AssertionError(f"{label}: expected problem code {code!r}, got {body}")


def main() -> int:
    args = parse_args()
    base_url = args.url.rstrip("/")

    health = request(base_url, "GET", "/api/foundationkit/health")
    require_status(health, 200, "health")
    health_payload = health.json()
    namespace = health_payload.get("databaseNamespace")
    if not isinstance(namespace, str) or not namespace:
        raise AssertionError(f"health did not expose a database namespace: {health_payload!r}")

    created = [
        create_customer(base_url, "Alpha", "read-engine-alpha"),
        create_customer(base_url, "Alpine", "read-engine-alpine"),
        create_customer(base_url, "Beta", "read-engine-beta"),
    ]

    query = urllib.parse.urlencode(
        [
            ("page", "1"),
            ("pageSize", "20"),
            ("filter", "Name|startswith|Al"),
            ("sort", "Name|desc"),
        ]
    )
    listed = request(
        base_url,
        "GET",
        f"/api/customers?{query}",
        headers=ADMIN_HEADERS,
    )
    require_status(listed, 200, "prefix filter + descending sort")
    payload = listed.json()
    if not isinstance(payload, dict):
        raise AssertionError(f"list returned unexpected payload {payload!r}")
    items = payload.get("items")
    names = [item.get("name") for item in items] if isinstance(items, list) else None
    if names != ["Alpine", "Alpha"]:
        raise AssertionError(f"expected SQL ordered prefix results ['Alpine', 'Alpha'], got {names!r}")
    if payload.get("totalCount") != 2:
        raise AssertionError(f"expected totalCount=2 for prefix filter, got {payload.get('totalCount')!r}")
    if payload.get("page") != 1 or payload.get("pageSize") != 20:
        raise AssertionError(f"unexpected paging metadata: {payload!r}")

    unsupported_field_query = urllib.parse.urlencode(
        [("filter", "Note|eq|proof-alpha")]
    )
    unsupported_field = request(
        base_url,
        "GET",
        f"/api/customers?{unsupported_field_query}",
        headers=ADMIN_HEADERS,
    )
    require_problem_code(
        unsupported_field,
        "Foundation.Crud.Query.FilterFieldUnsupported",
        "undeclared filter field",
    )

    unsupported_operator_query = urllib.parse.urlencode(
        [("filter", "Name|contains|ph")]
    )
    unsupported_operator = request(
        base_url,
        "GET",
        f"/api/customers?{unsupported_operator_query}",
        headers=ADMIN_HEADERS,
    )
    require_problem_code(
        unsupported_operator,
        "Foundation.Crud.Query.FilterOperatorUnsupported",
        "unsupported contains operator",
    )

    evidence = {
        "projectId": health_payload.get("projectId"),
        "databaseNamespace": namespace,
        "createdIds": [item.get("id") for item in created],
        "filter": "Name|startswith|Al",
        "sort": "Name|desc",
        "orderedNames": names,
        "totalCount": payload.get("totalCount"),
        "page": payload.get("page"),
        "pageSize": payload.get("pageSize"),
        "undeclaredFilterRejected": True,
        "unsupportedOperatorRejected": True,
    }
    args.evidence.parent.mkdir(parents=True, exist_ok=True)
    args.evidence.write_text(json.dumps(evidence, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(evidence, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
