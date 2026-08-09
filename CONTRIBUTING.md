# Contributing

## Start from repository truth

Before changing code, read `README.md`, `docs/ARCHITECTURE.md`, `docs/PACKAGES.md`, `docs/CORE-V0.1-BASELINE.md`, the canonical capability catalogs, and the docs/tests for the consumer you are changing.

Current repository roles are explicit:

```text
src/             reusable FoundationKit packages
samples/         executable architecture/reference consumers
examples/Athar/  complete Arabic reference product
apps/Madar/      operational case-management product through v0.10
```

For Windows local execution, use `docs/LOCAL-RUN-WINDOWS-AR.md`.

## Preserve boundaries

Reusable provider-neutral code belongs under `src/`. Product rules, Identity configuration, hosted applications, SQL Server selection, product schemas/migrations, Arabic copy, departments/routing, SLA, attachment/search/reporting policy, and deployment configuration remain consumer-owned.

Workbench, Athar, and Madar may reference SQL Server because they are explicit consumers. `scripts/verify-repository.sh` rejects SQL Server provider references or migration directories inside reusable packages and checks client/contract persistence boundaries.

Do not create a new FoundationKit runtime package simply because a product implements the behavior. Extraction requires independently useful provider-neutral semantics and evidence consistent with `CAPABILITY-EXTRACTION-STATUS.md`.

## Keep the tracked repository clean

Do not commit local/generated/sensitive artifacts such as:

- `bin/`, `obj/`, `artifacts/`, `TestResults/`, coverage, logs, packages;
- `.local/`, local `.env*`, User Secrets, IDE state, local databases;
- `.bak`, `.pfx`, `.p12`, `.key`, backups/private-key material;
- temporary audit output or generated files not owned by the canonical generator.

`.gitignore` is not the only defense; `scripts/repository-hygiene.py` checks the tracked Git set in CI.

## Capability/catalog changes

When public implemented behavior changes:

1. change code/tests;
2. update `catalog/foundationkit.catalog.json` if the human implemented-package surface changes;
3. update `CapabilityModel.cs` when composition identity/dependency/maturity changes;
4. update compatibility/maturity evidence when those contracts change;
5. run the catalog generator;
6. update relevant package/capability documentation and `CHANGELOG.md`.

```bash
dotnet run --project tools/FoundationKit.CatalogGenerator
```

Do not manually edit generated `docs/FEATURES.md`, `catalog/foundationkit.capabilities.json`, or `catalog/foundationkit.maturity-evidence.json` unless the generator contract explicitly says otherwise.

## Product persistence

EF migrations are the schema source of truth:

```text
Workbench → samples/FoundationKit.Workbench/Infrastructure/Migrations/
Athar     → examples/Athar/Athar.Infrastructure/Migrations/
Madar     → apps/Madar/Madar.Infrastructure/Migrations/
```

Schema changes require reviewed migrations where applicable and the affected SQL integration path. Never move product migrations into `src/FoundationKit.*`.

## Verification

Core local checks:

```bash
python3 scripts/repository-hygiene.py
bash scripts/verify-repository.sh
dotnet restore FoundationKit.sln
dotnet build FoundationKit.sln --configuration Release --no-restore
dotnet run --project tools/FoundationKit.CatalogGenerator --configuration Release --no-build -- --check
dotnet test FoundationKit.sln --configuration Release --no-build
bash scripts/pack.sh
```

Windows:

```powershell
.\foundationkit.ps1 doctor
.\foundationkit.ps1 verify
```

Meaningful PRs must also pass the repository's exact-head GitHub CI/Security/CodeQL and affected SQL/container gates before merge.

## Pull requests

Explain:

- what changed and why;
- whether the change belongs to reusable FoundationKit, Workbench, Athar, Madar, tooling, security/operations, or documentation;
- public API/contract/maturity/schema compatibility impact;
- tests/runtime/security evidence;
- generated metadata/docs updates;
- deployment/organizational controls deliberately left external.

The current repository is experimental/pre-production. PRs and exact-head gates are preferred during development; Issue #35 defines the additional protected-branch/independent-review controls required before real Production governance.
