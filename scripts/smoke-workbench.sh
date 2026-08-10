#!/usr/bin/env bash
set -euo pipefail

base_url="${WORKBENCH_URL:-http://localhost:8080}"

curl --fail --silent "$base_url/api/health" | grep -q 'healthy'

# Existing reference paths.
curl --fail --silent "$base_url/api/catalog" | grep -q 'FoundationKit.Domain'
curl --fail --silent "$base_url/api/catalog" | grep -q 'FoundationKit.Domain'

platform_reference="$(curl --fail --silent "$base_url/api/platform-reference")"
echo "$platform_reference" | grep -q '"defaultCulture":"ar-YE"'
echo "$platform_reference" | grep -q '"textDirection":"RightToLeft"'
echo "$platform_reference" | grep -q '"defaultTimeZone":"UTC"'
echo "$platform_reference" | grep -q '"catalogPreviewEnabled":true'

# Existing connected user/admin workflow.
user_response="$(curl --fail --silent \
  -H 'Content-Type: application/json' \
  -d '{
    "projectName":"CI Workbench",
    "projectType":"Internal platform",
    "audience":"Engineering team",
    "goal":"Verify the executable architecture reference against SQL Server.",
    "selectedCapabilityIds":["commands-queries","ef-repository"],
    "priorities":"Correctness and maintainability",
    "notes":"Automated public-safe smoke test"
  }' \
  "$base_url/api/user/requests")"

request_id="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])' <<< "$user_response")"
echo "$user_response" | grep -q '"status":"submitted"'
curl --fail --silent "$base_url/api/admin/requests?status=submitted" | grep -q 'CI Workbench'

review_response="$(curl --fail --silent \
  -H 'Content-Type: application/json' \
  -d '{"decision":"approve","reviewedBy":"CI Admin","notes":"Validated by integration smoke"}' \
  "$base_url/api/admin/requests/$request_id/review")"
echo "$review_response" | grep -q '"status":"approved"'
curl --fail --silent "$base_url/api/user/requests/$request_id" | grep -q '"status":"approved"'

# Core vNext generic CRUD engine: request -> endpoint -> generic application service -> EF -> SQL.
crud_create="$(curl --fail --silent \
  -H 'Content-Type: application/json' \
  -d '{"name":"CI Core CRUD"}' \
  "$base_url/api/core-crud")"
crud_id="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])' <<< "$crud_create")"
echo "$crud_create" | grep -q '"version":1'

curl --fail --silent "$base_url/api/core-crud/$crud_id" | grep -q 'CI Core CRUD'
curl --fail --silent "$base_url/api/core-crud?page=1&pageSize=20" | grep -q 'CI Core CRUD'

crud_update="$(curl --fail --silent -X PUT \
  -H 'Content-Type: application/json' \
  -d '{"name":"CI Core CRUD Updated","expectedVersion":1}' \
  "$base_url/api/core-crud/$crud_id")"
echo "$crud_update" | grep -q 'CI Core CRUD Updated'
echo "$crud_update" | grep -q '"version":2'

conflict_status="$(curl --silent --output /tmp/core-crud-conflict.json --write-out '%{http_code}' -X PUT \
  -H 'Content-Type: application/json' \
  -d '{"name":"Stale Update","expectedVersion":1}' \
  "$base_url/api/core-crud/$crud_id")"
test "$conflict_status" = "409"
grep -q 'CoreCrud.Version.Conflict' /tmp/core-crud-conflict.json

manager_status="$(curl --silent --output /tmp/core-crud-manager.json --write-out '%{http_code}' \
  -H 'Content-Type: application/json' \
  -d '{"name":"foundation"}' \
  "$base_url/api/core-crud")"
test "$manager_status" = "422"
grep -q 'CoreCrud.Name.Reserved' /tmp/core-crud-manager.json

curl --fail --silent --output /dev/null -X DELETE "$base_url/api/core-crud/$crud_id"
missing_status="$(curl --silent --output /tmp/core-crud-missing.json --write-out '%{http_code}' "$base_url/api/core-crud/$crud_id")"
test "$missing_status" = "404"

echo "Workbench SQL workflow plus generic Core CRUD create/read/list/update/concurrency/manager/delete proof passed."
