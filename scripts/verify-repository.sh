#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"

patterns=("EntertainmentDocs" "Entertainment Docs" "entertainment-api-docs")
for pattern in "${patterns[@]}"; do
  matches="$(grep -RIl --exclude-dir=.git --exclude-dir=bin --exclude-dir=obj --exclude='verify-repository.sh' -- "$pattern" . || true)"
  if [[ -n "$matches" ]]; then
    echo "Forbidden legacy product trace '$pattern' found in:" >&2
    echo "$matches" >&2
    exit 1
  fi
done

unexpected_top_level="$(find . -mindepth 1 -maxdepth 1 \
  ! -name '.git' ! -name '.github' ! -name '.dockerignore' ! -name '.editorconfig' ! -name '.gitignore' \
  ! -name 'CHANGELOG.md' ! -name 'CONTRIBUTING.md' ! -name 'Directory.Build.props' \
  ! -name 'Directory.Packages.props' ! -name 'FoundationKit.sln' ! -name 'LICENSE' ! -name 'README.md' \
  ! -name 'SECURITY.md' ! -name 'apps' ! -name 'catalog' ! -name 'deploy' ! -name 'docs' \
  ! -name 'examples' ! -name 'foundationkit.ps1' ! -name 'global.json' ! -name 'postman' \
  ! -name 'samples' ! -name 'scripts' ! -name 'site' ! -name 'src' ! -name 'tests' ! -name 'tools' -print)"
if [[ -n "$unexpected_top_level" ]]; then
  echo "Unexpected top-level entries found:" >&2
  echo "$unexpected_top_level" >&2
  exit 1
fi

provider_leaks="$(grep -RIl -- 'Microsoft.EntityFrameworkCore.SqlServer' src || true)"
if [[ -n "$provider_leaks" ]]; then
  echo "SQL Server provider coupling leaked into reusable packages:" >&2
  echo "$provider_leaks" >&2
  exit 1
fi

migration_leaks="$(find src -type d -iname migrations -print)"
if [[ -n "$migration_leaks" ]]; then
  echo "EF Core migrations must belong to consuming products, not reusable packages:" >&2
  echo "$migration_leaks" >&2
  exit 1
fi

required_files=(
  "README.md" "CHANGELOG.md" "CONTRIBUTING.md" "SECURITY.md" "foundationkit.ps1" "FoundationKit.sln"
  ".github/CODEOWNERS" ".github/pull_request_template.md"
  ".github/workflows/ci.yml" ".github/workflows/codeql.yml" ".github/workflows/security-scan.yml"
  ".github/workflows/pages.yml" ".github/workflows/windows-launcher-check.yml"
  "catalog/foundationkit.catalog.json" "catalog/foundationkit.capabilities.json" "catalog/foundationkit.maturity-evidence.json"
  "docs/ARCHITECTURE.md" "docs/PACKAGES.md" "docs/FEATURES.md" "docs/WORKBENCH.md" "docs/DUAL-FULL-STACK.md"
  "docs/CORE-V0.1-BASELINE.md" "docs/CAPABILITY-MODEL-V1.md" "docs/CAPABILITY-ROADMAP-V1.md"
  "docs/CAPABILITY-EXTRACTION-STATUS.md" "docs/CAPABILITY-MATURITY-EVIDENCE-V1.md" "docs/COMPOSER-CLI-V1.md"
  "docs/LOCAL-RUN-WINDOWS-AR.md" "docs/PRODUCTION-READINESS-AR.md" "docs/ADDING-A-PROJECT-AR.md"
  "docs/MADAR-OPERATIONS-AR.md" "docs/MADAR-COMMENTS-AR.md" "docs/MADAR-APPROVALS-AR.md"
  "docs/MADAR-NOTIFICATIONS-AR.md" "docs/MADAR-DEPARTMENT-ROUTING-AR.md"
  "docs/MADAR-DEPARTMENT-ADMINISTRATION-AR.md" "docs/MADAR-CASE-TRANSFER-AR.md"
  "docs/MADAR-ATTACHMENTS-AR.md" "docs/MADAR-SEARCH-REPORTING-AR.md"
  "docs/capabilities/AUDITING.md" "docs/capabilities/SECURITY.md" "docs/capabilities/IDENTITY.md"
  "docs/capabilities/AUTHORIZATION.md" "docs/capabilities/WORKFLOW.md" "docs/capabilities/APPROVALS.md"
  "docs/capabilities/NOTIFICATIONS.md" "docs/capabilities/SMTP-PROVIDER.md"
  "docs/security/CURRENT-SECURITY-STATUS.md" "docs/security/POLICY-IMPLEMENTATION-REGISTER.md"
  "docs/security/RISK-REGISTER.md" "docs/security/THREAT-MODEL.md" "docs/security/SECURITY-DECISIONS.md"
  "docs/security/PRODUCTION-GOVERNANCE-CHECKLIST.md" "docs/security/VULNERABILITY-MANAGEMENT.md"
  "docs/security/CHANGE-AND-RELEASE-EVIDENCE.md" "docs/security/PII-DATA-INVENTORY.md"
  "docs/security/CRYPTO-AND-SECRETS-INVENTORY.md" "docs/security/LOGGING-AND-MONITORING-CATALOG.md"
  "docs/security/INCIDENT-RECOVERY-RUNBOOK.md"
  "samples/FoundationKit.Workbench/FoundationKit.Workbench.Api.csproj"
  "samples/FoundationKit.Workbench.Client/FoundationKit.Workbench.Client.csproj"
  "samples/FoundationKit.Workbench.Contracts/FoundationKit.Workbench.Contracts.csproj"
  "examples/Athar/README.md" "examples/Athar/Athar.Api/Athar.Api.csproj" "tests/Athar.Tests/Athar.Tests.csproj"
  "apps/Madar/README.md" "apps/Madar/Madar.Api/Madar.Api.csproj" "tests/Madar.Tests/Madar.Tests.csproj"
  "postman/FoundationKit.Workbench.postman_collection.json" "postman/Athar.Api.postman_collection.json"
  "deploy/docker-compose.yml" "deploy/athar-compose.yml" "deploy/madar-compose.yml" "deploy/athar-production.example.yml"
  "scripts/athar-product.ps1" "scripts/madar-product.ps1" "scripts/expose-athar-tunnel.ps1"
  "scripts/run-workbench.ps1" "scripts/run-workbench.sh" "scripts/stop-workbench.ps1" "scripts/stop-workbench.sh"
  "scripts/pack.ps1" "scripts/pack.sh" "scripts/repository-hygiene.py" "scripts/verify-pages.py"
  "scripts/verify-athar-restore.sh" "scripts/security/scan-repository.py" "scripts/security/generate-sbom.py"
  "scripts/security/check-container-hardening.py" "scripts/security/negative-athar.sh"
  "site/index.html" "site/styles.css" "site/app.js" "site/portal-manifest.json" "site/favicon.svg"
  "tools/FoundationKit.Composer/FoundationKit.Composer.csproj" "tools/FoundationKit.Composer/ComposerCli.cs"
  "tools/FoundationKit.CatalogGenerator/FoundationKit.CatalogGenerator.csproj"
)

for required_file in "${required_files[@]}"; do
  if [[ ! -f "$required_file" ]]; then
    echo "Required repository file is missing: $required_file" >&2
    exit 1
  fi
done

# Keep the public/current-state descriptions synchronized with repository reality.
truth_files=(
  "README.md" "CONTRIBUTING.md" "SECURITY.md"
  "docs/ARCHITECTURE.md" "docs/PACKAGES.md" "docs/WORKBENCH.md" "docs/DUAL-FULL-STACK.md"
  "docs/CORE-V0.1-BASELINE.md" "docs/ADDING-A-PROJECT-AR.md" "docs/PRODUCTION-READINESS-AR.md"
  "docs/security/CURRENT-SECURITY-STATUS.md" "examples/Athar/README.md" "apps/Madar/README.md"
)
stale_truth_patterns=(
  "current feature branch"
  "يمكن إضافة مجلد apps/ مستقبلًا"
  "سيتم تنفيذ التجربة المحلية بعد اكتمال"
  "v0.10 remains in verification"
)
for pattern in "${stale_truth_patterns[@]}"; do
  matches="$(grep -FIl -- "$pattern" "${truth_files[@]}" || true)"
  if [[ -n "$matches" ]]; then
    echo "Stale current-state wording '$pattern' found in:" >&2
    echo "$matches" >&2
    exit 1
  fi
done

python3 scripts/repository-hygiene.py

if ! grep -q 'scripts/pack.ps1' foundationkit.ps1; then
  echo "The unified manager must delegate packaging to scripts/pack.ps1." >&2
  exit 1
fi
if grep -q 'src/FoundationKit.Domain/FoundationKit.Domain.csproj' foundationkit.ps1; then
  echo "The unified manager must not maintain a second hard-coded reusable-package list." >&2
  exit 1
fi
if ! grep -Fq 'Protect-LocalFile $WorkbenchEnvironmentFile' foundationkit.ps1; then
  echo "Workbench local credentials must be protected with a user-only Windows ACL." >&2
  exit 1
fi
if ! grep -Fq '$ErrorActionPreference = "SilentlyContinue"' foundationkit.ps1; then
  echo "Unified Docker readiness probing must tolerate an installed but stopped Docker daemon." >&2
  exit 1
fi

python3 - <<'PY'
import json
from pathlib import Path

root = Path('.')
project_ids = sorted(project.parent.name for project in root.glob('src/FoundationKit.*/FoundationKit.*.csproj'))

with (root / 'catalog/foundationkit.catalog.json').open(encoding='utf-8') as handle:
    human = json.load(handle)
human_ids = sorted(package['packageId'] for package in human['packages'])
if human_ids != project_ids:
    raise SystemExit(f'Human catalog package drift. projects={project_ids}; catalog={human_ids}')

with (root / 'site/portal-manifest.json').open(encoding='utf-8') as handle:
    portal = json.load(handle)
portal_sources = sorted(page['source'] for page in portal['pages'] if page.get('kind') == 'package')
expected_sources = sorted(f'src/{package_id}' for package_id in project_ids)
if portal_sources != expected_sources:
    raise SystemExit(f'Atlas package drift. expected={expected_sources}; actual={portal_sources}')

if len(project_ids) != 17:
    raise SystemExit(f'Expected 17 reusable projects, found {len(project_ids)}')

print(f'Reusable package consistency passed: {len(project_ids)} projects/catalog/Atlas package pages.')
PY

workbench_api="samples/FoundationKit.Workbench/FoundationKit.Workbench.Api.csproj"
workbench_client="samples/FoundationKit.Workbench.Client/FoundationKit.Workbench.Client.csproj"
athar_api="examples/Athar/Athar.Api/Athar.Api.csproj"
athar_client="examples/Athar/Athar.Client/Athar.Client.csproj"
madar_api="apps/Madar/Madar.Api/Madar.Api.csproj"
madar_client="apps/Madar/Madar.Client/Madar.Client.csproj"

for product_api in "$workbench_api" "$athar_api" "$madar_api"; do
  if ! grep -q 'Microsoft.EntityFrameworkCore.SqlServer' "$product_api"; then
    echo "$product_api must explicitly own SQL Server." >&2
    exit 1
  fi
done

if ! grep -q 'Microsoft.AspNetCore.Identity.EntityFrameworkCore' "$athar_api"; then
  echo "Athar API must own ASP.NET Core Identity persistence." >&2; exit 1
fi
if ! grep -q 'Microsoft.AspNetCore.Identity.EntityFrameworkCore' "$madar_api"; then
  echo "Madar API must own ASP.NET Core Identity persistence." >&2; exit 1
fi
for client in "$workbench_client" "$athar_client" "$madar_client"; do
  if ! grep -q 'MudBlazor' "$client"; then
    echo "$client must use MudBlazor." >&2; exit 1
  fi
done

client_persistence_leaks="$(grep -RIl --include='*.cs' --include='*.csproj' -- 'Microsoft.EntityFrameworkCore\|Microsoft.EntityFrameworkCore.SqlServer' \
  samples/FoundationKit.Workbench.Client samples/FoundationKit.Workbench.Contracts \
  examples/Athar/Athar.Client examples/Athar/Athar.Contracts \
  apps/Madar/Madar.Client apps/Madar/Madar.Contracts || true)"
if [[ -n "$client_persistence_leaks" ]]; then
  echo "Client/transport projects must not reference EF Core or SQL Server:" >&2
  echo "$client_persistence_leaks" >&2
  exit 1
fi

if ! grep -q 'X-CSRF-TOKEN' postman/Athar.Api.postman_collection.json; then
  echo "Athar Postman must demonstrate anti-CSRF." >&2; exit 1
fi
if ! grep -q 'AddRateLimiter' examples/Athar/Athar.Api/Program.cs; then
  echo "Athar must configure rate limiting." >&2; exit 1
fi
if ! grep -q 'SelfReviewNotAllowed' examples/Athar/Athar.Domain/Initiative.cs; then
  echo "Athar must retain maker-checker defense." >&2; exit 1
fi
if ! grep -q 'ProtectKeysWithCertificate' examples/Athar/Athar.Api/Program.cs; then
  echo "Athar production path must support protected persisted Data Protection keys." >&2; exit 1
fi
if ! grep -q 'ATHAR_ALLOW_RESTORE_DRILL' scripts/verify-athar-restore.sh; then
  echo "Restore drill must fail closed unless explicitly enabled for isolation." >&2; exit 1
fi
if ! grep -q 'uses: github/codeql-action/init@' .github/workflows/codeql.yml; then
  echo "CodeQL workflow is missing." >&2; exit 1
fi
if ! grep -q 'aquasecurity/trivy-action@' .github/workflows/security-scan.yml; then
  echo "Trivy security scanning is missing." >&2; exit 1
fi
if ! grep -q 'cp -R site release' .github/workflows/pages.yml; then
  echo "GitHub Pages must publish the dedicated repository portal." >&2; exit 1
fi

python3 scripts/verify-pages.py
python3 scripts/security/check-container-hardening.py

echo "FoundationKit Core, Workbench, Athar, Madar, repository hygiene, current-state descriptions, metadata, security evidence, and Atlas verification passed."
