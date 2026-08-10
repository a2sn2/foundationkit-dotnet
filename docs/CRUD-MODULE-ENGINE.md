# FoundationKit Module / CRUD Engine v1

## Purpose

The Module Engine turns FoundationKit's existing primitives into a configuration-first application surface. Its goal is to make repeatable infrastructure feel like configuration while leaving business rules explicit and editable.

CRUD is the first executable module capability. It is intentionally not a replacement for domain-specific commands, workflows, calculations, or transactional business processes.

## Minimal composition

```csharp
services.AddFoundationProject("acme-crm");

services.AddFoundationEfCrudModule<
    Customer, Guid,
    CreateCustomerRequest,
    UpdateCustomerRequest,
    CustomerResponse,
    CustomerMapper,
    AppDbContext>(module => module
        .Named("Customers", "customers")
        .Crud());
```

The host maps the generic API surface:

```csharp
app.MapFoundationCrud<
    Customer, Guid,
    CreateCustomerRequest,
    UpdateCustomerRequest,
    CustomerResponse>(customerModule);
```

With default API options that gives the enabled v1 operations under `/api/customers` without reproducing controller/service orchestration.

API-specific module configuration is documented in `API-ENGINE.md` and includes bounded filtering/sorting, idempotency-header intent, HTTP concurrency preconditions, rate-limit metadata, and route-prefix configuration.

## Business customization

Use a manager for rules that belong to the application:

```csharp
module
    .Crud()
    .UseManager<CustomerManager>();
```

`CustomerManager` implements the CRUD manager contract and can reject or prepare create/update/delete operations. The generic service owns the repeatable ordering and the manager owns business semantics.

Mapping is explicit through `ICrudMapper`. This avoids reflection-based entity over-posting and keeps transport contracts separate from EF entities.

## Validation

The service uses the existing `IValidator<T>` contracts. `AddFoundationEfCrudModule` registers `DataAnnotationsValidator<T>` by default, so simple field-level constraints should normally live on the request contract through attributes such as `Required`, `StringLength`, `Range`, and `RegularExpression`.

A host can replace that default with a custom validator for cross-field, contextual, asynchronous, or external rules. Domain methods must still protect true invariants independently of HTTP validation.

Validation failures become the standard FoundationKit validation result and HTTP Problem Details mapping.

## Authorization

Calling `.Authorization()` opts the module into semantic authorization.

If the host does not register `ICrudAuthorizationPolicy<TEntity,TId>`, the engine registers a deny-all fallback. This is intentional fail-closed behavior.

A module can also provide an ASP.NET authorization policy name through `.Authorization("policy-name")`; the endpoint group then requires that host policy in addition to the application-layer semantic policy.

The generic list operation authorizes the list operation as a whole. It does not invent row-level filtering. Applications requiring ownership/tenant/department row scope should implement that scope explicitly in their query policy/specification.

## Queries, paging, filtering, and sorting

List uses `PageRequest` / `PagedResult<T>` and Specification infrastructure. Page size is bounded by the Core global maximum and module maximum.

The API Engine parses the bounded transport syntax for `filter` and `sort`, but field semantics are owned by `ICrudQueryPolicy<TEntity,TId>`. The default policy accepts ordinary paging and rejects filter/sort expressions. This prevents generic reflection from accidentally exposing persistence fields.

## Concurrency

`.Concurrency()` records capability intent. `ICrudConcurrencyPolicy<TEntity,TUpdate>` receives the current entity, update request, and an optional `CrudConcurrencyPrecondition`.

API modules may require `If-Match`; when they do, the HTTP token is passed separately from the JSON DTO. An `IFoundationApiEntityTagProvider<TRead>` can emit the corresponding ETag on successful responses.

For EF, `ConcurrencyAwareEfUnitOfWork<TDbContext>` still translates `DbUpdateConcurrencyException` into the provider-neutral conflict contract. Explicit stale HTTP preconditions use `412`; missing required preconditions use `428`; persistence/application conflicts can still use `409`.

## Idempotency

The API Engine can declare `Idempotency-Key` as disabled, optional, or required and validates the header when enabled.

This module-engine phase does not claim durable replay or duplicate-response storage. Those require a dedicated reliability/persistence boundary and are tracked separately. Header acceptance alone is not called idempotency.

## Auditing

The engine exposes `ICrudOperationObserver`. `FoundationKit.Auditing` provides `CrudAuditObserver`, which records successful create/update/delete operations with the current project id.

Observers execute after database persistence. This gives a clean provider-neutral seam but does not pretend an arbitrary external sink is transactionally atomic with the application's database.

## API behavior

Create/read/list/update/delete use the standard FoundationKit `Result`/`Error` and WebApi Problem Details mapping. Disabled operations are not mapped by the generic HTTP mapper.

`FoundationApiOperationMetadata` records operation, route, authorization, rate-limit policy, idempotency mode, and concurrency mode. ApiExplorer metadata exposes the concrete request/response/query/header contract to OpenAPI.

## Isolation

The application registers one `FoundationProjectId`. Module metadata is held inside that application's DI container, and resource namespace generation includes the project id. No product-specific manager or policy is stored in a global FoundationKit static registry.

## Current boundaries

The engine deliberately does not claim:

- automatic row-level authorization semantics;
- soft delete/archive policy;
- reflection-based filter-field exposure;
- automatic object mapping by reflection;
- durable idempotency replay;
- multi-resource distributed transactions;
- automatic cache invalidation;
- workflow/approval behavior for every entity;
- generated frontend pages.

Those are separate increments and must reuse the same module model rather than expanding CRUD until it becomes an unsafe universal controller.
