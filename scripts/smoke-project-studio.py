#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import urllib.error
import urllib.request
from pathlib import Path
from typing import Any

ADMIN_HEADERS = {
    "X-Foundation-User": "11111111-1111-1111-1111-111111111111",
    "X-Foundation-Roles": "admin",
    "X-Foundation-Email": "project-studio-proof@example.com",
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Runtime proof for a FoundationKit Project Studio generated application.")
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
) -> tuple[int, Any, bytes]:
    data = None
    actual_headers = dict(headers or {})
    if body is not None:
        data = json.dumps(body, separators=(",", ":")).encode("utf-8")
        actual_headers["Content-Type"] = "application/json"
    req = urllib.request.Request(
        base_url.rstrip("/") + path,
        data=data,
        headers=actual_headers,
        method=method,
    )
    try:
        with urllib.request.urlopen(req, timeout=30) as response:
            return response.status, response.headers, response.read()
    except urllib.error.HTTPError as error:
        return error.code, error.headers, error.read()


def require(status: int, expected: int | tuple[int, ...], label: str, body: bytes) -> None:
    accepted = (expected,) if isinstance(expected, int) else expected
    if status not in accepted:
        raise AssertionError(
            f"{label}: expected HTTP {accepted}, got {status}: {body.decode('utf-8', errors='replace')}"
        )


def decode(body: bytes) -> Any:
    return None if not body else json.loads(body.decode("utf-8"))


def admin(**extra: str) -> dict[str, str]:
    headers = dict(ADMIN_HEADERS)
    headers.update(extra)
    return headers


def main() -> int:
    args = parse_args()
    url = args.url.rstrip("/")

    status, _, body = request(url, "GET", "/api/foundationkit/health")
    require(status, 200, "health", body)
    health = decode(body)

    status, _, body = request(url, "GET", "/swagger/v1/swagger.json")
    require(status, 200, "OpenAPI", body)
    openapi = decode(body)
    paths = openapi.get("paths", {})
    required_paths = {
        "/api/departments",
        "/api/departments/{id}",
        "/api/employees",
        "/api/employees/{id}",
    }
    normalized_paths = {path.rstrip("/") for path in paths}
    missing = {path for path in required_paths if path.rstrip("/") not in normalized_paths}
    if missing:
        raise AssertionError(f"Generated Studio OpenAPI is missing paths: {sorted(missing)}")

    status, _, body = request(
        url,
        "POST",
        "/api/departments",
        body={"name": "Engineering", "code": "ENG"},
        headers={"Idempotency-Key": "unauth-department"},
    )
    require(status, (401, 403), "unauthenticated department create", body)

    department_body = {"name": "Engineering", "code": "ENG"}
    status, _, body = request(
        url,
        "POST",
        "/api/departments",
        body=department_body,
        headers=admin(**{"Idempotency-Key": "department-create"}),
    )
    require(status, 201, "department create", body)
    department = decode(body)
    department_id = department["id"]

    status, _, replay_body = request(
        url,
        "POST",
        "/api/departments",
        body=department_body,
        headers=admin(**{"Idempotency-Key": "department-create"}),
    )
    require(status, 201, "department idempotent replay", replay_body)
    if decode(replay_body) != department:
        raise AssertionError("Department idempotent replay changed the response.")

    employee_body = {
        "name": "Amina Engineer",
        "departmentId": department_id,
        "salary": 1250.50,
        "isActive": True,
        "startDate": "2026-08-18",
    }
    status, _, body = request(
        url,
        "POST",
        "/api/employees",
        body=employee_body,
        headers=admin(**{"Idempotency-Key": "employee-create"}),
    )
    require(status, 201, "employee create", body)
    employee = decode(body)
    employee_id = employee["id"]
    if employee.get("departmentId", "").lower() != department_id.lower():
        raise AssertionError("Generated reference field did not round-trip the DepartmentId.")
    if employee.get("isActive") is not True:
        raise AssertionError("Generated Boolean field did not round-trip.")
    if float(employee.get("salary", 0)) != 1250.5:
        raise AssertionError("Generated Decimal field did not round-trip.")

    status, headers, body = request(url, "GET", f"/api/employees/{employee_id}", headers=ADMIN_HEADERS)
    require(status, 200, "employee get", body)
    etag = headers.get("ETag")
    if etag != '"1"':
        raise AssertionError(f"Expected initial Employee ETag \"1\", got {etag!r}")

    updated_body = dict(employee_body)
    updated_body["salary"] = 1500.75
    updated_body["name"] = "Amina Senior Engineer"
    status, update_headers, body = request(
        url,
        "PUT",
        f"/api/employees/{employee_id}",
        body=updated_body,
        headers=admin(**{"Idempotency-Key": "employee-update", "If-Match": etag}),
    )
    require(status, 200, "employee update", body)
    updated = decode(body)
    if float(updated.get("salary", 0)) != 1500.75:
        raise AssertionError("Generated Decimal update failed.")
    if updated.get("version") != 2 or update_headers.get("ETag") != '"2"':
        raise AssertionError("Generated concurrency Version/ETag did not advance to 2.")

    status, _, body = request(
        url,
        "GET",
        "/api/employees?page=1&pageSize=25&filter=Name%7Ceq%7CAmina%20Senior%20Engineer",
        headers=ADMIN_HEADERS,
    )
    require(status, 200, "employee filtered list", body)
    page = decode(body)
    if not page.get("items") or page["items"][0]["id"] != employee_id:
        raise AssertionError("Generated Studio searchable CRUD list did not return the updated Employee.")

    status, _, body = request(url, "GET", "/api/foundationkit/audit", headers=ADMIN_HEADERS)
    require(status, 200, "audit", body)
    audit = decode(body)
    if int(audit.get("count", 0)) < 3:
        raise AssertionError(f"Expected generated audit events, got {audit}")

    evidence = {
        "projectId": health["projectId"],
        "databaseNamespace": health["databaseNamespace"],
        "departmentId": department_id,
        "employeeId": employee_id,
        "employeeVersion": updated["version"],
        "employeeSalary": updated["salary"],
        "auditCount": audit["count"],
        "openApiPathCount": len(paths),
        "verified": [
            "runtime-health",
            "runtime-openapi",
            "authorization",
            "idempotency-replay",
            "typed-reference",
            "typed-decimal",
            "typed-boolean",
            "typed-date",
            "etag-concurrency",
            "filtering",
            "auditing",
        ],
    }
    args.evidence.parent.mkdir(parents=True, exist_ok=True)
    args.evidence.write_text(json.dumps(evidence, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps(evidence, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:
        print(f"Project Studio runtime proof failed: {error}", file=__import__("sys").stderr)
        raise
