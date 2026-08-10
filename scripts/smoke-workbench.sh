#!/usr/bin/env bash
set -euo pipefail

base_url="${WORKBENCH_URL:-http://localhost:8080}"

curl --fail --silent "$base_url/api/health" | grep -q 'healthy'

# Empty framework status codes are normalized into the same Problem Details contract.
method_status="$(curl --silent --output /tmp/foundation-http-method.json --write-out '%{http_code}' -X PATCH "$base_url/api/core-crud")"
test "$method_status" = "405"
grep -q 'Foundation.Http.MethodNotAllowed' /tmp/foundation-http-method.json
grep -q 'correlationId' /tmp/foundation-http-method.json
grep -q 'foundationkit-workbench' /tmp/foundation-http-method.json

# Existing reference paths.
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

# Mutating API operations prove a declared idempotency-key contract.
idempotency_status="$(curl --silent --output /tmp/core-crud-idempotency.json --write-out '%{http_code}' \
  -H 'Content-Type: application/json' \
  -d '{"name":"Missing Key"}' \
  "$base_url/api/core-crud")"
test "$idempotency_status" = "400"
grep -q 'Foundation.Api.IdempotencyKey.Required' /tmp/core-crud-idempotency.json

# DataAnnotations remain the default structural validator after API-level header validation.
annotation_status="$(curl --silent --output /tmp/core-crud-annotation.json --write-out '%{http_code}' \
  -H 'Content-Type: application/json' \
  -H 'Idempotency-Key: annotation-proof' \
  -d '{"name":"   "}' \
  "$base_url/api/core-crud")"
test "$annotation_status" = "400"
grep -q 'Foundation.Crud.Validation' /tmp/core-crud-annotation.json
grep -q 'Name' /tmp/core-crud-annotation.json
grep -q 'foundationkit-workbench' /tmp/core-crud-annotation.json

# API Engine -> generic CRUD service -> EF -> SQL.
crud_create="$(curl --fail --silent -D /tmp/core-crud-create.headers \
  -H 'Content-Type: application/json' \
  -H 'Idempotency-Key: create-core-crud' \
  -d '{"name":"CI Core CRUD"}' \
  "$base_url/api/core-crud")"
crud_id="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])' <<< "$crud_create")"
echo "$crud_create" | grep -q '"version":1'
grep -qi '^etag: "1"' /tmp/core-crud-create.headers

curl --fail --silent -D /tmp/core-crud-get.headers "$base_url/api/core-crud/$crud_id" | grep -q 'CI Core CRUD'
grep -qi '^etag: "1"' /tmp/core-crud-get.headers
curl --fail --silent "$base_url/api/core-crud?page=1&pageSize=20" | grep -q 'CI Core CRUD'

missing_precondition_status="$(curl --silent --output /tmp/core-crud-precondition-required.json --write-out '%{http_code}' -X PUT \
  -H 'Content-Type: application/json' \
  -H 'Idempotency-Key: missing-precondition' \
  -d '{"name":"No precondition"}' \
  "$base_url/api/core-crud/$crud_id")"
test "$missing_precondition_status" = "428"
grep -q 'Foundation.Api.IfMatch.Required' /tmp/core-crud-precondition-required.json

crud_update="$(curl --fail --silent -D /tmp/core-crud-update.headers -X PUT \
  -H 'Content-Type: application/json' \
  -H 'Idempotency-Key: update-core-crud' \
  -H 'If-Match: "1"' \
  -d '{"name":"CI Core CRUD Updated"}' \
  "$base_url/api/core-crud/$crud_id")"
echo "$crud_update" | grep -q 'CI Core CRUD Updated'
echo "$crud_update" | grep -q '"version":2'
grep -qi '^etag: "2"' /tmp/core-crud-update.headers

precondition_status="$(curl --silent --output /tmp/core-crud-precondition.json --write-out '%{http_code}' -X PUT \
  -H 'Content-Type: application/json' \
  -H 'Idempotency-Key: stale-core-crud' \
  -H 'If-Match: "1"' \
  -d '{"name":"Stale Update"}' \
  "$base_url/api/core-crud/$crud_id")"
test "$precondition_status" = "412"
grep -q 'CoreCrud.Version.PreconditionFailed' /tmp/core-crud-precondition.json

# Module-owned filtering/sorting policy is parsed by Core but owns the field semantics.
filtered="$(curl --fail --silent --get \
  --data-urlencode 'page=1' \
  --data-urlencode 'pageSize=20' \
  --data-urlencode 'filter=name|contains|Updated' \
  --data-urlencode 'sort=version|desc' \
  "$base_url/api/core-crud")"
echo "$filtered" | grep -q 'CI Core CRUD Updated'

unsupported_filter_status="$(curl --silent --output /tmp/core-crud-filter.json --write-out '%{http_code}' --get \
  --data-urlencode 'filter=unknown|eq|value' \
  "$base_url/api/core-crud")"
test "$unsupported_filter_status" = "400"
grep -q 'CoreCrud.Query.FilterFieldUnsupported' /tmp/core-crud-filter.json

manager_status="$(curl --silent --output /tmp/core-crud-manager.json --write-out '%{http_code}' \
  -H 'Content-Type: application/json' \
  -H 'Idempotency-Key: manager-proof' \
  -d '{"name":"foundation"}' \
  "$base_url/api/core-crud")"
test "$manager_status" = "422"
grep -q 'CoreCrud.Name.Reserved' /tmp/core-crud-manager.json

curl --fail --silent --output /dev/null -X DELETE \
  -H 'Idempotency-Key: delete-core-crud' \
  "$base_url/api/core-crud/$crud_id"
missing_status="$(curl --silent --output /tmp/core-crud-missing.json --write-out '%{http_code}' "$base_url/api/core-crud/$crud_id")"
test "$missing_status" = "404"

echo "Workbench SQL workflow plus API Engine validation/idempotency/ETag/If-Match/filter/sort/CRUD/error-contract proof passed."
