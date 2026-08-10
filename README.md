# FoundationKit for .NET

**FoundationKit** is a composable .NET foundation for building business systems without turning the reusable core into one giant application.

The repository separates reusable foundation code from its consumers deliberately:

```text
Reusable FoundationKit packages
        ↓
Optional capabilities and provider adapters
        ↓
Consumers
├── Workbench — executable architecture/reference consumer
├── Athar — complete Arabic reference product
└── Madar — operational case product under apps/ with product depth through v0.10
```

The current reusable output is **17 NuGet packages + 17 symbol packages**. Package existence does not mean every capability is `Stable`; maturity is tracked explicitly in the capability model. Every capability identity also publishes a machine-readable contract version for composition compatibility; contract version is distinct from both package version and maturity.

The active repository baseline targets **.NET 10 LTS / `net10.0`**. See [`docs/NET10-LTS-BASELINE.md`](docs/NET10-LTS-BASELINE.md) for the coordinated framework, SDK, dependency, container, CI, and compatibility decision.

FoundationKit Composer consumes that same model and can generate a deterministic product skeleton plus an architecture decision report either from a strict manifest or through the interactive questionnaire, without adding another runtime package or inventing product semantics.

> FoundationKit has a verified automated repository baseline for the documented scope. Production approval, organizational compliance, provider operations, and formal certification remain deployment- and organization-specific.

---

## Repository map

```text
foundationkit-dotnet/
├─ src/                         reusable FoundationKit packages
├─ samples/                     FoundationKit Workbench
├─ examples/Athar/              complete Arabic reference product
├─ apps/Madar/                  operational case product through v0.10
├─ tools/
│  ├─ FoundationKit.CatalogGenerator
│  └─ FoundationKit.Composer    validate/explain/generate tooling
├─ tests/                       core, Workbench, Athar, and Madar tests
├─ catalog/                     human and machine capability catalogs
├─ docs/                        architecture, capability, security, and runbooks
├─ deploy/                      Docker Compose definitions
├─ postman/                     API collections
├─ scripts/                     verification, packaging, smoke, and security scripts
├─ site/                        FoundationKit Atlas GitHub Pages portal
├─ FoundationKit.sln
└─ foundationkit.ps1            unified Windows repository manager
```

---

## The 17 reusable packages

The five base packages remain the architectural foundation. The remaining packages are opt-in capabilities/adapters and must be selected deliberately by a consuming product.

| Package | Purpose |
|---|---|
| `FoundationKit.Domain` | entities, aggregate roots, value objects, domain events |
| `FoundationKit.Application` | use-case contracts, results, validation, pagination, persistence ports, capability model |
| `FoundationKit.Infrastructure` | provider-neutral EF Core adapters and in-process domain-event dispatch |
| `FoundationKit.WebApi` | HTTP result mapping, Problem Details, correlation, baseline response headers |
| `FoundationKit.Blazor` | typed API results, resilient response parsing, reusable UI state |
| `FoundationKit.Auditing` | provider-neutral audit recording contracts |
| `FoundationKit.Security` | trusted-proxy, rate-limit partition, and MFA-assurance conventions |
| `FoundationKit.Identity` | account policy, notification ports, and sensitive-operation step-up requirements |
| `FoundationKit.Authorization` | permission, role-grant, subject, and ownership evaluation primitives |
| `FoundationKit.Workflow` | deterministic state/trigger transition definitions |
| `FoundationKit.Approvals` | narrow approve/reject + permission + maker-checker composition |
| `FoundationKit.Notifications` | channel-neutral message and delivery contracts |
| `FoundationKit.Notifications.Smtp` | narrow SMTP transport adapter |
| `FoundationKit.Settings` | bounded hierarchical setting resolution |
| `FoundationKit.FeatureManagement` | settings-backed Boolean feature decisions |
| `FoundationKit.Localization` | culture metadata, RTL/LTR, fallback, opaque time-zone identity |
| `FoundationKit.Caching` | bounded byte-cache contracts and an in-memory reference provider |

Canonical package contracts are documented in [`docs/PACKAGES.md`](docs/PACKAGES.md). The human-readable implemented surface is generated into [`docs/FEATURES.md`](docs/FEATURES.md).

### Capability maturity and contract compatibility

Capability maturity is not inferred from the presence of a project or class. The machine contract uses:

- `Stable`
- `Preview`
- `ReferenceOnly`
- `Planned`

Separately, every current capability/provider/tooling identity publishes **contract version `1`**. A project manifest may optionally require exact versions through `capabilityContracts`; Composer fails closed if an explicit requirement is unknown, unresolved, or incompatible. Existing manifests that omit contract requirements continue to work as before.

The source of truth is:

```text
src/FoundationKit.Application/Capabilities/CapabilityModel.cs
src/FoundationKit.Application/Capabilities/CapabilityCompatibility.cs
```

and its generated machine-readable form:

```text
catalog/foundationkit.capabilities.json
```

The lifecycle, compatibility, and stop rules are documented in:

- [`docs/CAPABILITY-MODEL-V1.md`](docs/CAPABILITY-MODEL-V1.md)
- [`docs/CAPABILITY-ROADMAP-V1.md`](docs/CAPABILITY-ROADMAP-V1.md)
- [`docs/CAPABILITY-EXTRACTION-STATUS.md`](docs/CAPABILITY-EXTRACTION-STATUS.md)

---

## Dependency rule

The reusable foundation keeps dependency direction explicit:

```text
Domain
  ↑
Application
  ↑
Infrastructure

Application / Domain
  ↑
WebApi or Blazor consumers
```

Optional capabilities compose around these boundaries. A lower-level package must not gain a dependency merely because a higher-level feature needs convenience.

Provider decisions also stay separate:

```text
Capability contract ≠ provider selection
```

Examples:

- `FoundationKit.Caching` does not force Redis.
- `FoundationKit.Notifications` does not force SMTP.
- `FoundationKit.Localization` does not force a translation store or OS-specific time-zone mapping.
- `FoundationKit.Settings` is not a secret store.
- `FoundationKit.Infrastructure` does not own a product DbContext or migrations.

---

## Workbench

`FoundationKit.Workbench` is the executable architecture/reference consumer. It demonstrates two connected vertical slices:

```text
User Full Stack
SQL Server → Domain → Use Case → Contracts → API → Blazor UI

Admin Full Stack
SQL Server → Domain → Use Case → Contracts → API → Blazor UI
```

They meet through a shared request lifecycle:

```text
submitted → approved | rejected
```

Workbench also provides real consumer evidence for reusable platform capabilities:

- Settings
- Feature Management
- Localization
- Caching

Caching, for example, is exercised on the existing embedded capability-catalog read path rather than through a synthetic cache-only endpoint.

Read [`docs/WORKBENCH.md`](docs/WORKBENCH.md) and [`docs/DUAL-FULL-STACK.md`](docs/DUAL-FULL-STACK.md).

---

## Athar

`examples/Athar` is a complete Arabic reference product rather than another generic layer.

It demonstrates real product ownership of concerns such as:

- ASP.NET Core Identity and account lifecycle;
- authentication cookies and anti-CSRF;
- authorization and product permissions;
- MFA/security-sensitive operations;
- initiatives and administrative review;
- maker-checker behavior;
- SQL Server migrations and persistence;
- idempotency and optimistic concurrency reference behavior;
- audit records;
- notification and SMTP-provider consumption;
- Arabic Blazor UX;
- Docker, health/readiness, backup/restore, and E2E verification.

Athar keeps product rules, Arabic copy, database schema, migrations, secrets, and deployment configuration outside the reusable packages.

Read [`examples/Athar/README.md`](examples/Athar/README.md).

---

## Madar

`apps/Madar` is the repository's operational case-management and orchestration product. It deliberately remains a product consumer of FoundationKit rather than turning its organization, routing, case, attachment, search, or reporting rules into speculative reusable packages.

The deterministic case lifecycle remains:

```text
new → assigned → in-progress → resolved → closed
```

Current implemented product depth is:

```text
v0.1   Identity + authorization + SQL + case lifecycle + audit + Arabic API/Blazor
v0.1.1 Readiness + bounded startup retry + local/Docker operational integration
v0.2   SLA deadlines + first breach/escalation evidence
v0.3   Append-only case comments
v0.4   Maker-checker approval gate for sensitive resolution
v0.5   Bounded operational notifications
v0.6   Department queues + routing + Operator claim flow
v0.7   Department administration + safe Operator membership
v0.8   Controlled transfer + reassignment
v0.9   Secure append-only case attachments/documents
v0.10  Authorized case search + same-scope operational reporting
```

Routing is contextual rather than a workflow state. Transfer is an explicit supervised operation: an already-routed active case can move to a different active department, become `new` and unassigned in the target queue, and keep its SLA evidence, comments, approvals, attachments, creator, and prior audit history. Reassignment changes the eligible Operator while preserving the active lifecycle status and SLA evidence. The corresponding transfer/reassignment permissions are held by Supervisor/Administrator, while Application-layer authorization remains the source of truth.

v0.9 adds product-owned attachments with SQL metadata and private content storage behind a Madar abstraction. Case visibility governs list/upload/download; files are bounded to 10 MiB and an allow-list of PDF/PNG/JPEG/TXT with basic signature checks. Storage keys are server-generated and content is not exposed as static files. Audit evidence records only bounded attachment identifiers. The current filesystem provider is for the experimental Development/CI topology; Production object-storage/KMS/malware-scanning/retention decisions remain deployment work.

v0.10 adds SQL-backed case search and a same-scope operational summary. The existing creator/assignee/`madar.cases.read-all` visibility boundary is applied before filters, counts, or paging, so narrower roles cannot infer hidden cases from result rows or counters. Search is bounded by validated filters and deterministic paging, remains product-owned, and does not introduce an external index or a reusable FoundationKit search/reporting package.

Madar reuses FoundationKit Domain/Application/Infrastructure/WebApi/Blazor together with Security, Authorization, Auditing, Workflow, Approvals, Notifications, and the optional SMTP adapter where their contracts fit. Madar keeps ASP.NET Core Identity configuration, product permissions, SQL schema/migrations, department/routing semantics, SLA values, attachment policy/storage abstraction, search/reporting semantics, API endpoints, audit sink, readiness policy, Docker topology, and Arabic UI inside `apps/Madar`.

Pull-request CI publishes Madar and exercises real SQL Server workflows for readiness, authentication/anti-CSRF, lifecycle/audit, SLA, comments, approvals, department routing/claim, department administration, reassignment, cross-department transfer, target-queue behavior, secure attachments, authorized search/reporting privacy, and final persisted SQL/audit evidence. Repository security gates also scan the Madar container and upload SARIF evidence.

This product depth still does **not** claim a production organization tree, multi-tenancy, production-grade object-storage/malware-scanning/retention infrastructure, external search/index infrastructure, saved/scheduled/exported/BI reports, external-channel ingestion, durable background scheduling/outbox delivery, generic routing/files/storage/search/reporting extraction, or production deployment approval.

Read:

- [`apps/Madar/README.md`](apps/Madar/README.md)
- [`docs/MADAR-OPERATIONS-AR.md`](docs/MADAR-OPERATIONS-AR.md)
- [`docs/MADAR-DEPARTMENT-ROUTING-AR.md`](docs/MADAR-DEPARTMENT-ROUTING-AR.md)
- [`docs/MADAR-DEPARTMENT-ADMINISTRATION-AR.md`](docs/MADAR-DEPARTMENT-ADMINISTRATION-AR.md)
- [`docs/MADAR-CASE-TRANSFER-AR.md`](docs/MADAR-CASE-TRANSFER-AR.md)
- [`docs/MADAR-ATTACHMENTS-AR.md`](docs/MADAR-ATTACHMENTS-AR.md)
- [`docs/MADAR-SEARCH-REPORTING-AR.md`](docs/MADAR-SEARCH-REPORTING-AR.md)

---

## FoundationKit Composer v1

Composer supports **strict composition analysis, deterministic project generation, and an interactive questionnaire** over the same manifest/capability graph.

Supported commands:

```powershell
dotnet run --project tools/FoundationKit.Composer -- capabilities
dotnet run --project tools/FoundationKit.Composer -- profiles
dotnet run --project tools/FoundationKit.Composer -- validate path/to/manifest.json
dotnet run --project tools/FoundationKit.Composer -- validate path/to/manifest.json --require-stable
dotnet run --project tools/FoundationKit.Composer -- explain path/to/manifest.json
dotnet run --project tools/FoundationKit.Composer -- new path/to/manifest.json --output path/to/new-system
dotnet run --project tools/FoundationKit.Composer -- new --interactive --output path/to/new-system
```

Current responsibilities:

- capability/profile discovery, including contract versions;
- strict project-manifest parsing;
- dependency and compatibility explanation;
- exact fail-closed capability-contract validation;
- optional stable-only maturity gate;
- interactive collection of project name, canonical profile, extra capabilities, and providers;
- dependency-first preview and explicit confirmation before interactive writes;
- deterministic Domain/Application/Infrastructure/API/Client/Test scaffolding from the resolved graph;
- generated normalized manifest and `ARCHITECTURE.md` decision report;
- package-reference mode for portable dependency declarations;
- repository-local `--foundation-root` project-reference mode for exact-head build/test proof;
- guarded `--force` regeneration that refuses unknown/user-added or edited generated files.

Interactive example:

```powershell
dotnet run --project tools/FoundationKit.Composer -- `
  new --interactive `
  --output artifacts/MySystem
```

The questionnaire accepts only canonical IDs, can be cancelled before writes, shows the resolved composition with maturity/contract information, and delegates to the same `CompositionAnalyzer` and `ComposerProjectGenerator` used by manifest-driven generation. Explicit `excludeCapabilities` and `capabilityContracts` remain available through the manifest path in interactive v1 rather than being inferred.

Repository-local deterministic proof remains available with:

```powershell
dotnet run --project tools/FoundationKit.Composer -- `
  new docs/examples/foundationkit.project.minimal.json `
  --output artifacts/composer-golden `
  --foundation-root .
```

The generator does not equate catalog presence with runtime implementation. If a resolved capability is planned/preview/reference-only or has no reusable package binding, that remains explicit in maturity warnings and the generated architecture report. No fake `FoundationKit.Files`, `FoundationKit.Search`, `FoundationKit.Organization`, or other speculative package is produced.

A dedicated Composer Generation workflow proves determinism by hashing generated files before/after guarded regeneration and then restoring, building, and testing the generated solution. Interactive session behavior is covered by the repository test suite.

Still future tooling:

```text
visual Workbench composer
richer provider-specific wiring where reusable provider contracts exist
```

Read [`docs/COMPOSER-CLI-V1.md`](docs/COMPOSER-CLI-V1.md).

---

## Windows unified manager

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 doctor
```

Useful commands:
```powershell
.\foundationkit.ps1 start -Target Athar -Mode Auto
.\foundationkit.ps1 start -Target Workbench -Mode Auto
.\foundationkit.ps1 start -Target Madar -Mode Docker
.\foundationkit.ps1 status -Target Madar
.\foundationkit.ps1 logs -Target Madar
.\foundationkit.ps1 start -Target All -Mode Auto
.\foundationkit.ps1 status -Target All
.\foundationkit.ps1 stop -Target All
.\foundationkit.ps1 verify
.\foundationkit.ps1 pack
.\foundationkit.ps1 production-check
```

`doctor` checks the required commands, availability of a .NET 10 SDK, visible local SQL Server services on Windows, the main local ports, Git state, and running application health where available, including Madar readiness on port `8100`.

Workbench, Athar, and Madar local credential/state files live under ignored `.local/` paths where applicable. The Windows launchers restrict credential files to the current Windows account and refuse to continue if required ACL protection cannot be applied.

`pack` delegates to the canonical `scripts/pack.ps1` path. Package discovery/count validation therefore has one source of truth rather than a second hard-coded list in the manager.

`Auto` uses Docker when Docker Desktop is ready and otherwise uses local .NET/SQL Server where supported. Madar currently has a Docker operational path only. To preserve the established Athar/Workbench native workflow, `-Target All -Mode Native` skips Madar with an explicit warning; `-Target All -Mode Auto` includes Madar when Docker is available.

For the exact first-run sequence, SQL Server instance overrides, port map, and failure diagnostics, read [`docs/LOCAL-RUN-WINDOWS-AR.md`](docs/LOCAL-RUN-WINDOWS-AR.md). For Madar-specific readiness, local credentials, logs, and Docker semantics, read [`docs/MADAR-OPERATIONS-AR.md`](docs/MADAR-OPERATIONS-AR.md).

---

## Build, test, and package

### Build

```bash
dotnet restore FoundationKit.sln
dotnet build FoundationKit.sln --configuration Release --no-restore
```

### Test

```bash
dotnet test FoundationKit.sln --configuration Release --no-build
```

### Verify generated capability metadata

```bash
dotnet run \
  --project tools/FoundationKit.CatalogGenerator \
  --configuration Release \
  --no-build \
  -- --check
```

### Package all reusable projects

Linux/macOS:

```bash
./scripts/pack.sh Release artifacts/packages
```

Windows:

```powershell
.\scripts\pack.ps1 -Configuration Release -Output artifacts/packages
```

Current invariant:

```text
17 .nupkg
17 .snupkg
```

The scripts discover `src/FoundationKit.*/*.csproj` and fail if the expected reusable package set drifts.

---

## SQL Server and migrations

FoundationKit does not centralize product schemas in the reusable packages.

The consuming application owns:

- its `DbContext`;
- relational provider selection;
- entity configurations;
- migrations;
- migration review;
- transactions;
- concurrency policy;
- production migration execution policy.

Workbench migrations live under:

```text
samples/FoundationKit.Workbench/Infrastructure/Migrations/
```

Athar migrations live under its product infrastructure project. Madar migrations live under:

```text
apps/Madar/Madar.Infrastructure/Migrations/
```

**EF migrations are the schema source of truth.** Documentation must not be treated as a substitute for migration/model inspection.

---

## Catalogs: two different contracts

FoundationKit intentionally has two catalogs with different responsibilities.

### Human implemented-package catalog

```text
catalog/foundationkit.catalog.json
```

It drives the human `FEATURES.md` reference and the embedded Workbench `/api/catalog` surface. It lists implemented public behavior only.

### Composition capability graph

```text
catalog/foundationkit.capabilities.json
```

It is generated from the compiled Capability Model and carries:

- capability IDs;
- dependencies;
- kinds;
- maturity;
- capability contract versions;
- composition profiles.

Do not infer `Stable` from the human catalog; maturity belongs to the composition capability graph. Do not infer a capability contract version from the NuGet package version; they are separate versioning concerns.

---

## Automated verification

Pull-request CI verifies the repository as one system, including applicable stages such as:

- tracked-source secret scanning;
- tracked-repository hygiene checks that reject local/generated/sensitive artifacts;
- repository boundary checks;
- JSON and Atlas validation, including actual Madar Razor routes;
- container hardening checks for repository-owned application containers;
- NuGet vulnerability audit;
- CycloneDX dependency SBOM generation;
- Release build with analyzers;
- generated capability/catalog drift checks, including contract metadata;
- unit and architecture tests, including Composer generation safety/determinism and interactive-session tests;
- Workbench, Athar, and Madar publish;
- all reusable NuGet + symbol packages;
- artifact SHA-256 evidence including Madar publish output;
- dedicated Composer golden generation → hash/re-generation equality → restore → Release build → test;
- Workbench SQL Server workflow;
- Athar readiness, non-root, Arabic/API surface, E2E workflow, and isolated backup/restore;
- Madar non-root runtime, Blazor/API surface, SQL migration/startup, liveness/readiness, authentication/authorization, lifecycle/audit, SLA/collaboration/approvals, department routing/claim/administration, reassignment, transfer, target-queue, secure attachment persistence/download/audit privacy, authorized search/reporting row/count isolation, and persisted SQL/audit E2E workflows;
- Trivy repository plus Athar and Madar image scanning, with Madar SARIF evidence uploaded to code scanning;
- black-box negative security tests;
- CodeQL for C# and JavaScript/TypeScript;
- Windows PowerShell 5.1 parsing and unified-manager diagnostics.

Exact evidence belongs to the pull request/head that produced it. A green historical run is not proof for a newer security- or behavior-relevant head.

---

## Security and production boundary

Repository automation can verify code and test evidence; it cannot invent deployment or organizational controls.

FoundationKit does **not** claim by repository existence alone:

- Production Approval;
- ISO/IEC 27001 certification;
- independent Segregation-of-Duties approval;
- a production KMS/Vault/SMTP/SIEM provider;
- production cloud/network architecture;
- legal retention periods;
- product-specific PII classification;
- production backup/RPO/RTO evidence for every deployment;
- production penetration/load acceptance.

Start with:

- [`docs/PRODUCTION-READINESS-AR.md`](docs/PRODUCTION-READINESS-AR.md)
- [`docs/security/CURRENT-SECURITY-STATUS.md`](docs/security/CURRENT-SECURITY-STATUS.md)
- [`docs/security/SECURITY-DECISIONS.md`](docs/security/SECURITY-DECISIONS.md)
- [`docs/security/POLICY-IMPLEMENTATION-REGISTER.md`](docs/security/POLICY-IMPLEMENTATION-REGISTER.md)

---

## Current autonomous stop boundary

The general-purpose reusable baseline has reached a deliberate consumer/policy boundary. Capability contract/version metadata, deterministic Composer generation, and the interactive questionnaire close the current CLI composition/tooling gap without adding another runtime package. New packages are **not** created merely to reduce roadmap checkboxes.

The following areas need a real product/provider decision or stronger consumer evidence before reusable runtime extraction:

- reusable Files / Documents and storage lifecycle beyond Madar v0.9's first product-owned evidence;
- Background Jobs and a real delayed/scheduled work consumer;
- Messaging / outbox / inbox / broker semantics;
- reusable Idempotency beyond Athar's product-specific behavior;
- reusable Concurrency beyond product-specific SQL Server behavior;
- Organization / Multi-Tenancy hierarchy and isolation topology beyond Madar's department model;
- reusable Search / Reporting beyond Madar v0.10's first product-owned evidence;
- Privacy / Retention and legal/product semantics;
- Money / Numbering and finance semantics;
- Redis/object storage/messaging/search/observability provider families;
- advanced approval routing;
- visual composition UX beyond the current CLI questionnaire;
- AI abstractions after real provider-neutral consumer requirements exist.

That stop rule is intentional: FoundationKit should be broadly useful without silently embedding one company's hierarchy, one product's policy, or one vendor's infrastructure. Madar is a concrete product-domain consumer that can provide evidence for future extraction decisions.

---

## Documentation index

Start here:

1. [`docs/NET10-LTS-BASELINE.md`](docs/NET10-LTS-BASELINE.md) — active .NET 10 LTS target, compatibility, and verification baseline.
2. [`docs/LOCAL-RUN-WINDOWS-AR.md`](docs/LOCAL-RUN-WINDOWS-AR.md) — Windows first run and diagnostics.
3. [`docs/MADAR-OPERATIONS-AR.md`](docs/MADAR-OPERATIONS-AR.md) — Madar local run, readiness, and operational diagnostics.
4. [`docs/MADAR-DEPARTMENT-ROUTING-AR.md`](docs/MADAR-DEPARTMENT-ROUTING-AR.md) — Madar department queues, routing, and claim semantics.
5. [`docs/MADAR-DEPARTMENT-ADMINISTRATION-AR.md`](docs/MADAR-DEPARTMENT-ADMINISTRATION-AR.md) — department lifecycle and Operator membership administration.
6. [`docs/MADAR-CASE-TRANSFER-AR.md`](docs/MADAR-CASE-TRANSFER-AR.md) — controlled reassignment and cross-department transfer.
7. [`docs/MADAR-ATTACHMENTS-AR.md`](docs/MADAR-ATTACHMENTS-AR.md) — secure case attachment policy, storage boundary, API, and audit privacy.
8. [`docs/MADAR-SEARCH-REPORTING-AR.md`](docs/MADAR-SEARCH-REPORTING-AR.md) — authorized case search, same-scope reporting, filters, and privacy boundary.
9. [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)
10. [`docs/PACKAGES.md`](docs/PACKAGES.md)
11. [`docs/FEATURES.md`](docs/FEATURES.md)
12. [`docs/CAPABILITY-MODEL-V1.md`](docs/CAPABILITY-MODEL-V1.md)
13. [`docs/CAPABILITY-ROADMAP-V1.md`](docs/CAPABILITY-ROADMAP-V1.md)
14. [`docs/CAPABILITY-EXTRACTION-STATUS.md`](docs/CAPABILITY-EXTRACTION-STATUS.md)
15. [`docs/COMPOSER-CLI-V1.md`](docs/COMPOSER-CLI-V1.md)
16. [`docs/WORKBENCH.md`](docs/WORKBENCH.md)
17. [`docs/DUAL-FULL-STACK.md`](docs/DUAL-FULL-STACK.md)
18. [`docs/VISUAL-STUDIO-2026-AR.md`](docs/VISUAL-STUDIO-2026-AR.md)
19. [`docs/ADDING-A-PROJECT-AR.md`](docs/ADDING-A-PROJECT-AR.md)
20. [`docs/PRODUCTION-READINESS-AR.md`](docs/PRODUCTION-READINESS-AR.md)
21. [`examples/Athar/README.md`](examples/Athar/README.md)
22. [`apps/Madar/README.md`](apps/Madar/README.md)

The GitHub Pages Atlas is generated from `site/portal-manifest.json` and provides a navigable view of the same repository surfaces.

---

## Versioning

Current package version:

```text
0.1.0
```

The current repository is still evolving. NuGet package version, capability maturity, capability contract version, and Composer generator contract are separate signals. Compatibility requirements should be read from the capability model/Composer contract metadata, while behavioral changes remain documented in the changelog.

See [`CHANGELOG.md`](CHANGELOG.md).
