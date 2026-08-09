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

create_case() {
  local cookie_file="$1"
  local token="$2"
  local title="$3"
  local case_type="$4"
  local priority="$5"
  local payload
  payload="$(python3 -c 'import json,sys; print(json.dumps({"title":sys.argv[1],"description":"Search and reporting SQL smoke case with enough detail to satisfy Madar validation.","caseType":sys.argv[2],"priority":sys.argv[3]}))' "$title" "$case_type" "$priority")"
  curl --fail --silent --show-error \
    -c "$cookie_file" \
    -b "$cookie_file" \
    -H 'Content-Type: application/json' \
    -H "X-CSRF-TOKEN: $token" \
    -d "$payload" \
    "$base_url/api/cases/"
}

admin_login="$(login "$admin_cookie" "$MADAR_ADMIN_EMAIL" "$MADAR_ADMIN_PASSWORD")"
python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["isAuthenticated"] is True; assert "Administrator" in item["roles"]' <<< "$admin_login"
admin_token="$(csrf_token "$admin_cookie")"

operator_login="$(login "$operator_cookie" "$MADAR_OPERATOR_EMAIL" "$MADAR_OPERATOR_PASSWORD")"
operator_id="$(python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["isAuthenticated"] is True; assert "Operator" in item["roles"]; print(item["userId"])' <<< "$operator_login")"
operator_token="$(csrf_token "$operator_cookie")"

marker="searchscope-$(python3 -c 'import uuid; print(uuid.uuid4().hex[:12])')"
hidden_json="$(create_case "$admin_cookie" "$admin_token" "$marker hidden admin case" "operational-incident" "critical")"
hidden_id="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])' <<< "$hidden_json")"

owned_json="$(create_case "$operator_cookie" "$operator_token" "$marker owned operator case" "technical-escalation" "high")"
owned_id="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])' <<< "$owned_json")"

assigned_json="$(create_case "$admin_cookie" "$admin_token" "$marker assigned operator case" "internal-service-request" "medium")"
assigned_id="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])' <<< "$assigned_json")"
assign_payload="$(python3 -c 'import json,sys; print(json.dumps({"assignedToUserId":sys.argv[1]}))' "$operator_id")"
curl --fail --silent --show-error \
  -c "$admin_cookie" \
  -b "$admin_cookie" \
  -H 'Content-Type: application/json' \
  -H "X-CSRF-TOKEN: $admin_token" \
  -d "$assign_payload" \
  "$base_url/api/cases/$assigned_id/assignment" >/dev/null

encoded_marker="$(python3 -c 'import sys,urllib.parse; print(urllib.parse.quote(sys.argv[1], safe=""))' "$marker")"
operator_search="$(curl --fail --silent --show-error \
  -b "$operator_cookie" \
  "$base_url/api/cases/search?query=$encoded_marker&offset=0&limit=25")"
python3 -c 'import json,sys; hidden,owned,assigned=sys.argv[1:4]; item=json.load(sys.stdin); ids={case["id"] for case in item["items"]}; assert item["total"] == 2, item; assert item["summary"]["total"] == 2, item; assert hidden not in ids; assert ids == {owned,assigned}; assert item["summary"]["assigned"] == 1; assert item["summary"]["new"] == 1' "$hidden_id" "$owned_id" "$assigned_id" <<< "$operator_search"

admin_search="$(curl --fail --silent --show-error \
  -b "$admin_cookie" \
  "$base_url/api/cases/search?query=$encoded_marker&offset=0&limit=25")"
python3 -c 'import json,sys; expected=set(sys.argv[1:4]); item=json.load(sys.stdin); ids={case["id"] for case in item["items"]}; assert item["total"] == 3, item; assert item["summary"]["total"] == 3; assert item["summary"]["unassigned"] == 2; assert item["summary"]["assigned"] == 1; assert ids == expected' "$hidden_id" "$owned_id" "$assigned_id" <<< "$admin_search"

assigned_filter="$(curl --fail --silent --show-error \
  -b "$operator_cookie" \
  "$base_url/api/cases/search?query=$encoded_marker&status=assigned&offset=0&limit=25")"
python3 -c 'import json,sys; assigned=sys.argv[1]; item=json.load(sys.stdin); assert item["total"] == 1; assert item["summary"]["total"] == 1; assert [case["id"] for case in item["items"]] == [assigned]' "$assigned_id" <<< "$assigned_filter"

high_filter="$(curl --fail --silent --show-error \
  -b "$operator_cookie" \
  "$base_url/api/cases/search?query=$encoded_marker&priority=high&offset=0&limit=25")"
python3 -c 'import json,sys; owned=sys.argv[1]; item=json.load(sys.stdin); assert item["total"] == 1; assert [case["id"] for case in item["items"]] == [owned]' "$owned_id" <<< "$high_filter"

page_one="$(curl --fail --silent --show-error \
  -b "$operator_cookie" \
  "$base_url/api/cases/search?query=$encoded_marker&offset=0&limit=1")"
page_two="$(curl --fail --silent --show-error \
  -b "$operator_cookie" \
  "$base_url/api/cases/search?query=$encoded_marker&offset=1&limit=1")"
python3 -c 'import json,sys; hidden,owned,assigned=sys.argv[1:4]; first=json.loads(sys.argv[4]); second=json.loads(sys.argv[5]); ids={first["items"][0]["id"],second["items"][0]["id"]}; assert first["total"] == 2 and second["total"] == 2; assert first["summary"]["total"] == 2 and second["summary"]["total"] == 2; assert hidden not in ids; assert ids == {owned,assigned}' "$hidden_id" "$owned_id" "$assigned_id" "$page_one" "$page_two"

invalid_status="$(curl --silent --show-error \
  -o "$workdir/invalid.json" \
  -w '%{http_code}' \
  -b "$operator_cookie" \
  "$base_url/api/cases/search?status=not-a-status&offset=0&limit=25")"
test "$invalid_status" = "400"
python3 -c 'import json,sys; item=json.load(open(sys.argv[1], encoding="utf-8")); assert item["title"] == "Madar.Search.InvalidStatus"' "$workdir/invalid.json"

invalid_limit="$(curl --silent --show-error \
  -o "$workdir/limit.json" \
  -w '%{http_code}' \
  -b "$operator_cookie" \
  "$base_url/api/cases/search?offset=0&limit=101")"
test "$invalid_limit" = "400"
python3 -c 'import json,sys; item=json.load(open(sys.argv[1], encoding="utf-8")); assert item["title"] == "Madar.Search.InvalidLimit"' "$workdir/limit.json"

echo "Madar authorized search + same-scope reporting SQL workflow passed for marker $marker"
