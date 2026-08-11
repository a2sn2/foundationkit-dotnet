# FoundationKit Typed Client Generation v1

## Purpose

FoundationKit derives deterministic Postman, typed C# transport and an opt-in Blazor application shell from the same serialized API contract instead of maintaining hand-copied request/response models.

```text
Backend contracts / endpoint metadata
        ↓
runtime OpenAPI
        ↓
  ┌───────────────┬─────────────────────┐
  ↓               ↓                     ↓
Postman       typed C# client     generated Blazor shell
                  ↑                     │
                  └──── canonical ──────┘
```

Runtime OpenAPI remains the source. The Blazor shell generator delegates to the typed-client generator; it does not implement another HTTP contract algorithm.

## Phase 13 typed-client evidence

The live OpenAPI document produced by a Composer-generated executable SQL product is used to generate C# source deterministically. CI compiles that source against exact-head `FoundationKit.Blazor` and executes the generated client against the same running API.

The runtime proof covers:

```text
Create
→ Location metadata
→ Get
→ ETag + CorrelationId metadata
→ Update with If-Match
→ refreshed ETag
→ List with query parameters
→ Delete
→ 404 after delete
```

This proves the client is executable transport, not merely syntactically generated source.

## Bounded generator contract

`scripts/generate-csharp-client-from-openapi.py` is intentionally **not** a general-purpose OpenAPI generator. It selects Foundation operations that publish both:

```text
x-foundation-module
x-foundation-operation
```

Supported Foundation operations:

```text
list
get
create
update
delete
```

Supported transport shapes include:

- OpenAPI 3.x;
- local `#/components/schemas/...` references;
- named object component schemas and arrays;
- strings, UUID, date-time, int32/int64, float/double and Boolean values;
- path/query/header parameters and repeated query-array values;
- JSON request/success bodies and no-content success responses;
- Foundation `Idempotency-Key` and `If-Match` headers.

Unsupported schema/ref/parameter shapes fail closed rather than producing partial C#.

## Blazor transport metadata

The existing `ApiResult` and protected `ApiClientBase.SendAsync` contracts remain compatible. Metadata-aware paths add:

```csharp
ApiResponseMetadata
ApiResponse
ApiResponse<T>
```

with:

```text
ETag
Location
CorrelationId
```

and protected:

```csharp
SendWithMetadataAsync(...)
SendWithMetadataAsync<T>(...)
```

The legacy result-only methods delegate to the metadata-aware path and retain the same `ApiResult` shape. Authentication is not synthesized into request DTOs; consumers configure their real `HttpClient`/handler identity integration.

## Determinism

For the same runtime OpenAPI, namespace, class name and generator version, generated C# bytes are deterministic:

```text
capture live OpenAPI
→ generate
→ SHA-256
→ generate again
→ identical SHA-256
→ --check
→ compile
→ execute against live API
```

No timestamp, machine path, random identifier, token, password or environment secret is written into generated client source.

## Phase 15 presentation boundary

`FoundationKit.Blazor` now also provides reusable presentation/query/display state without introducing a new package or a MudBlazor dependency. Browser state is presentation-only: backend authorization/query policy and Phase 14 SQL-view read models remain authoritative.

## Phase 16 generated application shell

`scripts/generate-blazor-app-from-openapi.py` closes the first-party frontend scaffolding chain. It validates safe app/namespace/client identifiers, references exact-head `FoundationKit.Blazor`, emits a .NET 10 Blazor WebAssembly shell and then invokes:

```text
scripts/generate-csharp-client-from-openapi.py
```

to populate its `Api/GeneratedApiClient.g.cs`.

The generated shell contains no synthesized authorization roles, relational joins, secrets or business workflow assumptions. Product-owned screens can be built on top of the typed operations while the server remains authoritative.

The dedicated frontend-generation workflow proves:

```text
OpenAPI-shaped input
→ deterministic shell + canonical typed client
→ generate again / identical SHA
→ --check
→ unsafe identifier rejection
→ restore
→ build with warnings-as-errors/analyzers
→ publish
```

Live runtime OpenAPI execution remains independently proven by `FoundationKit Typed Client Proof`; the frontend-generation workflow tests application scaffolding around that same client generator. These two gates are complementary rather than duplicate sources of truth.

## Compatibility and versioning

- `FoundationKit.Blazor.ApiResult` remains compatible;
- metadata-aware APIs and frontend presentation primitives are additive;
- package count remains 17;
- Composer schema v1/v2 generation is unaffected by client generation;
- runtime OpenAPI remains canonical; no second DTO/request model is introduced in Composer, Core Studio or generated frontend tooling.

A breaking change to the typed-client generation contract must introduce an explicitly versioned generator contract/output mode or follow FoundationKit's normal breaking/deprecation policy. Support for a new OpenAPI shape requires deterministic generation plus compile/runtime evidence.

## Migration

Existing manually written API clients can continue unchanged. New frontend work should prefer:

```text
runtime OpenAPI
→ generated typed C# client
→ generated or product-owned Blazor shell
→ ViewModel / UI
```

A consumer may wrap or extend the generated client at its own application boundary, but should not hand-copy generated transport DTOs or request paths into a parallel contract.

## CI gates

`FoundationKit Typed Client Proof` proves live runtime transport end to end. `FoundationKit Frontend Generation Proof` proves deterministic Blazor scaffolding and canonical client reuse. Both remain subordinate to the normal Core CI, Composer generation, full-stack SQL/read-engine, package integrity, Security Scan, CodeQL and Windows checks.

Core vNext v1 repository completion after Phase 16 therefore has one continuous frontend contract path rather than separate hand-authored API/client/UI definitions.
