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
- [x] CRUD mapper/validator/manager/authorization/concurrency/audit extension seams.
- [x] Generic CRUD HTTP endpoint mapping and Workbench SQL proof.

## Next backend/platform families

These remain evidence-driven and are not automatically packages:

- [ ] advanced approvals, tasks/work items, SLA/business-hours, activity/comments;
- [ ] notification templates/preferences/routing/retries/history;
- [ ] files/documents and storage providers;
- [ ] organization and multi-tenancy;
- [ ] jobs, durable messaging, outbox/inbox;
- [ ] webhooks and realtime;
- [ ] distributed caching provider;
- [ ] reusable idempotency contract;
- [ ] richer concurrency/precondition contract where evidence warrants it;
- [ ] external HTTP resilience conventions;
- [ ] search, reporting, import/export;
- [ ] privacy/PII, retention/anonymization;
- [ ] money/currency and numbering/sequences;
- [ ] PostgreSQL/Redis/object-storage/messaging/OpenTelemetry provider adapters where justified.

## Tooling and full-stack experience

- [ ] Composer manifest model for modules/resources and their per-module capabilities.
- [ ] generated OpenAPI/Postman contract evidence from one source of truth.
- [ ] visual Workbench composer using the same deterministic engine.
- [ ] first-party frontend template/design system after backend phases are stable.

## Definition of done

A reusable capability requires explicit purpose/non-goals, dependency boundary, provider-neutral public contracts where applicable, bounded inputs, security/privacy review, success/failure tests, architecture tests, Workbench/runtime proof when behavior is executable, compatibility/migration documentation, generated catalog synchronization, CI/security gates, and a maturity assessment matching actual evidence.

A roadmap item is never implemented solely to make the roadmap look complete.
