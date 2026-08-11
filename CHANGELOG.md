# Changelog

All notable FoundationKit Core changes are recorded here. Repository history before the Core-only refocus remains available through Git, but removed application-specific release notes are not part of the active product documentation.

## Unreleased — Core vNext

- Refocused the repository on FoundationKit Core, Composer, and Workbench.
- Added immutable `FoundationProjectId`, host-local project context, and canonical project-scoped resource namespacing.
- Added project-isolation, SemVer, capability-contract, migration, and deprecation rules.
- Added Module/Service Engine v1 definitions and host-local registry.
- Added generic CRUD application orchestration over existing repository/UoW/specification/validation/result contracts.
- Added explicit mapper, manager, authorization, concurrency, query-policy, and operation-observer extension points.
- Added fail-closed CRUD authorization when authorization is declared without an explicit semantic policy.
- Added EF concurrency translation and generic CRUD endpoint mapping.
- Added the Phase 7 API Engine: module API options, bounded pagination/filter/sort transport parsing, explicit query policies, idempotency-header intent, ETag/If-Match preconditions, typed 412/428 failures, rate-limit/security operation metadata, and OpenAPI header/query contracts.
- Added a Workbench SQL vertical proof for CRUD create/read/list/update/delete, DataAnnotations, idempotency-header validation, ETag/If-Match concurrency, filter/sort policy, manager override, auditing seam, OpenAPI, and Problem Details.
- Added Phase 8 contract source-of-truth tooling: runtime OpenAPI is the canonical serialized transport contract and Workbench Postman is generated deterministically from it.
- Added exact CI drift gates proving repeated OpenAPI-to-Postman generation is byte-identical and matches the committed generated collection.
- Kept runtime behavior proof separate from derived contract artifacts: SQL/HTTP smoke proves semantics while OpenAPI/Postman prove transport shape.
- Added Phase 9 unified module capability composition with declared/effective capability separation, deterministic dependency closure, one fluent builder vocabulary for existing cross-cutting capabilities, registry snapshots, and Workbench `/api/modules` runtime evidence.
- Reconciled the incomplete initial Phase 9 merge before starting the next reliability increment so the repository returned to an internally consistent buildable baseline.
- Added Phase 10 durable replay-safe HTTP idempotency behind the existing Phase 7 contract: provider-neutral Application contracts, relational EF acquisition/replay storage, project-scoped hashed keys, request fingerprints including `If-Match`, bounded response replay, fail-closed indeterminate outcomes, and consumer-owned schema/migrations.
- Added Workbench SQL evidence proving create/update/delete replay, duplicate-side-effect prevention, and fingerprint conflicts while retaining the same 17-package boundary.
- Added Phase 11 Composer schema v2: strict Project → Modules → Resources → Behaviors → Overrides → API intent, canonical capability reuse, bounded safe identifiers/routes/ID types, deterministic normalized v2 manifests and inspectable resource descriptors, while preserving schema-v1 generator contract and generated-project compatibility.
- Expanded Composer CI to independently generate, force-regenerate, restore, build, and test both v1 and descriptor-only v2 scaffolds on the same exact head.
- Added Phase 12 bounded executable schema-v2 `fields` generation for product-owned Domain entities, contracts, DataAnnotations validation, generic CRUD wiring, semantic admin authorization, audit, optimistic concurrency, durable idempotency, SQL Server DbContext/migrations, API endpoints, runtime OpenAPI, and deterministic Postman derivation.
- Added additive `UseFoundationRequestDiagnostics()` and `UseFoundationIdempotency()` WebApi helpers so authenticated hosts can preserve the Foundation security/Problem Details envelope while placing authorization before idempotency replay; the existing combined pipeline remains compatible.
- Added deterministic project-scoped SQL resource/idempotency/migration-history naming and an A/B generated-project CI proof that runs two generated APIs concurrently on one SQL Server database without data or replay-state collision.
- Added exact generated-project compatibility gates for schema v1, descriptor-only schema v2, and executable full-stack A/B, including deterministic force regeneration and generated-secret checks.
- Added dedicated generated runtime evidence covering validation, authorization, audit, create/update/delete replay, fingerprint conflicts, ETag/If-Match behavior, auth-before-replay, operation-level OpenAPI security, deterministic Postman, and direct SQL isolation checks.
- Advanced Idempotency maturity from Planned to ReferenceOnly based on implementation, quality, Workbench adoption, and generated A/B adoption evidence while explicitly withholding broader provider/compatibility claims.
- Added **Phase 12 closure — typed transport**: deterministic runtime OpenAPI → C# typed-client generation with fail-closed shape support, CLR/OpenAPI required-property alignment, nullable-safe response models, required idempotency/If-Match transport, and ETag/Location/CorrelationId metadata preservation through `FoundationKit.Blazor`.
- Added a live typed-client proof that generates from a running Composer product, reproduces the client byte-for-byte, builds it with analyzers/warnings-as-errors, and executes create/get/update/list/delete against SQL Server without exposing raw `HttpResponseMessage` handling to generated consumers.
- Added **Phase 12 closure — SQL read hardening**: explicit generated query/index intent (`none`/`exact`/`prefix`, sortable, indexed/unique), fail-closed field/operator validation, product-owned deterministic SQL Server indexes, and direct proof that EF executes `WHERE`/`ORDER BY`/paging in SQL rather than materialize-then-sort.
- Added the provider-neutral read-only `IReadModelStore<TReadModel>` boundary, query service/policies and WebApi read-model list endpoint.
- Added Composer-generated SQL-view-backed query/report read models with explicit columns, bounded validated LEFT JOINs, product-owned `CREATE VIEW` / `DROP VIEW` migration DDL, keyless `HasNoKey().ToView(...)` EF mappings, authorization, GET-only exposure and server-side filter/sort/count/page.
- Added generated `CustomerDirectory` and `CustomerStatement` runtime evidence proving joined-field filtering, LEFT JOIN null preservation, report projection and typed-client inclusion while preserving the existing Customer write DTO contract.
- Added **Phase 12 closure — frontend foundation**: reusable presentation/query/display state in the existing `FoundationKit.Blazor` package without adding package #18 or a MudBlazor dependency to reusable Core.
- Refocused the Workbench Blazor client into FoundationKit Core Studio for capability catalog, live module composition and contract-evidence inspection; removed the old product-style user/admin portal pages while retaining their backend workflow only as integration evidence.
- Documented the browser boundary explicitly: UI state is presentation-only, backend authorization remains authoritative, and multi-table/report UI data comes from read models rather than browser-side relational logic.
- Added **Phase 12 closure — tooling**: visual schema-v2 composition in Core Studio with a bounded `/api/composer/validate` transport that delegates authoritative validation directly to `ComposerManifestParser` and `CompositionAnalyzer` rather than introducing a browser-side manifest engine.
- Added deterministic OpenAPI → Blazor WebAssembly application scaffolding that validates safe identifiers, references exact-head `FoundationKit.Blazor`, and delegates all transport generation to the existing deterministic C# client generator.
- Added `FoundationKit Frontend Generation Proof` for repeat-generation SHA identity, `--check` drift detection, unsafe identifier rejection, restore/build/publish evidence and canonical typed-client wiring.
- Closed the approved **Core vNext roadmap at Phase 12** with the typed/read/frontend/tooling closure tracks above; no Phase 13–16 roadmap was introduced.
- Added the final **Soft Orbit shared UI baseline** before the first real consumer project: converted the existing `FoundationKit.Blazor` package into a browser-compatible Razor Class Library, added semantic light/dark design tokens and first-party product-neutral Razor primitives, persistent theme behavior, RTL/LTR-aware responsive shell behavior and the replaceable Orbit Nodes mark without adding package #18 or MudBlazor to reusable Core.
- Added Core Studio `/design` as a living design-system reference that renders the real reusable FoundationKit components, and refreshed Workbench/Studio surfaces away from the earlier dark/glow-heavy reference styling.
- Updated deterministic generated Blazor applications to consume the same `_content/FoundationKit.Blazor/foundationkit.css` / `foundationkit.js` assets and `Fk*` components as Core Studio, so examples and future projects no longer ship an independent visual DNA.
- Added explicit shared-design CI assertions for browser-compatible build/publish, deterministic generation, static-web-asset publication, no MudBlazor dependency in reusable Core, and one visual source of truth.
- Retained Production-governance separation under issue #35.
- Strengthened Core-only repository verification and CI.

## 0.1.0

- Closed the 17-package composable baseline on .NET 10.
- Added capability graph, profiles, contract compatibility, maturity evidence, deterministic Composer generation, interactive Composer questionnaire, packaging/security gates, and Workbench reference execution.
