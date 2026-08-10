# FoundationKit API Engine

## Purpose

FoundationKit treats repeated HTTP behavior as part of the module/platform contract rather than controller plumbing that every consumer rewrites. The API Engine composes explicit application contracts into bounded routes, validation, Problem Details, pagination/filter/sort transport rules, security metadata, concurrency preconditions, idempotency metadata, and runtime OpenAPI.

The engine does not replace product business logic. Request/response DTOs, allowed query fields, semantic authorization, concurrency comparison, managers/handlers, and persistence mappings remain explicit seams.

## Module API configuration

A resource can configure API behavior alongside CRUD:

```csharp
module
    .Named("Customers", "customers")
    .Crud()
    .Api(api =>
    {
        api.RoutePrefix = "api";
        api.Idempotency = FoundationApiIdempotencyMode.Required;
        api.Concurrency = FoundationApiConcurrencyMode.RequireIfMatch;
        api.MaximumFilters = 4;
        api.MaximumSorts = 2;
    });
```

The compatibility default remains `/api/{module-route}`.

## Explicit contracts

The generic CRUD API maps create/read/list/update/delete over explicit request/response types. FoundationKit does not bind arbitrary entity properties and does not infer writable/filterable fields through unsafe reflection.

Expected application failures use `Result` / `Result<T>` and the centralized FoundationKit Problem Details contract. Unexpected exceptions continue through `FoundationExceptionHandler` with safe output and correlation diagnostics.

## Structural validation

Simple field constraints use `DataAnnotationsValidator<T>` by default. Cross-field, contextual, asynchronous, or external validation can replace that with an explicit `IValidator<T>`. Domain entities still protect true domain invariants independently of HTTP binding.

## Pagination, filtering, and sorting

List endpoints accept positive `page` and `pageSize`; page size is bounded by both Core and module limits.

Filter syntax is:

```text
?filter=field|operator|value
```

Supported transport operators are `eq`, `ne`, `contains`, `startswith`, `endswith`, `gt`, `gte`, `lt`, and `lte`.

Sort syntax is:

```text
?sort=field|asc
?sort=field|desc
```

FoundationKit owns parsing/bounds only. `ICrudQueryPolicy<TEntity,TId>` owns which fields are valid and how they map to expressions. The default policy rejects filtering/sorting, preventing accidental exposure of persistence fields.

## Concurrency

API concurrency modes are:

```text
ApplicationPolicy
RequireIfMatch
```

With `RequireIfMatch`, update requires one bounded `If-Match` header. The token becomes `CrudConcurrencyPrecondition` and is passed separately to the concurrency policy; it is intentionally not duplicated inside the update JSON DTO.

A module may register `IFoundationApiEntityTagProvider<TRead>` to emit ETags on create/read/update.

Relevant outcomes include:

- `412 Precondition Failed` when the supplied token does not match current state;
- `428 Precondition Required` when a required precondition is missing;
- `409 Conflict` for other application/provider conflicts.

The original two-argument `ICrudConcurrencyPolicy<TEntity,TUpdate>.Validate(entity, request)` contract is preserved, while the richer default-interface overload accepts an optional HTTP precondition.

## Idempotency — HTTP contract

Mutating operations declare:

```text
Disabled
Optional
Required
```

The API boundary validates exactly one bounded `Idempotency-Key` when required and reflects requiredness in OpenAPI. This contract was established before durable replay existed, so consumers that do not install a durable store retain header validation without a false replay guarantee.

## Idempotency — durable replay

Phase 10 adds an opt-in durable implementation while retaining the Phase 7 API contract.

The existing packages own the layers:

```text
Application    → IIdempotencyStore contracts
Infrastructure → provider-neutral relational EF adapter
WebApi         → request fingerprint/acquire/replay orchestration
Consumer       → provider selection + schema/migration + operations
```

A relational consumer opts in with:

```csharp
builder.Services.AddFoundationEfIdempotencyStore<MyDbContext>();
```

and includes:

```csharp
modelBuilder.AddFoundationIdempotencyStore();
```

inside its own model before creating its application-owned migration.

The durable identity is project scoped:

```text
ProjectId + operation scope + SHA256(Idempotency-Key)
```

The persisted request fingerprint covers method, actual path/query, content type, `If-Match` when applicable, and SHA-256 of the body. Raw idempotency keys and raw request bodies are not stored.

Durable acquisition outcomes are:

- `Acquired`;
- `Replay`;
- `FingerprintConflict`;
- `InProgress`;
- `NonReplayable`.

A replayable response stores bounded status/body/content type/Location/ETag. Exact retry within the replay window returns that response without invoking the application mutation again. Same key with changed body/path/query/content type/precondition is a `409` fingerprint conflict.

Indeterminate behavior is fail-closed: exceptions, `5xx`, oversized replay responses, or unsafe finalization are not automatically executed again under the same key. This is replay-safe HTTP idempotency, **not** a distributed exactly-once claim. See `DURABLE-IDEMPOTENCY.md`.

The original `AddFoundationWebApi(IServiceCollection, Action<FoundationErrorHandlingOptions>?)` CLR signature is preserved. Durable settings use the additive `ConfigureFoundationIdempotency(...)` extension, and applications without an `IIdempotencyStore` keep the previous behavior.

## Security metadata

`FoundationApiOperationMetadata` records module, operation, method/route, authorization intent/policy, rate-limit policy, idempotency mode, and concurrency mode.

Declared ASP.NET authorization policies are attached to route groups. Semantic authorization remains fail-closed when declared without an explicit `ICrudAuthorizationPolicy`. Rate-limit registration remains host-owned because actual limits are environment/product decisions.

Durable replay is inside the standard Foundation response pipeline so security headers, status handling, exception handling, and correlation behavior are not bypassed.

## OpenAPI and derived contracts

Generated endpoints expose ApiExplorer metadata for request/response schemas, route/query parameters, headers, requiredness, and Problem Details outcomes.

Workbench CI captures the **running** Swagger document and verifies it structurally. That runtime OpenAPI document is the canonical serialized transport contract. Phase 8 derives the committed Postman collection deterministically from it and CI proves:

```text
runtime OpenAPI
→ generate Postman A
→ generate Postman B
→ A == B byte-for-byte
→ A == committed generated artifact
→ --check synchronized
```

Postman is therefore a derived representation, not an independent source of request truth. A future typed frontend client must use the same one-way contract path.

## Compatibility and migration

API Engine evolution remains additive on the pre-1.0 Core vNext surface:

- modules not configuring API options retain default routing/concurrency/idempotency behavior;
- legacy concurrency policies continue through the preserved two-argument contract;
- existing `AddFoundationWebApi` consumers retain the same CLR method signature;
- durable idempotency is opt-in;
- adopting the relational adapter requires an additive host-owned migration;
- no package #18 is introduced;
- package version, capability contract version, and maturity remain separate concepts.

Consumers adopting HTTP preconditions should remove duplicate expected-version transport fields, require `If-Match`, implement the richer concurrency policy overload, and emit ETags where appropriate.

Consumers adopting durable idempotency should register a store, add the model mapping, create their own migration, select replay/body limits, and define operational retention/reconciliation policy for non-replayable/expired entries.

## Runtime evidence

The Workbench SQL reference proves the full stack:

```text
module configuration
→ API Engine metadata
→ durable idempotency acquisition
→ CRUD application service
→ EF Core
→ SQL Server
→ HTTP response
→ durable completion/replay
```

The smoke suite proves missing/ambiguous headers, DataAnnotations, CRUD, ETag/If-Match, filter/sort policy, manager rejection, Problem Details, create replay with the same ID, update replay without a second version increment, fingerprint conflict when body or `If-Match` changes, and delete replay remaining `204` instead of re-executing.

## Acceptance gate

An API Engine/reliability increment is complete only when the exact PR head proves:

```text
Repository verification
→ Release build
→ Core + Workbench tests
→ generated catalog checks
→ exactly 17 packages + 17 symbol packages
→ Composer regression
→ Security scan
→ CodeQL
→ Windows manager check
→ Workbench SQL Server startup/migrations
→ runtime OpenAPI verification
→ deterministic Postman drift gate
→ API/SQL positive and negative smoke
```

Green unit tests alone are not sufficient evidence.
