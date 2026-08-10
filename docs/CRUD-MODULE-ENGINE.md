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

That gives the enabled v1 operations under `/api/customers` without reproducing controller/service orchestration.

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

The service uses the existing `IValidator<T>` contracts. A host may register create/update validators before module registration. If no validator is supplied, the default is a no-op validator.

Validation failures become the standard FoundationKit validation result and HTTP Problem Details mapping.

## Authorization

Calling `.Authorization()` opts the module into semantic authorization.

If the host does not register `ICrudAuthorizationPolicy<TEntity,TId>`, the engine registers a deny-all fallback. This is intentional fail-closed behavior.

A module can also provide an ASP.NET authorization policy name through `.Authorization("policy-name")`; the endpoint group then requires that host policy in addition to the application-layer semantic policy.

The generic list operation authorizes the list operation as a whole. It does not invent row-level filtering. Applications requiring ownership/tenant/department row scope should supply a dedicated query/specification until FoundationKit defines a provider-neutral scoped-query contract.

## Concurrency

`.Concurrency()` records the capability intent. The application service always supports an `ICrudConcurrencyPolicy<TEntity,TUpdate>` extension point so a request can carry an expected version/token.

For EF, `ConcurrencyAwareEfUnitOfWork<TDbContext>` translates `DbUpdateConcurrencyException` into the provider-neutral Foundation conflict contract, producing HTTP 409 through the normal result mapper.

## Auditing

The engine exposes `ICrudOperationObserver`. `FoundationKit.Auditing` provides `CrudAuditObserver`, which records successful create/update/delete operations with the current project id.

Observers execute after database persistence. This gives a clean provider-neutral seam but does not pretend an arbitrary external sink is transactionally atomic with the application's database.

## Paging and API behavior

List uses the existing `PageRequest` / `PagedResult<T>` and Specification infrastructure. Module page size is bounded by the Core's global maximum.

Create/read/list/update/delete use the standard FoundationKit `Result`/`Error` and WebApi Problem Details mapping. Disabled operations are not mapped by the generic HTTP mapper.

## Isolation

The application registers one `FoundationProjectId`. Module metadata is held inside that application's DI container, and resource namespace generation includes the project id. No product-specific manager or policy is stored in a global FoundationKit static registry.

## Current boundaries

v1 deliberately does not claim:

- generic row-level security filtering;
- soft delete/archive policy;
- generic search/filter DSL;
- automatic object mapping by reflection;
- multi-resource distributed transactions;
- automatic cache invalidation;
- workflow/approval behavior for every entity;
- generated frontend pages.

Those are separate increments and must reuse the same module model rather than expanding CRUD until it becomes an unsafe universal controller.
