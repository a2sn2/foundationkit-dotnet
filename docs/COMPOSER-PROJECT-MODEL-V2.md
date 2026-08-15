# FoundationKit Composer Project Model v2

## Purpose

Composer schema v2 expands FoundationKit composition from project/profile/capability selection into a bounded project model:

```text
Project
  → Modules
    → Resources
      → Behaviors
      → optional Fields
      → Overrides
      → API / Query intent
    → Read Models
  → Providers
```

It reuses the same canonical FoundationKit capability catalog, dependency resolver, capability-contract versions, maturity evidence, Module/API Engine, read-model boundary, and deterministic generator. It does not create a second capability graph or an opaque low-code runtime.

Schema v2 supports compatible resource modes:

```text
resource without fields
→ descriptor-only project intent

resource with supported explicit fields
→ bounded executable full-stack generation
```

Business logic and environment-specific production controls remain consumer-owned in both modes.

## Compatibility

Schema v1 remains supported unchanged:

```text
schemaVersion: 1
→ profile/capability/provider composition
→ generator contract 1
```

Schema v2 remains generator contract 2:

```text
schemaVersion: 2
→ same canonical profile/capability/provider composition
+ modules/resources/behaviors/overrides/API
+ optional executable fields/query intent
+ read-model declarations where supported
→ generator contract 2
```

Rules:

- v1 manifests do not accept `modules`;
- v2 manifests require at least one module;
- `new` chooses the generator from `schemaVersion`; there is no parallel v2 command;
- v1 manifests require no rewrite;
- existing descriptor-only v2 manifests require no rewrite;
- executable fields/query/read-model declarations are additive v2 intent;
- `--force` keeps exact owned-file and SHA-256 protection;
- unsupported executable intent fails closed rather than producing partially wired code;
- future breaking v2 semantics require a new schema version.

## Descriptor-only resource shape

The descriptor model remains valid for intent that should not synthesize a product domain or database model. For example:

```json
{
  "name": "Customer",
  "route": "customers",
  "idType": "guid",
  "behaviors": ["crud", "authorization", "caching"],
  "overrides": {
    "manager": "CustomerManager"
  },
  "api": {
    "routePrefix": "api",
    "idempotency": "optional",
    "concurrency": "application-policy",
    "maximumFilters": 4,
    "maximumSorts": 2,
    "rateLimitPolicyName": "customer-write"
  }
}
```

Without executable `fields`, Composer records deterministic intent/descriptors and does not invent a product domain model, database schema, business manager, query semantics, or provider behavior.

## Executable resources

Adding supported explicit fields requests the bounded executable overlay. The executable path remains deliberately narrower than the descriptor vocabulary so every generated behavior can be inspected, built, tested, and exercised.

The current first-party SQL Server path uses Guid resource IDs and bounded explicit text fields. Fields remain safe C# identifiers, case-insensitively unique, and reserve platform-owned names such as `Id` and `Version`. Maximum lengths are bounded; arbitrary CLR source, raw SQL, scripts, custom templates, or relationship expressions are not accepted from manifest input.

Executable resources may compose the proven CRUD, validation, authorization, auditing, concurrency, and HTTP idempotency paths when their required canonical capabilities are selected. Product-specific managers and environment-specific rate-limit registration remain explicit host work rather than hidden generated behavior.

## Query and index intent

Generated query behavior is explicit rather than inferred from every field. The proven SQL Server resource contract supports bounded text query intent including:

- `exact` filtering;
- `prefix` filtering;
- explicitly enabled sorting;
- explicitly requested product-owned indexes;
- uniqueness where the modeled field contract permits it;
- bounded maximum filter/sort counts.

Unsupported fields/operators fail closed. Filtering, sorting, counting, and paging stay provider-side through `IQueryable` until EF Core terminal execution; FoundationKit does not accept a materialize-then-filter generated path.

Indexes are product-owned schema artifacts. Composer does not index every field automatically and does not imply that substring search or every provider can use the same B-tree/index strategy.

## Read models

Complex multi-table responses, reports, statements, dashboards, and similar projections use explicit read models rather than writable aggregates or application-layer join accumulation.

For the proven SQL Server path:

```text
API
→ query service / read contract
→ EF keyless read-only mapping
→ product-owned SQL View
→ product-owned tables
```

Generated view DDL uses explicit deterministic columns, not `SELECT *`. View/query code remains read-only, authorization/project scope remains explicit, and public DTO/OpenAPI compatibility remains separate from the internal SQL-view implementation.

See `architecture/READ-MODEL-VIEW-POLICY.md` for the architectural rule and completed proof boundary.

## What executable generation produces

The normal Domain/Application/Infrastructure/API/Test scaffold remains. Depending on declared supported intent, the executable overlay adds product-owned source such as:

```text
src/<Product>.Domain/GeneratedModules/...
src/<Product>.Application/GeneratedModules/...
src/<Product>.Infrastructure/GeneratedModules/...
src/<Product>.Infrastructure/GeneratedPlatform/GeneratedDbContext.cs
src/<Product>.Infrastructure/GeneratedPlatform/Migrations/...
src/<Product>.Api/GeneratedPlatform/...
src/<Product>.Api/Program.cs
```

The generated product composes existing FoundationKit surfaces rather than duplicating platform logic:

```text
explicit manifest intent
→ product entities/contracts/read contracts
→ validation + CRUD/query application seams
→ authorization/audit/concurrency/idempotency seams
→ product-owned SQL Server tables/indexes/views/migrations
→ FoundationKit API Engine
→ runtime OpenAPI
       ↙         ↘
Postman        typed C# client
                  ↓
           generated Blazor application
```

Postman and typed clients are derived from runtime OpenAPI; they are not separate transport sources of truth.

## Database ownership and project isolation

Reusable FoundationKit packages own no product schema or migration. Generated DbContext mappings, migrations, resource/index/view DDL, idempotency state, and migration-history naming live in the generated product.

Composer derives deterministic project-scoped identities for product-owned database/runtime artifacts. The generated-project proof runs independently generated products against shared SQL infrastructure and verifies project/resource/idempotency isolation.

No database credential is generated into source. Runtime connection strings are supplied through normal consumer configuration, for example `ConnectionStrings__Generated`.

## Authorization reference adapter

Executable authorization uses the Core CRUD authorization seam and an intentionally small generated reference authentication adapter so generated products can be exercised in CI.

Reference headers may be used by the proof host, but that adapter is **not** the final production Identity composition. Real account persistence, login, MFA, recovery, federation, credential handling, secrets, and deployment identity policy remain product/platform work.

## Auth-safe request pipeline

Generated authenticated hosts preserve the intended ordering:

```text
Correlation / Problem Details / Security Headers
→ Authentication
→ Authorization
→ Durable idempotency
→ Endpoint
```

A completed idempotency replay therefore cannot bypass the current request's authentication/authorization decision.

## OpenAPI, typed transport, and frontend

Runtime DTOs and endpoint/module/security/query metadata remain the transport source. The delivered path is:

```text
C# / endpoint metadata
→ runtime OpenAPI
→ deterministic Postman
→ deterministic typed-client proof
→ generated Blazor consumer
```

Conceptually Postman and the typed client are siblings derived from the same OpenAPI contract; the linear form above describes the proof pipeline, not independent ownership.

Generated auth schemes and per-operation security requirements remain represented in OpenAPI. Anonymous operations remain anonymous in both runtime behavior and generated contract artifacts.

## Modules, IDs, and routes

General schema-v2 bounds remain intentionally finite. Module/resource names are safe identifiers and unique at their scopes, effective API routes are unique, descriptor ID types retain the compatible modeled set, and the proven executable path narrows that set where necessary. Route/prefix values use bounded safe ASCII segments.

## Behaviors versus canonical capabilities

Resource behaviors remain resource intent; the top-level capability graph remains canonical. Where resource behavior maps to an existing Core capability, Composer uses the same dependency resolver and explanation reasons. There is no second dependency graph.

Executable concurrency/idempotency additionally require their explicit canonical top-level capability selections so `explain` exposes their contract/maturity/reason truth rather than hiding generator-only dependencies.

## Determinism and destructive safety

For the same manifest, generator contract, FoundationKit baseline, and reference mode, Composer produces the same generated bytes.

The ownership marker records SHA-256 for the generated set. User-added or edited files block destructive `--force` regeneration. Generated output does not depend on timestamps, random project identifiers, machine names, database passwords, or local absolute paths.

## Security and anti-low-code boundary

Composer v2:

- rejects unknown or unsupported modeled input;
- rejects unsafe identifiers/routes and invalid combinations;
- never executes manifest content;
- never accepts arbitrary C#/SQL/script/template bodies from JSON;
- never infers package IDs from user-controlled text;
- keeps package/project bindings catalog-owned;
- keeps product business rules and production identity explicit;
- protects destructive regeneration through ownership/hash verification;
- fails closed when executable intent exceeds the proven generator surface.

The intended rule remains:

> Convention over repetition + configuration over boilerplate + code when business logic requires it.

## Interactive and visual composition

The interactive CLI remains a compatible schema-v1 questionnaire. Core Studio now provides the visual schema-v2 composition experience over this same project model and the same deterministic parser/analyzer/generator. It does not maintain a parallel graph or hidden project format.

## Current acceptance boundary

The Phase 12/Core vNext baseline now includes the backend full-stack proof plus the delivered SQL read engine, typed transport, and generated frontend/tooling tracks. Repository workflows exercise compatible v1/v2 generation, deterministic regeneration, product-owned SQL migrations, CRUD/validation/auth/audit/concurrency/idempotency, indexed server-side query behavior, SQL view-backed read models, runtime OpenAPI, Postman, typed C# clients, generated Blazor applications, project isolation, package integrity, security, and platform checks.

This is a consumer-ready **pre-production** baseline. It does not claim production approval, a complete production Identity system, universal provider portability, or implementation of every Planned/ReferenceOnly capability.
