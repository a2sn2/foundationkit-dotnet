#!/usr/bin/env bash
set -euo pipefail

base_url="${WORKBENCH_URL:-http://localhost:8080}"

curl --fail --silent "$base_url/api/health" | grep -q 'healthy'

# The native ASP.NET Core OpenAPI document is an additive platform-leverage surface.
# Canonical Swagger remains the deterministic transport SSOT until parity is separately proven.
curl --fail --silent "$base_url/openapi/v1.json" > /tmp/foundationkit-native-openapi.json
python3 - <<'PY'
import json
from pathlib import Path

document = json.loads(Path("/tmp/foundationkit-native-openapi.json").read_text(encoding="utf-8"))
paths = document.get("paths", {})
if not paths:
    raise SystemExit("Native ASP.NET Core OpenAPI document has no paths")
if "/api/core-crud" not in paths:
    raise SystemExit("Native ASP.NET Core OpenAPI is missing /api/core-crud")
print("Native ASP.NET Core OpenAPI runtime document verified.")
PY

# The hosted Workbench must expose the first-party Razor Class Library assets that its
# Blazor shell imports. A successful build/publish alone is not enough proof: these URLs
# must resolve from the running server with executable/stylesheet content types.
curl --fail --silent --show-error \
  -D /tmp/foundationkit-blazor-css.headers \
  -o /tmp/foundationkit-blazor.css \
  "$base_url/_content/FoundationKit.Blazor/foundationkit.css"
grep -Eqi '^content-type: text/css([;[:space:]]|$)' /tmp/foundationkit-blazor-css.headers
grep -q -- '--fk-color-primary:' /tmp/foundationkit-blazor.css

curl --fail --silent --show-error \
  -D /tmp/foundationkit-blazor-js.headers \
  -o /tmp/foundationkit-blazor.js \
  "$base_url/_content/FoundationKit.Blazor/foundationkit.js"
grep -Eqi '^content-type: (text|application)/javascript([;[:space:]]|$)' /tmp/foundationkit-blazor-js.headers
grep -q 'window.FoundationKitTheme' /tmp/foundationkit-blazor.js
grep -q 'window.FoundationKitLocale' /tmp/foundationkit-blazor.js

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

# Module composition exposes declared intent separately from dependency-expanded effective intent.
modules_json="$(curl --fail --silent "$base_url/api/modules")"
python3 - "$modules_json" <<'PY'
import json
import sys

modules = json.loads(sys.argv[1])
if len(modules) != 1:
    raise SystemExit(f"Expected one Workbench module, got {len(modules)}")
module = modules[0]
if module.get("name") != "CoreCrud" or module.get("apiRoute") != "/api/core-crud":
    raise SystemExit(f"Unexpected module snapshot: {module}")

declared = module.get("declaredCapabilities", [])
effective = module.get("effectiveCapabilities", [])
expected_declared = [
    "Crud",
    "Auditing",
    "Authorization",
    "Concurrency",
    "Caching",
    "FeatureManagement",
    "Localization",
]
if declared != expected_declared:
    raise SystemExit(f"Unexpected declared capabilities: {declared}")
for capability in ["Security", "Identity", "Settings"]:
    if capability not in effective:
        raise SystemExit(f"Effective capability closure is missing {capability}: {effective}")
print("Workbench module composition runtime snapshot verified.")
PY

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

# Mutating API operations prove the declared idempotency-key contract before durable acquisition.
idempotency_status="$(curl --silent --output /tmp/core-crud-idempotency.json --write-out '%{http_code}' \
  -H 'Content-Type: application/json' \
  -d '{"name":"Missing Key"}' \
  "$base_url/api/core-crud")"
test "$idempotency_status" = "400"
grep -q 'Foundation.Api.IdempotencyKey.Required' /tmp/core-crud-idempotency.json

ambiguous_idempotency_status="$(curl --silent --output /tmp/core-crud-idempotency-ambiguous.json --write-out '%{http_code}' \
  -H 'Content-Type: application/json' \
  -H 'Idempotency-Key: one' \
  -H 'Idempotency-Key: two' \
  -d '{"name":"Ambiguous Key"}' \
  "$base_url/api/core-crud")"
test "$ambiguous_idempotency_status" = "400"
grep -q 'Foundation.Api.IdempotencyKey.Invalid' /tmp/core-crud-idempotency-ambiguous.json

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

# API Engine -> durable idempotency -> generic CRUD service -> EF -> SQL.
crud_create="$(curl --fail --silent -D /tmp/core-crud-create.headers \
  -H 'Content-Type: application/json' \
  -H 'Idempotency-Key: create-core-crud' \
  -d '{"name":"CI Core CRUD"}' \
  "$base_url/api/core-crud")"
crud_id="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])' <<< "$crud_create")"
echo "$crud_create" | grep -q '"version":1'
grep -qi '^etag: "1"' /tmp/core-crud-create.headers

# Same key + same fingerprint replays the exact create result instead of inserting another row.
crud_create_replay="$(curl --fail --silent -D /tmp/core-crud-create-replay.headers \
  -H 'Content-Type: application/json' \
  -H 'Idempotency-Key: create-core-crud' \
  -d '{"name":"CI Core CRUD"}' \
  "$base_url/api/core-crud")"
replayed_crud_id="$(python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])' <<< "$crud_create_replay")"
test "$replayed_crud_id" = "$crud_id"
echo "$crud_create_replay" | grep -q '"version":1'
grep -qi '^etag: "1"' /tmp/core-crud-create-replay.headers

# Same key cannot be silently reused for changed request data.
create_conflict_status="$(curl --silent --output /tmp/core-crud-create-fingerprint.json --write-out '%{http_code}' \
  -H 'Content-Type: application/json' \
  -H 'Idempotency-Key: create-core-crud' \
  -d '{"name":"Changed Body"}' \
  "$base_url/api/core-crud")"
test "$create_conflict_status" = "409"
grep -q 'Foundation.Api.Idempotency.FingerprintConflict' /tmp/core-crud-create-fingerprint.json

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

long_if_match="$(python3 -c 'print("x" * 300)')"
invalid_precondition_status="$(curl --silent --output /tmp/core-crud-precondition-invalid.json --write-out '%{http_code}' -X PUT \
  -H 'Content-Type: application/json' \
  -H 'Idempotency-Key: invalid-precondition' \
  -H "If-Match: $long_if_match" \
  -d '{"name":"Invalid precondition"}' \
  "$base_url/api/core-crud/$crud_id")"
test "$invalid_precondition_status" = "400"
grep -q 'Foundation.Api.IfMatch.Invalid' /tmp/core-crud-precondition-invalid.json

crud_update="$(curl --fail --silent -D /tmp/core-crud-update.headers -X PUT \
  -H 'Content-Type: application/json' \
  -H 'Idempotency-Key: update-core-crud' \
  -H 'If-Match: "1"' \
  -d '{"name":"CI Core CRUD Updated"}' \
  "$base_url/api/core-crud/$crud_id")"
echo "$crud_update" | grep -q 'CI Core CRUD Updated'
echo "$crud_update" | grep -q '"version":2'
grep -qi '^etag: "2"' /tmp/core-crud-update.headers

# Replay happens before the now-stale If-Match reaches the application service, so version stays 2.
crud_update_replay="$(curl --fail --silent -D /tmp/core-crud-update-replay.headers -X PUT \
  -H 'Content-Type: application/json' \
  -H 'Idempotency-Key: update-core-crud' \
  -H 'If-Match: "1"' \
  -d '{"name":"CI Core CRUD Updated"}' \
  "$base_url/api/core-crud/$crud_id")"
echo "$crud_update_replay" | grep -q '"version":2'
grep -qi '^etag: "2"' /tmp/core-crud-update-replay.headers

# If-Match is part of the fingerprint: changing it under the same key is a conflict, not a new update.
update_fingerprint_status="$(curl --silent --output /tmp/core-crud-update-fingerprint.json --write-out '%{http_code}' -X PUT \
  -H 'Content-Type: application/json' \
  -H 'Idempotency-Key: update-core-crud' \
  -H 'If-Match: "2"' \
  -d '{"name":"CI Core CRUD Updated"}' \
  "$base_url/api/core-crud/$crud_id")"
test "$update_fingerprint_status" = "409"
grep -q 'Foundation.Api.Idempotency.FingerprintConflict' /tmp/core-crud-update-fingerprint.json

curl --fail --silent "$base_url/api/core-crud/$crud_id" | grep -q '"version":2'

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

# Delete is replay-safe too: the second attempt stays 204 instead of executing again and becoming 404.
delete_status="$(curl --silent --output /dev/null --write-out '%{http_code}' -X DELETE \
  -H 'Idempotency-Key: delete-core-crud' \
  "$base_url/api/core-crud/$crud_id")"
test "$delete_status" = "204"
delete_replay_status="$(curl --silent --output /dev/null --write-out '%{http_code}' -X DELETE \
  -H 'Idempotency-Key: delete-core-crud' \
  "$base_url/api/core-crud/$crud_id")"
test "$delete_replay_status" = "204"

missing_status="$(curl --silent --output /tmp/core-crud-missing.json --write-out '%{http_code}' "$base_url/api/core-crud/$crud_id")"
test "$missing_status" = "404"

echo "Workbench SQL workflow plus native OpenAPI, hosted FoundationKit.Blazor assets, durable replay-safe idempotency/module composition/API Engine proof passed."
