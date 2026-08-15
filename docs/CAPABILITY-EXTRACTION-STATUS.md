# Capability Extraction Status

Status date: 2026-08-15.

FoundationKit is now at the **Consumer-ready Core baseline — Pre-production**. The active repository is Core-focused, and runtime evidence is derived from reusable package tests, deterministic generated-project tests, dedicated full-stack/read/frontend proofs, and Workbench/Core Studio reference execution rather than retained application products.

## Current reusable packages

Exactly 17 reusable packages are shipped. No eighteenth package is justified by the current Core vNext evidence.

Base: Domain, Application, Infrastructure, WebApi, Blazor.

Optional/reference: Auditing, Security, Identity, Authorization, Workflow, Approvals, Notifications, Notifications.Smtp, Settings, FeatureManagement, Localization, Caching.

Core vNext deliberately hardened and composed those existing packages instead of converting every new behavior into another package. The completed baseline includes project isolation, Module/Service Engine, API Engine, contract-source-of-truth tooling, module capability composition, durable idempotency, Composer schema v2, bounded executable generated resources, SQL-first query/index behavior, SQL-view-backed read models, deterministic Postman and typed-client derivation, the shared Soft Orbit frontend foundation, Core Studio visual composition, local deterministic project generation, and generated Blazor application scaffolding.

## Current extraction decision

The completed Core vNext baseline does **not** justify package #18. The current full-stack composition uses existing reusable surfaces and emits product-owned source where a generated or consuming product must own semantics, schema, branding, or deployment decisions:

- product Domain entities/contracts derived from explicit bounded manifest fields;
- product DbContext, SQL Server mapping, resource/idempotency tables, indexes, views, and migrations;
- product read-model/view declarations and public DTO contracts;
- product reference authentication adapter in generated proof applications;
- FoundationKit generic CRUD/application/API/audit/concurrency/idempotency/query/read-model wiring;
- runtime OpenAPI with deterministic Postman and C# typed-client derivation;
- a product-neutral generated Blazor shell consuming the same `FoundationKit.Blazor` design-system assets as Core Studio.

Product-owned generated source is not automatically a reusable runtime package.

## Capability maturity interpretation

Durable HTTP idempotency has implementation, quality, Workbench adoption, and generated A/B adoption evidence. Its canonical maturity remains `ReferenceOnly`, not Preview or Stable, because broader provider compatibility and long-term support evidence are still incomplete.

Concurrency likewise remains `ReferenceOnly` despite generic policy/EF/API evidence and generated-product use.

The delivered read-model, typed-client, Core Studio, and generated-frontend proofs strengthen the consumer baseline without automatically promoting unrelated capability families or changing Production approval status.

## Still separate evidence-driven decisions

Files, documents, jobs, durable messaging/outbox/inbox, webhooks, realtime, organization, tenancy, external search engines, broader reporting/export, privacy, retention, money, numbering, AI, and additional provider families remain separate decisions. An implementation should be extracted only when a real consumer establishes a reusable provider-neutral boundary and the tests/reference/adoption evidence are strong enough to avoid encoding one product's semantics.

## Next extraction trigger

The next reusable Core increment should be driven by the **first real consumer project** or another concrete adoption need. Missing capability families are not backlog obligations merely because they appear in the roadmap vocabulary.

Until consumer evidence identifies a reusable gap, the Core baseline should remain closed rather than adding speculative packages, phases, providers, or abstractions.

## Production boundary

Repository completion is not Production approval. Protected-main enforcement, independent approval, organization-level controls, secrets/KMS, recovery, production identity, monitoring, compliance and deployment acceptance remain external/process controls tracked separately, including issue #35.
