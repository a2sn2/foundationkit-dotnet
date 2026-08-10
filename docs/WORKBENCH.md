# FoundationKit Workbench

Workbench is the executable architecture/reference consumer for FoundationKit Core. It is not a production service and does not define universal business semantics.

## What it proves

- SQL Server provider selection owned by the host;
- host-owned EF schema/migrations;
- database/domain/application/API/client boundaries;
- connected user/admin reference workflow;
- Settings, Feature Management, Localization, and Caching runtime paths;
- Core vNext Module/CRUD Engine through a real SQL table and generic endpoints;
- API Engine pagination/filter/sort/header/error/OpenAPI behavior;
- declared versus dependency-expanded effective module capability composition;
- runtime OpenAPI as the canonical serialized transport contract;
- deterministic Postman derivation from that OpenAPI contract;
- opt-in relational durable idempotency through a Workbench-owned SQL migration and FoundationKit's existing Application/Infrastructure/WebApi packages.

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

The Workbench host supplies request/response contracts, mapper, semantic authorization policy, concurrency policy, query policy, manager override, SQL entity configuration/migration, ETag provider, and audit sink. Simple structural request validation uses the Core default `DataAnnotationsValidator<T>`. FoundationKit supplies the generic orchestration and endpoint plumbing.

The reference API requires `Idempotency-Key` on mutations and `If-Match` on update. Workbench registers `AddFoundationEfIdempotencyStore<WorkbenchDbContext>()`, includes `AddFoundationIdempotencyStore()` in its model, and owns the `FoundationIdempotencyEntries` migration. The update JSON body contains business update data only; its concurrency token remains the HTTP precondition rather than a duplicate request property.

## Durable replay proof

The SQL smoke proves the Phase 10 behavior against the real Workbench SQL Server database:

- first create executes normally and returns ID/version/ETag;
- exact create retry with the same key returns the same ID/version/ETag rather than inserting again;
- reusing that key with a changed body returns `409 Foundation.Api.Idempotency.FingerprintConflict`;
- first update with `If-Match: "1"` advances the resource to version 2;
- exact update retry replays version 2 even though the original precondition is now stale, proving the application mutation did not run twice;
- changing `If-Match` under the same update key is a fingerprint conflict because the precondition is part of the request fingerprint;
- first delete returns 204 and the exact retry also returns the replayed 204 instead of executing again and becoming 404.

The durable reference is intentionally fail-closed. It does not claim distributed exactly-once semantics; see `DURABLE-IDEMPOTENCY.md`.

## Module composition discovery

Workbench exposes architecture evidence at:

```text
GET /api/modules
```

The snapshot distinguishes `declaredCapabilities` from `effectiveCapabilities`. The latter is the deterministic dependency closure used by FoundationKit composition. For example, Authorization contributes Identity/Security dependency intent and Feature Management contributes Settings. This does not claim that an environment-specific identity store, transport, or production provider has been provisioned.

## Contract source of truth

Swagger/OpenAPI is produced from the running Workbench at:

```text
/swagger/v1/swagger.json
```

The committed Postman collection is a generated artifact:

```text
postman/FoundationKit.Workbench.postman_collection.json
```

Do not edit that collection manually. `scripts/generate-postman-from-openapi.py` derives it from the captured runtime OpenAPI document, and CI performs byte-for-byte deterministic/drift checks. See `CONTRACT-SOURCE-OF-TRUTH.md`.

## Run

```powershell
.\foundationkit.ps1 start -Target Workbench
.\foundationkit.ps1 status -Target Workbench
.\foundationkit.ps1 logs -Target Workbench
.\foundationkit.ps1 stop -Target Workbench
```

Swagger UI: `/swagger`
Health: `/api/health`
Module composition: `/api/modules`

The SQL integration smoke exercises the user/admin reference, module composition discovery, generic CRUD, DataAnnotations, durable replay-safe idempotency, ETag/If-Match concurrency, module-owned filtering/sorting, manager overrides, Problem Details, runtime OpenAPI, and generated Postman contract synchronization.
