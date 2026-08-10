# FoundationKit Capability Roadmap v1

The roadmap describes direction, not a checklist that justifies empty packages.

## Delivered foundation

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
- [x] durable/replay-safe HTTP idempotency behind the Phase 7 contract using provider-neutral Application contracts, a relational EF adapter, bounded WebApi replay orchestration, consumer-owned schema, and Workbench SQL replay/fingerprint proof.
- [x] Composer schema v2 project model for Project → Modules → Resources → Behaviors → Overrides → API, mapped back into the canonical capability graph and generated deterministically while preserving schema v1 generation/build/test compatibility.
- [x] bounded schema-v2 executable resource generation for product-owned Domain/contracts/validation/CRUD/authorization/audit/concurrency/idempotency/SQL/API/OpenAPI with deterministic Postman derivation.
- [x] concurrent generated Project A/B proof on one SQL Server database with project/resource/idempotency/migration isolation while retaining legacy schema-v1 and descriptor-only schema-v2 compatibility.

## Next backend/platform families

These remain evidence-driven and are not automatically packages:

- [ ] advanced approvals, tasks/work items, SLA/business-hours, activity/comments;
- [ ] notification templates/preferences/routing/retries/history;
- [ ] files/documents and storage providers;
- [ ] organization and multi-tenancy;
- [ ] jobs, durable messaging, outbox/inbox;
- [ ] webhooks and realtime;
- [ ] distributed caching provider;
- [ ] external HTTP resilience conventions;
- [ ] search, reporting, import/export;
- [ ] privacy/PII, retention/anonymization;
- [ ] money/currency and numbering/sequences;
- [ ] PostgreSQL/Redis/object-storage/messaging/OpenTelemetry provider adapters where justified.

## Tooling and full-stack experience

- [x] derive deterministic Postman evidence from the runtime/OpenAPI contract source of truth.
- [ ] derive a future typed-client artifact from the same runtime/OpenAPI contract when the frontend phase begins.
- [x] Composer manifest model for Project → Modules → Resources → Behaviors → Providers → Overrides → API.
- [x] generated-project proof for Database + CRUD + Validation + Authorization + Audit + API + OpenAPI + Postman.
- [x] concurrent project-isolation and compatible legacy-consumer proof.
- [ ] visual Workbench/Studio composer using the same schema-v2 deterministic engine.
- [ ] first-party frontend template/design system after backend phases are stable.

## Definition of done

A reusable capability requires explicit purpose/non-goals, dependency boundary, provider-neutral public contracts where applicable, bounded inputs, security/privacy review, success/failure tests, architecture tests, Workbench/runtime proof when behavior is executable, compatibility/migration documentation, generated catalog synchronization, CI/security gates, and a maturity assessment matching actual evidence.

A roadmap item is never implemented solely to make the roadmap look complete.
