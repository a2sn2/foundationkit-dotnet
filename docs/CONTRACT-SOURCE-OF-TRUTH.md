# FoundationKit Contract Source of Truth

## Purpose

FoundationKit does not allow transport contracts to evolve independently in backend code, Swagger, Postman, typed clients, or generated frontend applications. A request/response model has one authoritative implementation path and every derived representation must be reproducible from it.

The current contract chain is:

```text
C# Request / Response contracts
+ Module API configuration
+ Endpoint / ApiExplorer metadata
                 |
                 v
        Runtime OpenAPI document
       canonical serialized contract
                 |
        +--------+---------+
        |                  |
        v                  v
    Swagger UI      deterministic artifacts
                       |             |
                       v             v
                    Postman     typed C# client
                                      |
                                      v
                              generated Blazor/client
```

The C# contract and module configuration remain the implementation source. Runtime OpenAPI is the canonical serialized transport contract because it is produced by the actual running endpoint surface. Postman and the typed C# client are derived artifacts and never independent authoring surfaces.

Phase 8 established the runtime-OpenAPI/Postman derivation boundary. Phase 12.C1 extended that same boundary to deterministic typed C# client generation; generated Blazor applications consume the typed transport instead of creating a parallel request model.

## Why runtime OpenAPI

Static DTO inspection alone is not enough to describe an HTTP contract. The effective contract also contains:

- route templates and HTTP methods;
- path, query, and header parameters;
- required versus optional headers;
- request and response media types;
- Problem Details responses;
- authorization/security metadata exposed by the host;
- pagination/filter/sort parameters;
- idempotency and concurrency preconditions;
- concrete schemas produced by the configured ASP.NET Core/OpenAPI pipeline.

Capturing OpenAPI from a running host therefore proves the same endpoint metadata that generated clients actually see.

## Ownership rules

### Backend contracts

Request/response types, DataAnnotations, module configuration, endpoint mappings, and API metadata are edited in C# or generated deterministically from the canonical Composer model.

### OpenAPI

OpenAPI is generated from the runtime application. The repository does not maintain a second hand-written Swagger contract.

### Postman

`postman/FoundationKit.Workbench.postman_collection.json` and generated-product Postman collections are derived from runtime OpenAPI with `scripts/generate-postman-from-openapi.py`.

Do **not** edit generated Postman requests manually. If a backend contract changes, run the application, capture its OpenAPI document, and regenerate the collection.

The generator supports a strict drift check with `--check`.

### Typed C# client

Typed client source is generated deterministically from the same runtime OpenAPI contract with the repository's typed-client generator. The generated client must not introduce routes, DTO fields, authorization assumptions, or error semantics that are absent from OpenAPI.

Generated Blazor applications consume this typed client path. Product-specific ViewModels/UI may add presentation behavior, but they do not become a competing transport contract.

## Determinism contract

Derived contract generators must produce byte-identical output for byte-equivalent OpenAPI input and the same explicit options.

They therefore do not use:

- current timestamps;
- random UUIDs;
- network lookups;
- machine-specific paths;
- environment-specific ordering;
- nondeterministic dictionary/set iteration for serialized output.

The Postman collection identifier is a stable UUID5 derived from the OpenAPI title/version. Operations, folders, parameters, and generated examples use deterministic ordering and values. Typed-client output follows the same reproducibility rule.

CI regenerates artifacts and rejects drift.

## Drift gates

The Workbench and generated-product proof jobs follow the same ownership direction:

```text
Start runtime host
        |
        v
Capture /swagger/v1/swagger.json
        |
        v
Verify structural OpenAPI expectations
        |
        +-------------------+
        |                   |
        v                   v
Generate Postman       Generate typed client
        |                   |
        v                   v
Determinism/check      Restore/build/use client
        |                   |
        +---------+---------+
                  |
                  v
          Runtime behavior proof
```

A DTO, route, parameter, header, security requirement, or response change therefore cannot silently leave committed/generated transport artifacts stale.

## Contract versus behavior

Generated artifacts represent the transport contract. They do not replace executable behavior tests.

For example, OpenAPI can state that `If-Match` is required and `412` is possible. Only runtime/SQL smoke proves that a stale ETag actually returns `412`, that a valid ETag updates the correct row, and that the new ETag advances.

FoundationKit therefore separates:

- **contract evidence** — runtime OpenAPI + deterministic Postman + deterministic typed client;
- **behavior evidence** — unit/integration/security tests, SQL/read-engine smoke, client compilation/use, and generated frontend runtime proof.

Neither one substitutes for the other.

## Generated examples

The Postman generator creates safe deterministic examples from OpenAPI schemas. Examples are intended to make the collection structurally usable, not to invent product business semantics.

Current deterministic examples include bounded placeholders for:

- strings;
- numbers and booleans;
- UUIDs;
- dates/date-times;
- email/URL formats;
- arrays and nested objects;
- enum first values;
- required API headers such as `Idempotency-Key` and `If-Match`.

Read-only schema properties are not inserted into request bodies.

## Generator boundaries

The derived generators deliberately support the contract surfaces proven by the repository. Unsupported OpenAPI features must fail visibly rather than silently producing a misleading artifact.

The Postman generator supports the established OpenAPI 3.x JSON/local-reference/object/array/primitive/`allOf`/JSON-body/path-query-header/tagged-operation surface. Broader external references, arbitrary multipart/file examples, callbacks, and advanced discrimination remain evidence-driven additions.

The typed-client generator follows the same fail-closed principle: only supported, tested transport constructs may become generated client code.

## Security

Generation happens from locally captured runtime OpenAPI. CI does not fetch arbitrary external schemas or URLs as contract input.

Generators do not execute values from the OpenAPI document. Generated examples use deterministic placeholders rather than copied secrets or runtime data.

Authentication credentials/tokens are never embedded into committed generated artifacts. Security schemes and per-operation requirements already exist in the canonical OpenAPI where applicable; generated client representations must use explicit caller-supplied credentials/placeholders and must not bypass server authorization.

## Compatibility

The contract-source-of-truth rule changes artifact ownership, not the 17-package reusable boundary. Package APIs, capability-contract versions, Composer schema versions, and generated transport versions remain separately governed concepts.

Existing consumers can continue using their current transport code, while generated/managed consumers use the canonical one-way derivation path.

## Current acceptance boundary

The consumer-ready Core baseline requires the exact tested head to prove, as applicable:

- repository/build/test/package/security gates remain green;
- runtime OpenAPI structural verification passes;
- deterministic Postman generation/check passes;
- deterministic typed C# client generation passes;
- generated typed clients restore/build and exercise the runtime contract;
- generated Blazor applications consume the generated transport path and build/run in proof workflows;
- SQL/read-engine and API behavior remain aligned with the public contract;
- no independent hand-maintained transport model is introduced;
- no eighteenth reusable package is introduced merely for transport generation.

This repository evidence establishes a reproducible consumer contract baseline. It does not by itself constitute production deployment approval.
