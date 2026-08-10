#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"

# The repository is Core-focused. Removed product/reference applications must not reappear by accident.
legacy_traces="$(git grep -iIl -E '(Athar|Madar|EntertainmentDocs|Entertainment Docs|entertainment-api-docs)' -- ':!scripts/verify-repository.sh' || true)"
if [[ -n "$legacy_traces" ]]; then
  echo "Removed product/legacy trace found in tracked files:" >&2
  echo "$legacy_traces" >&2
  exit 1
fi

for removed_path in apps examples tests/Athar.Tests tests/Madar.Tests site/athar-demo site/madar-demo; do
  if [[ -e "$removed_path" ]]; then
    echo "Removed product path must not exist: $removed_path" >&2
    exit 1
  fi
done

unexpected_top_level="$(find . -mindepth 1 -maxdepth 1 \
  ! -name '.git' ! -name '.github' ! -name '.dockerignore' ! -name '.editorconfig' ! -name '.gitignore' \
  ! -name 'CHANGELOG.md' ! -name 'CONTRIBUTING.md' ! -name 'Directory.Build.props' \
  ! -name 'Directory.Packages.props' ! -name 'FoundationKit.sln' ! -name 'LICENSE' ! -name 'README.md' \
  ! -name 'SECURITY.md' ! -name 'catalog' ! -name 'deploy' ! -name 'docs' ! -name 'foundationkit.ps1' \
  ! -name 'global.json' ! -name 'postman' ! -name 'samples' ! -name 'scripts' ! -name 'site' \
  ! -name 'src' ! -name 'tests' ! -name 'tools' -print)"
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
  echo "EF Core migrations must belong to consuming hosts, not reusable packages:" >&2
  echo "$migration_leaks" >&2
  exit 1
fi

required_files=(
  "README.md" "CHANGELOG.md" "CONTRIBUTING.md" "SECURITY.md" "FoundationKit.sln" "foundationkit.ps1"
  ".github/CODEOWNERS" ".github/pull_request_template.md"
  ".github/workflows/ci.yml" ".github/workflows/codeql.yml" ".github/workflows/security-scan.yml"
  ".github/workflows/pages.yml" ".github/workflows/windows-launcher-check.yml" ".github/workflows/composer-generation.yml"
  "catalog/foundationkit.catalog.json" "catalog/foundationkit.capabilities.json" "catalog/foundationkit.maturity-evidence.json"
  "docs/ARCHITECTURE.md" "docs/PACKAGES.md" "docs/FEATURES.md" "docs/WORKBENCH.md"
  "docs/CORE-V0.1-BASELINE.md" "docs/CAPABILITY-MODEL-V1.md" "docs/CAPABILITY-ROADMAP-V1.md"
  "docs/CAPABILITY-EXTRACTION-STATUS.md" "docs/CAPABILITY-MATURITY-EVIDENCE-V1.md" "docs/COMPOSER-CLI-V1.md"
  "docs/PROJECT-ISOLATION-AND-COMPATIBILITY.md" "docs/CORE-VNEXT-119-DECISION.md" "docs/CRUD-MODULE-ENGINE.md"
  "samples/FoundationKit.Workbench/FoundationKit.Workbench.Api.csproj"
  "samples/FoundationKit.Workbench.Contracts/FoundationKit.Workbench.Contracts.csproj"
  "samples/FoundationKit.Workbench.Client/FoundationKit.Workbench.Client.csproj"
  "tests/FoundationKit.Tests/FoundationKit.Tests.csproj" "tests/FoundationKit.Workbench.Tests/FoundationKit.Workbench.Tests.csproj"
  "postman/FoundationKit.Workbench.postman_collection.json" "deploy/docker-compose.yml"
  "scripts/run-workbench.ps1" "scripts/run-workbench.sh" "scripts/stop-workbench.ps1" "scripts/stop-workbench.sh"
  "scripts/pack.ps1" "scripts/pack.sh" "scripts/repository-hygiene.py" "scripts/verify-pages.py"
  "scripts/security/scan-repository.py" "scripts/security/generate-sbom.py" "scripts/security/check-container-hardening.py"
  "tools/FoundationKit.Composer/FoundationKit.Composer.csproj" "tools/FoundationKit.CatalogGenerator/FoundationKit.CatalogGenerator.csproj"
)
for required_file in "${required_files[@]}"; do
  if [[ ! -f "$required_file" ]]; then
    echo "Required repository file is missing: $required_file" >&2
    exit 1
  fi
done

stale_truth_patterns=(
  "interactive project generation remains future work"
  "does **not** generate a project yet"
  "v0.10 remains in verification"
)
truth_files=(README.md CONTRIBUTING.md SECURITY.md docs/*.md src/FoundationKit.Application/Capabilities/*.cs)
for pattern in "${stale_truth_patterns[@]}"; do
  matches="$(grep -FIl -- "$pattern" "${truth_files[@]}" 2>/dev/null || true)"
  if [[ -n "$matches" ]]; then
    echo "Stale current-state wording '$pattern' found in:" >&2
    echo "$matches" >&2
    exit 1
  fi
done

python3 scripts/repository-hygiene.py

if ! grep -Fq 'new FoundationProjectContext' src/FoundationKit.Infrastructure/Platform/FoundationPlatformServiceCollectionExtensions.cs; then
  echo "Project isolation DI registration is missing." >&2; exit 1
fi
if ! grep -Fq 'foundation:{projectContext.ProjectId.Value}' src/FoundationKit.Application/Isolation/FoundationProjectContext.cs; then
  echo "Project-scoped resource namespace prefix is missing." >&2; exit 1
fi
if ! grep -Fq 'AuthorizationPolicyMissing' src/FoundationKit.Application/Crud/CrudContracts.cs; then
  echo "CRUD authorization must fail closed when a module declares authorization without a policy." >&2; exit 1
fi
if ! grep -Fq 'FoundationConcurrencyException' src/FoundationKit.Infrastructure/Persistence/ConcurrencyAwareEfUnitOfWork.cs; then
  echo "CRUD EF concurrency translation is missing." >&2; exit 1
fi
if ! grep -Fq 'MapFoundationCrud' src/FoundationKit.WebApi/Crud/CrudEndpointExtensions.cs; then
  echo "Generic CRUD HTTP mapping is missing." >&2; exit 1
fi
if ! grep -Fq 'CoreCrudRecords' samples/FoundationKit.Workbench/Infrastructure/Migrations/20260810190000_CoreCrudEngine.cs; then
  echo "Workbench SQL proof migration for the Core CRUD engine is missing." >&2; exit 1
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
if len(project_ids) != 17:
    raise SystemExit(f'Expected 17 reusable projects, found {len(project_ids)}')
print(f'Reusable package consistency passed: {len(project_ids)} packages.')
PY

if ! grep -q 'Microsoft.EntityFrameworkCore.SqlServer' samples/FoundationKit.Workbench/FoundationKit.Workbench.Api.csproj; then
  echo "Workbench must explicitly own SQL Server provider selection." >&2; exit 1
fi
client_persistence_leaks="$(grep -RIl --include='*.cs' --include='*.csproj' -- 'Microsoft.EntityFrameworkCore\|Microsoft.EntityFrameworkCore.SqlServer' samples/FoundationKit.Workbench.Client samples/FoundationKit.Workbench.Contracts || true)"
if [[ -n "$client_persistence_leaks" ]]; then
  echo "Client/transport projects must not reference EF Core or SQL Server:" >&2
  echo "$client_persistence_leaks" >&2
  exit 1
fi
if ! grep -q 'MudBlazor' samples/FoundationKit.Workbench.Client/FoundationKit.Workbench.Client.csproj; then
  echo "Workbench client must retain its UI component dependency." >&2; exit 1
fi
if ! grep -q 'uses: github/codeql-action/init@' .github/workflows/codeql.yml; then
  echo "CodeQL workflow is missing." >&2; exit 1
fi
if ! grep -q 'aquasecurity/trivy-action@' .github/workflows/security-scan.yml; then
  echo "Trivy security scanning is missing." >&2; exit 1
fi

python3 scripts/verify-pages.py
python3 scripts/security/check-container-hardening.py

echo "FoundationKit Core-only repository, project isolation, module/CRUD engine, Workbench SQL proof, metadata, security, and packaging boundaries passed."
