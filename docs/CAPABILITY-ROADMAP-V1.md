# FoundationKit Capability Roadmap v1

The roadmap describes direction, not a checklist that justifies empty packages. Evidence-driven capability families may remain intentionally unimplemented until a real product need establishes their semantics.

## Delivered Core vNext v1 foundation

- [x] Domain/Application/Infrastructure/WebApi/Blazor base packages.
- [x] Capability graph, dependency resolver, seven profiles, contract versions, maturity evidence.
- [x] Auditing, Security, Identity, Authorization, Workflow, Approvals.
- [x] Notifications + SMTP reference transport.
- [x] Settings, Feature Management, Localization, Caching.
- [x] Composer strict manifests, diagnostics, deterministic generation, interactive questionnaire.
- [x] Workbench executable SQL reference.
- [x] Project identity/isolation contract and canonical resource namespace.
- [x] Module/Service Engine v1 and generic CRUD vertical capability.
- [x] CRUD mapper/validator/manager/authorization/concurrency/query/audit extension seams.
- [x] API Engine module configuration and generic CRUD route composition.
- [x] bounded pagination/filter/sort transport syntax with explicit module-owned query policies.
- [x] centralized validation/error/Problem Details contract plus typed 412/428 precondition failures.
- [x] ETag/If-Match concurrency contract kept separate from update DTOs.
- [x] API operation metadata for authorization, rate-limit policy, idempotency intent, and concurrency intent.
- [x] runtime OpenAPI metadata for CRUD schemas, queries, headers, responses, and Workbench SQL proof.
- [x] runtime OpenAPI as canonical serialized transport contract for derived client artifacts.
- [x] deterministic OpenAPI-to-Postman generation with exact committed-artifact drift detection in CI.
- [x] unified module capability composition with declared/effective separation, dependency closure, fluent cross-cutting capability intent, deterministic registry snapshots, and Workbench runtime discovery.
- [x] durable/replay-safe HTTP idempotency behind the API contract using provider-neutral Application contracts, relational EF adapter, bounded WebApi replay orchestration, consumer-owned schema, and SQL replay/fingerprint proof.
- [x] Composer schema v2 Project → Modules → Resources → Behaviors → Overrides → API model while preserving schema-v1 compatibility.
- [x] bounded schema-v2 executable resources for product-owned Domain/contracts/validation/CRUD/authorization/audit/concurrency/idempotency/SQL/API/OpenAPI.
- [x] concurrent generated Project A/B proof on one SQL Server database with project/resource/idempotency/migration isolation.
- [x] deterministic runtime OpenAPI → C# typed-client generation with CLR/OpenAPI requiredness alignment and transport metadata preservation.
- [x] explicit generated resource query/index policy with SQL Server WHERE/ORDER BY/paging and direct index metadata proof.
- [x] provider-neutral read-model boundary plus generated SQL-view-backed multi-table/query/report models with keyless EF mapping and server-side querying.
- [x] first-party reusable frontend presentation/query/display primitives in `FoundationKit.Blazor` without a new package or forced MudBlazor dependency.
- [x] Core-focused Workbench/Studio reference UI for capability/module/contract evidence; browser state remains presentation-only.
- [x] visual Core Studio Composer that submits schema-v2 JSON to the canonical `ComposerManifestParser` and `CompositionAnalyzer` rather than implementing a second manifest engine.
- [x] deterministic OpenAPI-wired Blazor WebAssembly application scaffolding that delegates typed transport generation to the canonical C# client generator and references exact-head `FoundationKit.Blazor`.

## Evidence-driven future capability families

These remain evidence-driven and are **not automatically packages** and are **not required for Core vNext v1 repository completion**:

- [ ] advanced approvals, tasks/work items, SLA/business-hours, activity/comments;
- [ ] notification templates/preferences/routing/retries/history;
- [ ] files/documents and storage providers;
- [ ] organization and multi-tenancy;
- [ ] jobs, durable messaging, outbox/inbox;
- [ ] webhooks and realtime;
- [ ] distributed caching provider;
- [ ] external HTTP resilience conventions;
- [ ] search, reporting, import/export beyond the current bounded SQL-view read-model foundation;
- [ ] privacy/PII, retention/anonymization;
- [ ] money/currency and numbering/sequences;
- [ ] PostgreSQL/Redis/object-storage/messaging/OpenTelemetry provider adapters where justified.

## Tooling and full-stack experience

- [x] derive deterministic Postman evidence from runtime OpenAPI.
- [x] derive deterministic C# typed clients from the same runtime OpenAPI.
- [x] Composer manifest model for Project → Modules → Resources → Behaviors → Providers → Overrides → API.
- [x] generated-project proof for Database + CRUD + Validation + Authorization + Audit + API + OpenAPI + Postman.
- [x] generated SQL query/index + SQL-view read-model/report proof.
- [x] concurrent project-isolation and compatible legacy-consumer proof.
- [x] first-party frontend state/design foundation plus Core Studio reference experience.
- [x] visual Workbench/Core Studio composer using the same schema-v2 parser/analyzer.
- [x] opt-in deterministic generated frontend application shell wired to the canonical typed transport contract.

The v1 repository implementation/tooling roadmap is closed by Phase 16. Future work should start from a concrete product or provider requirement rather than reopening completed v1 phases.

## Definition of done

A reusable capability requires explicit purpose/non-goals, dependency boundary, provider-neutral public contracts where applicable, bounded inputs, security/privacy review, success/failure tests, architecture tests, Workbench/runtime proof when behavior is executable, compatibility/migration documentation, generated catalog synchronization, CI/security gates, and a maturity assessment matching actual evidence.

**Core vNext v1 repository completion** means the backend/read/typed-client contracts, first-party frontend foundation, canonical visual Composer, deterministic generated frontend shell, active documentation, dependency hygiene, and exact-head quality/security/package gates are coherent on `main`.

This definition does **not** mean Production Approved. Protected-main enforcement, independent approval, organization-level security controls and real operational go-live requirements remain separate environment/process governance tracked by issue #35.

A roadmap item is never implemented solely to make the roadmap look complete.
