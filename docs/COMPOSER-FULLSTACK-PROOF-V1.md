# FoundationKit Composer Full-Stack Proof v1

## Problem

Composer schema v2 could describe modules/resources, but before Phase 12 it did not prove that one manifest could produce an actually executable product-owned vertical slice. The pre-frontend gate therefore required proof that FoundationKit can compose repeated platform behavior without hiding product business ownership or breaking schema-v1/descriptor-only consumers.

## Evidence target

One exact repository head must prove all of the following from Composer-generated source:

```text
Manifest v2
→ product Domain entity
→ Create / Update / Response contracts
→ DataAnnotations validation
→ FoundationKit generic CRUD application service
→ semantic authorization policy
→ audit observer
→ optimistic concurrency + ETag/If-Match
→ durable HTTP idempotency
→ product-owned EF Core SQL Server schema/migration
→ FoundationKit API Engine endpoints
→ runtime OpenAPI
→ deterministic Postman
```

The same head must also generate two independent projects from the same resource shape, run both concurrently against one SQL Server database, and prove that project identity, resource rows, idempotency state, and EF migration history remain isolated.

## Scope

Phase 12 deliberately keeps the first executable schema-v2 field model narrow:

- resource ID type: `guid`;
- explicit `fields` are optional; absence keeps the Phase 11 descriptor-only behavior;
- executable field type: `text`;
- at most 32 fields per resource;
- `maximumLength`: 1..4000;
- `Id` and `Version` are reserved generated infrastructure names;
- executable behaviors: `crud`, `auditing`, `authorization`, `concurrency` only;
- SQL Server is the first executable persistence provider;
- executable query generation is not claimed yet, so `maximumFilters=0` and `maximumSorts=0` are required;
- manager overrides and generated rate-limit policy registration remain explicit future/consumer code rather than partially implemented magic.

`concurrency` and `idempotency` must be explicitly selected in the canonical top-level capability composition when the executable API uses them. Resource intent does not silently rewrite global capability selection.

## API impact

The runtime package count remains unchanged at 17.

`FoundationKit.WebApi` adds two public pipeline helpers:

```csharp
UseFoundationRequestDiagnostics();
UseFoundationIdempotency();
```

The existing `UseFoundationRequestPipeline()` remains available and composes those helpers in its compatibility order. Hosts that authenticate requests can use the split form:

```text
Foundation diagnostics / Problem Details / security headers
→ authentication
→ authorization
→ Foundation idempotency
→ endpoint
```

This preserves the security/correlation envelope on authentication failures while ensuring a stored idempotency response cannot bypass the current request's authorization decision.

## Compatibility

- schema v1 remains on generator contract 1;
- schema-v2 resources without `fields` remain descriptor-only and retain generator contract 2 behavior;
- `fields` is an additive schema-v2 surface;
- unsupported executable intent fails closed instead of producing partially wired code;
- no consumer migration is required merely because Phase 12 exists;
- product database schema/migrations remain product-owned, not reusable-package migrations.

## Contract versioning

Capability contract versions remain unchanged at v1 because Phase 12 adds compatible surfaces and executable composition evidence rather than redefining existing capability contracts. A future breaking manifest meaning must use a new manifest schema version. A future breaking public runtime API follows FoundationKit's SemVer/deprecation policy.

Idempotency maturity advances only from `Planned` to `ReferenceOnly`: implementation, quality, and adoption evidence now exist, but provider breadth and long-term compatibility support are not sufficient for Preview/Stable claims.

## Security boundary

The generated header authentication adapter exists only to make the generated product proof executable:

```text
X-Foundation-User: <non-empty GUID>
X-Foundation-Roles: admin
X-Foundation-Email: optional
```

It is not a production identity system. Real user/account persistence, MFA, recovery, federation, secrets, deployment policy, SIEM, retention, and operational governance remain consumer/environment responsibilities.

No SQL credential is generated into source. The generated host requires the connection string at runtime through configuration such as `ConnectionStrings__Generated`.

OpenAPI declares the reference authentication schemes, but security requirements are attached only to operations whose endpoint metadata requires authorization; anonymous health endpoints remain anonymous in both runtime behavior and OpenAPI.

## Tests

The generated-project compatibility workflow proves on the same head:

```text
schema v1
→ generate
→ force-regenerate
→ identical hashes
→ restore/build/test

schema v2 descriptor-only
→ validate/generate
→ force-regenerate
→ identical hashes
→ restore/build/test

full-stack A
→ validate/generate
→ force-regenerate
→ identical hashes
→ secret-boundary checks
→ restore/build/test

full-stack B
→ same proof independently
```

The dedicated runtime workflow then starts one SQL Server database and both generated APIs concurrently. HTTP smoke proves validation, authorization, security headers on auth failures, authorization-before-idempotency replay, create/update/delete replay, fingerprint conflicts, ETag/If-Match 428/412/success behavior, audit recording, and cross-project data isolation.

## Migration / database ownership

Each generated executable project receives deterministic product-scoped names for:

```text
FoundationProjectId
resource tables
idempotency table
EF migrations-history table
```

Two generated projects may therefore share one SQL Server database while retaining separate schema ownership. The generated migration is a product migration located in the generated Infrastructure project; FoundationKit reusable packages still own no product migration.

## CI evidence

`FoundationKit Composer Full-Stack Proof` generates A/B from manifests, builds/tests them, runs both against one SQL Server database, captures live OpenAPI, derives Postman deterministically with `--check`, executes HTTP semantics, and queries SQL directly for resource/idempotency/migration isolation. The workflow uploads its runtime/OpenAPI/Postman/SQL evidence artifact.

Normal FoundationKit CI, package integrity, Security Scan, CodeQL, Windows Manager, Composer compatibility, and repository verification remain required on the exact PR head before merge.

## Acceptance criteria

Phase 12 is accepted only when one exact head proves:

```text
17 reusable packages unchanged
+ schema-v1 generation/build/test compatible
+ schema-v2 descriptor generation/build/test compatible
+ executable A/B deterministic generation/build/test
+ shared-database A/B runtime isolation
+ CRUD + validation + authorization + audit
+ concurrency + durable idempotency
+ runtime OpenAPI + deterministic Postman
+ no generated database secret
+ auth cannot be bypassed by replay
+ operation-level OpenAPI security matches runtime authorization
+ generated catalog/maturity truth synchronized
+ normal CI/security/review gates green
```

Passing Phase 12 closes the backend/generated-product proof required before FoundationKit begins its first-party frontend/design-system phase. It does not by itself claim production approval or a complete production Identity system.
