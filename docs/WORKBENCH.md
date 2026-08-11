# FoundationKit Workbench / Core Studio

Workbench is the executable architecture/reference consumer for FoundationKit Core. It is not a production service and does not define universal business semantics. Its Blazor client is now the **Core Studio reference experience**: a UI for inspecting the Core, not a product portal.

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
- a first-party frontend reference that presents Core state without moving authorization or relational join logic into the browser.

The original connected user/admin workflow remains in the Workbench backend and integration smoke as historical vertical-slice evidence. It is deliberately **not** the active frontend framing after Phase 15.

## Core Studio pages

```text
/           Core baseline, phase gates and contract flow
/studio     live capability catalog + declared/effective module composition
/evidence   runtime and engineering proof boundaries
/swagger    runtime OpenAPI/Swagger UI
```

The Studio UI consumes bounded Workbench transport contracts. It never treats hidden buttons, routes, or client state as authorization. Server policies remain authoritative.

## Reusable frontend boundary

`FoundationKit.Blazor` remains the reusable Core frontend package. Phase 15 adds framework-agnostic presentation/query/display state contracts there without adding a new package or a MudBlazor dependency to reusable Core.

MudBlazor remains a **Workbench sample dependency**. It is not silently promoted into the FoundationKit.Blazor package contract.

The reusable presentation layer provides:

- `PresentationState<T>` for idle/loading/ready/empty/error rendering;
- bounded `PagedQueryState` for presentation query intent while the server still validates actual filter/sort policies;
- `ResourceDisplayDescriptor` for safe display metadata without business-rule duplication.

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

The response distinguishes `declaredCapabilities` from `effectiveCapabilities`. Effective capabilities are the deterministic dependency closure used by FoundationKit composition. For example, Authorization contributes Identity/Security dependency intent and Feature Management contributes Settings. This does not claim that environment-specific identity, transport, or production providers have been provisioned.

## Contract source of truth

Runtime OpenAPI is produced at:

```text
/swagger/v1/swagger.json
```

The committed Postman collection is generated from that runtime document:

```text
postman/FoundationKit.Workbench.postman_collection.json
```

Do not edit the collection by hand. `scripts/generate-postman-from-openapi.py` owns deterministic derivation/drift checking. The Phase 13 C# typed-client generator uses the same runtime OpenAPI source and Phase 14 proves generated read-model list operations are included in that typed contract.

## Run

```powershell
.\foundationkit.ps1 start -Target Workbench
.\foundationkit.ps1 status -Target Workbench
.\foundationkit.ps1 logs -Target Workbench
.\foundationkit.ps1 stop -Target Workbench
```

Health: `/api/health`  
Module composition: `/api/modules`  
Core Studio: `/`  
Swagger UI: `/swagger`

## Evidence boundary

A green Workbench/CI run is repository engineering evidence. It does **not** by itself mean Production Approved. Protected-main enforcement, independent human approval and operational go-live controls remain governed separately by issue #35.
