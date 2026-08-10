# FoundationKit for .NET

**FoundationKit** is a composable .NET foundation for building business systems without turning the reusable core into one giant application.

The repository intentionally separates the reusable platform from its consumers:

```text
FoundationKit reusable packages
        ↓
Optional capabilities / providers
        ↓
Consumers
├── Workbench — executable architecture/reference consumer
├── Athar     — complete Arabic reference product
└── Madar     — operational case-management product through v0.10
```

Current baseline:

```text
.NET 10 LTS / net10.0
17 NuGet packages
17 symbol packages
Composer v1: analyze + validate + explain + deterministic/interactive generation
Madar v0.10: SQL-backed operational product
```

> Repository verification is evidence for the automated/tested scope. It is not Production Approval, external certification, or a substitute for environment-specific operational controls.

---

## Start here: run Madar locally

The primary **Windows human/UAT path** is Native Madar + local SQL Server. Docker remains available for container, CI, integration, and regression coverage; it is not required for the normal Windows UAT flow.

Requirements on Windows:

- Git
- PowerShell 5.1 or later
- .NET 10 SDK selected by `global.json`
- a running local SQL Server instance reachable as `Server=.` by default

From the repository root:

```powershell
git pull
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 doctor
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Madar -Mode Native
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 credentials -Target Madar
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 open -Target Madar
```

Default URL:

```text
http://localhost:8100
```

Useful commands while testing:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 status -Target Madar
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 logs -Target Madar
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 stop -Target Madar
```

Native stop preserves the local `MadarDb` SQL database. Visual Studio-generated `launchSettings.json` files are ignored and are not used by the canonical Native launcher, so they cannot move Madar away from port `8100`.

### Temporary UAT sharing

A running, ready Madar instance can be shared temporarily with testers through either independent route:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 expose -Target Madar -TunnelProvider Microsoft
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 expose -Target Madar -TunnelProvider Cloudflare
```

The Microsoft path uses an anonymous Dev Tunnel while the command is running. The Cloudflare path uses a temporary Quick Tunnel. These are **Development/UAT exposure paths only**, not Production hosting. Anyone who receives an anonymous UAT URL may be able to reach the Development app, so use test data/accounts and stop the tunnel with `Ctrl+C` when the session ends.

### Docker regression path

Docker remains deliberately supported:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Madar -Mode Docker
```

This preserves the container/readiness/security topology used by repository integration and CI evidence without making Docker a prerequisite for human UAT.

### Release publish

Create a Release publish artifact with:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\madar-product.ps1 publish
```

Output:

```text
artifacts/madar/publish/
artifacts/madar/Madar-net10.0-Release.zip
artifacts/madar/Madar-net10.0-Release.zip.sha256
```

Read before acceptance/testing:

- [`docs/MADAR-SPECIFICATION-AR.md`](docs/MADAR-SPECIFICATION-AR.md) — canonical v0.10 product specification.
- [`docs/MADAR-LOCAL-RUN-PUBLISH-AR.md`](docs/MADAR-LOCAL-RUN-PUBLISH-AR.md) — exact Native/Docker run, UAT sharing, credentials, acceptance, and publish flow.
- [`docs/MADAR-OPERATIONS-AR.md`](docs/MADAR-OPERATIONS-AR.md) — operational behavior/readiness/database/SLA/runtime details.
- [`apps/Madar/README.md`](apps/Madar/README.md) — product entry point.

GitHub Pages also exposes a **static Madar demo** under `site/madar-demo/`. It is deliberately labeled as a no-server demo: it does not run ASP.NET Core, SQL Server, authentication, or persistent storage. The real product is the Native/server or Docker runtime above.

---

## Repository map

```text
foundationkit-dotnet/
├─ src/                         reusable FoundationKit packages
├─ samples/                     FoundationKit Workbench
├─ examples/Athar/              complete Arabic reference product
├─ apps/Madar/                  operational case-management product
├─ tools/
│  ├─ FoundationKit.CatalogGenerator
│  └─ FoundationKit.Composer
├─ tests/                       FoundationKit/Workbench/Athar/Madar tests
├─ catalog/                     capability/catalog machine contracts
├─ docs/                        architecture, product, security and runbooks
├─ deploy/                      local/CI Docker Compose definitions
├─ postman/                     API collections
├─ scripts/                     launch, verify, smoke, package and security tooling
├─ site/                        FoundationKit Atlas + static product demos
├─ FoundationKit.sln
└─ foundationkit.ps1            unified Windows repository manager
```

---

## FoundationKit reusable output

The repository currently produces exactly **17 reusable NuGet packages + 17 symbol packages**.

| Package | Responsibility |
|---|---|
| `FoundationKit.Domain` | entities, aggregate roots, value objects, domain events |
| `FoundationKit.Application` | use-case/result/validation/pagination/persistence contracts and capability model |
| `FoundationKit.Infrastructure` | provider-neutral EF Core adapters and in-process domain-event dispatch |
| `FoundationKit.WebApi` | HTTP result mapping, Problem Details, correlation and baseline middleware |
| `FoundationKit.Blazor` | typed API results and reusable async UI state |
| `FoundationKit.Auditing` | provider-neutral audit contracts |
| `FoundationKit.Security` | trusted-proxy, rate-limit partition and assurance conventions |
| `FoundationKit.Identity` | account policy, notifications and sensitive-operation step-up contracts |
| `FoundationKit.Authorization` | permissions, role grants, subjects and ownership evaluation |
| `FoundationKit.Workflow` | deterministic workflow state/trigger definitions |
| `FoundationKit.Approvals` | narrow approve/reject + maker-checker composition |
| `FoundationKit.Notifications` | channel-neutral notification contracts |
| `FoundationKit.Notifications.Smtp` | SMTP provider adapter |
| `FoundationKit.Settings` | hierarchical setting resolution |
| `FoundationKit.FeatureManagement` | settings-backed feature decisions |
| `FoundationKit.Localization` | culture/direction/fallback/time-zone contracts |
| `FoundationKit.Caching` | bounded byte-cache contracts and in-memory reference provider |

Package existence is not the same as capability maturity. The capability model tracks:

```text
Stable
Preview
ReferenceOnly
Planned
```

Every current capability/provider/tooling identity also has contract version `1`. Contract version, NuGet package version and maturity are separate concepts.

Canonical sources:

```text
src/FoundationKit.Application/Capabilities/CapabilityModel.cs
src/FoundationKit.Application/Capabilities/CapabilityCompatibility.cs
catalog/foundationkit.capabilities.json
```

Documentation:

- [`docs/PACKAGES.md`](docs/PACKAGES.md)
- [`docs/FEATURES.md`](docs/FEATURES.md)
- [`docs/CAPABILITY-MODEL-V1.md`](docs/CAPABILITY-MODEL-V1.md)
- [`docs/CAPABILITY-ROADMAP-V1.md`](docs/CAPABILITY-ROADMAP-V1.md)
- [`docs/CAPABILITY-EXTRACTION-STATUS.md`](docs/CAPABILITY-EXTRACTION-STATUS.md)

---

## Architecture rule

Reusable dependency direction remains explicit:

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

And:

```text
Capability contract ≠ provider selection
```

Examples:

- Caching does not force Redis.
- Notifications does not force SMTP.
- Settings is not a secret store.
- Infrastructure does not own a product DbContext or migrations.
- Product-specific behavior is not extracted merely to reduce duplicate-looking concepts.

Madar therefore keeps these concerns product-owned today:

```text
Departments / routing
Attachments / content storage policy
SLA policy
Search / operational reporting
Case semantics
Product SQL schema and migrations
```

No `FoundationKit.Organization`, `FoundationKit.Files`, `FoundationKit.Search`, `FoundationKit.Reporting`, or package #18 is manufactured without independent reusable evidence.

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

---

## Madar — operational product

`apps/Madar` is the real operational case-management consumer in this repository.

Current product depth:

```text
v0.1   Identity + authorization + SQL + case lifecycle + audit + Arabic API/Blazor
v0.1.1 Readiness + bounded startup retry + local/Docker integration
v0.2   SLA deadlines + breach/escalation evidence
v0.3   Append-only case comments
v0.4   Maker-checker approval gate
v0.5   Bounded operational notifications
v0.6   Department queues + routing + Operator claim
v0.7   Department administration + Operator membership
v0.8   Controlled transfer + reassignment
v0.9   Private append-only case attachments/documents
v0.10  Authorized case search + same-scope operational reporting
```

Deterministic lifecycle:

```text
new → assigned → in-progress → resolved → closed
```

Current roles:

```text
Requester
Operator
Supervisor
Administrator
```

Current UI routes include:

```text
/
/login
/cases
/reports/cases
/cases/{CaseId:guid}
/admin/departments
```

Current API includes authentication, cases, search, assignment, route, transfer, reassignment, claim, transition, timeline, SLA evaluation, comments, approvals, attachments, department queues and department administration.

Authorization remains authoritative in the Application layer. Search/reporting counts are scoped after visibility so narrower users cannot infer hidden cases through aggregate counters.

Madar uses SQL Server and product-owned EF Core migrations. Attachment metadata is stored in SQL; Development/CI content storage is private filesystem storage outside `wwwroot` behind the product abstraction. Current attachment limits are 10 MiB and PDF/PNG/JPEG/TXT with bounded signature checks.

Read:

- [`apps/Madar/README.md`](apps/Madar/README.md)
- [`docs/MADAR-SPECIFICATION-AR.md`](docs/MADAR-SPECIFICATION-AR.md)
- [`docs/MADAR-LOCAL-RUN-PUBLISH-AR.md`](docs/MADAR-LOCAL-RUN-PUBLISH-AR.md)
- [`docs/MADAR-OPERATIONS-AR.md`](docs/MADAR-OPERATIONS-AR.md)
- [`docs/MADAR-COMMENTS-AR.md`](docs/MADAR-COMMENTS-AR.md)
- [`docs/MADAR-APPROVALS-AR.md`](docs/MADAR-APPROVALS-AR.md)
- [`docs/MADAR-NOTIFICATIONS-AR.md`](docs/MADAR-NOTIFICATIONS-AR.md)
- [`docs/MADAR-DEPARTMENT-ROUTING-AR.md`](docs/MADAR-DEPARTMENT-ROUTING-AR.md)
- [`docs/MADAR-DEPARTMENT-ADMINISTRATION-AR.md`](docs/MADAR-DEPARTMENT-ADMINISTRATION-AR.md)
- [`docs/MADAR-CASE-TRANSFER-AR.md`](docs/MADAR-CASE-TRANSFER-AR.md)
- [`docs/MADAR-ATTACHMENTS-AR.md`](docs/MADAR-ATTACHMENTS-AR.md)
- [`docs/MADAR-SEARCH-REPORTING-AR.md`](docs/MADAR-SEARCH-REPORTING-AR.md)

---

## FoundationKit Composer v1

Composer is developer tooling over the canonical capability model. It does not add another runtime package or a parallel capability graph.

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

The generator can create deterministic boundaries for:

```text
Domain
Application
Infrastructure
Api       when web-api resolves
Client    when blazor resolves
Tests
```

It also emits a normalized manifest and architecture decision report.

Safety/consistency properties include:

- strict manifest parsing;
- dependency-first resolution;
- exact capability-contract compatibility;
- optional `--require-stable` gate;
- deterministic file content and solution GUIDs;
- no timestamps/random machine state in generated output;
- guarded `--force` that checks generated-file ownership and SHA-256 before destructive regeneration;
- repository-local `--foundation-root` mode for exact-head project-reference verification;
- no invention of runtime packages for planned/reference-only capability vocabulary.

Read [`docs/COMPOSER-CLI-V1.md`](docs/COMPOSER-CLI-V1.md).

---

## Workbench

`FoundationKit.Workbench` is the executable architecture/reference consumer. It demonstrates connected full-stack slices using SQL Server → Domain/Application → API → Blazor and exercises platform-oriented capabilities such as Settings, Feature Management, Localization and Caching.

It is not the repository's operational product.

Read:

- [`docs/WORKBENCH.md`](docs/WORKBENCH.md)
- [`docs/DUAL-FULL-STACK.md`](docs/DUAL-FULL-STACK.md)

---

## Athar

`examples/Athar` is a complete Arabic reference product demonstrating a product-owned implementation of Identity/account lifecycle, authorization, initiatives/review, approvals, SQL Server, audit, notifications/SMTP, Arabic Blazor UX, health/readiness, Docker and backup/restore verification.

It remains the reference product; Madar is the operational product under `apps/`.

Read [`examples/Athar/README.md`](examples/Athar/README.md).

---

## Windows unified manager

The unified manager is designed to be invoked through Windows PowerShell 5.1-compatible syntax:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 help
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 doctor
```

Common product commands:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Athar -Mode Auto
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Workbench -Mode Auto
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Madar -Mode Native
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 status -Target All
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 stop -Target All
```

For Madar-specific credentials, sharing, and Release publish:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 credentials -Target Madar
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 expose -Target Madar -TunnelProvider Microsoft
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 expose -Target Madar -TunnelProvider Cloudflare
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\madar-product.ps1 publish
```

`doctor` reports .NET 10, local SQL Server services, Docker readiness, optional tunnel CLIs, ports, Git state, and known application health/listener state. Madar Native tracks its local process through ignored `.local` state. Docker remains a supported explicit mode for container regression and integration evidence.

Read:

- [`docs/LOCAL-RUN-WINDOWS-AR.md`](docs/LOCAL-RUN-WINDOWS-AR.md)
- [`docs/MADAR-LOCAL-RUN-PUBLISH-AR.md`](docs/MADAR-LOCAL-RUN-PUBLISH-AR.md)

---

## Verification

The normal repository gates cover, as applicable to the changed scope:

```text
tracked-source secret scan
repository hygiene / generated-file checks
JSON / catalog / Atlas validation
NuGet vulnerability audit
Release build with analyzers
FoundationKit + Workbench + Athar + Madar tests
Workbench / Athar / Madar publish
17 nupkg + 17 snupkg packaging
Composer deterministic generation/build/test
Windows PowerShell launcher checks
Workbench SQL integration
Athar SQL/E2E + isolated backup/restore
Madar SQL/E2E + department-routing workflow
Security Scan / Trivy / SARIF
CodeQL
```

The Madar handoff additionally verifies the user-facing Release publish path on Windows PowerShell 5.1, including the generated ZIP and its SHA-256 sidecar. Native UAT launcher behavior is checked separately from the existing Docker/SQL/E2E regression topology.

Run locally where supported:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 restore
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 build
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 test
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 verify
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 pack
```

---

## GitHub Pages / FoundationKit Atlas

`site/` is a static Arabic documentation portal.

It includes:

```text
FoundationKit Atlas
Athar static demo
Madar static demo
```

The static demos are intentionally bounded. They are useful for understanding flows and UI concepts, but they do not provide the real authentication/API/database behavior of the products.

The real Madar runtime is:

```text
Blazor + ASP.NET Core + SQL Server
```

and should be tested through the Native/server or Docker path with explicit environment configuration.

---

## .NET 10 baseline

The active repository baseline is **.NET 10 LTS / `net10.0`**.

The migration was coordinated across:

- target frameworks;
- SDK selection;
- ASP.NET Core / Identity / EF Core / DI;
- SqlClient compatibility;
- Docker build/runtime images;
- Composer-generated projects;
- CI/CodeQL/Windows workflows;
- active documentation.

Athar and Madar explicitly retain their established ASP.NET Identity composite-key length contract at `128`, avoiding an unnecessary schema widening caused solely by a changed framework default.

Read [`docs/NET10-LTS-BASELINE.md`](docs/NET10-LTS-BASELINE.md).

---

## Documentation index

Architecture and core:

- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)
- [`docs/CORE-V0.1-BASELINE.md`](docs/CORE-V0.1-BASELINE.md)
- [`docs/PACKAGES.md`](docs/PACKAGES.md)
- [`docs/FEATURES.md`](docs/FEATURES.md)
- [`docs/NET10-LTS-BASELINE.md`](docs/NET10-LTS-BASELINE.md)

Composer/capabilities:

- [`docs/COMPOSER-CLI-V1.md`](docs/COMPOSER-CLI-V1.md)
- [`docs/CAPABILITY-MODEL-V1.md`](docs/CAPABILITY-MODEL-V1.md)
- [`docs/CAPABILITY-ROADMAP-V1.md`](docs/CAPABILITY-ROADMAP-V1.md)
- [`docs/CAPABILITY-EXTRACTION-STATUS.md`](docs/CAPABILITY-EXTRACTION-STATUS.md)

Madar:

- [`apps/Madar/README.md`](apps/Madar/README.md)
- [`docs/MADAR-SPECIFICATION-AR.md`](docs/MADAR-SPECIFICATION-AR.md)
- [`docs/MADAR-LOCAL-RUN-PUBLISH-AR.md`](docs/MADAR-LOCAL-RUN-PUBLISH-AR.md)
- [`docs/MADAR-OPERATIONS-AR.md`](docs/MADAR-OPERATIONS-AR.md)
- [`docs/MADAR-COMMENTS-AR.md`](docs/MADAR-COMMENTS-AR.md)
- [`docs/MADAR-APPROVALS-AR.md`](docs/MADAR-APPROVALS-AR.md)
- [`docs/MADAR-NOTIFICATIONS-AR.md`](docs/MADAR-NOTIFICATIONS-AR.md)
- [`docs/MADAR-DEPARTMENT-ROUTING-AR.md`](docs/MADAR-DEPARTMENT-ROUTING-AR.md)
- [`docs/MADAR-DEPARTMENT-ADMINISTRATION-AR.md`](docs/MADAR-DEPARTMENT-ADMINISTRATION-AR.md)
- [`docs/MADAR-CASE-TRANSFER-AR.md`](docs/MADAR-CASE-TRANSFER-AR.md)
- [`docs/MADAR-ATTACHMENTS-AR.md`](docs/MADAR-ATTACHMENTS-AR.md)
- [`docs/MADAR-SEARCH-REPORTING-AR.md`](docs/MADAR-SEARCH-REPORTING-AR.md)

Operations/security:

- [`docs/LOCAL-RUN-WINDOWS-AR.md`](docs/LOCAL-RUN-WINDOWS-AR.md)
- [`docs/PRODUCTION-READINESS-AR.md`](docs/PRODUCTION-READINESS-AR.md)
- [`docs/security/CURRENT-SECURITY-STATUS.md`](docs/security/CURRENT-SECURITY-STATUS.md)
- [`SECURITY.md`](SECURITY.md)

---

## Production boundary

The repository can prove technical behavior in its automated/local scope, but a real Production deployment still requires environment-specific work such as:

- domain/HTTPS/ingress;
- secret vault and least-privilege identities;
- persistent Data Protection keys;
- production database topology and backups;
- central logs/metrics/traces/alerts;
- object storage/KMS/malware scanning/retention where attachments are used;
- durable external notification/background scheduling when required;
- privacy/legal/retention/accessibility decisions;
- performance/load acceptance;
- incident response and rollback procedures.

Do not treat a temporary UAT tunnel, static Pages demo, or successful `dotnet publish` as evidence that those external gates have been completed.

---

## Contribution rule

When a consumer reveals a missing concern:

1. solve it in the product if it is product-specific;
2. extract only after independent reuse/provider evidence demonstrates a clean provider-neutral contract;
3. keep dependency direction explicit;
4. add tests and documentation with the change;
5. do not create a new FoundationKit package merely because two names look similar.

See [`CONTRIBUTING.md`](CONTRIBUTING.md).
