# Core vNext — Issue 119 Decision

Status: implementation decision for phases 1–6.

## Decision

The next reusable increment is **existing-package hardening and extension**, not a new eighteenth package.

Selected increment:

> Project-isolated Module/Service Engine plus a generic CRUD vertical capability implemented across the existing Application, Infrastructure, WebApi, and Auditing boundaries and proven through Workbench against SQL Server.

The reusable output remains 17 NuGet packages plus 17 symbol packages.

## Why this increment

The Core already contained provider-neutral entity primitives, repositories, specifications, unit of work, validation, pagination, results/errors, HTTP result mapping, auditing, authorization primitives, and an executable Workbench. The repeated missing layer was an ergonomic composition surface that turns those primitives into a reusable application operation without forcing every new application to rebuild the same service and endpoint plumbing.

This is stronger evidence for hardening the existing base packages than for inventing a new package boundary.

## Package review

| Package | vNext classification | Decision |
|---|---|---|
| Domain | hardening | keep small; no new runtime dependency |
| Application | selected increment | project identity, module definitions, generic CRUD service/contracts |
| Infrastructure | selected supporting increment | EF module registration and concurrency translation |
| WebApi | selected supporting increment | generic CRUD endpoint mapping |
| Blazor | wait | frontend work is intentionally after backend phases 1–6 |
| Auditing | hardening | optional CRUD success observer |
| Security | maturity review | no promotion without stronger compatibility evidence |
| Identity | maturity review | keep current bounded contracts |
| Authorization | hardening seam | explicit CRUD authorization policy; fail closed when requested but missing |
| Workflow | wait | no API expansion required for CRUD v1 |
| Approvals | wait | no API expansion required for CRUD v1 |
| Notifications | wait | no API expansion required for CRUD v1 |
| Notifications.Smtp | wait | provider remains independent |
| Settings | keep | current Workbench evidence |
| FeatureManagement | keep | current Workbench evidence |
| Localization | keep | current Workbench evidence |
| Caching | keep | current Workbench evidence; module flag is metadata until an explicit cache policy is designed |

## Problem → Evidence → Scope

**Problem:** a consumer can reuse primitives, but still has to repeat application-service orchestration, validation ordering, authorization ordering, mapping, persistence calls, paging, HTTP endpoint plumbing, concurrency translation, and business override seams.

**Evidence:** those primitives already exist independently in the 17-package baseline and Workbench already proves the database/API path. The missing value is composition, not another low-level abstraction.

**Scope:** one v1 module definition and one CRUD application service with bounded extension points. CRUD is the first executable module capability, not a claim that every business operation is CRUD.

## API impact

The change is additive. New public types live inside existing packages. No existing public member is intentionally removed or redefined.

Primary API shape:

```csharp
services.AddFoundationProject("my-project");

services.AddFoundationEfCrudModule<
    Customer, Guid,
    CreateCustomerRequest,
    UpdateCustomerRequest,
    CustomerResponse,
    CustomerMapper,
    AppDbContext>(module => module
        .Named("Customers", "customers")
        .Crud()
        .Auditing()
        .Authorization()
        .Concurrency()
        .UseManager<CustomerManager>());
```

The HTTP host then maps the same module definition with `MapFoundationCrud<...>()`.

## Compatibility and contract versioning

- Package version remains the repository baseline until a release is cut.
- Capability contract version remains separate from package version.
- This increment is additive and does not justify a major version bump by itself.
- Project identity/resource naming is explicitly documented before external provider families are built, so future providers share one isolation rule.
- Breaking changes to this new public surface must follow the compatibility policy in `PROJECT-ISOLATION-AND-COMPATIBILITY.md`.

## Security and failure behavior

- A module that declares `.Authorization()` gets a deny-all semantic policy unless the host registers an explicit `ICrudAuthorizationPolicy`.
- Validation runs before create/update mutation.
- Update concurrency policy runs before the mapper mutates the entity.
- EF concurrency exceptions are translated to a provider-neutral conflict result.
- Business-specific logic belongs in `ICrudManager` rather than in the reusable service.
- CRUD list authorization is operation-level in v1; row-scoped filtering requires a host query/specification until a safe reusable row-scope contract exists.

## Transactions and audit

Each CRUD command has one `IUnitOfWork.SaveChangesAsync` persistence boundary. Multi-step cross-resource transactions remain host/provider policy.

CRUD observers run after successful persistence. The audit observer therefore proves reusable audit composition but does **not** claim that an arbitrary external audit sink is atomically committed with the business database. A deployment requiring atomic audit persistence must use an appropriate transactional design/provider.

## Tests

Acceptance evidence includes:

- project-id validation and namespace isolation;
- separate DI/project contexts;
- no mutable public static state in the new reusable surface;
- module registry duplicate protection;
- generic CRUD create/read/list/update/delete;
- manager override behavior;
- fail-closed authorization;
- validation and concurrency conflicts;
- audit observer contract;
- Workbench SQL migration and end-to-end CRUD smoke;
- Swagger/OpenAPI exposure;
- 17+17 package output.

## Migration

No reusable package owns a database migration. The reference `CoreCrudRecords` migration belongs to Workbench only and exists to prove the vertical path.

Existing consumers of the 17 packages are not required to opt into the module engine.

## CI

The repository CI must build/test/pack the Core, publish Workbench, run the exact-head catalog check, and run the Workbench SQL CRUD smoke before merge.

## Acceptance criteria

The increment is accepted only when a new resource can be wired through configuration plus host-specific mapper/policy/manager and receives database persistence, validation, authorization, concurrency handling, HTTP endpoints, Problem Details, OpenAPI, and audit extension behavior without reproducing the generic orchestration logic.
