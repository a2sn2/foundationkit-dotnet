# FoundationKit

FoundationKit is a composable **.NET 10 full-stack system-building foundation**. The active repository boundary is reusable Core packages, deterministic Composer tooling, runtime API contracts, SQL-first read models, typed clients, and an executable Workbench/Core Studio that proves the architecture against real SQL Server paths.

**Current engineering state:** **Consumer-ready Core baseline — Pre-production.** The approved Core vNext repository roadmap is closed at Phase 12. Further reusable Core work should be driven by a concrete consumer requirement rather than by adding speculative phases, packages, providers, or abstractions.

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
Visual/Core Composer -> canonical schema-v2 parser/analyzer
                         |
                         v
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
                  Blazor app shell
                          |
                          v
          FoundationKit.Blazor Soft Orbit
                          |
                          v
                    product UI
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

## Typed transport and generated frontend

Runtime OpenAPI is the canonical serialized transport contract. CI proves deterministic derivation of:

- Postman collections;
- C# typed clients;
- requiredness/nullability;
- idempotency and `If-Match` headers;
- ETag, Location and CorrelationId response metadata;
- CRUD and read-model list operations;
- a buildable .NET 10 Blazor WebAssembly reference shell that embeds the same generated typed client and exact-head `FoundationKit.Blazor` project reference;
- the shared Soft Orbit tokens/components/static assets used by Core Studio and generated applications.

Unsupported OpenAPI shapes fail closed instead of producing partial clients. The frontend shell generator delegates transport generation to `scripts/generate-csharp-client-from-openapi.py`; it does not implement a second API-client algorithm.

## Project isolation

Every host registers an immutable project identity. DI, configuration, business policies, database contexts, module definitions and generated SQL namespaces remain host-local or explicitly project-namespaced. See `docs/PROJECT-ISOLATION-AND-COMPATIBILITY.md`.

## Workbench / Core Studio

Workbench is the executable architecture/reference consumer. Its backend proves SQL, module composition, API behavior, idempotency and integration evidence. Its Blazor client is the **Core Studio** reference experience for inspecting capabilities, modules, contract evidence, schema-v2 composition, local project generation and the first-party design system; it is not a product portal and is not an authorization boundary.

The `/compose` page creates/edits bounded schema-v2 manifest JSON, validates it through the same `ComposerManifestParser` and `CompositionAnalyzer` used by Composer tooling, and can then generate the deterministic project through `ComposerProjectModelGenerator`. The browser neither becomes authoritative nor supplies arbitrary filesystem paths.

The normal local path is:

```text
clone/download FoundationKit
        ↓
.\foundationkit.ps1 start -Target Workbench
        ↓
http://localhost:8080/compose
        ↓
choose → apply → validate → Generate Project
        ↓
<repository>\generated\<ProjectName>
```

When started through Docker, FoundationKit `src/` is mounted read-only and only `generated/` is writable. Generated local solutions retain repository-local project references and Composer's ownership/hash safety for optional force regeneration. See `docs/LOCAL-STUDIO-GENERATION.md`.

The `/design` page is a living design-system reference. It renders the same `FoundationKit.Blazor` components consumed by generated applications rather than maintaining a mock component catalog.

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
Visual Composer + local generation: `/compose`  
Living Design System: `/design`  
Runtime OpenAPI: `/swagger`

## Composer

`FoundationKit.Composer` uses the canonical capability graph for capability/profile discovery, strict manifest validation, dependency explanation, compatibility checks, deterministic project generation and executable schema-v2 resources/read models. Schema v1 remains compatible; schema v2 is additive and fail-closed for unsupported executable combinations.

The proven backend chain covers:

- generated entities/contracts/application services;
- product-owned EF mappings/migrations;
- authorization, audit, concurrency and durable idempotency where declared;
- explicit searchable/sortable/indexed resource fields;
- generated SQL Server indexes;
- generated SQL-view-backed multi-table/report read models;
- OpenAPI -> Postman -> typed-client alignment;
- two generated projects sharing one SQL Server database without project data/schema/idempotency collisions.

## Frontend foundation — Soft Orbit

`FoundationKit.Blazor` is the reusable frontend/design-system package. It remains inside the 17-package baseline and does **not** depend on MudBlazor; MudBlazor remains a Workbench sample choice.

The package now owns:

- typed HTTP result/metadata handling;
- reusable presentation/query/display state;
- semantic Light/Dark design tokens;
- spacing/radius/elevation/motion/status/focus rules;
- responsive RTL/LTR application-shell behavior;
- the temporary replaceable FoundationKit Orbit Nodes mark;
- first-party product-neutral components such as `FkButton`, `FkCard`, `FkBadge`, `FkPageHeader`, `FkEmptyState`, `FkLoadingState`, `FkThemeToggle`, `FkAppShell` and `FkNavItem`.

The visual direction is **Soft Orbit**: light-first neutral surfaces, selective Iris emphasis, Aqua secondary signals, small warm accents, low shadows, controlled soft geometry and purposeful node/orbit micro-interactions. It does not reuse JAIB logos, JAIB brand colors, wallet-specific UI, or proprietary brand assets.

Generated applications link the same `_content/FoundationKit.Blazor/foundationkit.css` and `foundationkit.js` assets and use the same components. Product branding should override semantic tokens/logo/name at the host boundary rather than fork shared component CSS.

Browser state is presentation only. Server authorization, query policy and relational composition remain authoritative. `scripts/generate-blazor-app-from-openapi.py` provides an opt-in product-neutral starting shell, not universal product UX.

See `docs/DESIGN-SYSTEM.md`.

## Core vNext roadmap boundary

The approved implementation roadmap ends at **Phase 12**. The final closure is recorded as four tracks inside Phase 12:

```text
12.C1 Typed transport
12.C2 SQL read engine + view-backed read models
12.C3 Frontend foundation + Core Studio reference
12.C4 Visual Composer + deterministic generated frontend shell
```

These are closure tracks, not Phase 13–16.

The Soft Orbit UI baseline and local Studio generation workflow are final developer-experience closures required before the first real consumer project; they do not create another backend phase.

The current `main` baseline has repository evidence for the complete closure path: backend generation, SQL/read engine, OpenAPI/Postman/typed transport, Core Studio composition tooling, shared design system, local project generation, generated frontend scaffolding, documentation, security and the 17-package boundary.

The next engineering step is therefore a **real consumer project**. New reusable Core work should only reopen when consumer evidence identifies a gap that belongs in FoundationKit rather than in product-owned code.

This is **repository completion**, not Production approval. Protected-main enforcement, independent approval and real operational go-live controls remain environment/process governance under issue #35.

## Quality gates

Pull requests verify tracked-source hygiene, architecture boundaries, generated metadata, Release build/tests, package count/integrity, dependency/SBOM evidence, security scans, CodeQL, Composer generation, typed-client generation, shared-design generated frontend build/publish, Workbench publish, SQL integration, read-engine SQL execution and Windows checks.

## Key documentation

- `docs/ARCHITECTURE.md`
- `docs/PACKAGES.md`
- `docs/WORKBENCH.md`
- `docs/DESIGN-SYSTEM.md`
- `docs/LOCAL-STUDIO-GENERATION.md`
- `docs/PROJECT-ISOLATION-AND-COMPATIBILITY.md`
- `docs/CRUD-MODULE-ENGINE.md`
- `docs/CONTRACT-SOURCE-OF-TRUTH.md`
- `docs/TYPED-CLIENT-GENERATION-V1.md`
- `docs/architecture/READ-MODEL-VIEW-POLICY.md`
- `docs/CORE-VNEXT-119-DECISION.md`
- `docs/CAPABILITY-ROADMAP-V1.md`
- `docs/PRODUCTION-READINESS-AR.md`

License: MIT.