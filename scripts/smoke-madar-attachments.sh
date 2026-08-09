#!/usr/bin/env bash
set -euo pipefail

base_url="${MADAR_BASE_URL:-http://localhost:8100}"
: "${MADAR_ADMIN_EMAIL:?MADAR_ADMIN_EMAIL is required}"
: "${MADAR_ADMIN_PASSWORD:?MADAR_ADMIN_PASSWORD is required}"
: "${MADAR_OPERATOR_EMAIL:?MADAR_OPERATOR_EMAIL is required}"
: "${MADAR_OPERATOR_PASSWORD:?MADAR_OPERATOR_PASSWORD is required}"
: "${MADAR_SQL_PASSWORD:?MADAR_SQL_PASSWORD is required}"

workdir="$(mktemp -d)"
trap 'rm -rf "$workdir"' EXIT
admin_cookie="$workdir/admin.cookies"
operator_cookie="$workdir/operator.cookies"
pdf_file="$workdir/private-evidence-v09.pdf"
bad_pdf="$workdir/mismatch.pdf"
downloaded_file="$workdir/downloaded.pdf"
direct_file="$workdir/direct-response.bin"

printf '%%PDF-1.7\nMadar attachment smoke marker 20260809-51e9\n%%%%EOF\n' > "$pdf_file"
printf 'this is not a pdf\n' > "$bad_pdf"
pdf_size="$(wc -c < "$pdf_file" | tr -d ' ')"

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

admin_login="$(login "$admin_cookie" "$MADAR_ADMIN_EMAIL" "$MADAR_ADMIN_PASSWORD")"
python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["isAuthenticated"] is True; assert "Administrator" in item["roles"]' <<< "$admin_login"
admin_token="$(csrf_token "$admin_cookie")"

operator_login="$(login "$operator_cookie" "$MADAR_OPERATOR_EMAIL" "$MADAR_OPERATOR_PASSWORD")"
operator_id="$(python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["isAuthenticated"] is True; assert "Operator" in item["roles"]; print(item["userId"])' <<< "$operator_login")"
operator_token="$(csrf_token "$operator_cookie")"

create_payload='{"title":"Secure attachment SQL case","description":"Case used to prove private attachment persistence authorization download and audit privacy.","caseType":"internal-service-request","priority":"medium"}'
case_json="$(curl --fail --silent --show-error \
  -c "$admin_cookie" -b "$admin_cookie" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $admin_token" \
  -d "$create_payload" "$base_url/api/cases/")"
case_id="$(python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["status"] == "new"; print(item["id"])' <<< "$case_json")"

bad_status="$(curl --silent --show-error \
  -o "$workdir/bad-upload.json" --write-out '%{http_code}' \
  -c "$admin_cookie" -b "$admin_cookie" \
  -H "X-CSRF-TOKEN: $admin_token" \
  -F "file=@$bad_pdf;type=application/pdf;filename=mismatch.pdf" \
  "$base_url/api/cases/$case_id/attachments")"
test "$bad_status" = "400"
grep -q 'Madar.AttachmentInvalidContent' "$workdir/bad-upload.json"

upload_json="$(curl --fail --silent --show-error \
  -c "$admin_cookie" -b "$admin_cookie" \
  -H "X-CSRF-TOKEN: $admin_token" \
  -F "file=@$pdf_file;type=application/pdf;filename=private-evidence-v09.pdf" \
  "$base_url/api/cases/$case_id/attachments")"
attachment_id="$(python3 -c 'import json,sys; item=json.load(sys.stdin); assert item["originalFileName"] == "private-evidence-v09.pdf"; assert item["contentType"] == "application/pdf"; print(item["id"])' <<< "$upload_json")"
python3 -c 'import json,sys; expected=int(sys.argv[1]); item=json.load(sys.stdin); assert item["sizeBytes"] == expected; assert item["caseId"] == sys.argv[2]' "$pdf_size" "$case_id" <<< "$upload_json"

admin_list="$(curl --fail --silent --show-error \
  -b "$admin_cookie" \
  "$base_url/api/cases/$case_id/attachments")"
python3 -c 'import json,sys; attachment_id=sys.argv[1]; items=json.load(sys.stdin); matches=[item for item in items if item["id"] == attachment_id]; assert len(matches) == 1; assert matches[0]["originalFileName"] == "private-evidence-v09.pdf"' "$attachment_id" <<< "$admin_list"

unrelated_status="$(curl --silent --show-error \
  -o "$workdir/unrelated.json" --write-out '%{http_code}' \
  -b "$operator_cookie" \
  "$base_url/api/cases/$case_id/attachments")"
test "$unrelated_status" = "404"
grep -q 'Madar.CaseNotFound' "$workdir/unrelated.json"

assign_payload="$(python3 -c 'import json,sys; print(json.dumps({"assigneeUserId":sys.argv[1]}))' "$operator_id")"
curl --fail --silent --show-error \
  -c "$admin_cookie" -b "$admin_cookie" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $admin_token" \
  -d "$assign_payload" "$base_url/api/cases/$case_id/assignment" >/dev/null

curl --fail --silent --show-error \
  -c "$operator_cookie" -b "$operator_cookie" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $operator_token" \
  -d '{"trigger":"start-progress"}' "$base_url/api/cases/$case_id/transition" >/dev/null

resolved_json="$(curl --fail --silent --show-error \
  -c "$operator_cookie" -b "$operator_cookie" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $operator_token" \
  -d '{"trigger":"resolve"}' "$base_url/api/cases/$case_id/transition")"
python3 -c 'import json,sys; assert json.load(sys.stdin)["status"] == "resolved"' <<< "$resolved_json"

closed_json="$(curl --fail --silent --show-error \
  -c "$admin_cookie" -b "$admin_cookie" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $admin_token" \
  -d '{"trigger":"close"}' "$base_url/api/cases/$case_id/transition")"
python3 -c 'import json,sys; assert json.load(sys.stdin)["status"] == "closed"' <<< "$closed_json"

attachments_after_close="$(curl --fail --silent --show-error \
  -b "$operator_cookie" \
  "$base_url/api/cases/$case_id/attachments")"
python3 -c 'import json,sys; attachment_id=sys.argv[1]; items=json.load(sys.stdin); assert any(item["id"] == attachment_id for item in items), "Attachment history disappeared after case close"' "$attachment_id" <<< "$attachments_after_close"

curl --fail --silent --show-error \
  -b "$operator_cookie" \
  -o "$downloaded_file" \
  "$base_url/api/cases/$case_id/attachments/$attachment_id/content"
cmp "$pdf_file" "$downloaded_file"

sqlcmd_path='/opt/mssql-tools18/bin/sqlcmd'
if ! docker compose -f deploy/madar-compose.yml exec -T madar-sqlserver test -x "$sqlcmd_path"; then
  sqlcmd_path='/opt/mssql-tools/bin/sqlcmd'
fi

metadata="$(docker compose -f deploy/madar-compose.yml exec -T madar-sqlserver \
  "$sqlcmd_path" -S localhost -U sa -P "$MADAR_SQL_PASSWORD" -C -d MadarDb \
  -h -1 -W -s '|' -Q "SET NOCOUNT ON; SELECT [OriginalFileName], [ContentType], [SizeBytes], [StorageKey] FROM [madar].[CaseAttachments] WHERE [Id] = '$attachment_id' AND [CaseId] = '$case_id';")"
METADATA="$metadata" EXPECTED_SIZE="$pdf_size" python3 - <<'PY'
import os

parts = [part.strip() for part in os.environ['METADATA'].strip().split('|')]
assert len(parts) == 4, parts
assert parts[0] == 'private-evidence-v09.pdf', parts
assert parts[1] == 'application/pdf', parts
assert int(parts[2]) == int(os.environ['EXPECTED_SIZE']), parts
segments = parts[3].split('/')
assert len(segments) == 2, parts
assert all(len(segment) == 32 for segment in segments), parts
PY
storage_key="$(printf '%s' "$metadata" | awk -F'|' '{gsub(/^ +| +$/, "", $4); print $4}')"
docker compose -f deploy/madar-compose.yml exec -T madar-api \
  test -f "/app/data/attachments/$storage_key"

curl --silent --show-error \
  -o "$direct_file" \
  "$base_url/data/attachments/$storage_key" || true
if cmp -s "$pdf_file" "$direct_file"; then
  echo "Private attachment content was exposed as a static file." >&2
  exit 1
fi

audit_rows="$(docker compose -f deploy/madar-compose.yml exec -T madar-sqlserver \
  "$sqlcmd_path" -S localhost -U sa -P "$MADAR_SQL_PASSWORD" -C -d MadarDb \
  -h -1 -W -s '|' -Q "SET NOCOUNT ON; SELECT [Action], [AttributesJson] FROM [audit].[AuditEvents] WHERE [SubjectId] = '$case_id' AND [Action] IN ('madar.case.attachment-uploaded','madar.case.attachment-downloaded') ORDER BY [OccurredAtUtc];")"
AUDIT_ROWS="$audit_rows" ATTACHMENT_ID="$attachment_id" STORAGE_KEY="$storage_key" python3 - <<'PY'
import json
import os

rows = [line.strip() for line in os.environ['AUDIT_ROWS'].splitlines() if line.strip()]
assert len(rows) == 2, rows
actions = []
for row in rows:
    action, raw = row.split('|', 1)
    action = action.strip()
    attributes = json.loads(raw.strip())
    actions.append(action)
    assert attributes == {'attachmentId': os.environ['ATTACHMENT_ID']}, attributes
    serialized = json.dumps(attributes).lower()
    assert 'private-evidence-v09.pdf' not in serialized
    assert '20260809-51e9' not in serialized
    assert os.environ['STORAGE_KEY'].lower() not in serialized
assert sorted(actions) == ['madar.case.attachment-downloaded', 'madar.case.attachment-uploaded'], actions
PY

echo "Madar secure attachment SQL workflow passed for case $case_id and attachment $attachment_id"
