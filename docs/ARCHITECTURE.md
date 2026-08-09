# FoundationKit Technical Architecture

## Repository purpose

FoundationKit is a composable .NET foundation plus three deliberately different consumers:

```text
FoundationKit reusable packages (17)
        ↓
Capability graph + profiles + compatibility + maturity evidence
        ↓
Consumers
├── Workbench — executable architecture/reference consumer
├── Athar     — complete Arabic reference product
└── Madar     — operational case-management product through v0.10
```

The goal is not to move every useful behavior into `src/`. Reusable contracts are extracted only when a provider-neutral boundary is independently useful and supported by real consumer/provider evidence.

The active framework baseline is **.NET 10 LTS / `net10.0`**. See [`NET10-LTS-BASELINE.md`](NET10-LTS-BASELINE.md) for the coordinated SDK/runtime/dependency/container decision and its compatibility boundary.

## Reusable foundation

The five architectural packages are:

```text
FoundationKit.Domain
FoundationKit.Application
FoundationKit.Infrastructure
FoundationKit.WebApi
FoundationKit.Blazor
```

Optional/reference packages currently justified by evidence are:

```text
Auditing
Security
Identity
Authorization
Workflow
Approvals
Notifications
Notifications.Smtp
Settings
FeatureManagement
Localization
Caching
```

Together they produce exactly **17 `.nupkg` + 17 `.snupkg`**. Package existence does not imply `Stable` maturity or Production Approval.

## Dependency rules

```text
Domain <- Application <- Infrastructure
             ^
             |
           WebApi

Blazor remains client-oriented and does not depend on server persistence.
```

Optional capabilities compose around these boundaries. A lower layer must not gain a dependency merely because a higher-level feature wants convenience.

`FoundationKit.Infrastructure` may use provider-neutral EF Core abstractions, but products own their `DbContext`, relational provider, mappings, migrations, transactions, concurrency policy, and deployment migration process.

## Composition model

The canonical capability model lives in `FoundationKit.Application` and publishes:

- capability identity/kind/category;
- dependencies;
- maturity;
- contract version;
- maturity-evidence assessment;
- seven composition profiles.

Generated machine documents:

```text
catalog/foundationkit.capabilities.json
catalog/foundationkit.maturity-evidence.json
```

`FoundationKit.Composer` uses this same graph for discovery, strict manifest validation, dependency explanation, exact contract compatibility, maturity validation, deterministic project generation, and the interactive questionnaire. Both manifest-driven and interactive generation delegate to the same `CompositionAnalyzer` and `ComposerProjectGenerator`; the tooling does not maintain a second package/capability model.

The generated scaffold is intentionally bounded to structural Domain/Application/Infrastructure/API/Client/Test layers and real FoundationKit bindings that exist today. Planned or product-owned semantics stay explicit in `ARCHITECTURE.md` instead of being converted into fake reusable packages.

## Consumer 1 — Workbench

`FoundationKit.Workbench` is the executable architecture/reference consumer. It proves two connected SQL-backed vertical slices:

```text
User:  database → domain → use case → contract → API → Blazor
Admin: database → domain → use case → contract → API → Blazor
```

The slices meet at the request lifecycle:

```text
submitted → approved | rejected
```

Workbench also provides runtime proof for Settings, Feature Management, Localization, and Caching. It is a controlled reference surface, not a public Production product.

Detailed walkthrough: [`DUAL-FULL-STACK.md`](DUAL-FULL-STACK.md) and [`WORKBENCH.md`](WORKBENCH.md).

## Consumer 2 — Athar

Athar is the complete Arabic reference product under `examples/Athar`. It owns its business rules, SQL schema/migrations, ASP.NET Core Identity, product permissions, Arabic copy, deployment configuration, and user/admin UX.

It proves end-to-end composition of FoundationKit with authentication/account lifecycle, security boundaries, authorization, workflow, approvals, auditing, notifications/SMTP, idempotency/concurrency reference behavior, SQL Server, Docker, E2E, and backup/restore verification.

Athar is not a reusable layer and its product rules must not migrate into FoundationKit merely because they are useful.

## Consumer 3 — Madar

Madar is the operational product under `apps/Madar`, implemented through v0.10. It owns case lifecycle semantics, departments/routing, SLA policy, comments, sensitive-case approvals, notifications, transfers/reassignments, attachments, authorized search/reporting, SQL schema/migrations, Identity composition, Arabic UI, and operational topology.

Madar reuses FoundationKit contracts where they fit, but its product behavior does not automatically justify new `FoundationKit.Files`, `Organization`, `Jobs`, `Search`, or `Reporting` packages.

See [`../apps/Madar/README.md`](../apps/Madar/README.md).

## Database ownership

Each executable consumer owns its schema:

```text
Workbench → samples/FoundationKit.Workbench/Infrastructure/Migrations/
Athar     → examples/Athar/Athar.Infrastructure/Migrations/
Madar     → apps/Madar/Madar.Infrastructure/Migrations/
```

EF migrations are the schema source of truth. Reusable packages do not own product migrations or select SQL Server globally.

## HTTP/UI boundary

Transport contracts are not EF entities. Product endpoints map application/domain results into product DTOs. `FoundationKit.WebApi` supplies reusable result/Problem Details/correlation/security-header behavior; `FoundationKit.Blazor` supplies typed API/error/state primitives.

Authentication, product authorization, CORS, route ownership, UX, and deployment security remain product responsibilities.

## Domain events vs integration messaging

FoundationKit currently provides in-process domain-event dispatch after successful persistence. This is not a durable broker/outbox/inbox guarantee.

```text
Database save succeeds
        ↓
clear aggregate event queue
        ↓
dispatch in-process handlers
```

A product requiring cross-process durability must implement/choose the appropriate outbox, messaging, retry, and operational semantics; no generic messaging package is inferred from the in-process dispatcher.

## Repository verification

Pull-request verification treats the repository as one system:

- tracked-source secret/hygiene/boundary checks;
- JSON/Atlas/container-policy verification;
- NuGet vulnerability audit + CycloneDX dependency inventory;
- Release build with analyzers;
- generated capability/evidence drift checks;
- FoundationKit, Workbench, Athar, and Madar tests;
- Composer deterministic generated-project restore/build/test;
- Workbench/Athar/Madar publish;
- exact 17+17 reusable-package output + SHA-256 evidence;
- Workbench/Athar/Madar SQL Server integration/E2E;
- Athar backup/restore and negative-security flows;
- Madar operational/privacy regressions;
- Windows PowerShell launcher checks;
- Trivy and CodeQL.

Exact evidence belongs to the exact head that produced it.

## Production boundary

Repository automation can prove code/test/package behavior; it cannot create deployment or organizational controls. Production approval still requires product-specific ingress/TLS, secrets/KMS, SQL principals, backup operations, observability/SIEM, legal/privacy decisions, performance/penetration acceptance, and the protected-branch/independent-review governance tracked by Issue #35.

Targeting .NET 10 LTS removes the prior .NET 8 support-lifecycle deadline from the active baseline; it does not by itself satisfy any Production Approval or organizational control.
