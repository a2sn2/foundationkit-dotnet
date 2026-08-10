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
- Strengthened Core-only repository verification and CI.

## 0.1.0

- Closed the 17-package composable baseline on .NET 10.
- Added capability graph, profiles, contract compatibility, maturity evidence, deterministic Composer generation, interactive Composer questionnaire, packaging/security gates, and Workbench reference execution.
