#!/usr/bin/env bash
set -euo pipefail

base_url="${MADAR_BASE_URL:-http://localhost:8100}"
: "${MADAR_ADMIN_EMAIL:?MADAR_ADMIN_EMAIL is required}"
: "${MADAR_ADMIN_PASSWORD:?MADAR_ADMIN_PASSWORD is required}"
: "${MADAR_OPERATOR_EMAIL:?MADAR_OPERATOR_EMAIL is required}"
: "${MADAR_OPERATOR_PASSWORD:?MADAR_OPERATOR_PASSWORD is required}"

workdir="$(mktemp -d)"
trap 'rm -rf "$workdir"' EXIT
admin_cookie="$workdir/admin.cookies"
operator_cookie="$workdir/operator.cookies"

csrf_token() {
  local cookie_file="$1"
  curl --fail --silent --show-error \
    -c "$cookie_file" \
    -b "$cookie_file" \
    "$base_url/api/security/antiforgery" \
    | python3 -c 'import json,sys; print(json.load(sys.stdin)["token"])'
}

login() {
  local cookie_file="$1"
  local email="$2"
  local password="$3"
  local token payload
  token="$(csrf_token "$cookie_file")"
  payload="$(python3 -c 'import json,sys; print(json.dumps({"email":sys.argv[1],"password":sys.argv[2],"rememberMe":False}))' "$email" "$password")"
  curl --fail --silent --show-error \
    -c "$cookie_file" \
    -b "$cookie_file" \
    -H 'Content-Type: application/json' \
    -H "X-CSRF-TOKEN: $token" \
    -d "$payload" \
    "$base_url/api/auth/login"
}

reset_madar_api() {
  docker compose -f deploy/madar-compose.yml restart madar-api >/dev/null
  for attempt in {1..60}; do
    if curl --fail --silent "$base_url/health/ready" >/dev/null; then
      return 0
    fi
    if [ "$attempt" -eq 60 ]; then
      echo "Madar did not become ready after resetting the in-memory rate limiter." >&2
      docker compose -f deploy/madar-compose.yml ps >&2
      exit 1
    fi
    sleep 2
  done
}

admin_login="$(login "$admin_cookie" "$MADAR_ADMIN_EMAIL" "$MADAR_ADMIN_PASSWORD")"
python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["isAuthenticated"] is True; assert "Administrator" in item["roles"]' <<< "$admin_login"
admin_token="$(csrf_token "$admin_cookie")"

departments_json="$(curl --fail --silent --show-error \
  -b "$admin_cookie" \
  "$base_url/api/departments")"
department_id="$(python3 -c 'import json,sys; items=json.load(sys.stdin); matches=[item for item in items if item["code"] == "operations" and item["isActive"]]; assert len(matches) == 1, matches; print(matches[0]["id"])' <<< "$departments_json")"

create_payload='{"title":"Department routing smoke case","description":"Case used to prove department route queue claim persistence and authorization through real SQL Server.","caseType":"internal-service-request","priority":"medium"}'
case_json="$(curl --fail --silent --show-error \
  -c "$admin_cookie" \
  -b "$admin_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $admin_token" \
  -d "$create_payload" \
  "$base_url/api/cases/")"
case_id="$(python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["status"] == "new"; assert item["departmentId"] is None; print(item["id"])' <<< "$case_json")"

route_payload="$(python3 -c 'import json,sys; print(json.dumps({"departmentId":sys.argv[1]}))' "$department_id")"
routed_json="$(curl --fail --silent --show-error \
  -c "$admin_cookie" \
  -b "$admin_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $admin_token" \
  -d "$route_payload" \
  "$base_url/api/cases/$case_id/route")"
python3 -c 'import json,sys; department_id=sys.argv[1]; item=json.load(sys.stdin); assert item["status"] == "new"; assert item["assignedToUserId"] is None; assert item["departmentId"] == department_id; assert item["routedUtc"] is not None' "$department_id" <<< "$routed_json"

admin_queue="$(curl --fail --silent --show-error \
  -b "$admin_cookie" \
  "$base_url/api/departments/$department_id/queue")"
python3 -c 'import json,sys; case_id=sys.argv[1]; department_id=sys.argv[2]; item=json.load(sys.stdin); assert item["department"]["id"] == department_id; assert any(case["id"] == case_id for case in item["cases"])' "$case_id" "$department_id" <<< "$admin_queue"

operator_login="$(login "$operator_cookie" "$MADAR_OPERATOR_EMAIL" "$MADAR_OPERATOR_PASSWORD")"
operator_id="$(python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["isAuthenticated"] is True; assert "Operator" in item["roles"]; print(item["userId"])' <<< "$operator_login")"
operator_token="$(csrf_token "$operator_cookie")"

operator_departments="$(curl --fail --silent --show-error \
  -b "$operator_cookie" \
  "$base_url/api/departments")"
python3 -c 'import json,sys; department_id=sys.argv[1]; items=json.load(sys.stdin); assert any(item["id"] == department_id for item in items)' "$department_id" <<< "$operator_departments"

operator_queue="$(curl --fail --silent --show-error \
  -b "$operator_cookie" \
  "$base_url/api/departments/$department_id/queue")"
python3 -c 'import json,sys; case_id=sys.argv[1]; item=json.load(sys.stdin); assert any(case["id"] == case_id and case["status"] == "new" for case in item["cases"])' "$case_id" <<< "$operator_queue"

claimed_json="$(curl --fail --silent --show-error \
  -c "$operator_cookie" \
  -b "$operator_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $operator_token" \
  -d '{}' \
  "$base_url/api/cases/$case_id/claim")"
python3 -c 'import json,sys; operator_id=sys.argv[1]; department_id=sys.argv[2]; item=json.load(sys.stdin); assert item["status"] == "assigned"; assert item["assignedToUserId"] == operator_id; assert item["departmentId"] == department_id' "$operator_id" "$department_id" <<< "$claimed_json"

queue_after_claim="$(curl --fail --silent --show-error \
  -b "$operator_cookie" \
  "$base_url/api/departments/$department_id/queue")"
python3 -c 'import json,sys; case_id=sys.argv[1]; item=json.load(sys.stdin); assert all(case["id"] != case_id for case in item["cases"])' "$case_id" <<< "$queue_after_claim"

persisted_case="$(curl --fail --silent --show-error \
  -b "$operator_cookie" \
  "$base_url/api/cases/$case_id")"
python3 -c 'import json,sys; operator_id=sys.argv[1]; department_id=sys.argv[2]; item=json.load(sys.stdin); assert item["status"] == "assigned"; assert item["assignedToUserId"] == operator_id; assert item["departmentId"] == department_id; assert item["routedUtc"] is not None' "$operator_id" "$department_id" <<< "$persisted_case"

timeline="$(curl --fail --silent --show-error \
  -b "$operator_cookie" \
  "$base_url/api/cases/$case_id/timeline")"
python3 -c 'import json,sys; department_id=sys.argv[1]; operator_id=sys.argv[2]; items=json.load(sys.stdin); routed=[item for item in items if item["action"] == "madar.case.routed"]; claimed=[item for item in items if item["action"] == "madar.case.claimed"]; assert len(routed) == 1; assert len(claimed) == 1; assert routed[0]["attributes"] == {"departmentId":department_id}; assert claimed[0]["attributes"] == {"departmentId":department_id,"claimantUserId":operator_id}' "$department_id" "$operator_id" <<< "$timeline"

echo "Madar department route + queue + claim SQL workflow passed for case $case_id in department $department_id"

# Keep independent SQL suites deterministic while preserving the production limiter.
reset_madar_api
bash scripts/smoke-madar-department-admin.sh

# Department administration consumes real write permits too; isolate the v0.8
# transfer/reassignment proof rather than weakening the application rate limit.
reset_madar_api
bash scripts/smoke-madar-transfer.sh

# Attachments use the same protected write limiter. Reset only the in-memory
# limiter between independent SQL suites; do not weaken the product policy.
reset_madar_api
bash scripts/smoke-madar-attachments.sh

# Search/reporting creates a small private test set before executing read-only
# queries. Reset the in-memory limiter to keep this independent suite stable.
reset_madar_api
bash scripts/smoke-madar-search.sh
