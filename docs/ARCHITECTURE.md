# FoundationKit Technical Architecture

## Purpose

FoundationKit is a composable .NET 10 system-building foundation. The active repository consists of 17 reusable packages, Composer tooling, catalog/schema generation, tests, one executable Workbench reference, and generated-project proof gates. Phase 12 does not add an eighteenth reusable package.

## Base layers

```text
Domain <- Application <- Infrastructure
             ^
             |
           WebApi

Blazor is client-oriented and does not depend on server persistence.
```

- **Domain**: entities, aggregate roots, value objects, domain events/exceptions.
- **Application**: results, validation, use-case ports, repositories/specifications, UoW, pagination, project/module/CRUD contracts, capability composition, and provider-neutral durable-idempotency contracts.
- **Infrastructure**: provider-neutral EF adapters, explicit relational EF adapters where relational semantics are required, domain-event dispatch, and module registration helpers.
- **WebApi**: HTTP/Problem Details/correlation/security-header conventions, generic API/CRUD endpoint mapping, concurrency/idempotency HTTP orchestration, split auth-safe request-pipeline helpers, and runtime contract metadata.
- **Blazor**: typed API/error/state/ViewModel primitives.

Optional packages compose around those boundaries. Lower layers do not gain upper-layer dependencies for convenience.

## Project isolation

FoundationKit code is shared; application runtime state is not. Each host registers one immutable project identity and owns its DI container, configuration, policies, managers, database/provider, schema, migrations, credentials, and deployment. Shared provider resources must include the project namespace.

Durable idempotency follows the same rule: its database identity begins with `ProjectId`, so shared relational infrastructure does not collapse independent project key spaces.

Phase 12 extends this principle into generated products. The executable Composer overlay deterministically derives separate product identities, resource-table namespaces, idempotency tables, and EF migration-history tables. CI runs two generated products concurrently against one SQL Server database and proves they cannot see each other's resource rows or reuse each other's idempotency state.

## Module/Service Engine

A module definition describes an entity/resource and selected reusable behaviors. CRUD v1 is the first executable module capability. The generic application service orchestrates validation, semantic authorization, mapping, repository/UoW persistence, concurrency policy, and post-success observers while business rules remain host-specific managers/policies.

Module composition distinguishes declared capability intent from deterministic dependency-expanded effective intent. That effective set is composition metadata, not proof that an environment-specific provider/store/transport has been provisioned.

```text
Request
  ↓
API Engine
  ├─ bounded transport parsing
  ├─ idempotency / preconditions
  ↓
Generic CRUD service
  ├─ validation
  ├─ semantic authorization
  ├─ business manager hook
  ├─ mapper
  ├─ repository / UoW
  ├─ concurrency
  └─ success observers
  ↓
Result / Error
  ↓
HTTP / Problem Details / OpenAPI
```

## Database ownership

Reusable packages do not own a product schema or migration. Workbench selects SQL Server and owns `WorkbenchDbContext` plus migrations.

`FoundationKit.Infrastructure` has no SQL Server/PostgreSQL provider dependency. It may use EF Core Relational when a reusable adapter genuinely depends on relational semantics. The durable-idempotency adapter is one such boundary: FoundationKit provides the model-builder/store implementation, while the consumer decides the concrete relational provider and owns the migration/table in its schema.

Composer Phase 12 follows exactly the same rule. When a schema-v2 resource explicitly declares executable `fields` under the bounded SQL Server contract, Composer generates the product's DbContext, EF mapping, product migration, resource table, idempotency table where required, and product-scoped migration-history name inside the generated product. Those files are generated consumer code; no product migration is moved into a reusable FoundationKit package.

No database credential is emitted into generated source. The generated host requires its connection string at runtime through normal configuration, for example `ConnectionStrings__Generated`.

## HTTP request-pipeline security

`FoundationKit.WebApi` retains the compatibility helper:

```csharp
app.UseFoundationRequestPipeline();
```

and now exposes its additive parts:

```csharp
app.UseFoundationRequestDiagnostics();
app.UseFoundationIdempotency();
```

Authenticated hosts can therefore use:

```text
Correlation / Problem Details / Security Headers
        ↓
Authentication
        ↓
Authorization
        ↓
Durable idempotency replay
        ↓
Endpoint
```

This preserves FoundationKit correlation/security headers on 401/403 responses while ensuring a completed idempotency response is never replayed before the current request passes authentication/authorization. The generated A/B runtime proof explicitly exercises replay with and without authentication.

## API contract source of truth

Runtime C# DTOs, module/API configuration, endpoint metadata, authorization metadata, and ApiExplorer metadata produce the running OpenAPI contract. That OpenAPI document is the canonical serialized transport contract.

Derived artifacts flow one way:

```text
C# contracts/config/metadata
        ↓
runtime OpenAPI
       ↙         ↘
Postman        typed C# client
                  ↓
           generated Blazor/client consumers
```

Postman and the typed C# client are deterministic derived artifacts; neither is an independent transport source of truth. Workbench and generated-project CI reject contract drift, and the generated Blazor path consumes the same typed transport rather than maintaining a parallel client contract.

Generated reference authentication is represented in OpenAPI by security schemes, but requirements are attached per operation from authorization endpoint metadata. Anonymous health operations therefore stay anonymous in both runtime behavior and OpenAPI.

## Reliability

Current proven reliability surfaces are intentionally separated:

- domain-event dispatch remains in-process after successful persistence; it is not durable integration messaging;
- HTTP durable idempotency provides project-scoped relational acquisition and bounded completed-response replay for opted-in API operations;
- failed/indeterminate idempotent operations are fail-closed rather than automatically executed again under the same key;
- optimistic concurrency is explicit through policy plus ETag/If-Match rather than hidden in update DTOs.

FoundationKit does **not** infer from durable HTTP replay that it has implemented outbox/inbox, broker delivery, retry/dead-letter infrastructure, distributed transactions, or distributed exactly-once execution. Those remain separate evidence-driven roadmap boundaries.

Idempotency is assessed as `ReferenceOnly`, not Preview/Stable: implementation, quality, Workbench adoption, and generated A/B adoption evidence exist, while broader provider compatibility and long-term support remain incomplete.

## Composition model

The canonical capability graph, dependency resolver, profiles, contract versions, maturity evidence, project isolation contract, and module capability rules live in Application. Generated catalog JSON is checked for drift. Composer consumes the same capability truth for validation, explanation, compatibility, deterministic generation, and interactive generation.

Composer has two compatible manifest generations:

```text
schema v1
Project → Profile → Capabilities → Providers

schema v2
Project
  → Modules
    → Resources
      → Behaviors
      → optional executable Fields
      → Overrides
      → API
    → Read Models
  → Profile / Capabilities / Providers
```

Schema-v2 resource behaviors do not create a second dependency graph. Behaviors that correspond to reusable Core capabilities are mapped back into the canonical capability resolver. Executable concurrency/idempotency are also explicitly selected in the top-level canonical composition; Composer does not silently mutate capability selection from broad resource intent.

Generator compatibility is explicit:

```text
schema v1
→ generator contract 1
→ existing deterministic structural scaffold

schema v2 without fields
→ generator contract 2
→ structural scaffold + normalized project model + descriptors

schema v2 with bounded executable fields
→ generator contract 2
→ same model + generated product-owned full-stack overlay
```

The executable overlay supports a deliberately bounded contract: Guid IDs, explicit bounded text fields, SQL Server, CRUD, DataAnnotations validation, authorization/audit/concurrency/idempotency where declared, explicit exact/prefix filtering and sorting intent, deterministic product-owned indexes, generic API Engine routes, SQL-side paging/filter/order, SQL view-backed read models, runtime OpenAPI, deterministic Postman and typed C# client derivation, and generated Blazor consumers. Unsupported manager/rate-limit/field/query combinations fail closed rather than producing partial code.

The v1 generator is not silently redefined. CI independently proves v1, descriptor-only v2, executable generation, force-regeneration determinism, restore, build, tests, SQL/read-model behavior, typed transport, and generated frontend behavior on the same repository head.

## Workbench and generated proof roles

Workbench remains the executable hand-authored Core/SQL reference. It proves database → domain/application → API → client/reference behavior, module composition, API Engine behavior, OpenAPI/Postman contract derivation, and durable idempotency replay through consumer-owned migrations.

Generated projects are a separate Composer adoption proof. They establish that the platform can emit inspectable product-owned backend/read/frontend vertical slices and that independently generated products remain isolated on shared SQL infrastructure.

Neither Workbench nor generated reference authentication is a universal production deployment or production Identity system.

Core Studio provides the visual Composer experience over the same schema-v2 project model, analyzer, and deterministic generator. It does not introduce a parallel capability graph or hidden project format.

## Production boundary

Repository gates prove code/package/reference/generated-product behavior. Production approval remains deployment-specific and requires external operational, security, governance, recovery, identity, secrets, compliance, and acceptance controls.
