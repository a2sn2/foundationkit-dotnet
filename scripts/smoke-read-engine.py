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
        description="Prove generated FoundationKit SQL-first resources and SQL-view read models."
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


def require_status(response: Response, expected: int | tuple[int, ...], label: str) -> None:
    allowed = (expected,) if isinstance(expected, int) else expected
    if response.status not in allowed:
        body = response.body.decode("utf-8", errors="replace")
        raise AssertionError(f"{label}: expected HTTP {allowed}, got {response.status}: {body}")


def admin_headers(**extra: str) -> dict[str, str]:
    result = dict(ADMIN_HEADERS)
    result.update(extra)
    return result


def create_customer(base_url: str, code: str, name: str, key: str) -> dict[str, Any]:
    response = request(
        base_url,
        "POST",
        "/api/customers",
        body={"code": code, "name": name, "note": f"proof-{name.lower()}"},
        headers=admin_headers(**{"Idempotency-Key": key}),
    )
    require_status(response, 201, f"create customer {name}")
    payload = response.json()
    if not isinstance(payload, dict) or payload.get("code") != code or payload.get("name") != name:
        raise AssertionError(f"create customer {name}: unexpected payload {payload!r}")
    return payload


def create_profile(
    base_url: str,
    customer_code: str,
    status: str,
    detail: str,
    key: str,
) -> dict[str, Any]:
    response = request(
        base_url,
        "POST",
        "/api/customer-profiles",
        body={"customerCode": customer_code, "status": status, "detail": detail},
        headers=admin_headers(**{"Idempotency-Key": key}),
    )
    require_status(response, 201, f"create profile {customer_code}")
    payload = response.json()
    if not isinstance(payload, dict) or payload.get("customerCode") != customer_code:
        raise AssertionError(f"create profile {customer_code}: unexpected payload {payload!r}")
    return payload


def require_problem_code(response: Response, code: str, label: str) -> None:
    require_status(response, 400, label)
    body = response.body.decode("utf-8", errors="replace")
    if code not in body:
        raise AssertionError(f"{label}: expected problem code {code!r}, got {body}")


def paged_items(response: Response, label: str) -> tuple[list[dict[str, Any]], dict[str, Any]]:
    require_status(response, 200, label)
    payload = response.json()
    if not isinstance(payload, dict):
        raise AssertionError(f"{label}: unexpected payload {payload!r}")
    items = payload.get("items")
    if not isinstance(items, list) or any(not isinstance(item, dict) for item in items):
        raise AssertionError(f"{label}: items are invalid: {items!r}")
    return items, payload


def query_string(*pairs: tuple[str, str]) -> str:
    return urllib.parse.urlencode(list(pairs))


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
        create_customer(base_url, "C001", "Alpha", "read-engine-alpha"),
        create_customer(base_url, "C002", "Alpine", "read-engine-alpine"),
        create_customer(base_url, "C003", "Beta", "read-engine-beta"),
    ]
    profiles = [
        create_profile(base_url, "C001", "Active", "verified", "read-engine-profile-1"),
        create_profile(base_url, "C002", "Pending", "review", "read-engine-profile-2"),
    ]

    resource_query = query_string(
        ("page", "1"),
        ("pageSize", "20"),
        ("filter", "Name|startswith|Al"),
        ("sort", "Name|desc"),
    )
    resource_list = request(
        base_url,
        "GET",
        f"/api/customers?{resource_query}",
        headers=ADMIN_HEADERS,
    )
    resource_items, resource_payload = paged_items(
        resource_list,
        "resource prefix filter + descending sort",
    )
    resource_names = [item.get("name") for item in resource_items]
    if resource_names != ["Alpine", "Alpha"]:
        raise AssertionError(
            f"expected SQL ordered resource names ['Alpine', 'Alpha'], got {resource_names!r}"
        )
    if resource_payload.get("totalCount") != 2:
        raise AssertionError(
            f"expected resource totalCount=2, got {resource_payload.get('totalCount')!r}"
        )

    unsupported_field = request(
        base_url,
        "GET",
        "/api/customers?" + query_string(("filter", "Note|eq|proof-alpha")),
        headers=ADMIN_HEADERS,
    )
    require_problem_code(
        unsupported_field,
        "Foundation.Crud.Query.FilterFieldUnsupported",
        "undeclared resource filter field",
    )

    unsupported_operator = request(
        base_url,
        "GET",
        "/api/customers?" + query_string(("filter", "Name|contains|ph")),
        headers=ADMIN_HEADERS,
    )
    require_problem_code(
        unsupported_operator,
        "Foundation.Crud.Query.FilterOperatorUnsupported",
        "unsupported resource contains operator",
    )

    directory_query = query_string(
        ("page", "1"),
        ("pageSize", "20"),
        ("filter", "Name|startswith|Al"),
        ("sort", "Name|desc"),
    )
    directory_response = request(
        base_url,
        "GET",
        f"/api/customer-directory?{directory_query}",
        headers=ADMIN_HEADERS,
    )
    directory_items, directory_payload = paged_items(directory_response, "customer directory")
    directory_projection = [
        (item.get("code"), item.get("name"), item.get("profileStatus"))
        for item in directory_items
    ]
    if directory_projection != [
        ("C002", "Alpine", "Pending"),
        ("C001", "Alpha", "Active"),
    ]:
        raise AssertionError(f"unexpected directory view projection: {directory_projection!r}")
    if directory_payload.get("totalCount") != 2:
        raise AssertionError(f"directory totalCount mismatch: {directory_payload!r}")

    active_response = request(
        base_url,
        "GET",
        "/api/customer-directory?" + query_string(("filter", "ProfileStatus|eq|Active")),
        headers=ADMIN_HEADERS,
    )
    active_items, active_payload = paged_items(active_response, "directory joined-field filter")
    if active_payload.get("totalCount") != 1 or [item.get("code") for item in active_items] != ["C001"]:
        raise AssertionError(f"joined-field view filter mismatch: {active_payload!r}")

    left_join_response = request(
        base_url,
        "GET",
        "/api/customer-directory?" + query_string(("filter", "Code|eq|C003")),
        headers=ADMIN_HEADERS,
    )
    left_join_items, left_join_payload = paged_items(left_join_response, "directory left join")
    if left_join_payload.get("totalCount") != 1:
        raise AssertionError(f"left join did not preserve customer without profile: {left_join_payload!r}")
    if left_join_items[0].get("name") != "Beta" or left_join_items[0].get("profileStatus") is not None:
        raise AssertionError(f"left join null projection mismatch: {left_join_items!r}")

    statement_response = request(
        base_url,
        "GET",
        "/api/customer-statements?" + query_string(("filter", "Code|eq|C001")),
        headers=ADMIN_HEADERS,
    )
    statement_items, statement_payload = paged_items(statement_response, "customer statement report")
    if statement_payload.get("totalCount") != 1:
        raise AssertionError(f"statement totalCount mismatch: {statement_payload!r}")
    statement = statement_items[0]
    if (
        statement.get("code") != "C001"
        or statement.get("name") != "Alpha"
        or statement.get("profileStatus") != "Active"
    ):
        raise AssertionError(f"statement projection mismatch: {statement!r}")

    unauthenticated_view = request(base_url, "GET", "/api/customer-directory")
    require_status(unauthenticated_view, (401, 403), "unauthenticated read model")

    mutate_view = request(
        base_url,
        "POST",
        "/api/customer-directory",
        body={"code": "X"},
        headers=ADMIN_HEADERS,
    )
    require_status(mutate_view, 405, "read-model endpoint is GET-only")

    evidence = {
        "projectId": health_payload.get("projectId"),
        "databaseNamespace": namespace,
        "createdCustomerIds": [item.get("id") for item in created],
        "createdProfileIds": [item.get("id") for item in profiles],
        "resourceQuery": {
            "filter": "Name|startswith|Al",
            "sort": "Name|desc",
            "orderedNames": resource_names,
            "totalCount": resource_payload.get("totalCount"),
        },
        "customerDirectory": {
            "projection": directory_projection,
            "totalCount": directory_payload.get("totalCount"),
            "joinedFieldFilter": "ProfileStatus|eq|Active",
            "leftJoinNullPreserved": True,
        },
        "customerStatement": {
            "filter": "Code|eq|C001",
            "row": statement,
        },
        "undeclaredFilterRejected": True,
        "unsupportedOperatorRejected": True,
        "readModelsRequireAuthorization": True,
        "readModelsAreGetOnly": True,
    }
    args.evidence.parent.mkdir(parents=True, exist_ok=True)
    args.evidence.write_text(json.dumps(evidence, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(evidence, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
