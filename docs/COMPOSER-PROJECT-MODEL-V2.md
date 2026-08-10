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
      → API
  → Providers
```

It reuses the same canonical FoundationKit capability catalog, dependency resolver, capability-contract versions, maturity evidence, Module/API Engine, and deterministic generator. It does not create a second capability graph or an opaque low-code runtime.

Schema v2 now supports two compatible resource modes:

```text
resource without fields
→ descriptor-only project intent

resource with explicit fields
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
+ optional executable fields
→ generator contract 2
```

Rules:

- v1 manifests do not accept `modules`;
- v2 manifests require at least one module;
- `new` chooses the generator from `schemaVersion`; there is no parallel v2 command;
- v1 manifests require no rewrite;
- existing descriptor-only v2 manifests require no rewrite;
- `fields` is additive to schema v2;
- `--force` keeps exact owned-file and SHA-256 protection;
- unsupported executable intent fails closed rather than producing partially wired code;
- future breaking v2 semantics require a new schema version.

CI independently generates, force-regenerates, restores, builds, and tests schema v1, descriptor-only schema v2, and two executable schema-v2 products on the same repository head.

## Descriptor-only resource shape

The Phase 11 model remains valid. Example:

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

Without `fields`, Composer records deterministic intent/descriptors and does not invent a product domain model, database schema, business manager, query semantics, or provider behavior.

## Executable resource shape

Adding explicit `fields` requests the bounded Phase 12 executable overlay. Repository examples:

```text
docs/examples/foundationkit.project.fullstack-a.json
docs/examples/foundationkit.project.fullstack-b.json
```

Example resource:

```json
{
  "name": "Customer",
  "route": "customers",
  "idType": "guid",
  "behaviors": [
    "crud",
    "auditing",
    "authorization",
    "concurrency"
  ],
  "fields": [
    {
      "name": "Name",
      "type": "text",
      "required": true,
      "maximumLength": 120
    },
    {
      "name": "Note",
      "type": "text",
      "required": false,
      "maximumLength": 400
    }
  ],
  "api": {
    "routePrefix": "api",
    "idempotency": "required",
    "concurrency": "require-if-match",
    "maximumFilters": 0,
    "maximumSorts": 0
  }
}
```

The executable manifests explicitly select the canonical capabilities they rely on:

```json
"includeCapabilities": ["concurrency", "idempotency"],
"providers": ["provider-sqlserver"]
```

Composer does not silently turn a broad resource behavior into an unrelated global capability selection.

## Fields

Current executable field contract is intentionally narrow:

- `fields` is optional;
- if present it must contain 1–32 fields;
- field names are safe C# identifiers and unique case-insensitively;
- `Id` and `Version` are reserved;
- field type is currently `text` only;
- `maximumLength` is required and bounded to 1..4000;
- `required` defaults to `true`.

This boundary exists so generated code can be completely inspectable and executable. Arbitrary CLR types, raw SQL, source expressions, relationship declarations, scripts, or custom templates are not accepted from JSON.

## Executable behavior boundary

Descriptor-only resources may keep the broader v2 behavior vocabulary. Executable resources currently accept only:

```text
crud
auditing
authorization
concurrency
```

Executable generation also requires:

```text
idType = guid
provider-sqlserver
maximumFilters = 0
maximumSorts = 0
```

When concurrency is declared:

```text
api.concurrency = require-if-match
includeCapabilities contains concurrency
```

When HTTP idempotency is enabled:

```text
includeCapabilities contains idempotency
```

Executable resources currently reject manager overrides and rate-limit policy names because Composer does not yet generate the corresponding product-specific business manager or host policy registration. Query generation is also not claimed yet. Those restrictions prevent a manifest from promising behavior the generated product does not implement.

## What executable generation produces

The normal Domain/Application/Infrastructure/API/Test scaffold remains. The executable overlay then adds product-owned source such as:

```text
src/<Product>.Domain/GeneratedModules/<Module>/<Resource>.cs
src/<Product>.Application/GeneratedModules/<Module>/<Resource>Contracts.cs
src/<Product>.Application/GeneratedModules/<Module>/<Resource>Application.cs
src/<Product>.Infrastructure/GeneratedModules/<Module>/<Resource>EntityConfiguration.cs
src/<Product>.Infrastructure/GeneratedPlatform/GeneratedDbContext.cs
src/<Product>.Infrastructure/GeneratedPlatform/Migrations/<Initial>.cs
src/<Product>.Api/GeneratedPlatform/GeneratedHttpIdentity.cs
src/<Product>.Api/GeneratedPlatform/GeneratedApiSupport.cs
src/<Product>.Api/Program.cs
GENERATED-FULLSTACK.md
README.md
ARCHITECTURE.md
```

The generated product composes existing FoundationKit surfaces rather than duplicating their infrastructure logic:

```text
explicit fields
→ product entity/contracts
→ DataAnnotations validation metadata
→ FoundationKit generic CRUD service
→ semantic authorization/audit/concurrency seams
→ product-owned EF SQL Server persistence/migration
→ FoundationKit API Engine
→ runtime OpenAPI
→ deterministic Postman
```

## Database ownership and project isolation

Reusable FoundationKit packages still own no product schema or migration. The generated DbContext/migration live in the generated product.

Composer derives deterministic project-scoped identities for:

```text
FoundationProjectId
resource table names
idempotency table
EF migrations-history table
```

The Phase 12 CI proof generates Project A and Project B from the same resource shape, runs both simultaneously against one SQL Server database, and directly verifies that each project owns separate resource, idempotency, and migration-history tables. Each project can use the same HTTP idempotency key without consuming the other's replay state, and neither project can read the other's resource ID.

No database credential is generated into source. The generated app requires a runtime connection string such as:

```text
ConnectionStrings__Generated
```

## Authorization reference adapter

Executable authorization uses the Core CRUD authorization seam and an intentionally small generated reference authentication adapter so the generated product can be executed in CI.

Reference headers:

```text
X-Foundation-User: <non-empty GUID>
X-Foundation-Roles: admin
X-Foundation-Email: optional
```

This adapter is **not** FoundationKit's final production Identity composition. Real account persistence, login, MFA, recovery, federation, credential handling, and deployment identity policy remain separate product/platform work.

## Auth-safe request pipeline

FoundationKit WebApi exposes:

```csharp
UseFoundationRequestDiagnostics();
UseFoundationIdempotency();
```

while preserving the existing combined `UseFoundationRequestPipeline()` helper.

The generated authenticated host uses:

```text
Correlation / Problem Details / Security Headers
→ Authentication
→ Authorization
→ Durable idempotency
→ Endpoint
```

This means unauthorized requests still receive FoundationKit's diagnostics/security envelope, while a previously completed idempotent response cannot be replayed before the current request passes authorization. The runtime proof creates an idempotent response with admin credentials and then repeats the same key/body without credentials; the replay remains unauthorized rather than returning the stored success.

## OpenAPI and Postman

Runtime C# DTOs, endpoint metadata, module API configuration, idempotency/concurrency metadata, and authorization metadata remain the transport source of truth.

```text
C# / endpoint metadata
→ runtime OpenAPI
→ deterministic Postman
```

The generated host defines the reference auth schemes in OpenAPI, then attaches security requirements only to operations carrying authorization endpoint metadata. Anonymous health endpoints remain anonymous in both runtime behavior and OpenAPI.

The dedicated Phase 12 workflow captures OpenAPI independently from Project A and B and derives Postman with the existing `generate-postman-from-openapi.py` tool plus `--check`; Postman is not hand-maintained in the executable template.

## Modules, IDs, and routes

General schema-v2 bounds remain:

- 1–32 modules;
- 1–64 resources per module;
- at most 256 resources per project;
- module/resource names are safe identifiers and unique at their scopes;
- effective API routes are unique across the project;
- descriptor ID types remain `guid`, `string`, `long`, `int`;
- executable Phase 12 currently narrows that set to `guid`;
- route/prefix values use bounded safe ASCII segments.

## Behaviors versus canonical capabilities

Resource behaviors remain resource intent; the top-level capability graph remains canonical. Where resource behavior maps to an existing Core capability, Composer uses the same dependency resolver and reasons. There is no second dependency graph.

Example:

```text
Customer behavior: authorization
→ authorization capability
→ identity
→ security
```

Executable concurrency/idempotency additionally require their explicit canonical top-level capability selections, so `explain` can show their contract/maturity/reason truth rather than having hidden generator-only dependencies.

## Determinism and destructive safety

For the same manifest, generator contract, FoundationKit baseline, and reference mode, Composer produces the same generated bytes.

CI proves for v1, descriptor-v2, A, and B:

```text
generate
→ hash generated set
→ --force regenerate
→ identical hashes
→ restore
→ build
→ test
```

The ownership marker remains generator contract 2 for all schema-v2 output and records SHA-256 for the full generated set. User-added or edited files block destructive `--force` regeneration.

Generated output contains no timestamp, random project identifier, machine name, database password, or local absolute path. Runtime entity IDs may naturally be created by the running application; that does not affect deterministic source generation.

## Security and anti-low-code boundary

Composer v2:

- rejects unknown JSON fields at every modeled level;
- rejects unsafe identifiers/routes, unsupported executable field/ID types, duplicate fields/resources/routes, and unsupported executable behavior combinations;
- never executes manifest content;
- never accepts arbitrary C#/SQL/script/template bodies from JSON;
- never infers package IDs from user-controlled text;
- keeps package/project bindings catalog-owned;
- keeps product business rules and production identity explicit;
- keeps destructive regeneration protected by ownership/hash verification;
- fails closed when an executable request exceeds the generator's proven surface.

The intended rule remains:

> Convention over repetition + configuration over boilerplate + code when business logic requires it.

## Interactive and visual composition

The existing interactive CLI remains a compatible schema-v1 questionnaire. A future FoundationKit Studio/Workbench composer should author schema v2 visually, including executable fields where supported, but must serialize this same model and call the same deterministic analyzer/generator. It must not maintain a parallel graph or hidden project format.

## Phase 12 acceptance boundary

The formal completion gate is documented in `COMPOSER-FULLSTACK-PROOF-V1.md`.

Phase 12 requires one exact head to prove:

```text
schema-v1 compatibility
+ descriptor-v2 compatibility
+ deterministic executable A/B generation/build/test
+ product-owned SQL migrations
+ CRUD + validation + authorization + audit
+ ETag/If-Match concurrency
+ durable idempotency replay/fingerprint behavior
+ authorization before replay
+ shared-database A/B isolation
+ runtime OpenAPI with truthful operation-level security
+ deterministic Postman derivation
+ normal repository/package/security gates
```

Passing this gate closes the backend/generated-product proof required before first-party frontend/design-system work begins. It does not claim production approval or a complete production Identity system.
