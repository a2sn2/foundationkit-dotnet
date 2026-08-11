# FoundationKit

FoundationKit is a composable **.NET 10 full-stack system-building foundation**. The active repository boundary is reusable Core packages, deterministic Composer tooling, runtime API contracts, and an executable Workbench/Core Studio that proves the architecture against real SQL Server paths.

## Current reusable surface

FoundationKit ships exactly **17 NuGet packages + 17 symbol packages**:

- base architecture: Domain, Application, Infrastructure, WebApi, Blazor;
- governance/security: Auditing, Security, Identity, Authorization;
- process/communication: Workflow, Approvals, Notifications, Notifications.Smtp;
- platform: Settings, FeatureManagement, Localization, Caching.

Package existence, capability maturity, repository evidence, and Production approval are separate concepts. A new capability does not automatically justify package #18.

## Core vNext contract path

Core vNext is configuration-first without abandoning Clean Architecture:

```text
Composer schema v2
Project -> Modules -> Resources / Read Models
              |
              v
Generated Domain/Application/Infrastructure/API
              |
              v
SQL Server tables + product-owned indexes + SQL views
              |
              v
Runtime OpenAPI (transport SSOT)
          /             \
         v               v
 deterministic        deterministic
 Postman              C# typed client
                          |
                          v
                  Blazor / Core Studio
```

FoundationKit owns repeatable orchestration and bounded contracts. The generated/consumer application still owns product semantics, authorization rules, secrets, database/provider decisions, migrations, deployment policy, and any capability that is not explicitly implemented by Core.

## SQL-first reads

Write and read paths are deliberately separated:

```text
Write
API -> Application command/service -> Entity/Aggregate -> Repository/UoW -> Tables

Multi-table/report read
API -> Query service -> Read-model contract -> SQL View -> Tables
```

Simple single-aggregate reads may continue through normal repository/specification paths. Multi-table responses, reports and statements default to dedicated read models. Generated relational read models are keyless/read-only and keep filtering/sorting/counting/paging server-side. See `docs/architecture/READ-MODEL-VIEW-POLICY.md`.

## Typed transport

Runtime OpenAPI is the canonical serialized transport contract. CI proves deterministic derivation of:

- Postman collections;
- C# typed clients;
- requiredness/nullability;
- idempotency and `If-Match` headers;
- ETag, Location and CorrelationId response metadata;
- CRUD and read-model list operations.

Unsupported OpenAPI shapes fail closed instead of producing partial clients.

## Project isolation

Every host registers an immutable project identity. DI, configuration, business policies, database contexts, module definitions and generated SQL namespaces remain host-local or explicitly project-namespaced. See `docs/PROJECT-ISOLATION-AND-COMPATIBILITY.md`.

## Workbench / Core Studio

Workbench is the executable architecture/reference consumer. Its backend proves SQL, module composition, API behavior, idempotency and integration evidence. Its Blazor client is the **Core Studio** reference experience for inspecting capabilities, modules and contract evidence; it is not a product portal and is not an authorization boundary.

Run on Windows:

```powershell
.\foundationkit.ps1 start -Target Workbench
.\foundationkit.ps1 status -Target Workbench
.\foundationkit.ps1 logs -Target Workbench
.\foundationkit.ps1 stop -Target Workbench
```

Or run the API directly with a configured `ConnectionStrings:Workbench`:

```bash
dotnet run --project samples/FoundationKit.Workbench/FoundationKit.Workbench.Api.csproj
```

Core Studio: `/`  
Runtime OpenAPI: `/swagger`

## Composer

`FoundationKit.Composer` uses the canonical capability graph for capability/profile discovery, strict manifest validation, dependency explanation, compatibility checks, deterministic project generation and executable schema-v2 resources/read models. Schema v1 remains compatible; schema v2 is additive and fail-closed for unsupported executable combinations.

The proven backend chain now covers:

- generated entities/contracts/application services;
- product-owned EF mappings/migrations;
- authorization, audit, concurrency and durable idempotency where declared;
- explicit searchable/sortable/indexed resource fields;
- generated SQL Server indexes;
- generated SQL-view-backed multi-table/report read models;
- OpenAPI -> Postman -> typed-client alignment;
- two generated projects sharing one SQL Server database without project data/schema/idempotency collisions.

## Frontend foundation

`FoundationKit.Blazor` remains the reusable frontend package. It contains transport/result helpers plus reusable presentation/query/display state. The reusable package does **not** take a MudBlazor dependency; MudBlazor remains a Workbench sample choice.

Browser state is presentation only. Server authorization, query policy and relational composition remain authoritative.

## Quality gates

Pull requests verify tracked-source hygiene, architecture boundaries, generated metadata, Release build/tests, package count/integrity, dependency/SBOM evidence, security scans, CodeQL, Composer generation, typed-client generation, Workbench publish, SQL integration, read-engine SQL execution and Windows checks.

Green CI is engineering evidence, not a claim that external Production governance is configured. Production-go-live governance remains tracked separately in issue #35.

## Key documentation

- `docs/ARCHITECTURE.md`
- `docs/PACKAGES.md`
- `docs/WORKBENCH.md`
- `docs/PROJECT-ISOLATION-AND-COMPATIBILITY.md`
- `docs/CRUD-MODULE-ENGINE.md`
- `docs/CONTRACT-SOURCE-OF-TRUTH.md`
- `docs/architecture/READ-MODEL-VIEW-POLICY.md`
- `docs/CORE-VNEXT-119-DECISION.md`
- `docs/CAPABILITY-ROADMAP-V1.md`
- `docs/PRODUCTION-READINESS-AR.md`

License: MIT.
