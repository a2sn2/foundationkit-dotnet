# FoundationKit

FoundationKit is a composable **.NET 10 full-stack system-building foundation**. The active repository boundary is reusable Core packages, deterministic Composer tooling, runtime API contracts, SQL-first read models, typed clients, an executable Workbench, and Project Studio for visual full-project composition.

**Current engineering state:** **Consumer-ready Core baseline — Pre-production.** The approved Core vNext repository roadmap is closed at Phase 12. The historical `v0.1.0-consumer-baseline.1` remains frozen; Project Studio is post-baseline developer/product-composition work, not another numbered Core phase.

## Current reusable surface

FoundationKit ships exactly **17 NuGet packages + 17 symbol packages**:

- base architecture: Domain, Application, Infrastructure, WebApi, Blazor;
- governance/security: Auditing, Security, Identity, Authorization;
- process/communication: Workflow, Approvals, Notifications, Notifications.Smtp;
- platform: Settings, FeatureManagement, Localization, Caching.

Package existence, capability maturity, repository evidence, provider availability and Production approval are separate concepts. A new capability does not automatically justify package #18.

## Project Studio

`/studio` is the product-oriented visual composition environment. It lets a developer describe the project rather than manually assemble the general platform foundation first:

```text
Project / profile / Linked or Standalone binding
                    ↓
Platform features + providers
(.NET / FoundationKit / ABP OSS / Consumer)
                    ↓
Modules → Resources → typed fields / references
                    ↓
Dependency + provider resolution
                    ↓
Safe generation preview
                    ↓
Canonical Composer schema-v2 generation
                    ↓
Typed CLR + SQL/FK + API + Blazor business UI overlays
                    ↓
Generated full-stack project
                    ↓
Custom hard-coded product code
                    ↓
reopen Blueprint → Preview → Regenerate safely
```

Project Studio supports Text, Integer, Decimal, Boolean, Date, DateTime, Guid and Reference fields. A resource can generate Domain contracts, EF Core SQL Server mapping/migrations, CRUD API endpoints, OpenAPI surface, and runnable Blazor list/create/edit/delete pages. Reference fields produce deterministic relationship metadata and SQL foreign keys.

The feature catalog exposes general project concerns such as Identity, Authorization/Permissions, Auditing, Settings, Feature Management, Localization, Multi-Tenancy, Background Jobs/Workers, Messaging/Event Bus, BLOB storage, Distributed Locking, Caching, Observability, HTTP resilience and the broader FoundationKit capability vocabulary. Dependencies are resolved automatically and provider choice stays explicit.

Maturity is visible in Studio instead of pretending every checkbox is equivalent:

- `Generated` — directly executable Studio/Core output;
- `ProviderReady` — provider wiring is available while environment-specific setup may remain;
- `Reference` — bounded reusable/reference implementation;
- `Planned` — architecture vocabulary only, not a production-complete implementation.

ABP integration is **optional and OSS-only**. Native .NET remains preferred where it already solves the concern cleanly; FoundationKit keeps its differentiating conventions/generation layer; ABP is used when its mature infrastructure adds value. ABP Commercial is not silently introduced.

Before writing a target, Studio performs a full isolated generation preview and reports files to create/update/delete plus consumer files that will be preserved. `foundationkit.studio.json` is the persistent editable Blueprint.

Generated files remain hash-owned. Direct manual edits to generated-owned files fail regeneration closed. Normal hard-coded product work is supported: unowned consumer files are preserved, and `Custom` is the recommended location for partial hooks, product services, middleware, endpoints and replacement UI.

See `docs/PROJECT-STUDIO.md`.

## Core vNext contract path

Core vNext is configuration-first without abandoning Clean Architecture:

```text
Project Studio / Advanced Composer
                    ↓
canonical schema-v2 parser/analyzer
                    ↓
Project → Modules → Resources / Read Models
                    ↓
Generated Domain/Application/Infrastructure/API
                    ↓
SQL Server tables + product-owned indexes + SQL views
                    ↓
Runtime OpenAPI (transport SSOT)
          /                         \
         v                           v
 deterministic                  deterministic
 Postman                        C# typed client
                                    ↓
                            Blazor application
                                    ↓
                      FoundationKit.Blazor Soft Orbit
                                    ↓
                              product UI/custom code
```

FoundationKit owns repeatable orchestration and bounded contracts. The generated/consumer application still owns product semantics, environment secrets, deployment policy and any capability whose maturity/provider boundary says product configuration remains required.

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
- a buildable .NET 10 Blazor WebAssembly application shell with the shared `FoundationKit.Blazor` boundary;
- Project Studio generated business CRUD pages for typed resources;
- the shared Soft Orbit tokens/components/static assets used by Workbench and generated applications.

Unsupported OpenAPI shapes fail closed instead of producing partial clients. The frontend transport generator delegates client generation to `scripts/generate-csharp-client-from-openapi.py`; it does not implement a second API-client algorithm.

## Project isolation

Every host registers an immutable project identity. DI, configuration, business policies, database contexts, module definitions and generated SQL namespaces remain host-local or explicitly project-namespaced. See `docs/PROJECT-ISOLATION-AND-COMPATIBILITY.md`.

## Workbench, Project Studio and Advanced Composer

Workbench is the executable architecture/reference consumer and local host. Its backend proves SQL, module composition, API behavior, idempotency and integration evidence. Its Blazor client now exposes two deliberately different composition surfaces:

- `/studio` — **Project Studio**, the visual project factory for features/providers, typed data design, preview, generation and safe regeneration;
- `/compose` — **Advanced Composer**, the bounded schema-v2 manifest/editor engineering surface that directly exercises the canonical Composer parser/analyzer/generator.

The normal product-composition path is:

```text
clone/download FoundationKit
        ↓
.\foundationkit.ps1 start -Target Workbench
        ↓
http://localhost:8080/studio
        ↓
Project → Features/Providers → Modules/Data
        ↓
Preview Changes
        ↓
Generate Project
        ↓
<repository>\generated\<ProjectName>
        ↓
optional Custom hard-coded product work
        ↓
open Blueprint → Preview → Regenerate
```

When started through Docker, FoundationKit `src/` is mounted read-only and only `generated/` is writable. Linked mode retains repository-local FoundationKit references; Standalone/source-copy mode copies the required FoundationKit dependency closure so the generated product can move outside the repository. See `docs/PROJECT-STUDIO.md` and `docs/LOCAL-STUDIO-GENERATION.md`.

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

Workbench overview: `/`  
Project Studio: `/studio`  
Advanced Composer: `/compose`  
Living Design System: `/design`  
Evidence: `/evidence`  
Runtime OpenAPI: `/swagger`

## Composer

`FoundationKit.Composer` remains the canonical generation engine. It uses the capability graph for capability/profile discovery, strict manifest validation, dependency explanation, compatibility checks, deterministic project generation and executable schema-v2 resources/read models. Schema v1 remains compatible; schema v2 is additive and fail-closed for unsupported executable combinations.

Project Studio is a composition layer over Composer, not a parallel generator. It resolves visual project intent, compiles the supported executable subset to Composer, then applies deterministic typed/platform/UI overlays to that canonical base.

The proven backend chain covers:

- generated entities/contracts/application services;
- product-owned EF mappings/migrations;
- authorization, audit, concurrency and durable idempotency where declared;
- explicit searchable/sortable/indexed resource fields;
- generated SQL Server indexes and Project Studio reference foreign keys;
- generated SQL-view-backed multi-table/report read models;
- OpenAPI -> Postman -> typed-client alignment;
- generated Blazor application/UI;
- Linked and Standalone/source-copy Foundation binding.

## .NET-first and ABP-enabled platform leverage

FoundationKit should not reimplement general infrastructure that .NET or a mature OSS provider already supplies well. The preferred decision order is:

```text
.NET / ASP.NET Core native capability
        ↓ if more infrastructure is justified
ABP OSS provider
        ↓
FoundationKit conventions / composition / generation
        ↓
Consumer-specific code and environment configuration
```

Native leverage includes current .NET 10 / ASP.NET Core foundations such as EF Core, OpenAPI, `HybridCache`, `TimeProvider` and HTTP resilience. ABP provider bridges/integration are available where they provide real value, including current-user/permission/settings/feature infrastructure and Studio provider vocabulary for broader concerns such as multi-tenancy, background jobs, event bus, BLOB storage and distributed locking.

Provider availability is not a claim that every external store/transport is configured automatically. Credentials, durable job/event infrastructure, tenant policy, storage topology and production operations remain explicit product/environment choices.

## Frontend foundation — Soft Orbit

`FoundationKit.Blazor` is the reusable frontend/design-system package. It remains inside the 17-package baseline and does **not** depend on MudBlazor; MudBlazor remains a Workbench sample choice.

The package owns:

- typed HTTP result/metadata handling;
- reusable presentation/query/display state;
- semantic Light/Dark design tokens;
- spacing/radius/elevation/motion/status/focus rules;
- responsive RTL/LTR application-shell behavior;
- the temporary replaceable FoundationKit Orbit Nodes mark;
- first-party product-neutral components such as `FkButton`, `FkCard`, `FkBadge`, `FkPageHeader`, `FkEmptyState`, `FkLoadingState`, `FkThemeToggle`, `FkAppShell` and `FkNavItem`.

The visual direction is **Soft Orbit**: light-first neutral surfaces, selective Iris emphasis, Aqua secondary signals, small warm accents, low shadows, controlled soft geometry and purposeful node/orbit micro-interactions. It does not reuse JAIB logos, JAIB brand colors, wallet-specific UI, or proprietary brand assets.

Generated applications link the same `_content/FoundationKit.Blazor/foundationkit.css` and `foundationkit.js` assets and use the same components. Product branding and product-specific UI should extend or override at the host/consumer boundary rather than fork shared component CSS.

Browser state is presentation only. Server authorization, query policy and relational composition remain authoritative.

See `docs/DESIGN-SYSTEM.md`.

## Core vNext roadmap boundary

The approved implementation roadmap ends at **Phase 12**. The final closure is recorded as four tracks inside Phase 12:

```text
12.C1 Typed transport
12.C2 SQL read engine + view-backed read models
12.C3 Frontend foundation + Core Studio reference
12.C4 Visual Composer + deterministic generated frontend shell
```

These are closure tracks, not Phase 13–16. The frozen consumer baseline remains historical evidence of that closure.

Project Studio is post-baseline product-factory work driven by the concrete need to generate real consumer applications with reusable platform features, business data and safe customization. It does not reopen or renumber the old Core roadmap.

This remains **Pre-production**, not Production approval. Protected-main enforcement, independent approval and real operational go-live controls remain environment/process governance under issue #35.

## Quality gates

Pull requests verify tracked-source hygiene, architecture boundaries, generated metadata, Release build/tests, package count/integrity, dependency/SBOM evidence, security scans, CodeQL, Composer generation, typed-client generation, shared-design generated frontend build/publish, Workbench publish, SQL integration, read-engine SQL execution and Windows checks.

Project Studio adds a dedicated exact-head proof that generates a standalone full project, restores/builds/tests the generated solution, boots a real SQL Server, runs the generated API, exercises authorization, idempotency, typed reference/decimal/boolean/date values, ETag concurrency, filtering and auditing, and verifies the generated SQL foreign key directly.

## Key documentation

- `docs/PROJECT-STUDIO.md`
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
