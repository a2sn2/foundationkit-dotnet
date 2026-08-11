# FoundationKit Workbench / Core Studio

Workbench is the executable architecture/reference consumer for FoundationKit Core. It is not a production service and does not define universal business semantics. Its Blazor client is the **Core Studio reference experience**: a UI for inspecting, composing and visually validating against the Core, not a product portal.

The approved Core vNext implementation roadmap ends at **Phase 12**. Typed transport, SQL read hardening, frontend foundation, visual Composer and generated frontend scaffolding are Phase 12 closure tracks rather than additional phases. The Soft Orbit UI baseline is the final shared-design closure before the first real consumer project; it does not create a new backend phase.

## What it proves

- SQL Server provider selection owned by the host;
- host-owned EF schema/migrations;
- database/domain/application/API/client boundaries;
- Settings, Feature Management, Localization, and Caching runtime paths;
- Core vNext Module/CRUD Engine through a real SQL table and generic endpoints;
- API Engine pagination/filter/sort/header/error/OpenAPI behavior;
- declared versus dependency-expanded effective module capability composition;
- runtime OpenAPI as the canonical serialized transport contract;
- deterministic Postman and C# typed-client derivation from runtime OpenAPI;
- opt-in relational durable idempotency through a Workbench-owned SQL migration;
- SQL-first generated resource filtering/sorting/paging and product-owned indexes;
- read-only SQL-view-backed read models for multi-table/report projections in generated products;
- a first-party frontend reference that presents Core state without moving authorization or relational join logic into the browser;
- visual schema-v2 composition whose authoritative validation runs through the same `ComposerManifestParser` and `CompositionAnalyzer` as Composer tooling;
- deterministic Blazor WebAssembly shell generation that wires the canonical generated C# client instead of creating another transport layer;
- one shared Soft Orbit design system consumed by Core Studio and generated Blazor applications.

The original connected user/admin workflow remains in the Workbench backend and integration smoke as historical vertical-slice evidence. It is deliberately **not** the active frontend framing.

## Core Studio pages

```text
/           Core baseline, closure gates and contract flow
/studio     live capability catalog + declared/effective module composition
/compose    visual schema-v2 starter/editor + canonical Composer validation
/design     living Soft Orbit tokens + real first-party reusable components
/evidence   runtime and engineering proof boundaries
/swagger    runtime OpenAPI/Swagger UI
```

The Studio UI consumes bounded Workbench transport contracts. It never treats hidden buttons, routes, browser validation, or client state as authorization. Server policies remain authoritative.

## Visual Composer boundary

The `/compose` screen does **not** implement an alternate manifest schema in Razor. It only helps produce/edit JSON and submits it to:

```text
POST /api/composer/validate
```

The Workbench endpoint applies a bounded input-size guard, then calls the canonical:

```text
ComposerManifestParser.Parse(...)
CompositionAnalyzer.Analyze(...)
```

The response returns validation status, schema/project/profile counts, resolved capability evidence, maturity and warnings. Invalid manifests return Composer's bounded validation message rather than executing generation, SQL or arbitrary code.

Browser-side starter generation is convenience only. A manifest is valid only when the server-side Composer engine accepts it.

## Reusable frontend / design-system boundary

`FoundationKit.Blazor` remains the reusable Core frontend package and stays inside the 17-package baseline. It is now a Razor Class Library that owns transport/presentation helpers **and** the first-party product-neutral design system. It still has no MudBlazor dependency.

MudBlazor remains a **Workbench sample dependency** for controls that have not been promoted into reusable first-party primitives. Workbench-specific Mud surfaces are visually mapped back to FoundationKit semantic tokens.

Reusable FoundationKit.Blazor includes:

- `ApiResult` / response metadata handling;
- `PresentationState<T>`, `PagedQueryState`, `ResourceDisplayDescriptor`;
- semantic light/dark color tokens;
- spacing, radius, elevation, motion, focus, status and responsive rules;
- persistent theme behavior;
- RTL/LTR-aware shell behavior;
- temporary replaceable Orbit Nodes mark;
- `FkButton`, `FkCard`, `FkBadge`, `FkPageHeader`, `FkEmptyState`, `FkLoadingState`, `FkThemeToggle`, `FkAppShell`, `FkNavItem`.

The canonical stylesheet and behavior assets are:

```text
_content/FoundationKit.Blazor/foundationkit.css
_content/FoundationKit.Blazor/foundationkit.js
```

The `/design` page renders those real components. It must not become a visually similar fork.

See `docs/DESIGN-SYSTEM.md`.

## Generated frontend boundary

The generator:

```text
scripts/generate-blazor-app-from-openapi.py
```

accepts an OpenAPI 3.x input, safe app/namespace/client identifiers and the FoundationKit root. It produces a buildable .NET 10 Blazor WebAssembly reference shell and delegates transport generation to:

```text
scripts/generate-csharp-client-from-openapi.py
```

The full chain is:

```text
runtime OpenAPI
    ↓
canonical deterministic C# typed client
    ↓
FoundationKit.Blazor Soft Orbit tokens/components
    ↓
generated product-neutral Blazor shell
```

The generated shell does not synthesize authorization, relational joins, secrets or business workflows. Product screens consume typed client methods; backend policies and SQL-view-backed read models remain authoritative.

Generated applications may customize product name/logo and semantic brand tokens at their host boundary. They should not copy/fork the shared FoundationKit component stylesheet.

## Core CRUD reference

The `CoreCrud` module composes CRUD, API options, auditing, authorization, concurrency, Feature Management, Localization, and Caching through the FoundationKit module builder.

Its generic endpoints are:

```text
POST   /api/core-crud
GET    /api/core-crud
GET    /api/core-crud/{id}
PUT    /api/core-crud/{id}
DELETE /api/core-crud/{id}
```

The Workbench host supplies request/response contracts, mapper, semantic authorization policy, concurrency policy, query policy, manager override, SQL entity configuration/migration, ETag provider, and audit sink. FoundationKit supplies generic orchestration and endpoint plumbing.

The reference API requires `Idempotency-Key` on mutations and `If-Match` on update. Workbench registers the EF-backed idempotency store and owns its migration. The concurrency token remains an HTTP precondition rather than a duplicate body property.

## Module composition discovery

Workbench exposes bounded architecture evidence at:

```text
GET /api/modules
```

The response distinguishes `declaredCapabilities` from `effectiveCapabilities`. Effective capabilities are the deterministic dependency closure used by FoundationKit composition. This does not claim that environment-specific identity, transport, or production providers have been provisioned.

## Contract source of truth

Runtime OpenAPI is produced at:

```text
/swagger/v1/swagger.json
```

The committed Postman collection is generated from that runtime document:

```text
postman/FoundationKit.Workbench.postman_collection.json
```

Do not edit the collection by hand. `scripts/generate-postman-from-openapi.py` owns deterministic derivation/drift checking. The C# typed-client generator uses the same serialized transport source. The read closure proves generated read-model list operations; frontend generation proves the typed client and shared design system can be embedded into a deterministic Blazor application shell without a second API or visual contract.

## Run

```powershell
.\foundationkit.ps1 start -Target Workbench
.\foundationkit.ps1 status -Target Workbench
.\foundationkit.ps1 logs -Target Workbench
.\foundationkit.ps1 stop -Target Workbench
```

Health: `/api/health`  
Module composition: `/api/modules`  
Composer validation: `/api/composer/validate`  
Core Studio: `/`  
Visual Composer: `/compose`  
Design System: `/design`  
Swagger UI: `/swagger`

## Evidence boundary

A green Workbench/CI run is repository engineering evidence. It does **not** by itself mean Production Approved. Protected-main enforcement, independent human approval and operational go-live controls remain governed separately by issue #35.
