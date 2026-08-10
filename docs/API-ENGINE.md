# FoundationKit API Engine

## Problem

A reusable backend foundation is incomplete if every consumer still rebuilds route composition, request parsing, pagination, filtering syntax, validation boundaries, error contracts, concurrency headers, idempotency headers, security metadata, and OpenAPI plumbing.

FoundationKit therefore treats HTTP behavior as part of the module contract rather than as controller boilerplate owned independently by each application.

## Evidence

The Phase 1–6 CRUD vertical slice proved that the existing Domain, Application, Infrastructure, WebApi, Authorization, Auditing, validation, Result/Error, EF Core, and Workbench boundaries can execute one full SQL-backed resource lifecycle. Phase 7 hardens that repeated HTTP surface instead of adding another runtime package.

## Scope

Phase 7 extends the existing `FoundationKit.Application`, `FoundationKit.Infrastructure`, and `FoundationKit.WebApi` packages. Package count remains 17.

A module can configure API behavior alongside CRUD:

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

The default route remains `/api/{module-route}` for compatibility.

## Request and response contract

The generic CRUD API continues to expose create/read/list/update/delete through the configured module route. Request DTOs remain explicit application contracts; FoundationKit does not bind arbitrary entity properties or perform reflection-based over-posting.

Expected application failures use `Result` / `Result<T>` and the centralized FoundationKit Problem Details contract. Unexpected runtime exceptions continue through `FoundationExceptionHandler` with safe public output and correlation-based diagnostics.

## Structural validation

Simple field constraints use the existing `DataAnnotationsValidator<T>` default. Attributes such as `Required`, `StringLength`, `Range`, and `RegularExpression` therefore do not require a second hand-written validator.

A custom `IValidator<T>` remains appropriate for cross-field, contextual, asynchronous, or external validation. Domain entities must still protect true invariants in their own methods because entities can be created or mutated outside HTTP request binding.

## Pagination

List endpoints accept:

- `page` — positive integer, default `1`;
- `pageSize` — positive integer, bounded by both the Core maximum and module maximum.

Invalid values return the standard validation Problem Details response rather than relying on consumer-specific controller logic.

## Filtering

The transport syntax is bounded and owned by Core:

```text
?filter=field|operator|value
```

Multiple `filter` parameters are allowed up to the module's `MaximumFilters`.

Built-in operator identifiers are:

```text
eq
ne
contains
startswith
endswith
gt
gte
lt
lte
```

FoundationKit deliberately does **not** reflect over entity properties to decide what can be queried. Field semantics belong to an explicit `ICrudQueryPolicy<TEntity,TId>` supplied by the module. The default policy rejects filtering and sorting. This prevents accidental exposure of persistence fields and keeps authorization/data-scope decisions visible.

## Sorting

The transport syntax is:

```text
?sort=field|asc
?sort=field|desc
```

Multiple sort expressions are bounded by `MaximumSorts`. As with filtering, allowed fields and their expression mapping are owned by the module query policy.

## Idempotency contract

Mutating operations can declare one of:

```text
Disabled
Optional
Required
```

When required, `Idempotency-Key` is validated at the API boundary and is emitted as a required OpenAPI header. Exactly one non-empty bounded header value is accepted.

**Phase 7 does not claim durable request replay or duplicate-result storage.** It establishes the HTTP contract and operation metadata only. Durable/replay-safe idempotency requires a persistence/provider boundary and belongs to the later reliability capability phase.

This distinction is intentional: accepting a header is not the same as implementing idempotency.

## Concurrency and HTTP preconditions

Concurrency has two API modes:

```text
ApplicationPolicy
RequireIfMatch
```

With `RequireIfMatch`, updates require the `If-Match` header. The token is represented as `CrudConcurrencyPrecondition` and is passed separately to `ICrudConcurrencyPolicy<TEntity,TUpdate>`.

The token is intentionally **not** copied into the JSON update DTO. There is one source of truth for the HTTP precondition.

A module may register `IFoundationApiEntityTagProvider<TRead>` to emit an `ETag` on successful create/read/update responses. The module concurrency policy owns comparison semantics because FoundationKit cannot infer a product's version token safely.

Relevant HTTP responses are:

- `412 Precondition Failed` — supplied token does not match the current resource state;
- `428 Precondition Required` — required `If-Match` is missing;
- `409 Conflict` — provider/application concurrency conflict outside the explicit HTTP precondition path remains available.

## Security metadata

`FoundationApiOperationMetadata` is attached to generated CRUD endpoints and records:

- module name;
- CRUD operation;
- HTTP method and route;
- authorization intent and policy name;
- rate-limit policy name;
- idempotency mode;
- concurrency mode.

If a module declares an ASP.NET authorization policy, the route group requires it. If a module declares semantic authorization but no `ICrudAuthorizationPolicy`, the existing fail-closed policy remains in force.

A configured rate-limit policy name is attached through ASP.NET Core endpoint metadata. The host still owns registration of the concrete rate-limit policy because limits are environment/product decisions.

## OpenAPI

The generic endpoint mapper provides ApiExplorer metadata for:

- create/update JSON request schemas;
- read/create/update/list response schemas;
- path parameters;
- `page`, `pageSize`, `filter`, and `sort` query parameters;
- `Idempotency-Key` and `If-Match` headers, including requiredness implied by module configuration;
- standard Problem Details responses including `409`, `412`, `422`, and `428` where applicable.

Workbench CI captures the real runtime Swagger document and verifies its structure. Phase 8 will make OpenAPI the derivation input for deterministic Postman and later typed-client artifacts so those representations cannot drift independently.

## API impact and compatibility

Phase 7 is additive to the reusable Core surface. Existing modules that do not call `.Api(...)` retain the default `/api/{route}`, disabled idempotency-header requirement, and application-level concurrency behavior.

The existing two-argument `ICrudConcurrencyPolicy<TEntity,TUpdate>.Validate(entity, request)` contract is preserved. A new default interface overload accepts `CrudConcurrencyPrecondition?` and forwards to the original method unless the consumer overrides it. This means an existing concurrency policy continues to compile and behave as before, while a module that opts into HTTP preconditions can override the richer overload.

The Workbench reference DTO intentionally moves its version token from JSON to the standards-based `If-Match` header. Workbench is executable reference evidence, not a published application contract.

## Contract versioning and migration

The current packages remain pre-1.0 `0.1.0`; Core vNext is still an unreleased compatibility surface. No stable 1.0 guarantee is claimed by this phase.

No migration is required for an existing module that keeps its current API defaults and two-argument concurrency policy.

Migration for a module **choosing** HTTP preconditions:

1. remove duplicated expected-version fields from the transport DTO when they only represent HTTP concurrency;
2. configure `api.Concurrency = RequireIfMatch`;
3. override the richer concurrency-policy overload that receives `CrudConcurrencyPrecondition`;
4. register an `IFoundationApiEntityTagProvider<TRead>` when ETags should be returned;
5. send the ETag back through `If-Match` on updates.

## Tests

Tests focus on FoundationKit boundaries rather than retesting framework annotations. Required evidence includes:

- module API option bounds;
- legacy two-argument concurrency-policy source compatibility;
- generic application-service compatibility;
- Problem Details/error-type mapping;
- runtime OpenAPI structure;
- SQL-backed DataAnnotations validation;
- required idempotency header behavior;
- ETag emission;
- missing and stale `If-Match` behavior;
- module-owned filter/sort behavior;
- unsupported filter rejection;
- existing authorization, manager, auditing, CRUD, project-isolation, package, security, and Composer regression gates.

## CI and acceptance criteria

Phase 7 is complete only when the exact PR head proves:

```text
Repository verification
→ Release build
→ Core tests
→ Workbench tests
→ Catalog drift check
→ 17 package + 17 symbol-package integrity
→ Composer generation regression
→ Security scan
→ CodeQL
→ Windows manager check
→ Workbench SQL Server startup
→ runtime OpenAPI structural verification
→ API Engine HTTP/SQL smoke
```

A green unit test alone is not sufficient evidence for this phase.
