#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
import urllib.error
import urllib.request
from dataclasses import dataclass
from pathlib import Path
from typing import Any

ADMIN_USER = "11111111-1111-1111-1111-111111111111"
ADMIN_HEADERS = {
    "X-Foundation-User": ADMIN_USER,
    "X-Foundation-Roles": "admin",
    "X-Foundation-Email": "phase12@example.com",
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
        description="Prove generated Composer full-stack projects A/B at runtime."
    )
    parser.add_argument("--a-url", required=True)
    parser.add_argument("--b-url", required=True)
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


def require_security_envelope(response: Response, label: str) -> None:
    if response.headers.get("X-Content-Type-Options", "").lower() != "nosniff":
        raise AssertionError(f"{label}: missing FoundationKit X-Content-Type-Options security header")
    if response.headers.get("X-Frame-Options", "").upper() != "DENY":
        raise AssertionError(f"{label}: missing FoundationKit X-Frame-Options security header")


def admin_headers(**extra: str) -> dict[str, str]:
    result = dict(ADMIN_HEADERS)
    result.update(extra)
    return result


def normalized_path(document: dict[str, Any], wanted: str) -> tuple[str, dict[str, Any]]:
    paths = document.get("paths", {})
    for path, item in paths.items():
        if path.rstrip("/") == wanted.rstrip("/"):
            if not isinstance(item, dict):
                break
            return path, item
    raise AssertionError(f"OpenAPI path not found: {wanted}")


def header_parameter(operation: dict[str, Any], name: str) -> dict[str, Any]:
    for parameter in operation.get("parameters", []):
        if (
            isinstance(parameter, dict)
            and str(parameter.get("in", "")).lower() == "header"
            and str(parameter.get("name", "")).lower() == name.lower()
        ):
            return parameter
    raise AssertionError(f"OpenAPI header parameter not found: {name}")


def validate_openapi(base_url: str) -> dict[str, Any]:
    response = request(base_url, "GET", "/swagger/v1/swagger.json")
    require_status(response, 200, "OpenAPI")
    document = response.json()
    if not str(document.get("openapi", "")).startswith("3."):
        raise AssertionError("Generated API did not publish OpenAPI 3.x")

    _, collection = normalized_path(document, "/api/customers")
    _, item = normalized_path(document, "/api/customers/{id}")
    for method in ("get", "post"):
        if method not in collection:
            raise AssertionError(f"Generated OpenAPI missing {method.upper()} /api/customers")
    for method in ("get", "put", "delete"):
        if method not in item:
            raise AssertionError(f"Generated OpenAPI missing {method.upper()} /api/customers/{{id}}")

    post_key = header_parameter(collection["post"], "Idempotency-Key")
    put_key = header_parameter(item["put"], "Idempotency-Key")
    if_match = header_parameter(item["put"], "If-Match")
    delete_key = header_parameter(item["delete"], "Idempotency-Key")
    if not all(bool(parameter.get("required")) for parameter in (post_key, put_key, if_match, delete_key)):
        raise AssertionError("Generated OpenAPI did not preserve required idempotency/concurrency headers")

    schemes = document.get("components", {}).get("securitySchemes", {})
    for scheme in ("FoundationGeneratedUser", "FoundationGeneratedRoles"):
        if scheme not in schemes:
            raise AssertionError(f"Generated OpenAPI missing reference auth scheme {scheme}")

    return document


def main() -> int:
    args = parse_args()
    a_url = args.a_url.rstrip("/")
    b_url = args.b_url.rstrip("/")

    health_a = request(a_url, "GET", "/api/foundationkit/health")
    health_b = request(b_url, "GET", "/api/foundationkit/health")
    require_status(health_a, 200, "health A")
    require_status(health_b, 200, "health B")
    health_a_json = health_a.json()
    health_b_json = health_b.json()
    if health_a_json["projectId"] == health_b_json["projectId"]:
        raise AssertionError("Generated project identities collided")
    if health_a_json["databaseNamespace"] == health_b_json["databaseNamespace"]:
        raise AssertionError("Generated SQL namespaces collided")

    unauthenticated = request(
        a_url,
        "POST",
        "/api/customers",
        body={"name": "Alpha", "note": "shared"},
        headers={"Idempotency-Key": "unauthorized-before-replay"},
    )
    require_status(unauthenticated, (401, 403), "unauthenticated create")
    require_security_envelope(unauthenticated, "unauthenticated create")

    invalid = request(
        a_url,
        "POST",
        "/api/customers",
        body={"note": "missing required name"},
        headers=admin_headers(**{"Idempotency-Key": "validation-proof"}),
    )
    require_status(invalid, 400, "generated DataAnnotations validation")

    shared_body = {"name": "Alpha", "note": "shared"}
    create_a = request(
        a_url,
        "POST",
        "/api/customers",
        body=shared_body,
        headers=admin_headers(**{"Idempotency-Key": "shared-proof-key"}),
    )
    require_status(create_a, 201, "create A")
    created_a = create_a.json()
    a_id = created_a["id"]
    if created_a.get("version") != 1:
        raise AssertionError("Generated A create did not start at version 1")

    replay_a = request(
        a_url,
        "POST",
        "/api/customers",
        body=shared_body,
        headers=admin_headers(**{"Idempotency-Key": "shared-proof-key"}),
    )
    require_status(replay_a, 201, "create replay A")
    if replay_a.json() != created_a:
        raise AssertionError("Generated A create replay changed the response")

    replay_without_auth = request(
        a_url,
        "POST",
        "/api/customers",
        body=shared_body,
        headers={"Idempotency-Key": "shared-proof-key"},
    )
    require_status(replay_without_auth, (401, 403), "unauthenticated replay")
    require_security_envelope(replay_without_auth, "unauthenticated replay")

    conflict = request(
        a_url,
        "POST",
        "/api/customers",
        body={"name": "Different", "note": "same key must conflict"},
        headers=admin_headers(**{"Idempotency-Key": "shared-proof-key"}),
    )
    require_status(conflict, 409, "create fingerprint conflict A")

    get_a = request(a_url, "GET", f"/api/customers/{a_id}", headers=ADMIN_HEADERS)
    require_status(get_a, 200, "get A")
    if get_a.headers.get("ETag") != '"1"':
        raise AssertionError(f"Generated A initial ETag mismatch: {get_a.headers.get('ETag')}")

    update_without_match = request(
        a_url,
        "PUT",
        f"/api/customers/{a_id}",
        body={"name": "Alpha Updated", "note": "v2"},
        headers=admin_headers(**{"Idempotency-Key": "update-proof"}),
    )
    require_status(update_without_match, 428, "missing If-Match")

    stale_update = request(
        a_url,
        "PUT",
        f"/api/customers/{a_id}",
        body={"name": "Alpha Updated", "note": "v2"},
        headers=admin_headers(**{"Idempotency-Key": "stale-update", "If-Match": '"99"'}),
    )
    require_status(stale_update, 412, "stale If-Match")

    update_a = request(
        a_url,
        "PUT",
        f"/api/customers/{a_id}",
        body={"name": "Alpha Updated", "note": "v2"},
        headers=admin_headers(**{"Idempotency-Key": "update-proof", "If-Match": '"1"'}),
    )
    require_status(update_a, 200, "update A")
    updated_a = update_a.json()
    if updated_a.get("version") != 2 or update_a.headers.get("ETag") != '"2"':
        raise AssertionError("Generated A update did not advance Version/ETag to 2")

    replay_update = request(
        a_url,
        "PUT",
        f"/api/customers/{a_id}",
        body={"name": "Alpha Updated", "note": "v2"},
        headers=admin_headers(**{"Idempotency-Key": "update-proof", "If-Match": '"1"'}),
    )
    require_status(replay_update, 200, "update replay A")
    if replay_update.json().get("version") != 2:
        raise AssertionError("Generated A update replay executed the side effect twice")

    update_fingerprint_conflict = request(
        a_url,
        "PUT",
        f"/api/customers/{a_id}",
        body={"name": "Alpha Updated", "note": "v2"},
        headers=admin_headers(**{"Idempotency-Key": "update-proof", "If-Match": '"2"'}),
    )
    require_status(update_fingerprint_conflict, 409, "If-Match fingerprint conflict")

    delete_create = request(
        a_url,
        "POST",
        "/api/customers",
        body={"name": "Delete Me", "note": None},
        headers=admin_headers(**{"Idempotency-Key": "delete-target-create"}),
    )
    require_status(delete_create, 201, "delete target create")
    delete_id = delete_create.json()["id"]

    delete_a = request(
        a_url,
        "DELETE",
        f"/api/customers/{delete_id}",
        headers=admin_headers(**{"Idempotency-Key": "delete-proof"}),
    )
    require_status(delete_a, 204, "delete A")
    delete_replay = request(
        a_url,
        "DELETE",
        f"/api/customers/{delete_id}",
        headers=admin_headers(**{"Idempotency-Key": "delete-proof"}),
    )
    require_status(delete_replay, 204, "delete replay A")
    deleted_get = request(a_url, "GET", f"/api/customers/{delete_id}", headers=ADMIN_HEADERS)
    require_status(deleted_get, 404, "deleted A resource")

    audit_a = request(a_url, "GET", "/api/foundationkit/audit", headers=ADMIN_HEADERS)
    require_status(audit_a, 200, "audit A")
    audit_json = audit_a.json()
    if int(audit_json.get("count", 0)) < 4:
        raise AssertionError(f"Generated audit proof recorded too few events: {audit_json}")
    audit_unauth = request(a_url, "GET", "/api/foundationkit/audit")
    require_status(audit_unauth, (401, 403), "unauthenticated audit evidence")
    require_security_envelope(audit_unauth, "unauthenticated audit evidence")

    create_b = request(
        b_url,
        "POST",
        "/api/customers",
        body=shared_body,
        headers=admin_headers(**{"Idempotency-Key": "shared-proof-key"}),
    )
    require_status(create_b, 201, "create B with same idempotency key")
    created_b = create_b.json()
    b_id = created_b["id"]
    if b_id == a_id:
        raise AssertionError("Generated projects produced the same independent resource ID")

    cross_a_from_b = request(b_url, "GET", f"/api/customers/{a_id}", headers=ADMIN_HEADERS)
    cross_b_from_a = request(a_url, "GET", f"/api/customers/{b_id}", headers=ADMIN_HEADERS)
    require_status(cross_a_from_b, 404, "B must not see A data")
    require_status(cross_b_from_a, 404, "A must not see B data")

    openapi_a = validate_openapi(a_url)
    openapi_b = validate_openapi(b_url)

    evidence = {
        "projectA": {
            "url": a_url,
            "projectId": health_a_json["projectId"],
            "databaseNamespace": health_a_json["databaseNamespace"],
            "resourceId": a_id,
            "version": updated_a["version"],
            "auditCount": audit_json["count"],
            "openApiPathCount": len(openapi_a.get("paths", {})),
        },
        "projectB": {
            "url": b_url,
            "projectId": health_b_json["projectId"],
            "databaseNamespace": health_b_json["databaseNamespace"],
            "resourceId": b_id,
            "version": created_b["version"],
            "openApiPathCount": len(openapi_b.get("paths", {})),
        },
        "proofs": {
            "validation": True,
            "authorization": True,
            "securityEnvelopeOnAuthFailure": True,
            "authBeforeIdempotencyReplay": True,
            "createReplay": True,
            "fingerprintConflict": True,
            "concurrency": True,
            "updateReplay": True,
            "deleteReplay": True,
            "auditing": True,
            "crossProjectDataIsolation": True,
            "sameIdempotencyKeyAcrossProjects": True,
            "openApi": True,
        },
    }
    args.evidence.parent.mkdir(parents=True, exist_ok=True)
    args.evidence.write_text(json.dumps(evidence, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(evidence, indent=2))
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as error:
        print(f"ERROR: {error}", file=sys.stderr)
        raise SystemExit(1) from error
