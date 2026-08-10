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
- deterministic Postman derivation from that OpenAPI contract.

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

The reference API requires `Idempotency-Key` on mutations and `If-Match` on update so the Workbench can prove those API Engine contracts. The update JSON body contains business update data only; its concurrency token is the HTTP precondition rather than a duplicate request property.

## Module composition discovery

Workbench exposes architecture evidence at:

```text
GET /api/modules
```

The snapshot distinguishes `declaredCapabilities` from `effectiveCapabilities`. The latter is the deterministic dependency closure used by FoundationKit composition. For example, Authorization contributes the Identity/Security dependency intent and Feature Management contributes Settings. This does not claim that an environment-specific identity store, transport, or production provider has been provisioned.

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

The SQL integration smoke exercises the existing user/admin reference plus module composition discovery, generic CRUD create/read/list/update/delete, DataAnnotations, required/ambiguous idempotency headers, ETag emission, missing/malformed/stale `If-Match`, module-owned filtering/sorting, custom manager rejection, Problem Details, and post-delete not-found behavior.
